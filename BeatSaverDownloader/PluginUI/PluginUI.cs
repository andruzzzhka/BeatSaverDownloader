using BeatSaverDownloader.Misc;
using HMUI;
using IllusionPlugin;
using SimpleJSON;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace BeatSaverDownloader.PluginUI
{
    class PluginUI : MonoBehaviour
    {
        public Action<Song> downloadStarted;
        public Action<Song> downloadFinished;

        public static PluginUI _instance;
        private VotingUI _votingUI;
        private SongListUITweaks _tweaks;

        private Logger log = new Logger("BeatSaverDownloader");

        public BeatSaverNavigationController _beatSaverViewController;

        private RectTransform _mainMenuRectTransform;
        private StandardLevelSelectionFlowCoordinator _standardLevelSelectionFlowCoordinator;
        private StandardLevelListViewController _standardLevelListViewController;
        private GameplayMode _gameplayMode;

        private MainMenuViewController _mainMenuViewController;

        private StandardLevelDetailViewController _songDetailViewController;

        private Button _beatSaverButton;
        private Button _deleteButton;
        private Button _playButton;
        private Button _favButton;
        private Prompt _confirmDeleteState;

        public static string playerId;

        private bool isDeleting;
        public LevelCollectionsForGameplayModes _levelCollections;
        public List<LevelCollectionsForGameplayModes.LevelCollectionForGameplayMode> _levelCollectionsForGameModes;

        private bool _deleting
        {
            get { return isDeleting; }
            set
            {
                isDeleting = value;
                if (value)
                {
                    _playButton.interactable = false;
                }
                else
                {
                    _playButton.interactable = true;
                }
            }
        }

        public static void OnLoad()
        {
            if (_instance != null)
            {
                return;
            }
            new GameObject("BeatSaver Plugin").AddComponent<PluginUI>();
        }

        public void Awake()
        {
            _instance = this;
            _votingUI = gameObject.AddComponent<VotingUI>();
            _tweaks = gameObject.AddComponent<SongListUITweaks>();
        }

        public void Start()
        {
            playerId = ReflectionUtil.GetPrivateField<string>(PersistentSingleton<PlatformLeaderboardsModel>.instance, "_playerId");

            StartCoroutine(_votingUI.WaitForResults());

            if (!PluginConfig.disableSongListTweaks)
            {
                StartCoroutine(WaitForSongListUI());
            }

            try
            {
                _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                _mainMenuRectTransform = _mainMenuViewController.transform as RectTransform;

                _standardLevelSelectionFlowCoordinator = Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().First();
                _gameplayMode = ReflectionUtil.GetPrivateField<GameplayMode>(_standardLevelSelectionFlowCoordinator, "_gameplayMode");

                if (!PluginConfig.disableSongListTweaks)
                {
                    _standardLevelListViewController = ReflectionUtil.GetPrivateField<StandardLevelListViewController>(_standardLevelSelectionFlowCoordinator, "_levelListViewController");
                    _standardLevelListViewController.didSelectLevelEvent += PluginUI_didSelectSongEvent;

                    if (_standardLevelListViewController.selectedLevel != null)
                    {
                        UpdateDetailsUI(null, _standardLevelListViewController.selectedLevel.levelID);
                    }
                }

                CreateBeatSaverButton();
                _beatSaverButton.interactable = false;
            }
            catch (Exception e)
            {
                Logger.Exception("EXCEPTION ON AWAKE(TRY CREATE BUTTON): " + e);
            }


            if (SongLoader.AreSongsLoaded)
            {
                SongLoader_SongsLoadedEvent(null, null);
            }
            else
            {
                SongLoader.SongsLoadedEvent += SongLoader_SongsLoadedEvent;
            }
        }


        private void SongLoader_SongsLoadedEvent(SongLoader arg1, List<CustomLevel> arg2)
        {
            _levelCollections = Resources.FindObjectsOfTypeAll<LevelCollectionsForGameplayModes>().FirstOrDefault();
            _levelCollectionsForGameModes = ReflectionUtil.GetPrivateField<LevelCollectionsForGameplayModes.LevelCollectionForGameplayMode[]>(_levelCollections, "_collections").ToList();
            
            _beatSaverButton.interactable = true;

            try
            {
                Logger.Log("Creating default playlists...");

                List<CustomLevel> customLevels = SongLoader.CustomLevels;
                List<IStandardLevel> oneSaberLevels = _levelCollections.GetLevels(GameplayMode.SoloOneSaber).Where(x => !customLevels.Cast<IStandardLevel>().Contains(x)).Cast<IStandardLevel>().ToList();
                List<IStandardLevel> regularLevels = _levelCollections.GetLevels(GameplayMode.SoloStandard).Where(x => !customLevels.Cast<IStandardLevel>().Contains(x)).Cast<IStandardLevel>().ToList();

                Playlist _allPlaylist = new Playlist() { playlistTitle = "All songs", playlistAuthor = "You", image = Base64Sprites.BeastSaberLogo, icon = Base64ToSprite(Base64Sprites.BeastSaberLogo), fileLoc = "" };
                _allPlaylist.songs = new List<PlaylistSong>();
                _allPlaylist.songs.AddRange(regularLevels.Select(x => new PlaylistSong() { songName = $"{x.songName} {x.songSubName}", level = x, oneSaber = false, path = "", key = "", levelId = x.levelID }));
                _allPlaylist.songs.AddRange(oneSaberLevels.Select(x => new PlaylistSong() { songName = $"{x.songName} {x.songSubName}", level = x, oneSaber = true, path = "", key = "", levelId = x.levelID }));
                _allPlaylist.songs.AddRange(customLevels.Select(x => new PlaylistSong() { songName = $"{x.songName} {x.songSubName}", level = x, oneSaber = false, path = x.customSongInfo.path, key = "", levelId = x.levelID }));
                Logger.Log($"Created \"{_allPlaylist.playlistTitle}\" playlist with {_allPlaylist.songs.Count} songs!");

                Playlist _favPlaylist = new Playlist() { playlistTitle = "Your favorite songs", playlistAuthor = "You", image = Base64Sprites.BeastSaberLogo, icon = Base64ToSprite(Base64Sprites.BeastSaberLogo), fileLoc = "" };
                _favPlaylist.songs = new List<PlaylistSong>();
                _favPlaylist.songs.AddRange(regularLevels.Where(x => PluginConfig.favoriteSongs.Contains(x.levelID)).Select(x => new PlaylistSong() { songName = $"{x.songName} {x.songSubName}", level = x, oneSaber = false, path = "", key = "", levelId = x.levelID }));
                _favPlaylist.songs.AddRange(oneSaberLevels.Where(x => PluginConfig.favoriteSongs.Contains(x.levelID)).Select(x => new PlaylistSong() { songName = $"{x.songName} {x.songSubName}", level = x, oneSaber = true, path = "", key = "", levelId = x.levelID }));
                _favPlaylist.songs.AddRange(customLevels.Where(x => PluginConfig.favoriteSongs.Contains(x.levelID)).Select(x => new PlaylistSong() { songName = $"{x.songName} {x.songSubName}", level = x, oneSaber = false, path = x.customSongInfo.path, key = "", levelId = x.levelID }));
                Logger.Log($"Created \"{_favPlaylist.playlistTitle}\" playlist with {_favPlaylist.songs.Count} songs!");

                if (PluginConfig.playlists.Any(x => x.playlistTitle == "All songs" || x.playlistTitle == "Your favorite songs"))
                {
                    PluginConfig.playlists.RemoveAt(0);
                    PluginConfig.playlists.RemoveAt(0);
                }

                PluginConfig.playlists.Insert(0, _favPlaylist);
                PluginConfig.playlists.Insert(0, _allPlaylist);

                if (SongListUITweaks.lastPlaylist == null || SongListUITweaks.lastPlaylist.playlistTitle == "All songs")
                {
                    SongListUITweaks.lastPlaylist = _allPlaylist;
                }
                _tweaks.ShowPlaylist(SongListUITweaks.lastPlaylist);
            }catch(Exception e)
            {
                Logger.Exception($"Can't create default playlists! Exception: {e}");
            }
        }

        private void PluginUI_didSelectSongEvent(StandardLevelListViewController sender, IStandardLevel level)
        {
            UpdateDetailsUI(sender, level.levelID);
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                PluginConfig.LoadOrCreateConfig();
            }
        }

        private IEnumerator WaitForSongListUI()
        {
            Logger.Log("Waiting for song list...");

            yield return new WaitUntil(delegate () { return Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().Any(); });

            Logger.Log("Found song list!");

            _tweaks.SongListUIFound();

            if (SongListUITweaks.lastSortMode != SortMode.Default)
            {
                Logger.Log("Called ShowLevels, lastSortMode="+ SongListUITweaks.lastSortMode);
                _tweaks.ShowLevels(SongListUITweaks.lastSortMode);
            }

        }

        private void UpdateDetailsUI(StandardLevelListViewController sender, string selectedLevel)
        {

            if (_deleting)
            {
                _confirmDeleteState = Prompt.No;
            }


            if (_songDetailViewController == null)
            {
                _songDetailViewController = ReflectionUtil.GetPrivateField<StandardLevelDetailViewController>(_standardLevelSelectionFlowCoordinator, "_levelDetailViewController");
                
            }

            RectTransform detailsRectTransform = _songDetailViewController.GetComponent<RectTransform>();

            if (_deleteButton == null)
            {
                _deleteButton = BeatSaberUI.CreateUIButton(detailsRectTransform, "PlayButton");

                BeatSaberUI.SetButtonText(_deleteButton, "Delete");

                (_deleteButton.transform as RectTransform).anchoredPosition = new Vector2(27f, 10.75f);
                (_deleteButton.transform as RectTransform).sizeDelta = new Vector2(18f, 10f);

                if (selectedLevel.Length > 32)
                {
                    _deleteButton.onClick.RemoveAllListeners();
                    _deleteButton.onClick.AddListener(delegate ()
                    {
                        StartCoroutine(DeleteSong(selectedLevel));
                    });

                    _deleteButton.interactable = !PluginConfig.disableDeleteButton;
                }
                else
                {
                    _deleteButton.interactable = false;
                }
            }
            else
            {
                if(selectedLevel.Length > 32)
                {
                    _deleteButton.onClick.RemoveAllListeners();
                    _deleteButton.onClick.AddListener(delegate ()
                    {
                        StartCoroutine(DeleteSong(selectedLevel));
                    });

                    _deleteButton.interactable = !PluginConfig.disableDeleteButton;
                }
                else
                {
                    _deleteButton.interactable = false;
                }
            }

            if(_playButton == null)
            {
                _playButton = _songDetailViewController.GetComponentInChildren<Button>();
                (_playButton.transform as RectTransform).sizeDelta = new Vector2(30f, 10f);
                (_playButton.transform as RectTransform).anchoredPosition = new Vector2(2f, 6f);
                _playButton.interactable = true;
            }

            if (_favButton == null)
            {
                _favButton = BeatSaberUI.CreateUIButton(detailsRectTransform, "SettingsButton");

                RectTransform iconTransform = _favButton.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "Icon");
                iconTransform.gameObject.SetActive(true);
                Destroy(iconTransform.parent.GetComponent<HorizontalLayoutGroup>());
                iconTransform.sizeDelta = new Vector2(8f, 8f);
                iconTransform.anchoredPosition = new Vector2(2f, -2f);

                Destroy(_favButton.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "Text").gameObject);

                BeatSaberUI.SetButtonText(_favButton, "");
                BeatSaberUI.SetButtonIcon(_favButton, Base64ToSprite(PluginConfig.favoriteSongs.Contains(selectedLevel) ? Base64Sprites.RemoveFromFavorites : Base64Sprites.AddToFavorites));
                (_favButton.transform as RectTransform).anchoredPosition = new Vector2(-39f, 6f);
                (_favButton.transform as RectTransform).sizeDelta = new Vector2(10f, 10f);

                _favButton.onClick.RemoveAllListeners();
                _favButton.onClick.AddListener(delegate ()
                {
                    ToggleFavoriteSong(selectedLevel);
                });
            }
            else
            {
                BeatSaberUI.SetButtonIcon(_favButton, Base64ToSprite(PluginConfig.favoriteSongs.Contains(selectedLevel) ? Base64Sprites.RemoveFromFavorites : Base64Sprites.AddToFavorites));

                _favButton.onClick.RemoveAllListeners();
                _favButton.onClick.AddListener(delegate ()
                {
                    ToggleFavoriteSong(selectedLevel);
                });
            }

        }
        
        public void ToggleFavoriteSong(string selectedLevel)
        {
            if (PluginConfig.favoriteSongs.Contains(selectedLevel))
            {

                PluginConfig.favoriteSongs.Remove(selectedLevel);
                PluginConfig.playlists.FirstOrDefault(x => x.playlistTitle == "Your favorite songs")?.songs.RemoveAll(x => x.level.levelID == selectedLevel);
            }
            else
            {
                PluginConfig.favoriteSongs.Add(selectedLevel);

                CustomLevel customSong = SongLoader.CustomLevels.FirstOrDefault(x => x.levelID == selectedLevel);
                IStandardLevel regularSong = _levelCollections.GetLevels(GameplayMode.SoloStandard).FirstOrDefault(x => x.levelID == selectedLevel);
                IStandardLevel oneSaberSong = _levelCollections.GetLevels(GameplayMode.SoloOneSaber).FirstOrDefault(x => x.levelID == selectedLevel);

                if (customSong != null)
                {
                    PluginConfig.playlists.FirstOrDefault(x => x.playlistTitle == "Your favorite songs")?.songs.Add(new PlaylistSong() { songName = $"{customSong.songName} {customSong.songSubName}", level = customSong, oneSaber = false, path = customSong.customSongInfo.path, key = "" });
                }
                else if(regularSong != null)
                {
                    PluginConfig.playlists.FirstOrDefault(x => x.playlistTitle == "Your favorite songs")?.songs.Add(new PlaylistSong() { songName = $"{regularSong.songName} {regularSong.songSubName}", level = regularSong, oneSaber = false, path = "", key = "" });
                }else if(oneSaberSong != null)
                {
                    PluginConfig.playlists.FirstOrDefault(x => x.playlistTitle == "Your favorite songs")?.songs.Add(new PlaylistSong() { songName = $"{oneSaberSong.songName} {oneSaberSong.songSubName}", level = oneSaberSong, oneSaber = true, path = "", key = "" });
                }
            }
            BeatSaberUI.SetButtonIcon(_favButton, Base64ToSprite(PluginConfig.favoriteSongs.Contains(selectedLevel) ? Base64Sprites.RemoveFromFavorites : Base64Sprites.AddToFavorites));
            PluginConfig.SaveConfig();
        }

        IEnumerator DeleteSong(string levelId)
        {
            IStandardLevel[] _levelsForGamemode = ReflectionUtil.GetPrivateField<IStandardLevel[]>(ReflectionUtil.GetPrivateField<StandardLevelListViewController>(_standardLevelSelectionFlowCoordinator, "_levelListViewController"), "_levels");

            if (levelId.Length > 32 && _levelsForGamemode.Any(x => x.levelID == levelId) )
            {

                int currentSongIndex = _levelsForGamemode.ToList().FindIndex(x => x.levelID == levelId);

                currentSongIndex += (currentSongIndex == 0) ? 1 : -1;

                string nextLevelId = _levelsForGamemode[currentSongIndex].levelID;
                
                bool zippedSong = false;
                _deleting = true;

                string _songPath = SongLoader.CustomLevels.First(x => x.levelID == levelId).customSongInfo.path;

                if (!string.IsNullOrEmpty(_songPath) && _songPath.Contains("/.cache/"))
                {
                    zippedSong = true;
                }

                if (string.IsNullOrEmpty(_songPath))
                {
                    Logger.Error("Song path is null or empty!");
                    _playButton.interactable = true;
                    yield break;
                }
                if (!Directory.Exists(_songPath))
                {
                    Logger.Error("Song folder does not exists!");
                    _playButton.interactable = true;
                    yield break;
                }

                yield return PromptDeleteFolder(_songPath);

                if (_confirmDeleteState == Prompt.Yes)
                {
                    if (zippedSong)
                    {
                        Logger.Log("Deleting \"" + _songPath.Substring(_songPath.LastIndexOf('/')) + "\"...");
                        Directory.Delete(_songPath, true);

                        string songHash = Directory.GetParent(_songPath).Name;

                        if (Directory.GetFileSystemEntries(_songPath.Substring(0, _songPath.LastIndexOf('/'))).Length == 0)
                        {
                            Logger.Log("Deleting empty folder \"" + _songPath.Substring(0, _songPath.LastIndexOf('/')) + "\"...");
                            Directory.Delete(_songPath.Substring(0, _songPath.LastIndexOf('/')), false);
                        }

                        string docPath = Application.dataPath;
                        docPath = docPath.Substring(0, docPath.Length - 5);
                        docPath = docPath.Substring(0, docPath.LastIndexOf("/"));
                        string customSongsPath = docPath + "/CustomSongs/";

                        string hash = "";

                        foreach (string file in Directory.GetFiles(customSongsPath, "*.zip"))
                        {
                            if (CreateMD5FromFile(file, out hash))
                            {
                                if (hash == songHash)
                                {
                                    File.Delete(file);
                                    break;
                                }
                            }
                        }

                    }
                    else
                    {
                        Logger.Log("Deleting \"" + _songPath.Substring(_songPath.LastIndexOf('/')) + "\"...");
                        Directory.Delete(_songPath, true);
                        if (Directory.GetFileSystemEntries(_songPath.Substring(0, _songPath.LastIndexOf('/'))).Length == 0)
                        {
                            Logger.Log("Deleting empty folder \"" + _songPath.Substring(0, _songPath.LastIndexOf('/')) + "\"...");
                            Directory.Delete(_songPath.Substring(0, _songPath.LastIndexOf('/')), false);
                        }
                    }

                    SongListUITweaks.RemoveLevelFromPlaylists(levelId);

                    SongLoader.Instance.RemoveSongWithLevelID(levelId);
                    
                    _tweaks.SetSongListLevels(_tweaks.GetSortedLevels(SongListUITweaks.lastSortMode), nextLevelId);
                }
                _confirmDeleteState = Prompt.NotSelected;

                _deleting = false;
            }
            else
            {
                yield return null;
            }
            
        }

        IEnumerator PromptDeleteFolder(string dirName)
        {
            TextMeshProUGUI _deleteText = BeatSaberUI.CreateText(_songDetailViewController.rectTransform, String.Format("Delete folder \"{0}\"?", dirName.Substring(dirName.LastIndexOf('/')).Trim('/')), new Vector2(18f, -64f));

            Button _confirmDelete = BeatSaberUI.CreateUIButton(_songDetailViewController.rectTransform, "SettingsButton");

            BeatSaberUI.SetButtonText(_confirmDelete, "Yes");
            (_confirmDelete.transform as RectTransform).sizeDelta = new Vector2(15f, 10f);
            (_confirmDelete.transform as RectTransform).anchoredPosition = new Vector2(-23f, 6f);
            _confirmDelete.onClick.AddListener(delegate () { _confirmDeleteState = Prompt.Yes; });

            Button _discardDelete = BeatSaberUI.CreateUIButton(_songDetailViewController.rectTransform, "SettingsButton");

            BeatSaberUI.SetButtonText(_discardDelete, "No");
            (_discardDelete.transform as RectTransform).sizeDelta = new Vector2(15f, 10f);
            (_discardDelete.transform as RectTransform).anchoredPosition = new Vector2(-8f, 6f);
            _discardDelete.onClick.AddListener(delegate () { _confirmDeleteState = Prompt.No; });


            (_playButton.transform as RectTransform).anchoredPosition = new Vector2(2f, -10f);

            yield return new WaitUntil(delegate () { return (_confirmDeleteState == Prompt.Yes || _confirmDeleteState == Prompt.No); });

            (_playButton.transform as RectTransform).anchoredPosition = new Vector2(2f, 6f);

            Destroy(_deleteText.gameObject);
            Destroy(_confirmDelete.gameObject);
            Destroy(_discardDelete.gameObject);

        }

        public IEnumerator DownloadSongCoroutine(Song songInfo)
        {
            songInfo.songQueueState = SongQueueState.Downloading;
            
            downloadStarted?.Invoke(songInfo);

            UnityWebRequest www;
            bool timeout = false;
            float time = 0f;
            UnityWebRequestAsyncOperation asyncRequest;

            try
            {
                www = UnityWebRequest.Get(songInfo.downloadUrl);
                
                asyncRequest = www.SendWebRequest();
            }
            catch
            {
                songInfo.songQueueState = SongQueueState.Error;
                songInfo.downloadingProgress = 1f;

                yield break;
            }

            while ((!asyncRequest.isDone || songInfo.downloadingProgress != 1f) && songInfo.songQueueState != SongQueueState.Error)
            {
                yield return null;

                time += Time.deltaTime;

                if ((time >= 15f && asyncRequest.progress == 0f) || songInfo.songQueueState == SongQueueState.Error)
                {
                    www.Abort();
                    timeout = true;
                }

                songInfo.downloadingProgress = asyncRequest.progress;
            }


            if (www.isNetworkError || www.isHttpError || timeout || songInfo.songQueueState == SongQueueState.Error)
            {
                if (timeout)
                {
                    songInfo.songQueueState = SongQueueState.Error;
                    TextMeshProUGUI _errorText = BeatSaberUI.CreateText(_songDetailViewController.rectTransform, "Request timeout", new Vector2(18f, -64f));
                    Destroy(_errorText.gameObject, 2f);
                }
                else
                {
                    songInfo.songQueueState = SongQueueState.Error;
                    Logger.Error($"Downloading error: {www.error}");
                    TextMeshProUGUI _errorText = BeatSaberUI.CreateText(_songDetailViewController.rectTransform, www.error, new Vector2(18f, -64f));
                    Destroy(_errorText.gameObject, 2f);
                }

            }
            else
            {

                Logger.Log("Received response from BeatSaver.com...");

                string zipPath = "";
                string docPath = "";
                string customSongsPath = "";

                byte[] data = www.downloadHandler.data;

                try
                {

                    docPath = Application.dataPath;
                    docPath = docPath.Substring(0, docPath.Length - 5);
                    docPath = docPath.Substring(0, docPath.LastIndexOf("/"));
                    customSongsPath = docPath + "/CustomSongs/" + songInfo.id + "/";
                    zipPath = customSongsPath + songInfo.id + ".zip";
                    if (!Directory.Exists(customSongsPath))
                    {
                        Directory.CreateDirectory(customSongsPath);
                    }
                    File.WriteAllBytes(zipPath, data);
                    Logger.Log("Downloaded zip file!");
                }
                catch (Exception e)
                {
                    Logger.Exception("EXCEPTION: " + e);
                    songInfo.songQueueState = SongQueueState.Error;
                    yield break;
                }

                Logger.Log("Extracting...");

                try
                {
                    ZipFile.ExtractToDirectory(zipPath, customSongsPath);
                }
                catch (Exception e)
                {
                    Logger.Exception($"Can't extract ZIP! Exception: {e}");
                }

                songInfo.path = Directory.GetDirectories(customSongsPath).FirstOrDefault();

                if (string.IsNullOrEmpty(songInfo.path))
                {
                    songInfo.path = customSongsPath;
                }

                try
                {
                    File.Delete(zipPath);
                }
                catch (IOException e)
                {
                    Logger.Warning($"Can't delete zip! Exception: {e}");
                }

                songInfo.songQueueState = SongQueueState.Downloaded;

                Logger.Log("Downloaded!");

                downloadFinished?.Invoke(songInfo);
            }
        }

        private void CreateBeatSaverButton()
        {
            _beatSaverButton = BeatSaberUI.CreateUIButton(_mainMenuRectTransform, "QuitButton");

            try
            {
                (_beatSaverButton.transform as RectTransform).anchoredPosition = new Vector2(30f, 7f);
                (_beatSaverButton.transform as RectTransform).sizeDelta = new Vector2(28f, 10f);

                BeatSaberUI.SetButtonText(_beatSaverButton, "BeatSaver");
                
                _beatSaverButton.onClick.AddListener(delegate () {
                    
                    if (_beatSaverViewController == null)
                    {
                        _beatSaverViewController = BeatSaberUI.CreateViewController<BeatSaverNavigationController>();
                    }
                    _mainMenuViewController.PresentModalViewController(_beatSaverViewController, null, false);

                });

            }
            catch (Exception e)
            {
                Logger.Exception("Can't create button! Exception: " + e);
            }

        }

        public static bool CreateMD5FromFile(string path, out string hash)
        {
            hash = "";
            if (!File.Exists(path)) return false;
            using (MD5 md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);

                    StringBuilder sb = new StringBuilder();
                    foreach (byte hashByte in hashBytes)
                    {
                        sb.Append(hashByte.ToString("X2"));
                    }

                    hash = sb.ToString();
                    return true;
                }
            }
        }

        public static Sprite Base64ToSprite(string base64)
        {
            Texture2D tex = Base64ToTexture2D(base64);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), (Vector2.one / 2f));
        }

        public static Texture2D Base64ToTexture2D(string encodedData)
        {
            byte[] imageData = Convert.FromBase64String(encodedData);

            Texture2D texture = new Texture2D(0, 0, TextureFormat.ARGB32, false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Trilinear;
            texture.LoadImage(imageData);
            return texture;
        }
    }
}
