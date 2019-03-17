using IllusionPlugin;
using System;
using UnityEngine.SceneManagement;
using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI;
using System.Collections.Generic;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using UnityEngine;
using Logger = BeatSaverDownloader.Misc.Logger;
using BS_Utils.Gameplay;

namespace BeatSaverDownloader
{
    public class Plugin : IPlugin
    {
        string IPlugin.Name { get { return "BeatSaver Downloader"; } }

        string IPlugin.Version { get { return "3.2.9"; } }
        
        public void OnApplicationQuit()
        {
            PluginConfig.SaveConfig();
        }
        
        public void OnApplicationStart()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged; ;
            PluginConfig.LoadOrCreateConfig();
            Sprites.ConvertToSprites();
            PlaylistsCollection.ReloadPlaylists();
            SongLoader.SongsLoadedEvent += SongLoader_SongsLoadedEvent;
            
            BSEvents.OnLoad();
            BSEvents.menuSceneLoadedFresh += OnMenuSceneLoadedFresh;
        }

        private void OnMenuSceneLoadedFresh()
        {
            try
            {
                PluginUI.Instance.OnLoad();
                VotingUI.Instance.OnLoad();
                SongListTweaks.Instance.OnLoad();

                GetUserInfo.GetUserName();
            }
            catch (Exception e)
            {
                Logger.Exception("Exception on fresh menu scene change: " + e);
            }
        }

        private void SongLoader_SongsLoadedEvent(SongLoader sender, List<CustomLevel> levels)
        {
            try
            {
                PlaylistsCollection.MatchSongsForAllPlaylists(true);
            }
            catch(Exception e)
            {
                Misc.Logger.Exception("Unable to match songs for all playlists! Exception: "+e);
            }
        }
        
        private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
        {
            Logger.Log($"Active scene changed from \"{arg0.name}\" to \"{arg1.name}\"");
        }

        private void SceneManager_sceneLoaded(Scene to, LoadSceneMode loadMode)
        {
            Logger.Log($"Loaded scene \"{to.name}\"");
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
            if (Input.GetKeyDown(KeyCode.R))
            {
                PlaylistsCollection.ReloadPlaylists();
            }
        }
    }
}
