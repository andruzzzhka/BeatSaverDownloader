using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BeatSaverDownloader.UI.FlowCoordinators;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using CustomUI.MenuButton;
using TMPro;
using BeatSaverDownloader.Misc;
using System;
using CustomUI.Settings;

namespace BeatSaverDownloader.UI
{
    class PluginUI : MonoBehaviour
    {
        public bool initialized = false;

        private static PluginUI _instance = null;
        public static PluginUI Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = new GameObject("BeatSaverDownloader").AddComponent<PluginUI>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        public MoreSongsFlowCoordinator moreSongsFlowCoordinator;
        public MorePlaylistsFlowCoordinator morePlaylistsFlowCoordinator;

        //private Button _moreSongsButton;

        public void OnLoad()
        {
            initialized = false;
            SetupUI();

            if (!SongLoader.AreSongsLoaded)
                SongLoader.SongsLoadedEvent += SongLoader_SongsLoadedEvent;
            else
                SongLoader_SongsLoadedEvent(null, SongLoader.CustomLevels);

            StartCoroutine(ScrappedData.Instance.DownloadScrappedData((List<ScrappedSong> songs) => {
                if (PlaylistsCollection.loadedPlaylists.Any(x => x.playlistTitle == "Your favorite songs"))
                {
                    PlaylistsCollection.loadedPlaylists.First(x => x.playlistTitle == "Your favorite songs").SavePlaylist("Playlists\\favorites.json");
                }
            }));
        }

        private void SongLoader_SongsLoadedEvent(SongLoader arg1, List<CustomLevel> arg2)
        {
            //_moreSongsButton.interactable = true;
        }

        private void SetupUI()
        {
            if (initialized) return;
            
            RectTransform mainMenu = (Resources.FindObjectsOfTypeAll<MainMenuViewController>().First().rectTransform);

            MenuButtonUI.AddButton("More songs...", BeatSaverButtonPressed);
            MenuButtonUI.AddButton("More playlists...", PlaylistsButtonPressed);
            //_moreSongsButton.interactable = false;

            var downloaderSubMenu = SettingsUI.CreateSubMenu("Downloader");

            var disableDeleteButton = downloaderSubMenu.AddBool("Disable delete button");
            disableDeleteButton.GetValue += delegate { return PluginConfig.disableDeleteButton; };
            disableDeleteButton.SetValue += delegate (bool value) { PluginConfig.disableDeleteButton = value; PluginConfig.SaveConfig(); };

            var deleteToRecycleBin = downloaderSubMenu.AddBool("Delete to Recycle Bin");
            deleteToRecycleBin.GetValue += delegate { return PluginConfig.deleteToRecycleBin; };
            deleteToRecycleBin.SetValue += delegate (bool value) { PluginConfig.deleteToRecycleBin = value; PluginConfig.SaveConfig(); };

            var maxSimultaneousDownloads = downloaderSubMenu.AddInt("Max simultaneous downloads", 1, 10, 1);
            maxSimultaneousDownloads.GetValue += delegate { return PluginConfig.maxSimultaneousDownloads; };
            maxSimultaneousDownloads.SetValue += delegate (int value) { PluginConfig.maxSimultaneousDownloads = value; PluginConfig.SaveConfig(); };

            initialized = true;
        }

        public void BeatSaverButtonPressed()
        {
            if (moreSongsFlowCoordinator == null)
                moreSongsFlowCoordinator = new GameObject("MoreSongsFlowCoordinator").AddComponent<MoreSongsFlowCoordinator>();

            MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();

            mainFlow.InvokeMethod("PresentFlowCoordinator", moreSongsFlowCoordinator, null, false, false);
        }

        public void PlaylistsButtonPressed()
        {
            if (morePlaylistsFlowCoordinator == null)
                morePlaylistsFlowCoordinator = new GameObject("MorePlaylistsFlowCoordinator").AddComponent<MorePlaylistsFlowCoordinator>();

            MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();

            mainFlow.InvokeMethod("PresentFlowCoordinator", morePlaylistsFlowCoordinator, null, false, false);
        }

    }
}
