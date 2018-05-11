using HMUI;
using IllusionPlugin;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeatSaverDownloader
{
    public class Plugin : IPlugin
    {
        




        string IPlugin.Name { get { return "BeatSaver Downloader"; } }

        string IPlugin.Version { get { return "0.0.0.2"; } }

        void IPlugin.OnApplicationQuit()
        {
            
        }

        void IPlugin.OnApplicationStart()
        {
            
        }

        void IPlugin.OnFixedUpdate()
        {
            
        }

        void IPlugin.OnLevelWasInitialized(int level)
        {
            
            
        }

        void IPlugin.OnLevelWasLoaded(int level)
        {
            if (level == 1)
            {
                CustomUI.OnLoad();

            }
        }

        void IPlugin.OnUpdate()
        {
            
        }
        
    }
}
