using SimpleJSON;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BeatSaverDownloader.Misc
{
    public static class PlaylistsCollection
    {
        public static List<Playlist> loadedPlaylists = new List<Playlist>();

        public static void ReloadPlaylists()
        {
            try
            {
                loadedPlaylists.Clear();

                List<string> playlistFiles = new List<string>();

                if (PluginConfig.beatDropInstalled)
                {
                    string[] beatDropPlaylists = Directory.GetFiles(Path.Combine(PluginConfig.beatDropPlaylistsLocation, "playlists"), "*.json");
                    playlistFiles.AddRange(beatDropPlaylists);
                    Logger.Log($"Found {beatDropPlaylists.Length} playlists in BeatDrop folder");
                }

                string[] localPlaylists = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Playlists"), "*.json");
                playlistFiles.AddRange(localPlaylists);

                Logger.Log($"Found {localPlaylists.Length} playlists in Playlists folder");

                foreach (string path in playlistFiles)
                {
                    try
                    {
                        Playlist playlist = Playlist.LoadPlaylist(path);
                        loadedPlaylists.Add(playlist);
                        Logger.Log($"Found \"{playlist.playlistTitle}\" by {playlist.playlistAuthor}");
                    }
                    catch (Exception e)
                    {
                        Logger.Log($"Unable to parse playlist @ {path}! Exception: {e}");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Exception("Unable to load playlists! Exception: " + e);
            }
        }

        public static void AddSongToPlaylist(Playlist playlist, PlaylistSong song)
        {
            playlist.songs.Add(song);
        }

        public static void RemoveLevelFromPlaylists(string levelId)
        {
            foreach (Playlist playlist in loadedPlaylists)
            {
                if (playlist.songs.Where(y => y.level != null).Any(x => x.level.levelID == levelId))
                {
                    playlist.songs.First(x => x.level != null && x.level.levelID == levelId).level = null;
                }
            }
        }

        public static void RemoveLevelFromPlaylist(Playlist playlist, string levelId)
        {
            if (playlist.songs.Where(y => y.level != null).Any(x => x.level.levelID == levelId))
            {
                playlist.songs.First(x => x.level != null && x.level.levelID == levelId).level = null;
            }
        }

        public static void MatchSongsForPlaylist(Playlist playlist, bool matchAll = false)
        {
            if (!SongLoader.AreSongsLoaded || SongLoader.AreSongsLoading || playlist.playlistTitle == "All songs" || playlist.playlistTitle == "Your favorite songs") return;
            if (!playlist.songs.All(x => x.level != null) || matchAll)
            {
                playlist.songs.ForEach(x =>
                {
                    if (x.level == null || matchAll)
                    {
                        x.level = SongLoader.CustomLevels.FirstOrDefault(y => (y.customSongInfo.path.Contains(x.key) && Directory.Exists(y.customSongInfo.path)) || (string.IsNullOrEmpty(x.levelId) ? false : y.levelID.StartsWith(x.levelId)));
                    }
                });
            }
        }

        public static void MatchSongsForAllPlaylists(bool matchAll = false)
        {
            Logger.Log("Matching songs for all playlists!");
            foreach (Playlist playlist in loadedPlaylists)
            {
                MatchSongsForPlaylist(playlist, matchAll);
            }
        }
    }

    public class PlaylistSong
    {
        public string key { get; set; }
        public string songName { get; set; }
        public string levelId { get; set; }

        [NonSerialized]
        public LevelSO level;
        [NonSerialized]
        public bool oneSaber;
        [NonSerialized]
        public string path;
    }

    public class Playlist
    {
        public string playlistTitle { get; set; }
        public string playlistAuthor { get; set; }
        public string image { get; set; }
        public int songCount { get; set; }
        public List<PlaylistSong> songs { get; set; }
        public string fileLoc { get; set; }
        public string customDetailUrl { get; set; }
        public string customArchiveUrl { get; set; }

        [NonSerialized]
        public Sprite icon;

        public Playlist()
        {

        }

        public Playlist(JSONNode playlistNode)
        {
            string image = playlistNode["image"].Value;
            if (!string.IsNullOrEmpty(image))
            {
                try
                {
                    icon = Base64Sprites.Base64ToSprite(image.Substring(image.IndexOf(",") + 1));
                }
                catch
                {
                    Logger.Exception("Unable to convert playlist image to sprite!");
                    icon = Base64Sprites.BeastSaberLogo;
                }
            }
            else
            {
                icon = Base64Sprites.BeastSaberLogo;
            }
            playlistTitle = playlistNode["playlistTitle"];
            playlistAuthor = playlistNode["playlistAuthor"];
            customDetailUrl = playlistNode["customDetailUrl"];
            customArchiveUrl = playlistNode["customArchiveUrl"];
            if (!string.IsNullOrEmpty(customDetailUrl))
            {
                if (!customDetailUrl.EndsWith("/"))
                    customDetailUrl += "/";
                Logger.Log("Found playlist with customDetailUrl! Name: " + playlistTitle + ", CustomDetailUrl: " + customDetailUrl);
            }
            if (!string.IsNullOrEmpty(customArchiveUrl) && customArchiveUrl.Contains("[KEY]"))
            {
                Logger.Log("Found playlist with customArchiveUrl! Name: " + playlistTitle + ", CustomArchiveUrl: " + customArchiveUrl);
            }

            songs = new List<PlaylistSong>();

            foreach (JSONNode node in playlistNode["songs"].AsArray)
            {
                PlaylistSong song = new PlaylistSong();
                song.key = node["key"];
                song.songName = node["songName"];
                song.levelId = node["levelId"];

                songs.Add(song);
            }

            if (playlistNode["playlistSongCount"] != null)
            {
                songCount = playlistNode["playlistSongCount"].AsInt;
            }
            if (playlistNode["fileLoc"] != null)
                fileLoc = playlistNode["fileLoc"];

            if (playlistNode["playlistURL"] != null)
                fileLoc = playlistNode["playlistURL"];
        }

        public static Playlist LoadPlaylist(string path)
        {
            return new Playlist(JSON.Parse(File.ReadAllText(path)));
        }
    }
}
