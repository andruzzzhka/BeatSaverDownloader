using IllusionPlugin;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SongBrowserPlugin;

namespace BeatSaverDownloader.Misc
{
    internal class PluginConfig
    {
        static private bool beatDropInstalled = false;
        static private string beatDropPlaylistsLocation = "";

        static private string configPath = "UserData\\favoriteSongs.cfg";
        static private string oldConfigPath = "favoriteSongs.cfg";
        static private string songBrowserSettings = "song_browser_settings.xml";

        public static List<string> favoriteSongs = new List<string>();

        public static string beatsaverURL = "https://beatsaver.com";
        public static string apiAccessToken { get; private set; }

        static public string apiTokenPlaceholder = "replace-this-with-your-api-token";

        public static bool disableSongListTweaks = false;
        public static bool disableDeleteButton = false;

        public static int maxSimultaneousDownloads = 3;


        public static void LoadOrCreateConfig()
        {
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
                else
                {
                    Logger.Log("Loaded config");
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

            if (!ModPrefs.HasKey("BeatSaverDownloader", "maxSimultaneousDownloads"))
            {
                ModPrefs.SetInt("BeatSaverDownloader", "maxSimultaneousDownloads", 3);
                Logger.Log("Created config");
            }
            else
            {
                maxSimultaneousDownloads = ModPrefs.GetInt("BeatSaverDownloader", "maxSimultaneousDownloads", 3, true);
            }

            if (File.Exists(oldConfigPath))
            {
                File.Move(oldConfigPath, configPath);
            }

            if (!File.Exists(configPath))
            {
                File.Create(configPath).Close();
            }

            favoriteSongs.AddRange(File.ReadAllLines(configPath, Encoding.UTF8));

            if (IllusionInjector.PluginManager.Plugins.Count(x => x.Name == "Song Browser") > 0)
            {
                Logger.Log("Song Browser installed, disabling Song List Tweaks");
                disableSongListTweaks = true;
                return;
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
                    Logger.Log("Can't find the BeatDrop installation folder!");
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
                Logger.Log($"Can't parse BeatSaberSongBrowser settings file! Exception: {e}");
                if (fs != null) { fs.Close(); }
            }
        }

        public static void SaveConfig()
        {
            File.WriteAllLines(configPath, favoriteSongs.Distinct().ToArray(), Encoding.UTF8);
        }
    }
}