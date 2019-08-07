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
        public const int songsPerPage = 10;

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
        internal static List<Song> currentSortSongs = new List<Song>();
        internal static int collectedPages = 0;
        private Song _lastSelectedSong;

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

        private void ResetDetailView()
        {
            currentSortSongs.Clear();
            collectedPages = 0;
            if (_songDetailViewController.isInViewControllerHierarchy)
            {
                PopViewControllerFromNavigationController(_moreSongsNavigationController);
                _moreSongsListViewController.ResetOffset();
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


                _moreSongsListViewController.sortByTop += () => { ResetDetailView(); currentSortMode = "hot"; currentPage = 0; StartCoroutine(GetPage(currentPage, currentSortMode)); currentSearchRequest = ""; };
                _moreSongsListViewController.sortByNew += () => { ResetDetailView(); currentSortMode = "latest"; currentPage = 0; StartCoroutine(GetPage(currentPage, currentSortMode)); currentSearchRequest = ""; };
                _moreSongsListViewController.sortByBestRating += () => { ResetDetailView(); currentSortMode = "rating"; currentPage = 0; StartCoroutine(GetPage(currentPage, currentSortMode)); currentSearchRequest = ""; };
                _moreSongsListViewController.sortByMostDownloads += () => { ResetDetailView(); currentSortMode = "downloads"; currentPage = 0; StartCoroutine(GetPage(currentPage, currentSortMode)); currentSearchRequest = ""; };

                _moreSongsListViewController.sortByNewlyRanked += () => { ResetDetailView(); currentScoreSaberSortMode = 1; currentPage = 0; StartCoroutine(GetPageScoreSaber(currentPage, currentScoreSaberSortMode)); };
                _moreSongsListViewController.sortByTrending += () => { ResetDetailView(); currentScoreSaberSortMode = 0; currentPage = 0; StartCoroutine(GetPageScoreSaber(currentPage, currentScoreSaberSortMode)); };
                _moreSongsListViewController.sortByDifficulty += () => { ResetDetailView(); currentScoreSaberSortMode = 3; currentPage = 0; StartCoroutine(GetPageScoreSaber(currentPage, currentScoreSaberSortMode)); };

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
            if (PluginConfig.favoriteSongs.Any(x => x.Contains(song.hash)))
            {
                PluginConfig.favoriteSongs.Remove(SongDownloader.GetHash(song));
                PluginConfig.SaveConfig();

                _songDetailViewController.SetFavoriteState(false);
                PlaylistsCollection.RemoveLevelFromPlaylist(PlaylistsCollection.loadedPlaylists.First(x => x.playlistTitle == "Your favorite songs"), song.hash);
            }
            else
            {
                PluginConfig.favoriteSongs.Add(SongDownloader.GetHash(song));
                PluginConfig.SaveConfig();

                _songDetailViewController.SetFavoriteState(true);
                PlaylistsCollection.AddSongToPlaylist(PlaylistsCollection.loadedPlaylists.First(x => x.playlistTitle == "Your favorite songs"), new PlaylistSong() { levelId = SongDownloader.GetHash(song), songName = song.songName, level = SongDownloader.GetLevel(SongDownloader.GetHash(song)), key = song.key });
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
                            DeleteSong(_lastSelectedSong);
                    });
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
            if (currentSortSongs != null && currentSortSongs.Contains(downloadedSong))
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
                _songDetailViewController.SetContent(this, currentSortSongs[(currentPage * 6) + row]);
                _descriptionViewController.SetDescription(currentSortSongs[(currentPage * 6) + row].description);
                _lastSelectedSong = currentSortSongs[(currentPage * 6) + row];
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

            if (!string.IsNullOrWhiteSpace(obj))
            {
                currentSortSongs.Clear();
                collectedPages = 0;
                currentPage = 0;
                currentSearchRequest = obj;
                StartCoroutine(GetSearchResults(currentPage, currentSearchRequest));
            }

        }

        private void _searchViewController_backButtonPressed()
        {
            DismissViewController(_searchViewController);
            _moreSongsListViewController.SelectTopButtons(TopButtonsState.Select);
        }

        private void _moreSongsListViewController_pageDownPressed()
        {
            currentPage++;
            if (string.IsNullOrEmpty(currentSearchRequest))
                if (!scoreSaber)
                {
                    StartCoroutine(GetPage(currentPage, currentSortMode));
                }
                else
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
                        StartCoroutine(GetPageScoreSaber(currentPage, currentScoreSaberSortMode));
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
            
            UnityWebRequest www = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/maps/by-hash/{currentSortSongs[(currentPage * 6) + row].hash}");
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
                    currentSortSongs[(currentPage * 6) + row] = new Song((JObject)jNode, false);
                    //      currentPageSongs[row] = new Song(node["songs"][0], false);
                    _songDetailViewController.SetContent(this, currentSortSongs[(currentPage * 6) + row]);
                    _descriptionViewController.SetDescription(currentSortSongs[(currentPage * 6) + row].description);
                    _lastSelectedSong = currentSortSongs[(currentPage * 6) + row];
                }
                catch (Exception e)
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
            if (page <= collectedPages)
            {
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
                        currentSortSongs.AddRange(currentPageSongs);
                        collectedPages++;

                        _moreSongsListViewController.SetContent(currentSortSongs.GetRange(page * 6, Math.Min(6, currentSortSongs.Count - (currentPage * 6))));
                    }
                    catch (Exception e)
                    {
                        Plugin.log.Critical("Unable to parse response! Exception: " + e);
                    }
                }
            }
            else
            {
                _moreSongsListViewController.SetContent(currentSortSongs.GetRange(page * 6, Math.Min(6, currentSortSongs.Count - (currentPage * 6))));
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
            if (page <= collectedPages)
            {
                UnityWebRequest www = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/maps/{sortBy}/{(page)}");
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
                        currentSortSongs.AddRange(currentPageSongs);
                        collectedPages++;
            //            Plugin.log.Info(currentSortSongs.Count.ToString());
                 //       Plugin.log.Info((page * 6).ToString());
               //         Plugin.log.Info((currentSortSongs.Count - (currentPage * 6)).ToString());
                        _moreSongsListViewController.SetContent(currentSortSongs.GetRange(page * 6, Math.Min(6, currentSortSongs.Count - (currentPage * 6))));
                    }
                    catch (Exception e)
                    {
                        Plugin.log.Critical("Unable to parse response! Exception: " + e);
                    }
                }
            }
            else
            {
                Plugin.log.Info(currentSortSongs.Count.ToString());
                Plugin.log.Info((page * 6).ToString());
                Plugin.log.Info((currentSortSongs.Count - (currentPage * 6)).ToString());
                _moreSongsListViewController.SetContent(currentSortSongs.GetRange(page * 6, Math.Min(6, currentSortSongs.Count - (currentPage * 6))));
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
            if (page <= collectedPages)
            {
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

                        currentSortSongs.AddRange(currentPageSongs);
                        collectedPages++;
                        _moreSongsListViewController.SetContent(currentSortSongs.GetRange(page * 6, Math.Min(6, currentSortSongs.Count - (currentPage * 6))));
                    }
                    catch (Exception e)
                    {
                        Plugin.log.Critical("Unable to parse response! Exception: " + e);
                    }
                }
            }
            else
            {
                _moreSongsListViewController.SetContent(currentSortSongs.GetRange(page * 6, Math.Min(6, currentSortSongs.Count - (currentPage * 6))));
            }
            _moreSongsListViewController.SetLoadingState(false);
        }
    }

}
