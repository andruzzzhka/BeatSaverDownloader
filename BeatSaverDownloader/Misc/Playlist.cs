using BeatSaverDownloader.UI;
using Newtonsoft.Json;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static void ReloadPlaylists(bool fullRefresh = true)
        {
            try
            {
                List<string> playlistFiles = new List<string>();

                if (PluginConfig.beatDropInstalled && Directory.Exists(Path.Combine(PluginConfig.beatDropPlaylistsLocation, "playlists")))
                {
                    try
                    {
                        string[] beatDropJSONPlaylists = Directory.GetFiles(Path.Combine(PluginConfig.beatDropPlaylistsLocation, "playlists"), "*.json");
                        string[] beatDropBPLISTPlaylists = Directory.GetFiles(Path.Combine(PluginConfig.beatDropPlaylistsLocation, "playlists"), "*.bplist");
                        playlistFiles.AddRange(beatDropJSONPlaylists);
                        playlistFiles.AddRange(beatDropBPLISTPlaylists);
                        Plugin.log.Info($"Found {beatDropJSONPlaylists.Length + beatDropBPLISTPlaylists.Length} playlists in BeatDrop folder");
                    }
                    catch (Exception e)
                    {
                        Plugin.log.Warn("Unable to load playlists from BeatDrop folder! Exception: " + e);
                    }
                }

                string[] localJSONPlaylists = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Playlists"), "*.json");
                string[] localBPLISTPlaylists = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Playlists"), "*.bplist");
                playlistFiles.AddRange(localJSONPlaylists);
                playlistFiles.AddRange(localBPLISTPlaylists);

                Plugin.log.Info($"Found {localJSONPlaylists.Length + localBPLISTPlaylists.Length} playlists in Playlists folder");

                if (fullRefresh)
                {
                    loadedPlaylists.Clear();

                    foreach (string path in playlistFiles)
                    {
                        try
                        {
                            Playlist playlist = Playlist.LoadPlaylist(path);
                            if (Path.GetFileName(path) == "favorites.json" && playlist.playlistTitle == "Your favorite songs")
                                continue;
                            loadedPlaylists.Add(playlist);
                        }
                        catch (Exception e)
                        {
                            Plugin.log.Info($"Unable to parse playlist @ {path}! Exception: {e}");
                        }
                    }
                }
                else
                {
                    foreach (string path in playlistFiles)
                    {
                        if (!loadedPlaylists.Any(x => x.fileLoc == path))
                        {
                            try
                            {
                                Playlist playlist = Playlist.LoadPlaylist(path);
                                if (Path.GetFileName(path) == "favorites.json" && playlist.playlistTitle == "Your favorite songs")
                                    continue;
                                loadedPlaylists.Add(playlist);
                                //bananbread songloader loaded playlist id
                                if (SongCore.Loader.AreSongsLoaded)
                                {
                                    MatchSongsForPlaylist(playlist);
                                }
                            }
                            catch (Exception e)
                            {
                                Plugin.log.Info($"Unable to parse playlist @ {path}! Exception: {e}");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.log.Critical("Unable to load playlists! Exception: " + e);
            }
        }

        public static void AddSongToPlaylist(Playlist playlist, PlaylistSong song)
        {
            playlist.songs.Add(song);
            if (playlist.playlistTitle == "Your favorite songs")
            {
                playlist.SavePlaylist();
            }
              (SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.FirstOrDefault(x => x is PlaylistLevelPackSO && (x as PlaylistLevelPackSO).playlist == playlist) as PlaylistLevelPackSO)?.UpdateDataFromPlaylist();
        }

        public static void RemoveLevelFromPlaylists(string levelId)
        {
            foreach (Playlist playlist in loadedPlaylists)
            {
                if (playlist.songs.Where(y => y.level != null).Any(x => x.level.levelID == levelId))
                {
                    PlaylistSong song = playlist.songs.First(x => x.level != null && x.level.levelID == levelId);
                    song.level = null;
                    song.levelId = "";
                }
                if (playlist.playlistTitle == "Your favorite songs")
                {
                    playlist.SavePlaylist();
                }
            }

            foreach (var pack in SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks)
            {
                if (pack is PlaylistLevelPackSO)
                {
                    (pack as PlaylistLevelPackSO).UpdateDataFromPlaylist();
                }
            }

        }

        public static void RemoveLevelFromPlaylist(Playlist playlist, string hash)
        {
            if (playlist.songs.Any(x => x.hash == hash ))
            {
                PlaylistSong song = playlist.songs.First(x => x.hash == hash);
                song.level = null;
                playlist.songs.Remove(song);
            }
            if (playlist.playlistTitle == "Your favorite songs")
            {
                playlist.SavePlaylist();
            }
            (SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.FirstOrDefault(x => x is PlaylistLevelPackSO && (x as PlaylistLevelPackSO).playlist == playlist) as PlaylistLevelPackSO)?.UpdateDataFromPlaylist();
        }

        public static void MatchSongsForPlaylist(Playlist playlist, bool matchAll = false)
        {
            //bananbread playlist id  
            if (!SongCore.Loader.AreSongsLoaded || SongCore.Loader.AreSongsLoading || playlist.playlistTitle == "All songs" || playlist.playlistTitle == "Your favorite songs") return;

            if (!playlist.songs.All(x => x.level != null) || matchAll)
            {
                playlist.songs.AsParallel().ForAll(x =>
                {
                //    Plugin.log.Info(x.songName);
                //    Plugin.log.Info(x.key);
                    if (x.level == null || matchAll)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(x.levelId)) //check that we have levelId and if we do, try to match level
                            {
                                x.level = SongCore.Loader.CustomLevels.Values.FirstOrDefault(y => y.levelID == x.levelId);
                            }
                            if (x.level == null && !string.IsNullOrEmpty(x.hash)) //if level is still null, check that we have hash and if we do, try to match level
                            {
                                x.level = SongCore.Loader.CustomLevels.Values.FirstOrDefault(y => string.Equals(y.levelID.Split('_')[2], x.hash, StringComparison.OrdinalIgnoreCase));
                            }
                            /*
                            if (x.level == null && !string.IsNullOrEmpty(x.key)) //if level is still null, check that we have key and if we do, try to match level
                            {
                                var pathKey = SongCore.Loader.CustomLevels.Keys.FirstOrDefault(path => path.Contains($"\\{x.key} "));
                                if (!string.IsNullOrEmpty(pathKey))
                                {
                                    x.level = SongCore.Loader.CustomLevels[pathKey];
                                }

                            }
                            if (x.level == null) //If Still null, grab first level with same songName
                            {
                                               x.level = SongCore.Loader.CustomLevels.Values.FirstOrDefault(y => y.songName == x.songName);
                            }
                            */
                        }
                        catch (Exception e)
                        {
                            Plugin.log.Warn($"Unable to match song with {(string.IsNullOrEmpty(x.key) ? " unknown key!" : ("key " + x.key + " !"))}");
                        }
                    }
                });
            }

        }

        public static void MatchSongsForAllPlaylists(bool matchAll = false)
        {
            Plugin.log.Info("Matching songs for all playlists!");
            Task.Run(() =>
            {
                for (int i = 0; i < loadedPlaylists.Count; i++)
                {
                    MatchSongsForPlaylist(loadedPlaylists[i], matchAll);
                }
                //bananbread playlist levelpacks 
                HMMainThreadDispatcher.instance.Enqueue(() => { SongListTweaks.Instance.UpdateLevelPacks(); });
            });
        }
    }

    public class PlaylistSong
    {
        public string key { get { if (_key == null) return ""; else return _key; } set { _key = value; } }
        private string _key;
        public string newKey = "";
        public string songName { get { if (_songName == null) return ""; else return _songName; } set { _songName = value; } }
        private string _songName;

        public string hash { get { if (_hash == null) return ""; else return _hash; } set { _hash = value; } }
        private string _hash;

        public string levelId { get { if (_levelId == null) return ""; else return _levelId; } set { _levelId = value; } }
        private string _levelId;

        [JsonIgnore]
        public CustomPreviewBeatmapLevel level { get { return _level; } set { if (_level != value) { _level = value; UpdateSongInfo(); } } }
        private CustomPreviewBeatmapLevel _level;

        [NonSerialized]
        public bool oneSaber;
        [NonSerialized]
        public string path;

        public IEnumerator MatchKey()
        {
            if (!string.IsNullOrEmpty(key) || level == null || !(level is CustomPreviewBeatmapLevel))
                yield break;

            if (!string.IsNullOrEmpty(hash))
            {
                ScrappedSong song = ScrappedData.Songs.FirstOrDefault(x => hash.ToUpper() == x.Hash);
                if (song != null)
                    key = song.Key;
                else
                    yield return SongDownloader.Instance.RequestSongByLevelIDCoroutine(hash, (Song bsSong) => { if (bsSong != null) key = bsSong.key; });
            }
            else if (!string.IsNullOrEmpty(levelId))
            {
                ScrappedSong song = ScrappedData.Songs.FirstOrDefault(x => levelId.StartsWith(x.Hash));
                if (song != null)
                    key = song.Key;
                else
                    yield return SongDownloader.Instance.RequestSongByLevelIDCoroutine(level.levelID.Split('_')[2], (Song bsSong) => { if (bsSong != null) key = bsSong.key; });
            }
            else if (level != null)
            {
                ScrappedSong song = ScrappedData.Songs.FirstOrDefault(x => level.levelID.StartsWith(x.Hash));
                if (song != null)
                    key = song.Key;
                else
                    yield return SongDownloader.Instance.RequestSongByLevelIDCoroutine(level.levelID.Split('_')[2], (Song bsSong) => { if (bsSong != null) key = bsSong.key; });
            }
        }

        private void UpdateSongInfo()
        {
            if (level != null)
            {
                songName = level.songName + " " + level.songSubName;
                levelId = level.levelID;
                //bananbread id customlevel 
                hash = SongCore.Utilities.Hashing.GetCustomLevelHash(level);
            }
        }

        public bool Compare(Song song)
        {
            if (!string.IsNullOrEmpty(hash) && !string.IsNullOrEmpty(song.hash))
            {
                return hash.ToUpper() == song.hash.ToUpper();
            }
            if (!string.IsNullOrEmpty(levelId) && !string.IsNullOrEmpty(song.hash))
            {
                return levelId.ToUpper().StartsWith(song.hash.ToUpper());
            }
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(song.key))
            {
                return key.ToUpper() == song.key.ToUpper();
            }
            return false;
        }
    }

    public class Playlist
    {
        public string playlistTitle { get; set; }
        public string playlistAuthor { get; set; }
        public string image { get; set; }
        public int playlistSongCount { get; set; }
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
                    icon = Sprites.Base64ToSprite(image.Substring(image.IndexOf(",") + 1));
                }
                catch
                {
                    Plugin.log.Critical("Unable to convert playlist image to sprite!");
                    icon = Sprites.BeastSaberLogo;
                }
            }
            else
            {
                icon = Sprites.BeastSaberLogo;
            }
            playlistTitle = playlistNode["playlistTitle"];
            playlistAuthor = playlistNode["playlistAuthor"];
            customDetailUrl = playlistNode["customDetailUrl"];
            customArchiveUrl = playlistNode["customArchiveUrl"];
            if (!string.IsNullOrEmpty(customDetailUrl))
            {
                if (!customDetailUrl.EndsWith("/"))
                    customDetailUrl += "/";
                Plugin.log.Info("Found playlist with customDetailUrl! Name: " + playlistTitle + ", CustomDetailUrl: " + customDetailUrl);
            }
            if (!string.IsNullOrEmpty(customArchiveUrl) && customArchiveUrl.Contains("[KEY]"))
            {
                Plugin.log.Info("Found playlist with customArchiveUrl! Name: " + playlistTitle + ", CustomArchiveUrl: " + customArchiveUrl);
            }

            songs = new List<PlaylistSong>();

            foreach (JSONNode node in playlistNode["songs"].AsArray)
            {
                PlaylistSong song = new PlaylistSong();
                song.key = node["key"];
                song.songName = node["songName"];
                song.hash = node["hash"];
                song.levelId = node["levelId"];

                songs.Add(song);
            }

            if (playlistNode["playlistSongCount"] != null)
            {
                playlistSongCount = playlistNode["playlistSongCount"].AsInt;
            }
            else
            {
                playlistSongCount = songs.Count;
            }
            if (playlistNode["fileLoc"] != null)
                fileLoc = playlistNode["fileLoc"];

            if (playlistNode["playlistURL"] != null)
                fileLoc = playlistNode["playlistURL"];
        }

        public static Playlist LoadPlaylist(string path)
        {
            Playlist playlist = new Playlist(JSON.Parse(File.ReadAllText(path)));
            playlist.fileLoc = path;
            return playlist;
        }

        public void SavePlaylist(string path = "")
        {
            if (ScrappedData.Songs.Count > 0)
                SharedCoroutineStarter.instance.StartCoroutine(SavePlaylistCoroutine(path));
        }

        public IEnumerator SavePlaylistCoroutine(string path = "")
        {
            Plugin.log.Info($"Saving playlist \"{playlistTitle}\"...");
            try
            {
                image = Sprites.SpriteToBase64(icon);
                playlistSongCount = songs.Count;
            }
            catch (Exception e)
            {
                Plugin.log.Critical("Unable to save playlist! Exception: " + e);
                yield break;
            }
            foreach (PlaylistSong song in songs)
            {
                yield return song.MatchKey();
            }

            try
            {
                if (!string.IsNullOrEmpty(path))
                {
                    fileLoc = Path.GetFullPath(path);
                }

                File.WriteAllText(fileLoc, JsonConvert.SerializeObject(this, Formatting.Indented));

                Plugin.log.Info("Playlist saved!");
            }
            catch (Exception e)
            {
                Plugin.log.Critical("Unable to save playlist! Exception: " + e);
                yield break;
            }
        }

        public bool PlaylistEqual(object obj)
        {
            if (obj == null) return false;

            var playlist = obj as Playlist;

            if (playlist == null) return false;

            int songCountThis = (songs != null ? (songs.Count > 0 ? songs.Count : playlistSongCount) : playlistSongCount);
            int songCountObj = (playlist.songs != null ? (playlist.songs.Count > 0 ? playlist.songs.Count : playlist.playlistSongCount) : playlist.playlistSongCount);

            return playlistTitle == playlist.playlistTitle &&
                   playlistAuthor == playlist.playlistAuthor &&
                   songCountThis == songCountObj;
        }
    }
}
