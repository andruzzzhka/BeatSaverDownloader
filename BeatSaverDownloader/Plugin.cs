using IllusionPlugin;
using System.IO;
using UnityEngine.SceneManagement;

namespace BeatSaverDownloader
{
    public class Plugin : IPlugin
    {
        string IPlugin.Name { get { return "BeatSaver Downloader"; } }

        string IPlugin.Version { get { return "1.5"; } }

        static public string beatsaverURL;

        public static void LoadConfig()
        {
            if (!Directory.Exists("UserData"))
            {
                Directory.CreateDirectory("UserData");
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "beatsaverURL"))
            {
                ModPrefs.SetString("BeatSaverDownloader", "beatsaverURL", "https://beatsaver.com");
                Logger.StaticLog("Created config");
            }
            else
            {
                beatsaverURL = ModPrefs.GetString("BeatSaverDownloader", "beatsaverURL");
                if (string.IsNullOrEmpty(beatsaverURL))
                {
                    ModPrefs.SetString("BeatSaverDownloader", "beatsaverURL", "https://beatsaver.com");
                    beatsaverURL = "https://beatsaver.com";
                    Logger.StaticLog("Created config");
                }
                else
                {
                    Logger.StaticLog("Loaded config");
                }
                
            }
        }

        public void OnApplicationQuit()
        {
        }
        
        public void OnApplicationStart()
        {
            LoadConfig();
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
