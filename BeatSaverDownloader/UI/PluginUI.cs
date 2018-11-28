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

        private Button _moreSongsButton;

        public void OnLoad()
        {
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            SetupUI();

            if (!SongLoader.AreSongsLoaded)
                SongLoader.SongsLoadedEvent += SongLoader_SongsLoadedEvent;
            else
                SongLoader_SongsLoadedEvent(null, SongLoader.CustomLevels);
        }

        private void SongLoader_SongsLoadedEvent(SongLoader arg1, List<CustomLevel> arg2)
        {
            _moreSongsButton.interactable = true;
        }

        private void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            if (to.name == "EmptyTransition")
            {
                if (Instance)
                    Destroy(Instance.gameObject);
                Instance = null;
            }
        }

        private void SetupUI()
        {
            if (initialized) return;
            
            RectTransform mainMenu = (Resources.FindObjectsOfTypeAll<MainMenuViewController>().First().rectTransform);

            //_moreSongsButton = BeatSaberUI.CreateUIButton(mainMenu, "CreditsButton", new Vector2(-21f, -15f), new Vector2(40, 8.8f), BeatSaverButtonPressed,  buttonText: "More songs...");
            //_moreSongsButton.interactable = false;
            MenuButtonUI.AddButton("More songs...", BeatSaverButtonPressed);

            initialized = true;
        }

        public void BeatSaverButtonPressed()
        {
            if (moreSongsFlowCoordinator == null)
                moreSongsFlowCoordinator = new GameObject("MoreSongsFlowCoordinator").AddComponent<MoreSongsFlowCoordinator>();
            
            MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
            
            mainFlow.InvokeMethod("PresentFlowCoordinator", moreSongsFlowCoordinator, null, false, false);
        }

    }
}
