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

namespace BeatSaverDownloader
{
    public class Plugin : IPlugin
    {
        string IPlugin.Name { get { return "BeatSaver Downloader"; } }

        string IPlugin.Version { get { return "3.2.0"; } }
        
        public void OnApplicationQuit()
        {
            PluginConfig.SaveConfig();
        }
        
        public void OnApplicationStart()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            PluginConfig.LoadOrCreateConfig();
            Base64Sprites.ConvertToSprites();
            PlaylistsCollection.ReloadPlaylists();
            SongLoader.SongsLoadedEvent += SongLoader_SongsLoadedEvent;
        }

        private void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            Logger.Log($"Active scene changed from \"{from.name}\" to \"{to.name}\"");

            if (from.name == "EmptyTransition" && to.name.Contains("Menu"))
            {
                try
                {
                    PluginUI.Instance.OnLoad();
                    VotingUI.Instance.OnLoad();
                    if (!PluginConfig.disableSongListTweaks)
                        SongListTweaks.Instance.OnLoad();
                }catch(Exception e)
                {
                    Logger.Exception("Exception on scene change: "+e);
                }
            }
        }

        private void SongLoader_SongsLoadedEvent(SongLoader sender, List<CustomLevel> levels)
        {
            PlaylistsCollection.MatchSongsForAllPlaylists(true);
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
