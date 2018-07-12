using IllusionPlugin;
using UnityEngine.SceneManagement;

namespace BeatSaverDownloader
{
    public class Plugin : IPlugin
    {
        string IPlugin.Name { get { return "BeatSaver Downloader"; } }

        string IPlugin.Version { get { return "2.0"; } }

        

        public void OnApplicationQuit()
        {
            PluginConfig.SaveConfig();
        }
        
        public void OnApplicationStart()
        {
            PluginConfig.LoadOrCreateConfig();
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnLevelWasLoaded(int level)
        {
            if (level == 1)
            {
                BeatSaberUI.OnLoad();
                PluginUI.PluginUI.OnLoad();

            }
        }

        public void OnUpdate()
        {
        }
    }
}
