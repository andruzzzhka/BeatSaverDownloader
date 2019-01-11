using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI.ViewControllers;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using SimpleJSON;
using SongLoaderPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using VRUI;
using Logger = BeatSaverDownloader.Misc.Logger;

namespace BeatSaverDownloader.UI.FlowCoordinators
{
    class MorePlaylistsFlowCoordinator : FlowCoordinator
    {
        public static string playlistAPI_URL = "https://bsaber.com/PlaylistAPI/playlistAPI.json";
        
        private BackButtonNavigationController _playlistsNavigationController;
        private PlaylistListViewController _playlistsListViewController;
        private PlaylistDetailViewController _playlistDetailViewController;

        private List<Playlist> playlists = new List<Playlist>();

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                title = "More Playlists";

                _playlistsNavigationController = BeatSaberUI.CreateViewController<BackButtonNavigationController>();
                _playlistsNavigationController.didFinishEvent += _morePlaylistsNavigationController_didFinishEvent;

                _playlistsListViewController = BeatSaberUI.CreateViewController<PlaylistListViewController>();
                _playlistsListViewController.didSelectRow += _morePlaylistsListViewController_didSelectRow;
                _playlistsListViewController.highlightDownloadedPlaylists = true;

                GameObject _songDetailGameObject = Instantiate(Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First(), _playlistsNavigationController.rectTransform, false).gameObject;
                Destroy(_songDetailGameObject.GetComponent<StandardLevelDetailViewController>());
                _playlistDetailViewController = _songDetailGameObject.AddComponent<PlaylistDetailViewController>();
                _playlistDetailViewController.selectButtonPressed += _playlistDetailViewController_selectButtonPressed;
                _playlistDetailViewController.SetSelectButtonText("Add");
                _playlistDetailViewController.addDownloadButton = false;
            }

            SetViewControllersToNavigationConctroller(_playlistsNavigationController, new VRUIViewController[]
            {
                _playlistsListViewController
            });
            ProvideInitialViewControllers(_playlistsNavigationController, null, null);


            StartCoroutine(GetPlaylists());
        }

        private void _playlistDetailViewController_selectButtonPressed(Playlist playlist)
        {
            _playlistDetailViewController.SetSelectButtonState(false);
            StartCoroutine(DownloadPlaylistFile(playlist.fileLoc, (path) => {
                _playlistDetailViewController.SetSelectButtonState(true);
                PlaylistsCollection.loadedPlaylists.Add(Playlist.LoadPlaylist(path));
            }));
        }

        private void _morePlaylistsListViewController_didSelectRow(Playlist playlist)
        {
            if (!_playlistDetailViewController.isInViewControllerHierarchy)
            {
                PushViewControllerToNavigationController(_playlistsNavigationController, _playlistDetailViewController);
            }
            
            _playlistDetailViewController.SetContent(playlist);
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            if (deactivationType == DeactivationType.RemovedFromHierarchy)
            {
                PopViewControllerFromNavigationController(_playlistsNavigationController);
            }
        }

        private void _morePlaylistsNavigationController_didFinishEvent()
        {
            MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();

            mainFlow.InvokeMethod("DismissFlowCoordinator", this, null, false);
        }

        private void _moreSongsListViewController_didSelectRow(int row)
        {
            if (!_playlistDetailViewController.isInViewControllerHierarchy)
            {
                PushViewControllerToNavigationController(_playlistsNavigationController, _playlistDetailViewController);
            }

            _playlistDetailViewController.SetContent(playlists[row]);
        }

        public IEnumerator GetPlaylists()
        {
            yield return null;

            _playlistsListViewController.SetContent(null);

            UnityWebRequest www = UnityWebRequest.Get(playlistAPI_URL);
            www.timeout = 15;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Unable to connect to BeastSaber playlist API! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                try
                {
                    JSONNode node = JSON.Parse(www.downloadHandler.text);

                    playlists.Clear();

                    for (int i = 0; i < node.Count; i++)
                    {
                        playlists.Add(new Playlist(node[i]));
                    }

                    _playlistsListViewController.SetContent(playlists);
                }
                catch (Exception e)
                {
                    Logger.Exception("Unable to parse response! Exception: " + e);
                }
            }
        }

        public IEnumerator DownloadPlaylistFile(string url, Action<string> playlistDownloaded)
        {
            yield return null;

            UnityWebRequest www = UnityWebRequest.Get(url);
            www.timeout = 15;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Unable to connect to BeastSaber playlist API! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
                playlistDownloaded?.Invoke(null);
            }
            else
            {
                try
                {
                    string docPath = Application.dataPath;
                    docPath = docPath.Substring(0, docPath.Length - 5);
                    docPath = docPath.Substring(0, docPath.LastIndexOf("/"));
                    File.WriteAllText(docPath + "/Playlists/"+ Path.GetFileName(www.uri.LocalPath), www.downloadHandler.text);
                    playlistDownloaded?.Invoke(docPath + "/Playlists/" + Path.GetFileName(www.uri.LocalPath));
                }
                catch (Exception e)
                {
                    Logger.Exception("Unable to parse response! Exception: " + e);
                    playlistDownloaded?.Invoke(null);
                }
            }
        }

    }

}
