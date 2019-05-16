using System;
using UnityEngine.SceneManagement;
using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI;
using System.Collections.Generic;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using UnityEngine;
using BS_Utils.Gameplay;
using IPA;

namespace BeatSaverDownloader
{
    public class Plugin : IBeatSaberPlugin
    {
        public static Plugin instance;
        public static IPA.Logging.Logger log;
        
        public void Init(object nullObject, IPA.Logging.Logger logger)
        {
            log = logger;
        }

        public void OnApplicationQuit()
        {
            PluginConfig.SaveConfig();
        }
        
        public void OnApplicationStart()
        {
            instance = this;
            PluginConfig.LoadConfig();
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
                Plugin.log.Critical("Exception on fresh menu scene change: " + e);
            }
        }

        public void SongLoader_SongsLoadedEvent(SongLoader sender, List<CustomLevel> levels)
        {
            try
            {
                PlaylistsCollection.MatchSongsForAllPlaylists(true);
            }
            catch(Exception e)
            {
                Plugin.log.Critical("Unable to match songs for all playlists! Exception: "+e);
            }
        }

        public void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                PlaylistsCollection.ReloadPlaylists();
            }
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
        }

        public void OnSceneUnloaded(Scene scene)
        {
        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
        }

        public void OnFixedUpdate()
        {
        }
    }
}
