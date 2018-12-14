using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI.ViewControllers;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using SimpleJSON;
using SongLoaderPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using VRUI;
using Logger = BeatSaverDownloader.Misc.Logger;

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
        private SimpleDialogPromptViewController _simpleDialog;

        public int currentPage = 0;
        public string currentSortMode = "top";
        public string currentSearchRequest = "";

        private List<Song> currentPageSongs = new List<Song>();

        private Song _lastSelectedSong;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                title = "More Songs";

                SongDownloader.Instance.songDownloaded += SongDownloader_songDownloaded;

                _moreSongsNavigationController = BeatSaberUI.CreateViewController<BackButtonNavigationController>();
                _moreSongsNavigationController.didFinishEvent += _moreSongsNavigationController_didFinishEvent;

                _moreSongsListViewController = BeatSaberUI.CreateViewController<MoreSongsListViewController>();
                _moreSongsListViewController.pageDownPressed += _moreSongsListViewController_pageDownPressed;
                _moreSongsListViewController.pageUpPressed += _moreSongsListViewController_pageUpPressed;
                _moreSongsListViewController.sortByTop += () => { currentSortMode = "top"; currentPage = 0; StartCoroutine(GetPage(currentPage, currentSortMode)); currentSearchRequest = ""; };
                _moreSongsListViewController.sortByNew += () => { currentSortMode = "new"; currentPage = 0; StartCoroutine(GetPage(currentPage, currentSortMode)); currentSearchRequest = ""; };
                _moreSongsListViewController.sortByPlays += () => { currentSortMode = "plays"; currentPage = 0; StartCoroutine(GetPage(currentPage, currentSortMode)); currentSearchRequest = ""; };
                _moreSongsListViewController.searchButtonPressed += _moreSongsListViewController_searchButtonPressed;
                _moreSongsListViewController.didSelectRow += _moreSongsListViewController_didSelectRow;

                GameObject _songDetailGameObject = Instantiate(Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First(), _moreSongsNavigationController.rectTransform, false).gameObject;
                Destroy(_songDetailGameObject.GetComponent<StandardLevelDetailViewController>());
                _songDetailViewController = _songDetailGameObject.AddComponent<SongDetailViewController>();
                _songDetailViewController.downloadButtonPressed += _songDetailViewController_downloadButtonPressed;
                _songDetailViewController.favoriteButtonPressed += _songDetailViewController_favoriteButtonPressed; ;

                _downloadQueueViewController = BeatSaberUI.CreateViewController<DownloadQueueViewController>();

                _simpleDialog = CustomUI.Utilities.ReflectionUtil.GetPrivateField<SimpleDialogPromptViewController>(Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First(), "_simpleDialogPromptViewController");
                _simpleDialog = Instantiate(_simpleDialog.gameObject, _simpleDialog.transform.parent).GetComponent<SimpleDialogPromptViewController>();
            }

            SetViewControllersToNavigationConctroller(_moreSongsNavigationController, new VRUIViewController[]
            {
                _moreSongsListViewController
            });
            ProvideInitialViewControllers(_moreSongsNavigationController, _downloadQueueViewController, null);
            
            StartCoroutine(GetPage(0, "top"));
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            if (deactivationType == DeactivationType.RemovedFromHierarchy)
            {
                PopViewControllerFromNavigationController(_moreSongsNavigationController);
            }
        }

        private void _moreSongsNavigationController_didFinishEvent()
        {
            if (!_downloadQueueViewController.queuedSongs.Any(x => x.songQueueState == SongQueueState.Downloading || x.songQueueState == SongQueueState.Queued))
            {
                SongLoader.Instance.RefreshSongs(false);
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
                PlaylistsCollection.AddSongToPlaylist(PlaylistsCollection.loadedPlaylists.First(x => x.playlistTitle == "Your favorite songs"), new PlaylistSong() { levelId = SongDownloader.GetLevelID(song), songName = song.songName, level = SongDownloader.GetLevel(SongDownloader.GetLevelID(song)), key = song.id });
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
                _simpleDialog.Init("Delete song", $"Do you really want to delete \"{ song.songName} {song.songSubName}\"?", "Delete", "Cancel");
                _simpleDialog.didFinishEvent -= (SimpleDialogPromptViewController sender, bool delete) => { DismissViewController(_simpleDialog, null, false); if (delete) DeleteSong(song); };
                _simpleDialog.didFinishEvent += (SimpleDialogPromptViewController sender, bool delete) => { DismissViewController(_simpleDialog, null, false); if (delete) DeleteSong(song); };
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

            _songDetailViewController.SetContent(this, currentPageSongs[row]);
            _lastSelectedSong = currentPageSongs[row];
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
                StartCoroutine(GetPage(currentPage, currentSortMode));
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
                    StartCoroutine(GetPage(currentPage, currentSortMode));
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
        
        public IEnumerator GetPage(int page, string sortBy)
        {
            yield return null;

            _moreSongsListViewController.SetLoadingState(true);
            _moreSongsListViewController.TogglePageUpDownButtons((page > 0), true);
            _moreSongsListViewController.SetContent(null);
            
            UnityWebRequest www = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/songs/{sortBy}/{(page * 6)}");
            www.timeout = 15;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Unable to connect to {PluginConfig.beatsaverURL}! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                try
                {
                    JSONNode node = JSON.Parse(www.downloadHandler.text);

                    currentPageSongs.Clear();

                    for (int i = 0; i < Math.Min(node["songs"].Count, songsPerPage); i++)
                    {
                        currentPageSongs.Add(new Song(node["songs"][i]));
                    }

                    _moreSongsListViewController.SetContent(currentPageSongs);
                }
                catch (Exception e)
                {
                    Logger.Exception("Unable to parse response! Exception: " + e);
                }
            }
            _moreSongsListViewController.SetLoadingState(false);
        }

        public IEnumerator GetSearchResults(int page, string search)
        {
            yield return null;

            _moreSongsListViewController.SetLoadingState(true);
            _moreSongsListViewController.TogglePageUpDownButtons((page > 0), true);
            _moreSongsListViewController.SetContent(null);

            UnityWebRequest www = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/songs/search/all/{search}");

            www.timeout = 30;
            yield return www.SendWebRequest();
            
            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Unable to connect to {PluginConfig.beatsaverURL}! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                try
                {
                    JSONNode node = JSON.Parse(www.downloadHandler.text);

                    currentPageSongs.Clear();

                    for (int i = (page * songsPerPage); i < Math.Min(node["songs"].Count, ((page + 1) * songsPerPage)); i++)
                    {
                        currentPageSongs.Add(Song.FromSearchNode(node["songs"][i]));
                    }

                    _moreSongsListViewController.SetContent(currentPageSongs);
                }
                catch (Exception e)
                {
                    Logger.Exception("Unable to parse response! Exception: " + e);
                }
            }
            _moreSongsListViewController.SetLoadingState(false);
        }
    }

}
