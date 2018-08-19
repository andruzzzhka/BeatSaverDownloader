using IllusionPlugin;
using System;
using UnityEngine.SceneManagement;

namespace BeatSaverDownloader
{
    public class Plugin : IPlugin
    {
        string IPlugin.Name { get { return "BeatSaver Downloader"; } }

        string IPlugin.Version { get { return "2.5"; } }

        

        public void OnApplicationQuit()
        {
            PluginConfig.SaveConfig();
        }
        
        public void OnApplicationStart()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            PluginConfig.LoadOrCreateConfig();
        }

        private void SceneManager_sceneLoaded(Scene loadedScene, LoadSceneMode loadMode)
        {
            if (loadedScene.name == "Menu")
            {
                BeatSaberUI.OnLoad();
                PluginUI.PluginUI.OnLoad();
            }
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnUpdate()
        {
        }
    }
}
