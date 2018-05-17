using IllusionPlugin;
using UnityEngine.SceneManagement;

namespace BeatSaverDownloader
{
    public class Plugin : IEnhancedPlugin
    {
        string IPlugin.Name { get { return "BeatSaver Downloader"; } }

        string IPlugin.Version { get { return "1.3"; } }

        string[] IEnhancedPlugin.Filter { get; }


        void IPlugin.OnApplicationQuit()
        {
            
        }

        void IPlugin.OnApplicationStart()
        {
            
        }

        void IPlugin.OnFixedUpdate()
        {
            
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode) {
            if (scene.buildIndex == 1)
            {
                BeatSaverUI.OnLoad();
                
            }
        }
        
        public void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene) {
            
        }

        public void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene prevScene, UnityEngine.SceneManagement.Scene nextScene) {
            
        }

        void IEnhancedPlugin.OnLateUpdate()
        {
            
        }

        void IPlugin.OnUpdate()
        {
            
        }
        
    }
}
