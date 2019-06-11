using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI.ViewControllers;
using CustomUI.BeatSaber;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using VRUI;
using Newtonsoft.Json.Linq;
namespace BeatSaverDownloader.UI.FlowCoordinators
{
    class MoreSongsFlowCoordinator : FlowCoordinator
    {
        public const int songsPerPage = 6;

        private BackButtonNavigationController _moreSongsNavigationController;
        private MoreSongsListViewController _moreSongsListViewController;
        private SongDetailViewController _songDetailViewController;
        private SearchKeyboardViewController _searchViewController;
        private DownloadQueueViewController _downloadQueueViewController;
        private SongDescriptionViewController _descriptionViewController;
        private SimpleDialogPromptViewController _simpleDialog;

        public int currentPage = 0;
        public string currentSortMode = "hot";
        public string currentSearchRequest = "";
        public int currentScoreSaberSortMode = 0;
        public bool scoreSaber = false;

        private List<Song> currentPageSongs = new List<Song>();

        private Song _lastSelectedSong;

        private Song _lastDeletedSong;

        public void Awake()
        {
            if (_songDetailViewController == null && _moreSongsNavigationController == null)
            {
                _moreSongsNavigationController = BeatSaberUI.CreateViewController<BackButtonNavigationController>();
                _moreSongsNavigationController.didFinishEvent += _moreSongsNavigationController_didFinishEvent;

                GameObject _songDetailGameObject = Instantiate(Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First(), _moreSongsNavigationController.rectTransform, false).gameObject;
                Destroy(_songDetailGameObject.GetComponent<StandardLevelDetailViewController>());
                _songDetailViewController = _songDetailGameObject.AddComponent<SongDetailViewController>();
                _songDetailViewController.downloadButtonPressed += _songDetailViewController_downloadButtonPressed;
                _songDetailViewController.favoriteButtonPressed += _songDetailViewController_favoriteButtonPressed;
            }
        }

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                title = "More Songs";
                
                _moreSongsListViewController = BeatSaberUI.CreateViewController<MoreSongsListViewController>();
                _moreSongsListViewController.pageDownPressed += _moreSongsListViewController_pageDownPressed;
                _moreSongsListViewController.pageUpPressed += _moreSongsListViewController_pageUpPressed;


                _moreSongsListViewController.sortByTop += () => { currentSortMode = "hot"; currentPage = 0; StartCoroutine(GetPage(currentPage, currentSortMode)); currentSearchRequest = ""; };
                _moreSongsListViewController.sortByNew += () => { currentSortMode = "latest"; currentPage = 0; StartCoroutine(GetPage(currentPage, currentSortMode)); currentSearchRequest = ""; };

                _moreSongsListViewController.sortByNewlyRanked += () => { currentScoreSaberSortMode = 1; currentPage = 0; StartCoroutine(GetPageScoreSaber(currentPage, currentScoreSaberSortMode)); };
                _moreSongsListViewController.sortByTrending += () => { currentScoreSaberSortMode = 0; currentPage = 0; StartCoroutine(GetPageScoreSaber(currentPage, currentScoreSaberSortMode)); };
                _moreSongsListViewController.sortByDifficulty += () => { currentScoreSaberSortMode = 3; currentPage = 0; StartCoroutine(GetPageScoreSaber(currentPage, currentScoreSaberSortMode)); };

                _moreSongsListViewController.searchButtonPressed += _moreSongsListViewController_searchButtonPressed;
                _moreSongsListViewController.didSelectRow += _moreSongsListViewController_didSelectRow;

                _downloadQueueViewController = BeatSaberUI.CreateViewController<DownloadQueueViewController>();

                _descriptionViewController = BeatSaberUI.CreateViewController<SongDescriptionViewController>();
                _descriptionViewController.linkClicked += LinkClicked;

                _simpleDialog = CustomUI.Utilities.ReflectionUtil.GetPrivateField<SimpleDialogPromptViewController>(Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First(), "_simpleDialogPromptViewController");
                _simpleDialog = Instantiate(_simpleDialog.gameObject, _simpleDialog.transform.parent).GetComponent<SimpleDialogPromptViewController>();
            }

            SongDownloader.Instance.songDownloaded -= SongDownloader_songDownloaded;
            SongDownloader.Instance.songDownloaded += SongDownloader_songDownloaded;
            
            SetViewControllersToNavigationConctroller(_moreSongsNavigationController, new VRUIViewController[]
            {
                _moreSongsListViewController
            });
            ProvideInitialViewControllers(_moreSongsNavigationController, _downloadQueueViewController, _descriptionViewController);

            currentPage = 0;
            currentSortMode = "top";
            currentSearchRequest = "";
            StartCoroutine(GetPageScoreSaber(0, 0));
        }

        private void LinkClicked(string link)
        {
            _simpleDialog.Init("Open link?", $"Are you sure you want to open this link?\n<color=blue>{link}</color>", "Open", "Cancel",
                   (buttonIndex) =>
                   {
                       SetRightScreenViewController(_descriptionViewController);
                       _descriptionViewController.SetDescription(_lastSelectedSong.description);
                       if (buttonIndex == 0)
                       {
                           Application.OpenURL(link);
                       }
                   }
               );
            SetRightScreenViewController(_simpleDialog);
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            if (deactivationType == DeactivationType.RemovedFromHierarchy)
            {
                PopViewControllerFromNavigationController(_moreSongsNavigationController);
                SongDownloader.Instance.songDownloaded -= SongDownloader_songDownloaded;
            }
        }

        private void _moreSongsNavigationController_didFinishEvent()
        {
            if (!_downloadQueueViewController.queuedSongs.Any(x => x.songQueueState == SongQueueState.Downloading || x.songQueueState == SongQueueState.Queued))
            {
                MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();

                mainFlow.InvokeMethod("DismissFlowCoordinator", this, null, false);
            }
        }

        private void _songDetailViewController_favoriteButtonPressed(Song song)
        {
            if(PluginConfig.favoriteSongs.Any(x => x.Contains(song.hash)))
            {
                PluginConfig.favoriteSongs.Remove(SongDownloader.GetLevelID(song));
                PluginConfig.SaveConfig();
                
                _songDetailViewController.SetFavoriteState(false);
                PlaylistsCollection.RemoveLevelFromPlaylist(PlaylistsCollection.loadedPlaylists.First(x => x.playlistTitle == "Your favorite songs"), SongDownloader.GetLevelID(song));
            }
            else
            {
                PluginConfig.favoriteSongs.Add(SongDownloader.GetLevelID(song));
                PluginConfig.SaveConfig();

                _songDetailViewController.SetFavoriteState(true);
                PlaylistsCollection.AddSongToPlaylist(PlaylistsCollection.loadedPlaylists.First(x => x.playlistTitle == "Your favorite songs"), new PlaylistSong() { levelId = SongDownloader.GetLevelID(song), songName = song.songName, level = SongDownloader.GetLevel(SongDownloader.GetLevelID(song)), key = song.key });
            }
        }

        private void _songDetailViewController_downloadButtonPressed(Song song)
        {
            if (!SongDownloader.Instance.IsSongDownloaded(song))
            {
                _downloadQueueViewController.EnqueueSong(song, true);
                _songDetailViewController.SetDownloadState(DownloadState.Downloading);
            }
            else
            {
                _simpleDialog.Init("Delete song", $"Do you really want to delete \"{song.songName} {song.songSubName}\"?", "Delete", "Cancel",
                    (selectedButton) => 
                    {
                        DismissViewController(_simpleDialog, null, false);
                        if (selectedButton == 0)
                            DeleteSong(_lastDeletedSong);
                        _lastDeletedSong = null;
                    });
                _lastDeletedSong = song;
                PresentViewController(_simpleDialog, null, false);
            }
        }

        private void DeleteSong(Song song)
        {
                 SongDownloader.Instance.DeleteSong(song);
            _songDetailViewController.SetDownloadState(DownloadState.NotDownloaded);
            _moreSongsListViewController.Refresh();
        }

        public bool IsDownloadingSong(Song song)
        {
            return _downloadQueueViewController.queuedSongs.Any(x => x.Compare(song) && (x.songQueueState == SongQueueState.Downloading || x.songQueueState == SongQueueState.Queued));
        }

        private void SongDownloader_songDownloaded(Song downloadedSong)
        {
            if (currentPageSongs != null &&currentPageSongs.Contains(downloadedSong))
            {
                _moreSongsListViewController.Refresh();
            }
            if (downloadedSong != null && downloadedSong.Compare(_lastSelectedSong))
            {
                _songDetailViewController.SetDownloadState(DownloadState.Downloaded);
            }
        }

        private void _moreSongsListViewController_didSelectRow(int row)
        {
            if (!_songDetailViewController.isInViewControllerHierarchy)
            {
                PushViewControllerToNavigationController(_moreSongsNavigationController, _songDetailViewController);
            }

            if (!scoreSaber) 
            {
                _songDetailViewController.SetContent(this, currentPageSongs[row]);
                _descriptionViewController.SetDescription(currentPageSongs[row].description);
                _lastSelectedSong = currentPageSongs[row];
            } 
            else
            {
                StartCoroutine(DidSelectRow(row));
            }
            
        }

        private void _moreSongsListViewController_searchButtonPressed()
        {
            if (_searchViewController == null)
            {
                _searchViewController = BeatSaberUI.CreateViewController<SearchKeyboardViewController>();
                _searchViewController.backButtonPressed += _searchViewController_backButtonPressed;
                _searchViewController.searchButtonPressed += _searchViewController_searchButtonPressed;
            }

            PresentViewController(_searchViewController);
        }

        private void _searchViewController_searchButtonPressed(string obj)
        {
            DismissViewController(_searchViewController);
            _moreSongsListViewController.SelectTopButtons(TopButtonsState.Select);

            currentPage = 0;
            currentSearchRequest = obj;
            StartCoroutine(GetSearchResults(currentPage, currentSearchRequest));
        }

        private void _searchViewController_backButtonPressed()
        {
            DismissViewController(_searchViewController);
            _moreSongsListViewController.SelectTopButtons(TopButtonsState.Select);
        }

        private void _moreSongsListViewController_pageDownPressed()
        {
            currentPage++;
            if(string.IsNullOrEmpty(currentSearchRequest))
                if (!scoreSaber) 
                {
                    StartCoroutine(GetPage(currentPage, currentSortMode));
                } else 
                {
                    StartCoroutine(GetPageScoreSaber(currentPage, currentScoreSaberSortMode));
                }
            else
                StartCoroutine(GetSearchResults(currentPage, currentSearchRequest));
            _moreSongsListViewController.TogglePageUpDownButtons(true, true);
        }

        private void _moreSongsListViewController_pageUpPressed()
        {
            if (currentPage > 0)
            {
                currentPage--;
                if (string.IsNullOrEmpty(currentSearchRequest))
                    if (!scoreSaber)
                    {
                        StartCoroutine(GetPage(currentPage, currentSortMode));
                    } 
                    else 
                    {
                        StartCoroutine(GetPageScoreSaber(currentPage,currentScoreSaberSortMode));
                    }
                else
                    StartCoroutine(GetSearchResults(currentPage, currentSearchRequest));

                if (currentPage == 0)
                {
                    _moreSongsListViewController.TogglePageUpDownButtons(false, true);
                }
                else
                {
                    _moreSongsListViewController.TogglePageUpDownButtons(true, true);
                }
            }
        }
        
        public IEnumerator DidSelectRow(int row)
        {

            yield return null;
            _songDetailViewController.SetLoadingState(true);
            UnityWebRequest www = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/maps/by-hash/{currentPageSongs[row].hash}");
            www.timeout = 15;
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                Plugin.log.Error($"Unable to connect to {PluginConfig.beatsaverURL}! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                try
                {
                    
                    Newtonsoft.Json.Linq.JObject jNode = JObject.Parse(www.downloadHandler.text);
               //     JSONNode node = JSON.Parse(www.downloadHandler.text);
                    currentPageSongs[row] = new Song((JObject)jNode, false);
                    //      currentPageSongs[row] = new Song(node["songs"][0], false);
                    _songDetailViewController.SetContent(this, currentPageSongs[row]);
                    _descriptionViewController.SetDescription(currentPageSongs[row].description);
                    _lastSelectedSong = currentPageSongs[row];
                } catch (Exception e)
                {
                    Plugin.log.Critical("Unable to parse response! Exception: " + e);
                }
            }
            _songDetailViewController.SetLoadingState(false);
        }

        public IEnumerator GetPageScoreSaber(int page, int cat) 
        {

            yield return null;
            scoreSaber = true;
            _moreSongsListViewController.SetLoadingState(true);
            _moreSongsListViewController.TogglePageUpDownButtons((page > 0), true);
            _moreSongsListViewController.SetContent(null);

            string url = $"{PluginConfig.scoresaberURL}/api.php?function=get-leaderboards&cat={cat}&limit=6&page={(page + 1)}&unique=1";
            if (cat == 3) { url = url + "&ranked=1"; }
            UnityWebRequest www = UnityWebRequest.Get(url);
            www.timeout = 15;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
           {
                Plugin.log.Error($"Unable to connect to {PluginConfig.scoresaberURL}! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                try 
                {
                    JObject jNode = JObject.Parse(www.downloadHandler.text);
                    currentPageSongs.Clear();
                    for (int i = 0; i < Math.Min(jNode["songs"].Children().Count(), songsPerPage); i++)
                    {
                        currentPageSongs.Add(new Song((JObject)jNode["songs"][i], true));
                    }

                    _moreSongsListViewController.SetContent(currentPageSongs);
                }
                catch (Exception e) 
                {
                    Plugin.log.Critical("Unable to parse response! Exception: " + e);
                }
            }
            _moreSongsListViewController.SetLoadingState(false);
        }

        public IEnumerator GetPage(int page, string sortBy)
        {
            yield return null;
            scoreSaber = false;
            _moreSongsListViewController.SetLoadingState(true);
            _moreSongsListViewController.TogglePageUpDownButtons((page > 0), true);
            _moreSongsListViewController.SetContent(null);
            
            UnityWebRequest www = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/maps/{sortBy}/{(page * 6)}");
            www.timeout = 15;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Plugin.log.Error($"Unable to connect to {PluginConfig.beatsaverURL}! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                try
                {
                    JObject jNode = JObject.Parse(www.downloadHandler.text);

                    currentPageSongs.Clear();

                    for (int i = 0; i < Math.Min(jNode["docs"].Children().Count(), songsPerPage); i++)
                    {
                        currentPageSongs.Add(new Song((JObject)jNode["docs"][i], false));
                    }

                    _moreSongsListViewController.SetContent(currentPageSongs);
                }
                catch (Exception e)
                {
                    Plugin.log.Critical("Unable to parse response! Exception: " + e);
                }
            }
            _moreSongsListViewController.SetLoadingState(false);
        }

        public IEnumerator GetSearchResults(int page, string search)
        {
            yield return null;
            scoreSaber = false;
            _moreSongsListViewController.SetLoadingState(true);
            _moreSongsListViewController.TogglePageUpDownButtons((page > 0), true);
            _moreSongsListViewController.SetContent(null);

            UnityWebRequest www = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/search/text/{page}?q={search}");

            www.timeout = 30;
            yield return www.SendWebRequest();
            
            if (www.isNetworkError || www.isHttpError)
            {
                Plugin.log.Error($"Unable to connect to {PluginConfig.beatsaverURL}! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                try
                {
                    JObject jNode = JObject.Parse(www.downloadHandler.text);

                    currentPageSongs.Clear();

                    for (int i = 0; i < Math.Min(jNode["docs"].Children().Count(), songsPerPage); i++)
                    {
                        currentPageSongs.Add(new Song((JObject)jNode["docs"][i], false));
                    }

                    _moreSongsListViewController.SetContent(currentPageSongs);
                }
                catch (Exception e)
                {
                    Plugin.log.Critical("Unable to parse response! Exception: " + e);
                }
            }
            _moreSongsListViewController.SetLoadingState(false);
        }
    }

}
