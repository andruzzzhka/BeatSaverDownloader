using IllusionPlugin;
using UnityEngine.SceneManagement;

namespace BeatSaverDownloader
{
    public class Plugin : IPlugin
    {
        string IPlugin.Name { get { return "BeatSaver Downloader"; } }

        string IPlugin.Version { get { return "1.5"; } }

        

        public void OnApplicationQuit()
        {
        }
        
        public void OnApplicationStart()
        {
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
                BeatSaverUI.OnLoad();

            }
        }

        public void OnUpdate()
        {
        }
    }
}
