using IllusionPlugin;

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

        void IEnhancedPlugin.OnLateUpdate()
        {
            
        }

        void IPlugin.OnLevelWasInitialized(int level)
        {
            
            
        }

        void IPlugin.OnLevelWasLoaded(int level)
        {
            if (level == 1)
            {
                BeatSaverUI.OnLoad();
                
            }
        }

        void IPlugin.OnUpdate()
        {
            
        }
        
    }
}
