using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using BeatSaverDownloader.UI;
using IPA.Config;

namespace BeatSaverDownloader.Misc
{
    public enum VoteType { Upvote, Downvote };

    public struct SongVote
    {
        public string key;
        [JsonConverter(typeof(StringEnumConverter))]
        public VoteType voteType;

        public SongVote(string key, VoteType voteType)
        {
            this.key = key;
            this.voteType = voteType;
        }
    }

    public struct SongReview
    {
        public string key;
        public float fun_factor;
        public float rhythm;
        public float flow;
        public float pattern_quality;
        public float readability;
        public float level_quality;

        public SongReview(string key, float fun_factor, float rhythm, float flow, float pattern_quality, float readability, float level_quality)
        {
            this.key = key;
            this.fun_factor = fun_factor;
            this.rhythm = rhythm;
            this.flow = flow;
            this.pattern_quality = pattern_quality;
            this.readability = readability;
            this.level_quality = level_quality;
        }
    }

    public class PluginConfig
    {
        static public bool beatDropInstalled = false;
        static public string beatDropPlaylistsLocation = "";

        static private BS_Utils.Utilities.Config config = new BS_Utils.Utilities.Config("BeatSaverDownloader");

        static private string votedSongsPath = "UserData\\votedSongs.json";
        static private string reviewedSongsPath = "UserData\\reviewedSongs.json";
        static private string favSongsPath = "UserData\\favoriteSongs.cfg";
        static private string oldFavSongsPath = "favoriteSongs.cfg";

        public static List<string> favoriteSongs = new List<string>();
        public static Dictionary<string, SongVote> votedSongs = new Dictionary<string, SongVote>();
        public static Dictionary<string, SongReview> reviewedSongs = new Dictionary<string, SongReview>();

        public static string beatsaverURL = "https://beatsaver.com";
        public static string apiAccessToken { get; private set; }

        static public string apiTokenPlaceholder = "replace-this-with-your-api-token";

        static public string lastSelectedPack = "";
        static public string lastSelectedSong = "";
        static public SortMode lastSelectedSortMode = SortMode.Default;

        public static bool disableSongListTweaks = false;
        public static bool disableDeleteButton = false;
        public static bool deleteToRecycleBin = true;
        public static bool enableSongIcons = true;
        public static bool rememberLastPackAndSong = true;

        public static int maxSimultaneousDownloads = 3;
        public static int fastScrollSpeed = 5;
        
        public static void LoadConfig()
        {
            if (IPA.Loader.PluginManager.AllPlugins.Select(x => x.Metadata.Name) //BSIPA Plugins
                .Concat(IPA.Loader.PluginManager.Plugins.Select(x => x.Name))    //Old IPA Plugins
                .Any(x => x == "Song Browser" || x == "Song Browser Plugin"))
            {
                Plugin.log.Info("Song Browser installed, disabling Song List Tweaks");
                disableSongListTweaks = true;
            }

            if (!Directory.Exists("UserData"))
            {
                Directory.CreateDirectory("UserData");
            }

            LoadIni();

            if (File.Exists(oldFavSongsPath) && !File.Exists(favSongsPath))
            {
                File.Move(oldFavSongsPath, favSongsPath);
            }

            if (!File.Exists(favSongsPath))
            {
                File.Create(favSongsPath).Close();
            }

            favoriteSongs.AddRange(File.ReadAllLines(favSongsPath, Encoding.UTF8));

            if (!File.Exists(votedSongsPath))
            {
                File.WriteAllText(votedSongsPath, JsonConvert.SerializeObject(votedSongs), Encoding.UTF8);
            }
            else
            {
                votedSongs = JsonConvert.DeserializeObject<Dictionary<string, SongVote>>(File.ReadAllText(votedSongsPath, Encoding.UTF8));
            }

            if (!File.Exists(reviewedSongsPath))
            {
                File.WriteAllText(reviewedSongsPath, JsonConvert.SerializeObject(reviewedSongs), Encoding.UTF8);
            }
            else
            {
                reviewedSongs = JsonConvert.DeserializeObject<Dictionary<string, SongReview>>(File.ReadAllText(reviewedSongsPath, Encoding.UTF8));
            }

            try
            {
                if (Registry.CurrentUser.OpenSubKey(@"Software").GetSubKeyNames().Contains("178eef3d-4cea-5a1b-bfd0-07a21d068990"))
                {
                    beatDropPlaylistsLocation = (string)Registry.CurrentUser.OpenSubKey(@"Software\178eef3d-4cea-5a1b-bfd0-07a21d068990").GetValue("InstallLocation", "");
                    if (Directory.Exists(beatDropPlaylistsLocation))
                    {
                        beatDropInstalled = true;
                    }
                    else if (Directory.Exists("%LocalAppData%\\Programs\\BeatDrop\\playlists"))
                    {
                        beatDropInstalled = true;
                        beatDropPlaylistsLocation = "%LocalAppData%\\Programs\\BeatDrop\\playlists";
                    }
                    else
                    {
                        beatDropInstalled = false;
                        beatDropPlaylistsLocation = "";
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.log.Info($"Can't open registry key! Exception: {e}");
                if (Directory.Exists("%LocalAppData%\\Programs\\BeatDrop\\playlists"))
                {
                    beatDropInstalled = true;
                    beatDropPlaylistsLocation = "%LocalAppData%\\Programs\\BeatDrop\\playlists";
                }
                else
                {
                    Plugin.log.Info("Unable to find BeatDrop installation folder!");
                }
            }

            if (!Directory.Exists("Playlists"))
            {
                Directory.CreateDirectory("Playlists");
            }
        }

        public static void LoadIni()
        {
            if(config.HasKey("BeatSaverDownloader", "beatsaverURL"))
            {
                beatsaverURL = config.GetString("BeatSaverDownloader", "beatsaverURL", "https://beatsaver.com");
                if (string.IsNullOrEmpty(beatsaverURL))
                {
                    config.SetString("BeatSaverDownloader", "beatsaverURL", "https://beatsaver.com");
                    beatsaverURL = "https://beatsaver.com";
                }
            }
            else
            {
                LoadOldIni();
                SaveConfig();
                return;
            }

            if (config.HasKey("BeatSaverDownloader", "apiAccessToken"))
            {
                apiAccessToken = config.GetString("BeatSaverDownloader", "apiAccessToken", apiTokenPlaceholder);
                if (string.IsNullOrEmpty(apiAccessToken))
                {
                    config.SetString("BeatSaverDownloader", "apiAccessToken", apiTokenPlaceholder);
                    apiAccessToken = apiTokenPlaceholder;
                }
            }

            if (config.HasKey("BeatSaverDownloader", "disableDeleteButton"))
            {
                disableDeleteButton = config.GetBool("BeatSaverDownloader", "disableDeleteButton", false);
            }

            if (config.HasKey("BeatSaverDownloader", "deleteToRecycleBin"))
            {
                deleteToRecycleBin = config.GetBool("BeatSaverDownloader", "deleteToRecycleBin", true);
            }

            if (config.HasKey("BeatSaverDownloader", "enableSongIcons"))
            {
                enableSongIcons = config.GetBool("BeatSaverDownloader", "enableSongIcons", true);
            }

            if (config.HasKey("BeatSaverDownloader", "rememberLastPackAndSong"))
            {
                rememberLastPackAndSong = config.GetBool("BeatSaverDownloader", "rememberLastPackAndSong", false);
            }

            if (config.HasKey("BeatSaverDownloader", "lastSelectedSong"))
            {
                lastSelectedSong = config.GetString("BeatSaverDownloader", "lastSelectedSong", "");
            }

            if (config.HasKey("BeatSaverDownloader", "lastSelectedPack"))
            {
                lastSelectedPack = config.GetString("BeatSaverDownloader", "lastSelectedPack", "");
            }

            if (config.HasKey("BeatSaverDownloader", "lastSelectedSortMode"))
            {
                lastSelectedSortMode = (SortMode)config.GetInt("BeatSaverDownloader", "lastSelectedSortMode", 0);
            }

            if (config.HasKey("BeatSaverDownloader", "maxSimultaneousDownloads"))
            {
                maxSimultaneousDownloads = config.GetInt("BeatSaverDownloader", "maxSimultaneousDownloads", 3);
            }

            if (config.HasKey("BeatSaverDownloader", "fastScrollSpeed"))
            {
                fastScrollSpeed = config.GetInt("BeatSaverDownloader", "fastScrollSpeed", 5);
            }
        }

        public static bool LoadOldIni()
        {
            if (!ModPrefs.HasKey("BeatSaverDownloader", "beatsaverURL"))
            {
                return false;
            }
            else
            {
                beatsaverURL = ModPrefs.GetString("BeatSaverDownloader", "beatsaverURL");
                if (string.IsNullOrEmpty(beatsaverURL))
                {
                    beatsaverURL = "https://beatsaver.com";
                }
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "apiAccessToken"))
            {
                return false;
            }
            else
            {
                apiAccessToken = ModPrefs.GetString("BeatSaverDownloader", "apiAccessToken");
                if (string.IsNullOrEmpty(apiAccessToken) || apiAccessToken == apiTokenPlaceholder)
                {
                    apiAccessToken = apiTokenPlaceholder;
                }
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "disableDeleteButton"))
            {
                return false;
            }
            else
            {
                disableDeleteButton = ModPrefs.GetBool("BeatSaverDownloader", "disableDeleteButton", false, true);
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "deleteToRecycleBin"))
            {
                return false;
            }
            else
            {
                deleteToRecycleBin = ModPrefs.GetBool("BeatSaverDownloader", "deleteToRecycleBin", true, true);
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "enableSongIcons"))
            {
                return false;
            }
            else
            {
                enableSongIcons = ModPrefs.GetBool("BeatSaverDownloader", "enableSongIcons", true, true);
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "rememberLastPackAndSong"))
            {
                return false;
            }
            else
            {
                rememberLastPackAndSong = ModPrefs.GetBool("BeatSaverDownloader", "rememberLastPackAndSong", true, true);
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "lastSelectedSong"))
            {
                return false;
            }
            else
            {
                lastSelectedSong = ModPrefs.GetString("BeatSaverDownloader", "lastSelectedSong");
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "lastSelectedPack"))
            {
                return false;
            }
            else
            {
                lastSelectedPack = ModPrefs.GetString("BeatSaverDownloader", "lastSelectedPack");
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "lastSelectedSortMode"))
            {
                return false;
            }
            else
            {
                lastSelectedSortMode = (SortMode)ModPrefs.GetInt("BeatSaverDownloader", "lastSelectedSortMode");
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "maxSimultaneousDownloads"))
            {
                return false;
            }
            else
            {
                maxSimultaneousDownloads = ModPrefs.GetInt("BeatSaverDownloader", "maxSimultaneousDownloads", 3, true);
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "fastScrollSpeed"))
            {
                return false;
            }
            else
            {
                fastScrollSpeed = ModPrefs.GetInt("BeatSaverDownloader", "fastScrollSpeed", 5, true);
            }

            return true;
        }

        public static void SaveConfig()
        {
            File.WriteAllText(votedSongsPath, JsonConvert.SerializeObject(votedSongs, Formatting.Indented), Encoding.UTF8);
            File.WriteAllText(reviewedSongsPath, JsonConvert.SerializeObject(reviewedSongs, Formatting.Indented), Encoding.UTF8);
            File.WriteAllLines(favSongsPath, favoriteSongs.Distinct().ToArray(), Encoding.UTF8);

            config.SetString("BeatSaverDownloader", "beatsaverURL", beatsaverURL);
            config.SetBool("BeatSaverDownloader", "disableDeleteButton", disableDeleteButton);
            config.SetBool("BeatSaverDownloader", "deleteToRecycleBin", deleteToRecycleBin);
            config.SetBool("BeatSaverDownloader", "enableSongIcons", enableSongIcons);
            config.SetBool("BeatSaverDownloader", "rememberLastPackAndSong", rememberLastPackAndSong);
            config.SetInt("BeatSaverDownloader", "maxSimultaneousDownloads", maxSimultaneousDownloads);
            config.SetInt("BeatSaverDownloader", "fastScrollSpeed", fastScrollSpeed);
            config.SetString("BeatSaverDownloader", "lastSelectedPack", lastSelectedPack ?? "");
            config.SetString("BeatSaverDownloader", "lastSelectedSong", lastSelectedSong ?? "");
            config.SetInt("BeatSaverDownloader", "lastSelectedSortMode", (int)lastSelectedSortMode);
        }
    }
}