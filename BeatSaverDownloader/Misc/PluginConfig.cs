using IllusionPlugin;
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

        static private string votedSongsPath = "UserData\\votedSongs.json";
        static private string reviewedSongsPath = "UserData\\reviewedSongs.json";
        static private string configPath = "UserData\\favoriteSongs.cfg";
        static private string oldConfigPath = "favoriteSongs.cfg";

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


        public static void LoadOrCreateConfig()
        {
            if (IllusionInjector.PluginManager.Plugins.Any(x => x.Name == "Song Browser"))
            {
                Logger.Log("Song Browser installed, disabling Song List Tweaks");
                disableSongListTweaks = true;
            }

            if (!Directory.Exists("UserData"))
            {
                Directory.CreateDirectory("UserData");
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "beatsaverURL"))
            {
                ModPrefs.SetString("BeatSaverDownloader", "beatsaverURL", "https://beatsaver.com");
                Logger.Log("Created config");
            }
            else
            {
                beatsaverURL = ModPrefs.GetString("BeatSaverDownloader", "beatsaverURL");
                if (string.IsNullOrEmpty(beatsaverURL))
                {
                    ModPrefs.SetString("BeatSaverDownloader", "beatsaverURL", "https://beatsaver.com");
                    beatsaverURL = "https://beatsaver.com";
                    Logger.Log("Created config");
                }
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "apiAccessToken"))
            {
                ModPrefs.SetString("BeatSaverDownloader", "apiAccessToken", apiTokenPlaceholder);
            }
            else
            {
                apiAccessToken = ModPrefs.GetString("BeatSaverDownloader", "apiAccessToken");
                if (string.IsNullOrEmpty(apiAccessToken) || apiAccessToken == apiTokenPlaceholder)
                {
                    ModPrefs.SetString("BeatSaverDownloader", "apiAccessToken", apiTokenPlaceholder);
                    apiAccessToken = apiTokenPlaceholder;
                }
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "disableDeleteButton"))
            {
                ModPrefs.SetBool("BeatSaverDownloader", "disableDeleteButton", false);
            }
            else
            {
                disableDeleteButton = ModPrefs.GetBool("BeatSaverDownloader", "disableDeleteButton", false, true);
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "deleteToRecycleBin"))
            {
                ModPrefs.SetBool("BeatSaverDownloader", "deleteToRecycleBin", true);
            }
            else
            {
                deleteToRecycleBin = ModPrefs.GetBool("BeatSaverDownloader", "deleteToRecycleBin", true, true);
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "enableSongIcons"))
            {
                ModPrefs.SetBool("BeatSaverDownloader", "enableSongIcons", true);
            }
            else
            {
                enableSongIcons = ModPrefs.GetBool("BeatSaverDownloader", "enableSongIcons", true, true);
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "rememberLastPackAndSong"))
            {
                ModPrefs.SetBool("BeatSaverDownloader", "rememberLastPackAndSong", true);
            }
            else
            {
                rememberLastPackAndSong = ModPrefs.GetBool("BeatSaverDownloader", "rememberLastPackAndSong", true, true);
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "lastSelectedSong"))
            {
                ModPrefs.SetString("BeatSaverDownloader", "lastSelectedSong", "");
            }
            else
            {
                lastSelectedSong = ModPrefs.GetString("BeatSaverDownloader", "lastSelectedSong");
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "lastSelectedPack"))
            {
                ModPrefs.SetString("BeatSaverDownloader", "lastSelectedPack", "");
            }
            else
            {
                lastSelectedPack = ModPrefs.GetString("BeatSaverDownloader", "lastSelectedPack");
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "lastSelectedSortMode"))
            {
                ModPrefs.SetInt("BeatSaverDownloader", "lastSelectedSortMode", 0);
            }
            else
            {
                lastSelectedSortMode = (SortMode)ModPrefs.GetInt("BeatSaverDownloader", "lastSelectedSortMode");
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "maxSimultaneousDownloads"))
            {
                ModPrefs.SetInt("BeatSaverDownloader", "maxSimultaneousDownloads", 3);
            }
            else
            {
                maxSimultaneousDownloads = ModPrefs.GetInt("BeatSaverDownloader", "maxSimultaneousDownloads", 3, true);
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "fastScrollSpeed"))
            {
                ModPrefs.SetInt("BeatSaverDownloader", "fastScrollSpeed", 5);
                Logger.Log("Created config");
            }
            else
            {
                fastScrollSpeed = ModPrefs.GetInt("BeatSaverDownloader", "fastScrollSpeed", 5, true);
            }

            if (File.Exists(oldConfigPath) && !File.Exists(configPath))
            {
                File.Move(oldConfigPath, configPath);
            }

            if (!File.Exists(configPath))
            {
                File.Create(configPath).Close();
            }
            
            favoriteSongs.AddRange(File.ReadAllLines(configPath, Encoding.UTF8));

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
                Logger.Log($"Can't open registry key! Exception: {e}");
                if (Directory.Exists("%LocalAppData%\\Programs\\BeatDrop\\playlists"))
                {
                    beatDropInstalled = true;
                    beatDropPlaylistsLocation = "%LocalAppData%\\Programs\\BeatDrop\\playlists";
                }
                else
                {
                    Logger.Log("Unable to find BeatDrop installation folder!");
                }
            }

            if (!Directory.Exists("Playlists"))
            {
                Directory.CreateDirectory("Playlists");
            }
        }

        public static void SaveConfig()
        {
            File.WriteAllText(votedSongsPath, JsonConvert.SerializeObject(votedSongs, Formatting.Indented), Encoding.UTF8);
            File.WriteAllText(reviewedSongsPath, JsonConvert.SerializeObject(reviewedSongs, Formatting.Indented), Encoding.UTF8);
            File.WriteAllLines(configPath, favoriteSongs.Distinct().ToArray(), Encoding.UTF8);

            ModPrefs.SetBool("BeatSaverDownloader", "disableDeleteButton", disableDeleteButton);
            ModPrefs.SetBool("BeatSaverDownloader", "deleteToRecycleBin", deleteToRecycleBin);
            ModPrefs.SetBool("BeatSaverDownloader", "enableSongIcons", enableSongIcons);
            ModPrefs.SetBool("BeatSaverDownloader", "rememberLastPackAndSong", rememberLastPackAndSong);
            ModPrefs.SetInt("BeatSaverDownloader", "maxSimultaneousDownloads", maxSimultaneousDownloads);
            ModPrefs.SetInt("BeatSaverDownloader", "fastScrollSpeed", fastScrollSpeed);
            ModPrefs.SetString("BeatSaverDownloader", "lastSelectedPack", lastSelectedPack ?? "");
            ModPrefs.SetString("BeatSaverDownloader", "lastSelectedSong", lastSelectedSong ?? "");
            ModPrefs.SetInt("BeatSaverDownloader", "lastSelectedSortMode", (int)lastSelectedSortMode);
        }
    }
}