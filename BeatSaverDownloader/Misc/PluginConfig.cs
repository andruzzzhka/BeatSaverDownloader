using IllusionPlugin;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SongBrowserPlugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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

    public class PluginConfig
    {
        static public bool beatDropInstalled = false;
        static public string beatDropPlaylistsLocation = "";

        static private string votedSongsPath = "UserData\\votedSongs.json";
        static private string configPath = "UserData\\favoriteSongs.cfg";
        static private string oldConfigPath = "favoriteSongs.cfg";
        static private string songBrowserSettings = "song_browser_settings.xml";

        public static List<string> favoriteSongs = new List<string>();
        public static Dictionary<string, SongVote> votedSongs = new Dictionary<string, SongVote>();

        public static string beatsaverURL = "https://beatsaver.com";
        public static string apiAccessToken { get; private set; }

        static public string apiTokenPlaceholder = "replace-this-with-your-api-token";

        public static bool disableSongListTweaks = false;
        public static bool disableDeleteButton = false;
        public static bool deleteToRecycleBin = true;
        public static bool enableSongIcons = true;

        public static int maxSimultaneousDownloads = 3;


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

            if (!ModPrefs.HasKey("BeatSaverDownloader", "maxSimultaneousDownloads"))
            {
                ModPrefs.SetInt("BeatSaverDownloader", "maxSimultaneousDownloads", 3);
                Logger.Log("Created config");
            }
            else
            {
                maxSimultaneousDownloads = ModPrefs.GetInt("BeatSaverDownloader", "maxSimultaneousDownloads", 3, true);
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
            
            if(!disableSongListTweaks)
                LoadSongBrowserConfig();
        }

        public static void LoadSongBrowserConfig()
        {
            if (!File.Exists(songBrowserSettings))
            {
                return;
            }

            FileStream fs = null;
            try
            {
                fs = File.OpenRead(songBrowserSettings);
                XmlSerializer serializer = new XmlSerializer(typeof(SongBrowserSettings));
                SongBrowserSettings settings = (SongBrowserSettings)serializer.Deserialize(fs);
                favoriteSongs.AddRange(settings.favorites);
                fs.Close();
                SaveConfig();
            }
            catch (Exception e)
            {
                Logger.Log($"Unable to parse Song Browser settings file! Exception: {e}");
                if (fs != null) { fs.Close(); }
            }
        }

        public static void SaveConfig()
        {
            File.WriteAllText(votedSongsPath, JsonConvert.SerializeObject(votedSongs, Formatting.Indented), Encoding.UTF8);
            File.WriteAllLines(configPath, favoriteSongs.Distinct().ToArray(), Encoding.UTF8);

            ModPrefs.SetBool("BeatSaverDownloader", "disableDeleteButton", disableDeleteButton);
            ModPrefs.SetBool("BeatSaverDownloader", "deleteToRecycleBin", deleteToRecycleBin);
            ModPrefs.SetInt("BeatSaverDownloader", "maxSimultaneousDownloads", maxSimultaneousDownloads);
        }
    }
}