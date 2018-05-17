using IllusionPlugin;
using UnityEngine.SceneManagement;

namespace BeatSaverDownloader
{
    public class Plugin : IPlugin
    {
        string IPlugin.Name { get { return "BeatSaver Downloader"; } }

        string IPlugin.Version { get { return "1.4"; } }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
        }

        public void OnApplicationQuit()
        {
        }
        
        public void OnApplicationStart()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            if (scene.buildIndex == 1)
            {
                BeatSaverUI.OnLoad();

            }

        }

        public void OnSceneUnloaded(Scene scene)
        {
        }

        public void OnUpdate()
        {
        }
    }
}
