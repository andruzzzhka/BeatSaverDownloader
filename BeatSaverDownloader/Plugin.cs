using IllusionPlugin;
using System;
using UnityEngine.SceneManagement;
using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI;

namespace BeatSaverDownloader
{
    public class Plugin : IPlugin
    {
        string IPlugin.Name { get { return "BeatSaver Downloader"; } }

        string IPlugin.Version { get { return "3.0.0"; } }
        
        public void OnApplicationQuit()
        {
            PluginConfig.SaveConfig();
        }
        
        public void OnApplicationStart()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            PluginConfig.LoadOrCreateConfig();
            Base64Sprites.ConvertToSprites();
        }

        private void SceneManager_sceneLoaded(Scene to, LoadSceneMode loadMode)
        {
            if(to.name.Contains("Menu"))
                PluginUI.Instance.OnLoad();
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
