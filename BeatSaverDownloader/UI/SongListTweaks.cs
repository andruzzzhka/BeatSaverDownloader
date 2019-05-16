using CustomUI.BeatSaber;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BeatSaverDownloader.UI.ViewControllers;
using VRUI;
using SongLoaderPlugin.OverrideClasses;
using System.IO;
using BeatSaverDownloader.Misc;
using HMUI;
using BeatSaverDownloader.UI.FlowCoordinators;
using TMPro;
using Harmony;
using System.Reflection;
using CustomUI.Utilities;
using UnityEngine.Networking;
using SimpleJSON;

namespace BeatSaverDownloader.UI
{
    public enum SortMode { Default, Difficulty, Newest };

    public class SongListTweaks : MonoBehaviour
    {

        public bool initialized = false;

        public static SortMode lastSortMode
        {
            get
            {
                return _lastSortMode;
            }
            set
            {
                _lastSortMode = value;
                PluginConfig.lastSelectedSortMode = _lastSortMode;
                PluginConfig.SaveConfig();
            }
        }
        private static SortMode _lastSortMode;

        public static IBeatmapLevelPack lastPack
        {
            get {
                return _lastPack;
            }
            set {
                _lastPack = value;
                if (_lastPack != null)
                {
                    Plugin.log.Info($"Selected pack: {_lastPack.packName}");
                    PluginConfig.lastSelectedPack = _lastPack.packID;
                    PluginConfig.SaveConfig();
                }
            }
        }
        private static IBeatmapLevelPack _lastPack;

        private static SongListTweaks _instance = null;
        public static SongListTweaks Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = new GameObject("SongListTweaks").AddComponent<SongListTweaks>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        public FlowCoordinator freePlayFlowCoordinator;
        
        private MainFlowCoordinator _mainFlowCoordinator;
        private LevelPackLevelsViewController _levelListViewController;
        private StandardLevelDetailViewController _detailViewController;
        private LevelPacksViewController _levelPacksViewController;
        private SearchKeyboardViewController _searchViewController;
        private SimpleDialogPromptViewController _simpleDialog;

        private Button _buyPackButton;
        private TextMeshProUGUI _buyPackText;
        private Button.ButtonClickedEvent _originalBuyButtonEvent;
        private Button.ButtonClickedEvent _downloadButtonEvent;
        private bool _downloadingPlaylist;

        private DownloadQueueViewController _downloadQueueViewController;

        private Button _fastPageUpButton;
        private Button _fastPageDownButton;

        private Button _randomButton;
        private Button _searchButton;
        private Button _sortByButton;

        private Button _defButton;
        private Button _newButton;
        private Button _difficultyButton;

        private Button _favoriteButton;
        private Button _deleteButton;

        private TextMeshProUGUI _starStatText;
        private TextMeshProUGUI _upvoteStatText;
        private TextMeshProUGUI _downvoteStatText;

        public void OnLoad()
        {
            initialized = false;
            SetupTweaks();

            if (PluginConfig.disableSongListTweaks)
                return;

            if (SongLoaderPlugin.SongLoader.AreSongsLoaded)
            {
                AddDefaultPlaylists();
            }
            else
            {
                SongLoaderPlugin.SongLoader.SongsLoadedEvent += SongLoader_SongsLoadedEvent;
            }
        }

        private void SongLoader_SongsLoadedEvent(SongLoaderPlugin.SongLoader arg1, List<CustomLevel> arg2)
        {
            SongLoaderPlugin.SongLoader.SongsLoadedEvent -= SongLoader_SongsLoadedEvent;
            AddDefaultPlaylists();
        }

        private void SetupTweaks()
        {
            _mainFlowCoordinator = FindObjectOfType<MainFlowCoordinator>();
            _mainFlowCoordinator.GetPrivateField<MainMenuViewController>("_mainMenuViewController").didFinishEvent += MainMenuViewController_didFinishEvent;

            RectTransform viewControllersContainer = FindObjectsOfType<RectTransform>().First(x => x.name == "ViewControllers");

            if (initialized || PluginConfig.disableSongListTweaks) return;

            Plugin.log.Info("Setting up song list tweaks...");

            try
            {
                var harmony = HarmonyInstance.Create("BeatSaverDownloaderHarmonyInstance");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                Plugin.log.Info("Unable to patch level list! Exception: " + e);
            }

            _simpleDialog = ReflectionUtil.GetPrivateField<SimpleDialogPromptViewController>(_mainFlowCoordinator, "_simpleDialogPromptViewController");
            _simpleDialog = Instantiate(_simpleDialog.gameObject, _simpleDialog.transform.parent).GetComponent<SimpleDialogPromptViewController>();
            
            _levelListViewController = viewControllersContainer.GetComponentInChildren<LevelPackLevelsViewController>(true);
            _levelListViewController.didSelectLevelEvent += _levelListViewController_didSelectLevelEvent;
            _levelListViewController.didSelectPackEvent += _levelListViewController_didSelectPackEvent;

            _levelPacksViewController = viewControllersContainer.GetComponentInChildren<LevelPacksViewController>(true);
            _levelPacksViewController.didSelectPackEvent += _levelPacksViewController_didSelectPackEvent;

            TableView _songSelectionTableView = _levelListViewController.GetComponentsInChildren<TableView>(true).First();
            RectTransform _tableViewRectTransform = _levelListViewController.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "LevelsTableView");
                
            _tableViewRectTransform.sizeDelta = new Vector2(0f, -20.5f);
            _tableViewRectTransform.anchoredPosition = new Vector2(0f, -2.5f);
                
            Button _pageUp = _tableViewRectTransform.GetComponentsInChildren<Button>(true).First(x => x.name == "PageUpButton");
            (_pageUp.transform as RectTransform).anchoredPosition = new Vector2(0f, -1.75f);

            Button _pageDown = _tableViewRectTransform.GetComponentsInChildren<Button>(true).First(x => x.name == "PageDownButton");
            (_pageDown.transform as RectTransform).anchoredPosition = new Vector2(0f, 1f);
                
            _fastPageUpButton = Instantiate(_pageUp, _tableViewRectTransform, false);
            (_fastPageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
            (_fastPageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
            (_fastPageUpButton.transform as RectTransform).anchoredPosition = new Vector2(-26f, 0.25f);
            (_fastPageUpButton.transform as RectTransform).sizeDelta = new Vector2(8f, 6f);
            _fastPageUpButton.GetComponentsInChildren<RectTransform>().First(x => x.name == "BG").sizeDelta = new Vector2(8f, 6f);
            _fastPageUpButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Arrow").sprite = Sprites.DoubleArrow;
            _fastPageUpButton.onClick.AddListener(delegate ()
            {
                FastScrollUp(_songSelectionTableView, PluginConfig.fastScrollSpeed);
            });
            
            _fastPageDownButton = Instantiate(_pageDown, _tableViewRectTransform, false);
            (_fastPageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
            (_fastPageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
            (_fastPageDownButton.transform as RectTransform).anchoredPosition = new Vector2(-26f, -1f);
            (_fastPageDownButton.transform as RectTransform).sizeDelta = new Vector2(8f, 6f);
            _fastPageDownButton.GetComponentsInChildren<RectTransform>().First(x => x.name == "BG").sizeDelta = new Vector2(8f, 6f);
            _fastPageDownButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Arrow").sprite = Sprites.DoubleArrow;
            _fastPageDownButton.onClick.AddListener(delegate ()
            {
                FastScrollDown(_songSelectionTableView, PluginConfig.fastScrollSpeed);
            });

            _randomButton = Instantiate(viewControllersContainer.GetComponentsInChildren<Button>(true).First(x => x.name == "PracticeButton"), _levelListViewController.rectTransform, false);
            _randomButton.onClick = new Button.ButtonClickedEvent();
            _randomButton.onClick.AddListener(() =>
            {
                int randomRow = UnityEngine.Random.Range(0, _songSelectionTableView.dataSource.NumberOfCells());
                _songSelectionTableView.ScrollToCellWithIdx(randomRow, TableView.ScrollPositionType.Beginning, false);
                _songSelectionTableView.SelectCellWithIdx(randomRow, true);
            });
            _randomButton.name = "CustomUIButton";

            (_randomButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
            (_randomButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
            (_randomButton.transform as RectTransform).anchoredPosition = new Vector2(24f, 36.5f);
            (_randomButton.transform as RectTransform).sizeDelta = new Vector2(12f, 6f);

            _randomButton.SetButtonText("");
            _randomButton.SetButtonIcon(Sprites.RandomIcon);
            _randomButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Stroke").sprite = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "RoundRectSmallStroke");
            
            var _randomIconLayout = _randomButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content");
            _randomIconLayout.padding = new RectOffset(0, 0, 0, 0);

            _searchButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(-18f, 36.5f), new Vector2(24f, 6f), SearchPressed, "Search");
            _searchButton.SetButtonTextSize(3f);
            _searchButton.ToggleWordWrapping(false);
            
            _sortByButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(6f, 36.5f), new Vector2(24f, 6f), () =>
            {
                SelectTopButtons(TopButtonsState.SortBy);
            }, "Sort By");
            _sortByButton.SetButtonTextSize(3f);
            _sortByButton.ToggleWordWrapping(false);

            _defButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(-20f, 36.5f), new Vector2(20f, 6f), () =>
            {
                SelectTopButtons(TopButtonsState.Select);
                SetLevels(SortMode.Default, "");
            },
                "Default");

            _defButton.SetButtonTextSize(3f);
            _defButton.ToggleWordWrapping(false);
            _defButton.gameObject.SetActive(false);

            _newButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(0f, 36.5f), new Vector2(20f, 6f), () =>
            {
                SelectTopButtons(TopButtonsState.Select);
                SetLevels(SortMode.Newest, "");
            }, "Newest");

            _newButton.SetButtonTextSize(3f);
            _newButton.ToggleWordWrapping(false);
            _newButton.gameObject.SetActive(false);


            _difficultyButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(20f, 36.5f), new Vector2(20f, 6f), () =>
            {
                SelectTopButtons(TopButtonsState.Select);
                SetLevels(SortMode.Difficulty, "");
            }, "Difficulty");
            
            _difficultyButton.SetButtonTextSize(3f);
            _difficultyButton.ToggleWordWrapping(false);
            _difficultyButton.gameObject.SetActive(false);

            var packDetailViewController = viewControllersContainer.GetComponentsInChildren<LevelPackDetailViewController>(true).First(x => x.name == "LevelPackDetailViewController");
            _buyPackText = packDetailViewController.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(x => x.name.EndsWith("InfoText"));
            _buyPackButton = packDetailViewController.GetComponentsInChildren<Button>(true).FirstOrDefault(x => x.name == "BuyPackButton");
            _originalBuyButtonEvent = _buyPackButton.onClick;

            _detailViewController = viewControllersContainer.GetComponentsInChildren<StandardLevelDetailViewController>(true).First(x => x.name == "LevelDetailViewController");
            _detailViewController.didChangeDifficultyBeatmapEvent += _difficultyViewController_didSelectDifficultyEvent;
            
            RectTransform buttonsRect = _detailViewController.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "PlayButtons");
            
            _favoriteButton = Instantiate(viewControllersContainer.GetComponentsInChildren<Button>(true).First(x => x.name == "PracticeButton"), buttonsRect, false);
            _favoriteButton.onClick = new Button.ButtonClickedEvent();
            _favoriteButton.onClick.AddListener(() =>
            {
                if (PluginConfig.favoriteSongs.Any(x => x.Contains(_detailViewController.selectedDifficultyBeatmap.level.levelID)))
                {
                    PluginConfig.favoriteSongs.Remove(_detailViewController.selectedDifficultyBeatmap.level.levelID);
                    PluginConfig.SaveConfig();
                    _favoriteButton.SetButtonIcon(Sprites.AddToFavorites);
                    PlaylistsCollection.RemoveLevelFromPlaylist(PlaylistsCollection.loadedPlaylists.First(x => x.playlistTitle == "Your favorite songs"), _detailViewController.selectedDifficultyBeatmap.level.levelID);
                }
                else
                {
                    PluginConfig.favoriteSongs.Add(_detailViewController.selectedDifficultyBeatmap.level.levelID);
                    PluginConfig.SaveConfig();
                    _favoriteButton.SetButtonIcon(Sprites.RemoveFromFavorites);
                    PlaylistsCollection.AddSongToPlaylist(PlaylistsCollection.loadedPlaylists.First(x => x.playlistTitle == "Your favorite songs"), new PlaylistSong() { levelId = _detailViewController.selectedDifficultyBeatmap.level.levelID, songName = _detailViewController.selectedDifficultyBeatmap.level.songName, level = SongDownloader.GetLevel(_detailViewController.selectedDifficultyBeatmap.level.levelID) });
                }
            });
            _favoriteButton.name = "CustomUIButton";
            _favoriteButton.SetButtonIcon(Sprites.AddToFavorites);
            (_favoriteButton.transform as RectTransform).sizeDelta = new Vector2(12f, 8.8f);
            var _favoriteIconLayout = _favoriteButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content");
            _favoriteIconLayout.padding = new RectOffset(3, 3, 0, 0);
            _favoriteButton.transform.SetAsFirstSibling();

            Button practiceButton = buttonsRect.GetComponentsInChildren<Button>().First(x => x.name == "PracticeButton");
            (practiceButton.transform as RectTransform).sizeDelta = new Vector2(12f, 8.8f);
            var _practiceIconLayout = practiceButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content");
            _practiceIconLayout.padding = new RectOffset(3, 3, 0, 0);

            _deleteButton = Instantiate(viewControllersContainer.GetComponentsInChildren<Button>(true).First(x => x.name == "PracticeButton"), buttonsRect, false);
            _deleteButton.onClick = new Button.ButtonClickedEvent();
            _deleteButton.onClick.AddListener(DeletePressed);
            _deleteButton.name = "CustomUIButton";
            _deleteButton.SetButtonIcon(Sprites.DeleteIcon);
            _deleteButton.interactable = !PluginConfig.disableDeleteButton;
            (_deleteButton.transform as RectTransform).sizeDelta = new Vector2(8.8f, 8.8f);
            _deleteButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Stroke").sprite = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "RoundRectSmallStroke");

            var _deleteIconLayout = _deleteButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content");
            _deleteIconLayout.padding = new RectOffset(0, 0, 1, 1);

            _deleteButton.transform.SetAsLastSibling();

            //based on https://github.com/halsafar/BeatSaberSongBrowser/blob/master/SongBrowserPlugin/UI/Browser/SongBrowserUI.cs#L416
            var statsPanel = _detailViewController.GetPrivateField<StandardLevelDetailView>("_standardLevelDetailView").GetPrivateField<LevelParamsPanel>("_levelParamsPanel");
            var statTransforms = statsPanel.GetComponentsInChildren<RectTransform>();
            var valueTexts = statsPanel.GetComponentsInChildren<TextMeshProUGUI>().Where(x => x.name == "ValueText").ToList();

            RectTransform panelRect = (statsPanel.transform as RectTransform);
            panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x * 1.2f, panelRect.sizeDelta.y * 1.2f);

            for (int i = 0; i < statTransforms.Length; i++)
            {
                var r = statTransforms[i];
                if (r.name == "Separator")
                {
                    continue;
                }
                r.sizeDelta = new Vector2(r.sizeDelta.x * 0.75f, r.sizeDelta.y * 0.75f);
            }

            for (int i = 0; i < valueTexts.Count; i++)
            {
                var text = valueTexts[i];
                text.fontSize = 3.25f;
            }

            var _starStatTransform = Instantiate(statTransforms[1], statsPanel.transform, false);
            _starStatText = _starStatTransform.GetComponentInChildren<TextMeshProUGUI>(true);
            _starStatTransform.GetComponentInChildren<UnityEngine.UI.Image>(true).sprite = Sprites.StarFull;
            _starStatText.text = "--";

            var _upvoteStatTransform = Instantiate(statTransforms[1], statsPanel.transform, false);
            _upvoteStatText = _upvoteStatTransform.GetComponentInChildren<TextMeshProUGUI>(true);
            _upvoteStatTransform.GetComponentInChildren<UnityEngine.UI.Image>(true).sprite = Sprites.ThumbUp;
            _upvoteStatText.text = "--";

            var _downvoteStatTransform = Instantiate(statTransforms[1], statsPanel.transform, false);
            _downvoteStatText = _downvoteStatTransform.GetComponentInChildren<TextMeshProUGUI>(true);
            _downvoteStatTransform.GetComponentInChildren<UnityEngine.UI.Image>(true).sprite = Sprites.ThumbDown;
            _downvoteStatText.text = "--";
            
            ResultsViewController _standardLevelResultsViewController = viewControllersContainer.GetComponentsInChildren<ResultsViewController>(true).First(x => x.name == "StandardLevelResultsViewController");
            _standardLevelResultsViewController.continueButtonPressedEvent += _standardLevelResultsViewController_continueButtonPressedEvent;
            
            initialized = true;
        }

        public void AddDefaultPlaylists()
        {
            try
            {
                Plugin.log.Info("Creating default playlist...");

                var levels = SongLoaderPlugin.SongLoader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.SelectMany(x => x.beatmapLevelCollection.beatmapLevels).ToList();

                Playlist _favPlaylist = new Playlist() { playlistTitle = "Your favorite songs", playlistAuthor = "", image = Sprites.SpriteToBase64(Sprites.BeastSaberLogo), icon = Sprites.BeastSaberLogo, fileLoc = "" };
                _favPlaylist.songs = new List<PlaylistSong>();
                _favPlaylist.songs.AddRange(levels.Where(x =>  PluginConfig.favoriteSongs.Contains(x.levelID)).Select(x =>new PlaylistSong() { songName = $"{x.songName} {x.songSubName}", level = x as BeatmapLevelSO, oneSaber = x.beatmapCharacteristics.Any(y => y.characteristicName == "One Saber"), path = "", key = "", levelId = x.levelID, hash = CustomHelpers.CheckHex(x.levelID.Substring(0, Math.Min(32, x.levelID.Length))) }));
                Plugin.log.Info($"Created \"{_favPlaylist.playlistTitle}\" playlist with {_favPlaylist.songs.Count} songs!");

                if (PlaylistsCollection.loadedPlaylists.Any(x => x.playlistTitle == "Your favorite songs"))
                {
                    PlaylistsCollection.loadedPlaylists.RemoveAll(x => x.playlistTitle == "Your favorite songs");
                }

                PlaylistsCollection.loadedPlaylists.Insert(0, _favPlaylist);

                _favPlaylist.SavePlaylist("Playlists\\favorites.json");
            }catch(Exception e)
            {
                Plugin.log.Critical($"Unable to create default playlist! Exception: {e}");
            }
            UpdateLevelPacks();
        }

        public void UpdateLevelPacks()
        {

            CustomBeatmapLevelPackCollectionSO newCollection = SongLoaderPlugin.SongLoader.CustomBeatmapLevelPackCollectionSO;

            List<BeatmapLevelPackSO> _customBeatmapLevelPacks = newCollection.GetPrivateField<List<BeatmapLevelPackSO>>("_customBeatmapLevelPacks");
            List<IBeatmapLevelPack> _allBeatmapLevelPacks = newCollection.GetPrivateField<IBeatmapLevelPack[]>("_allBeatmapLevelPacks").ToList();

            _customBeatmapLevelPacks.RemoveAll(x => x.packID.StartsWith("Playlist_"));
            _allBeatmapLevelPacks.RemoveAll(x => x.packID.StartsWith("Playlist_"));

            newCollection.SetPrivateField("_customBeatmapLevelPacks", _customBeatmapLevelPacks);
            newCollection.SetPrivateField("_allBeatmapLevelPacks", _allBeatmapLevelPacks.ToArray());
            
            foreach (var playlist in PlaylistsCollection.loadedPlaylists)
            {
                PlaylistLevelPackSO levelPack = ScriptableObject.CreateInstance<PlaylistLevelPackSO>();
                levelPack.playlist = playlist;

                newCollection.AddLevelPack(levelPack);
            }

            Plugin.log.Info("Updating level packs... New level packs count: "+newCollection.beatmapLevelPacks.Length);
      //      SongLoaderPlugin.SongLoader.Instance.InvokeMethod("ReloadHashes");
        }

        private void MainMenuViewController_didFinishEvent(MainMenuViewController sender, MainMenuViewController.MenuButton result)
        {
            lastPack = null;
            if (result == MainMenuViewController.MenuButton.SoloFreePlay)
            {
                freePlayFlowCoordinator = FindObjectOfType<SoloFreePlayFlowCoordinator>();
                (freePlayFlowCoordinator as SoloFreePlayFlowCoordinator).didFinishEvent += soloFreePlayFlowCoordinator_didFinishEvent;
                SongDownloader.Instance.songDownloaded -= SongDownloader_songDownloaded;
                SongDownloader.Instance.songDownloaded += SongDownloader_songDownloaded;

                if (PluginConfig.rememberLastPackAndSong)
                {
                    StartCoroutine(SelectLastPackAndSong());
                }
                else
                {
                    lastPack = SongLoaderPlugin.SongLoader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks[_levelPacksViewController.GetPrivateField<int>("_selectedPackNum")];
                }
            }
            else if (result == MainMenuViewController.MenuButton.Party)
            {
                freePlayFlowCoordinator = FindObjectOfType<PartyFreePlayFlowCoordinator>();
                (freePlayFlowCoordinator as PartyFreePlayFlowCoordinator).didFinishEvent += partyFreePlayFlowCoordinator_didFinishEvent;
                SongDownloader.Instance.songDownloaded -= SongDownloader_songDownloaded;
                SongDownloader.Instance.songDownloaded += SongDownloader_songDownloaded;

                if (PluginConfig.rememberLastPackAndSong)
                {
                    StartCoroutine(SelectLastPackAndSong());
                }
                else
                {
                    lastPack = SongLoaderPlugin.SongLoader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks[_levelPacksViewController.GetPrivateField<int>("_selectedPackNum")];
                }
            }
            else
            {
                freePlayFlowCoordinator = null;
            }
        }

        private IEnumerator SelectLastPackAndSong()
        {
            yield return null;
            yield return null;

            lastSortMode = PluginConfig.lastSelectedSortMode;

            if (!string.IsNullOrEmpty(PluginConfig.lastSelectedPack) && SongLoaderPlugin.SongLoader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.Any(x => x.packID == PluginConfig.lastSelectedPack))
            {
                int packIndex = Array.FindIndex(SongLoaderPlugin.SongLoader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks, x => x.packID == PluginConfig.lastSelectedPack);

                if (packIndex < 0)
                {
                    Plugin.log.Warn($"Unable to find last selected pack with ID \"{PluginConfig.lastSelectedPack}\"");
                    lastPack = SongLoaderPlugin.SongLoader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks[_levelPacksViewController.GetPrivateField<int>("_selectedPackNum")];
                    yield break;
                }

                lastPack = SongLoaderPlugin.SongLoader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks[packIndex];

                SetLevels(lastSortMode, "");
                
                yield return null;

                if (!string.IsNullOrEmpty(PluginConfig.lastSelectedSong) && lastPack.beatmapLevelCollection.beatmapLevels.Any(x => x.levelID == PluginConfig.lastSelectedSong))
                {

                    var levelsTableView = _levelListViewController.GetPrivateField<LevelPackLevelsTableView>("_levelPackLevelsTableView");

                    int songIndex = Array.FindIndex(_levelListViewController.GetPrivateField<IBeatmapLevelPack>("_levelPack").beatmapLevelCollection.beatmapLevels, x => x.levelID == PluginConfig.lastSelectedSong);

                    if (songIndex < 0)
                    {
                        Plugin.log.Warn($"Unable to find last selected song with ID \"{PluginConfig.lastSelectedSong}\"");
                        yield break;
                    }

                    if (levelsTableView.GetPrivateField<bool>("_showLevelPackHeader"))
                    {
                        songIndex++;
                    }

                    var tableView = levelsTableView.GetPrivateField<TableView>("_tableView");
                    tableView.ScrollToCellWithIdx(songIndex, TableView.ScrollPositionType.Beginning, false);
                    tableView.SelectCellWithIdx(songIndex, true);
                }
            }
            else
            {
                lastPack = SongLoaderPlugin.SongLoader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks[_levelPacksViewController.GetPrivateField<int>("_selectedPackNum")];
            }
        }
        
        public void SelectTopButtons(TopButtonsState _newState)
        {
            switch (_newState)
            {
                case TopButtonsState.Select:
                    {
                        _sortByButton.gameObject.SetActive(true);
                        _searchButton.gameObject.SetActive(true);
                        _randomButton.gameObject.SetActive(true);

                        _defButton.gameObject.SetActive(false);
                        _newButton.gameObject.SetActive(false);
                        _difficultyButton.gameObject.SetActive(false);
                    }; break;
                case TopButtonsState.SortBy:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);
                        _randomButton.gameObject.SetActive(false);

                        _defButton.gameObject.SetActive(true);
                        _newButton.gameObject.SetActive(true);
                        _difficultyButton.gameObject.SetActive(true);
                    }; break;
                case TopButtonsState.Search:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);
                        _randomButton.gameObject.SetActive(false);

                        _defButton.gameObject.SetActive(false);
                        _newButton.gameObject.SetActive(false);
                        _difficultyButton.gameObject.SetActive(false);

                    }; break;
            }
        }

        private void _difficultyViewController_didSelectDifficultyEvent(StandardLevelDetailViewController sender, IDifficultyBeatmap beatmap)
        {
            _favoriteButton.SetButtonIcon(PluginConfig.favoriteSongs.Any(x => x.Contains(beatmap.level.levelID)) ? Sprites.RemoveFromFavorites : Sprites.AddToFavorites);
            _favoriteButton.interactable = !(beatmap.level is PreviewBeatmapLevelSO);

            _deleteButton.interactable = !PluginConfig.disableDeleteButton && (beatmap.level.levelID.Length >= 32);

            if (beatmap.level.levelID.Length >= 32)
            {
                ScrappedSong song = ScrappedData.Songs.FirstOrDefault(x => x.Hash == beatmap.level.levelID.Substring(0, 32));
                if (song != null)
                {
                    _upvoteStatText.text = song.Upvotes.ToString();
                    _downvoteStatText.text = song.Downvotes.ToString();
                    if (song.Diffs.Any())
                        _starStatText.text = song.Diffs.Max(x => x.Stars).ToString();
                    else
                        _starStatText.text = "--";
                }
                else
                {
                    _starStatText.text = "--";
                    _upvoteStatText.text = "--";
                    _starStatText.text = "--";
                }
            }
            else
            {
                _starStatText.text = "--";
                _upvoteStatText.text = "--";
                _downvoteStatText.text = "--";
            }
        }
        
        private void _levelPacksViewController_didSelectPackEvent(LevelPacksViewController arg1, IBeatmapLevelPack arg2)
        {
            lastPack = arg2;

            if (arg2 is PlaylistLevelPackSO)
            {
                StartCoroutine(ShowDownloadQueue(arg2));
            }
            else
            {
                StartCoroutine(HideDownloadQueue());
            }
        }

        private void _levelListViewController_didSelectPackEvent(LevelPackLevelsViewController arg1, IBeatmapLevelPack arg2)
        {
            lastPack = arg2;
            Plugin.log.Info("Selected pack header! IsPlaylist="+(arg2 is PlaylistLevelPackSO));

            if (arg2 is PlaylistLevelPackSO)
            {
                StartCoroutine(ShowDownloadQueue(arg2));
            }
            else
            {
                StartCoroutine(HideDownloadQueue());
            }
        }
        
        private void partyFreePlayFlowCoordinator_didFinishEvent(PartyFreePlayFlowCoordinator obj)
        {
            (freePlayFlowCoordinator as PartyFreePlayFlowCoordinator).didFinishEvent -= partyFreePlayFlowCoordinator_didFinishEvent;
            SongDownloader.Instance.songDownloaded -= SongDownloader_songDownloaded;
            StartCoroutine(HideDownloadQueue());
            lastPack = null;
        }

        private void soloFreePlayFlowCoordinator_didFinishEvent(SoloFreePlayFlowCoordinator obj)
        {
            (freePlayFlowCoordinator as SoloFreePlayFlowCoordinator).didFinishEvent -= soloFreePlayFlowCoordinator_didFinishEvent;
            SongDownloader.Instance.songDownloaded -= SongDownloader_songDownloaded;
            StartCoroutine(HideDownloadQueue());
            lastPack = null;
        }

        private IEnumerator HideDownloadQueue()
        {
            _buyPackButton.interactable = true;
            _buyPackButton.SetButtonText("BUY MUSIC PACK");
            _buyPackButton.onClick = _originalBuyButtonEvent;
            _buyPackText.gameObject.SetActive(true);

            yield return null;

            if (_downloadQueueViewController != null)
            {
                _downloadingPlaylist = false;
                _downloadQueueViewController.AbortDownloads();
            }

            yield return null;
            yield return null;
        }

        private IEnumerator ShowDownloadQueue(IBeatmapLevelPack pack)
        {
            if (_downloadButtonEvent == null)
                _downloadButtonEvent = new Button.ButtonClickedEvent();

            _downloadButtonEvent.RemoveAllListeners();
            _downloadButtonEvent.AddListener(() =>
            {
                StartCoroutine(DownloadPlaylist((pack as PlaylistLevelPackSO).playlist));
            });

            _buyPackButton.interactable = ((pack as PlaylistLevelPackSO).playlist.songs.Count > pack.beatmapLevelCollection.beatmapLevels.Length);
            _buyPackButton.SetButtonText("DOWNLOAD");
            _buyPackButton.onClick = _downloadButtonEvent;
            _buyPackText.gameObject.SetActive(false);

            if (_downloadQueueViewController == null)
                _downloadQueueViewController = BeatSaberUI.CreateViewController<DownloadQueueViewController>();

            yield return null;

            freePlayFlowCoordinator.InvokePrivateMethod("SetRightScreenViewController", new object[] { _downloadQueueViewController, false });

            yield return null;
            yield return null;

            if (_downloadQueueViewController != null)
            {
                _downloadingPlaylist = false;
                _downloadQueueViewController.AbortDownloads();
            }
        }

        private void _levelListViewController_didSelectLevelEvent(LevelPackLevelsViewController sender, IPreviewBeatmapLevel beatmap)
        {
            PluginConfig.lastSelectedSong = beatmap.levelID;
            PluginConfig.SaveConfig();

            _favoriteButton.SetButtonIcon(PluginConfig.favoriteSongs.Any(x => x.Contains(beatmap.levelID)) ? Sprites.RemoveFromFavorites : Sprites.AddToFavorites);
            _favoriteButton.interactable = !(beatmap is PreviewBeatmapLevelSO);

            _deleteButton.interactable = !PluginConfig.disableDeleteButton && (beatmap.levelID.Length >= 32);

            if (beatmap.levelID.Length >= 32)
            {
                ScrappedSong song = ScrappedData.Songs.FirstOrDefault(x => x.Hash == beatmap.levelID.Substring(0, 32));
                if (song != null)
                {
                    _upvoteStatText.text = song.Upvotes.ToString();
                    _downvoteStatText.text = song.Downvotes.ToString();
                    if (song.Diffs.Any())
                        _starStatText.text = song.Diffs.Max(x => x.Stars).ToString();
                    else
                        _starStatText.text = "--";
                }
                else
                {
                    _starStatText.text = "--";
                    _upvoteStatText.text = "--";
                    _downvoteStatText.text = "--";
                }
            }
            else
            {
                _starStatText.text = "--";
                _upvoteStatText.text = "--";
                _downvoteStatText.text = "--";
            }

            StartCoroutine(HideDownloadQueue());
        }

        private void DeletePressed()
        {
            IBeatmapLevel level = _detailViewController.selectedDifficultyBeatmap.level;
            _simpleDialog.Init("Delete song", $"Do you really want to delete \"{ level.songName} {level.songSubName}\"?", "Delete", "Cancel", 
                (selectedButton) => 
                {
                    freePlayFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _simpleDialog, null, false });
                    if (selectedButton == 0)
                    {
                        try
                        {
                            var levelsTableView = _levelListViewController.GetPrivateField<LevelPackLevelsTableView>("_levelPackLevelsTableView");

                            List<IPreviewBeatmapLevel> levels = levelsTableView.GetPrivateField<IBeatmapLevelPack>("_pack").beatmapLevelCollection.beatmapLevels.ToList();
                            int selectedIndex = levels.FindIndex(x => x.levelID == _detailViewController.selectedDifficultyBeatmap.level.levelID);

                            SongDownloader.Instance.DeleteSong(new Song(SongLoaderPlugin.SongLoader.CustomLevels.First(x => x.levelID == _detailViewController.selectedDifficultyBeatmap.level.levelID)));
                            
                            if (selectedIndex > -1)
                            {
                                int removedLevels = levels.RemoveAll(x => x.levelID == _detailViewController.selectedDifficultyBeatmap.level.levelID);
                                Plugin.log.Info("Removed " + removedLevels + " level(s) from song list!");
                                
                                _levelListViewController.SetData(CustomHelpers.GetLevelPackWithLevels(levels.Cast<BeatmapLevelSO>().ToArray(), lastPack?.packName ?? "Custom Songs", lastPack?.coverImage));
                                TableView listTableView = levelsTableView.GetPrivateField<TableView>("_tableView");
                                listTableView.ScrollToCellWithIdx(selectedIndex, TableView.ScrollPositionType.Beginning, false);
                                levelsTableView.SetPrivateField("_selectedRow", selectedIndex);
                                listTableView.SelectCellWithIdx(selectedIndex, true);
                            }
                        }
                        catch (Exception e)
                        {
                            Plugin.log.Error("Unable to delete song! Exception: " + e);
                        }
                    }
                });
            freePlayFlowCoordinator.InvokePrivateMethod("PresentViewController", new object[] { _simpleDialog, null, false });
        }

        private void SearchPressed()
        {
            if (_searchViewController == null)
            {
                _searchViewController = BeatSaberUI.CreateViewController<SearchKeyboardViewController>();
                _searchViewController.backButtonPressed += _searchViewController_backButtonPressed;
                _searchViewController.searchButtonPressed += _searchViewController_searchButtonPressed;
            }

            freePlayFlowCoordinator.InvokePrivateMethod("PresentViewController", new object[] { _searchViewController, null, false });
        }

        private void _searchViewController_searchButtonPressed(string request)
        {
            freePlayFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _searchViewController, null, false });
            SetLevels(SortMode.Default, request);
        }

        private void _searchViewController_backButtonPressed()
        {
            freePlayFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _searchViewController, null, false });
        }

        public void SetLevels(SortMode sortMode, string searchRequest)
        {
            lastSortMode = sortMode;

            BeatmapLevelSO[] levels = null;
            if (lastPack != null)
            {
                levels = lastPack.beatmapLevelCollection.beatmapLevels.Cast<BeatmapLevelSO>().ToArray();
            }
            else
            {
                levels = SongLoaderPlugin.SongLoader.CustomBeatmapLevelPackSO.beatmapLevelCollection.beatmapLevels.Cast<BeatmapLevelSO>().ToArray();
            }

            if (string.IsNullOrEmpty(searchRequest))
            {
                switch (sortMode)
                {
                    case SortMode.Newest: { levels = SortLevelsByCreationTime(levels); }; break;
                    case SortMode.Difficulty:
                        {
                            levels = levels.AsParallel().OrderBy(x => { int index = ScrappedData.Songs.FindIndex(y => x.levelID.StartsWith(y.Hash)); return (index == -1 ? (x.levelID.Length < 32 ? int.MaxValue : int.MaxValue - 1) : index); }).ToArray();
                        }; break;
                }
            }
            else
            {
                levels = levels.Where(x => ($"{x.songName} {x.songSubName} {x.levelAuthorName} {x.songAuthorName}".ToLower().Contains(searchRequest))).ToArray();
            }

            _levelListViewController.SetData(CustomHelpers.GetLevelPackWithLevels(levels, lastPack?.packName ?? "Custom Songs" ,lastPack?.coverImage));
            PopDifficultyAndDetails();
        }

        public BeatmapLevelSO[] SortLevelsByCreationTime(BeatmapLevelSO[] levels)
        {
            DirectoryInfo customSongsFolder = new DirectoryInfo(Environment.CurrentDirectory.Replace('\\', '/') + "/CustomSongs/");

            List<string> sortedFolders = customSongsFolder.GetDirectories().OrderByDescending(x => x.CreationTime.Ticks).Select(x => x.FullName.Replace('\\', '/')).ToList();

            List<string> sortedLevelIDs = new List<string>();

            foreach (string path in sortedFolders)
            {
                CustomLevel song = SongLoaderPlugin.SongLoader.CustomLevels.FirstOrDefault(x => x.customSongInfo.path.StartsWith(path));
                if (song != null)
                {
                    sortedLevelIDs.Add(song.levelID);
                }
            }

            List<BeatmapLevelSO> notSorted = new List<BeatmapLevelSO>(levels);

            List<BeatmapLevelSO> sortedLevels = new List<BeatmapLevelSO>();

            foreach (string levelId in sortedLevelIDs)
            {
                BeatmapLevelSO data = notSorted.FirstOrDefault(x => x.levelID == levelId);
                if (data != null)
                {
                    sortedLevels.Add(data);
                }
            }

            sortedLevels.AddRange(notSorted.Except(sortedLevels));

            return sortedLevels.ToArray();
        }

        private void PopDifficultyAndDetails()
        {
            bool isSolo = (freePlayFlowCoordinator is SoloFreePlayFlowCoordinator);

            if (isSolo)
            {
                SoloFreePlayFlowCoordinator soloCoordinator = freePlayFlowCoordinator as SoloFreePlayFlowCoordinator;
                int controllers = 0;
                if (soloCoordinator.GetPrivateField<StandardLevelDetailViewController>("_levelDetailViewController").isInViewControllerHierarchy)
                {
                    controllers++;
                }
                if (controllers > 0)
                {
                    soloCoordinator.InvokePrivateMethod("PopViewControllersFromNavigationController", new object[] { soloCoordinator.GetPrivateField<DismissableNavigationController>("_navigationController"), controllers, null, false });
                }
            }
            else
            {
                PartyFreePlayFlowCoordinator partyCoordinator = freePlayFlowCoordinator as PartyFreePlayFlowCoordinator;
                int controllers = 0;
                if (partyCoordinator.GetPrivateField<StandardLevelDetailViewController>("_levelDetailViewController").isInViewControllerHierarchy)
                {
                    controllers++;
                }
                if (controllers > 0)
                {
                    partyCoordinator.InvokePrivateMethod("PopViewControllersFromNavigationController", new object[] { partyCoordinator.GetPrivateField<DismissableNavigationController>("_navigationController"), controllers, null, false });
                }
            }
        }

        private void _standardLevelResultsViewController_continueButtonPressedEvent(ResultsViewController sender)
        {
            try
            {
                TableView _levelListTableView = _levelListViewController.GetComponentInChildren<TableView>();

                _levelListTableView.RefreshTable();
            }catch(Exception e)
            {
                Plugin.log.Warn("Unable to refresh song list! Exception: "+e);
            }
        }

        private void FastScrollUp(TableView tableView, int pages)
        {
            float targetPosition = tableView.GetProperty<float>("position") - (Mathf.Max(1f, tableView.GetNumberOfVisibleCells() - 1f) * tableView.GetPrivateField<float>("_cellSize") * pages);
            if (targetPosition < 0f)
            {
                targetPosition = 0f;
            }

            tableView.SetPrivateField("_targetPosition", targetPosition);

            tableView.enabled = true;
            tableView.RefreshScrollButtons();
        }

        private void FastScrollDown(TableView tableView, int pages)
        {
            float num = (tableView.GetPrivateField<TableView.TableType>("_tableType") != TableView.TableType.Vertical) ? tableView.GetPrivateField<RectTransform>("_scrollRectTransform").rect.width : tableView.GetPrivateField<RectTransform>("_scrollRectTransform").rect.height;
            float num2 = tableView.GetPrivateField<int>("_numberOfCells") * tableView.GetPrivateField<float>("_cellSize") - num;
            
            float targetPosition = tableView.GetProperty<float>("position") + (Mathf.Max(1f, tableView.GetNumberOfVisibleCells() - 1f) * tableView.GetPrivateField<float>("_cellSize") * pages);
            
            if (targetPosition > num2)
            {
                targetPosition = num2;
            }
            
            tableView.SetPrivateField("_targetPosition", targetPosition);

            tableView.enabled = true;
            tableView.RefreshScrollButtons();
        }
        
        private void SongDownloader_songDownloaded(Song song)
        {
            if(lastPack is PlaylistLevelPackSO)
            {
                Playlist playlist = (lastPack as PlaylistLevelPackSO).playlist;

                if (playlist.songs.Any(x => x.Compare(song)))
                {
                    (lastPack as PlaylistLevelPackSO).UpdateDataFromPlaylist();
                    TableView levelsTableView = _levelListViewController.GetPrivateField<LevelPackLevelsTableView>("_levelPackLevelsTableView").GetPrivateField<TableView>("_tableView");
                    levelsTableView.ReloadData();
                    levelsTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
                    levelsTableView.SelectCellWithIdx(0, false);
                }
            }
        }

        public IEnumerator DownloadPlaylist(Playlist playlist)
        {
            PlaylistsCollection.MatchSongsForPlaylist(playlist, true);

            List<PlaylistSong> needToDownload = playlist.songs.Where(x => x.level == null).ToList();
            Plugin.log.Info($"Need to download {needToDownload.Count} songs for playlist {playlist.playlistTitle} by {playlist.playlistAuthor}");

            _downloadingPlaylist = true;
            foreach (var item in needToDownload)
            {
                if (!_downloadingPlaylist)
                    yield break;

                Song beatSaverSong = null;

                if (String.IsNullOrEmpty(playlist.customArchiveUrl))
                {
                    Plugin.log.Info("Obtaining hash and url for " + item.key + ": " + item.songName);
                    yield return GetInfoForSong(playlist, item, (Song song) => { beatSaverSong = song; });
                }
                else
                {
                    string archiveUrl = playlist.customArchiveUrl.Replace("[KEY]", item.key);

                    beatSaverSong = new Song()
                    {
                        songName = item.songName,
                        id = item.key,
                        downloadingProgress = 0f,
                        hash = (item.levelId == null ? "" : item.levelId),
                        downloadUrl = archiveUrl
                    };
                }

                if (!_downloadingPlaylist)
                    yield break;

                if (beatSaverSong != null && !SongLoaderPlugin.SongLoader.CustomLevels.Any(x => x.levelID.Substring(0, 32) == beatSaverSong.hash.ToUpper()))
                {
                    _downloadQueueViewController.EnqueueSong(beatSaverSong, true);
                }
            }
            _downloadingPlaylist = false;
        }

        public IEnumerator GetInfoForSong(Playlist playlist, PlaylistSong song, Action<Song> songCallback)
        {
            string url = "";
            bool _usingHash = false;
            if (!string.IsNullOrEmpty(song.key))
            {
                url = $"{PluginConfig.beatsaverURL}/api/songs/detail/{song.key}";
                if (!string.IsNullOrEmpty(playlist.customDetailUrl))
                {
                    url = playlist.customDetailUrl + song.key;
                }
            }
            else if (!string.IsNullOrEmpty(song.hash))
            {
                url = $"{PluginConfig.beatsaverURL}/api/songs/search/hash/{song.hash}";
                _usingHash = true;
            }
            else if (!string.IsNullOrEmpty(song.levelId))
            {
                string hash = CustomHelpers.CheckHex(song.levelId.Substring(0, Math.Min(32, song.levelId.Length)));
                url = $"{PluginConfig.beatsaverURL}/api/songs/search/hash/{hash}";
                _usingHash = true;
            }
            else
            {
                yield break;
            }

            UnityWebRequest www = UnityWebRequest.Get(url);
            www.timeout = 15;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Plugin.log.Error($"Unable to connect to {PluginConfig.beatsaverURL}! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                try
                {
                    JSONNode node = JSON.Parse(www.downloadHandler.text);

                    if (_usingHash)
                    {
                        if (node["songs"].Count == 0)
                        {
                            Plugin.log.Error($"Song {song.songName} doesn't exist on BeatSaver!");
                            songCallback?.Invoke(null);
                            yield break;
                        }
                        songCallback?.Invoke(Song.FromSearchNode(node["songs"][0]));
                    }
                    else
                    {
                        songCallback?.Invoke(new Song(node["song"]));
                    }
                }
                catch (Exception e)
                {
                    Plugin.log.Critical("Unable to parse response! Exception: " + e);
                }
            }
        }
    }

    [HarmonyPatch(typeof(LevelPackLevelsTableView))]
    [HarmonyPatch("CellForIdx")]
    [HarmonyPatch(new Type[] { typeof(int) })]
    class LevelListTableViewPatch
    {

        static TableCell Postfix(TableCell __result, LevelPackLevelsTableView __instance, int row)
        {
            try
            {
                if (!PluginConfig.enableSongIcons) return __result;

                bool showHeader = __instance.GetPrivateField<bool>("_showLevelPackHeader");

                if (row == 0 && showHeader)
                {
                    return __result;
                }

                string levelId = __instance.GetPrivateField<IBeatmapLevelPack>("_pack").beatmapLevelCollection.beatmapLevels[(showHeader ? (row - 1) : row)].levelID;
                levelId = levelId.Substring(0, Math.Min(32, levelId.Length));

                UnityEngine.UI.Image icon = null;

                UnityEngine.UI.Image[] levelIcons = __result.GetPrivateField<UnityEngine.UI.Image[]>("_beatmapCharacteristicImages");
                float[] levelIconAlphas = __result.GetPrivateField<float[]>("_beatmapCharacteristicAlphas");

                if (levelIcons.Any(x => x.name == "LevelTypeIconExtra"))
                {
                    icon = levelIcons.First(x => x.name == "LevelTypeIconExtra");
                }
                else
                {
                    icon = GameObject.Instantiate(__result.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "LevelTypeIcon0"), __result.transform, true);

                    (icon.transform as RectTransform).anchoredPosition = new Vector2(-14.5f, 0f);
                    icon.transform.name = "LevelTypeIconExtra";

                    levelIcons = levelIcons.AddToArray(icon);
                    __result.SetPrivateField("_beatmapCharacteristicImages", levelIcons);

                    levelIconAlphas = levelIconAlphas.AddToArray(0.1f);
                    __result.SetPrivateField("_beatmapCharacteristicAlphas", levelIconAlphas);

                    foreach (var levelIcon in levelIcons)
                    {
                        levelIcon.rectTransform.anchoredPosition = new Vector2(levelIcon.rectTransform.anchoredPosition.x, -2f);
                    }
                }

                if (PluginConfig.favoriteSongs.Any(x => x.StartsWith(levelId)))
                {
                    levelIconAlphas[3] = 1f;
                    icon.sprite = Sprites.StarFull;
                }
                else if (PluginConfig.votedSongs.ContainsKey(levelId))
                {
                    switch (PluginConfig.votedSongs[levelId].voteType)
                    {
                        case VoteType.Upvote:
                            {
                                levelIconAlphas[3] = 1f;
                                icon.sprite = Sprites.ThumbUp;
                            }
                            break;
                        case VoteType.Downvote:
                            {
                                levelIconAlphas[3] = 1f;
                                icon.sprite = Sprites.ThumbDown;
                            }
                            break;
                    }
                }
                else
                {
                    levelIconAlphas[3] = 0.1f;
                    icon.sprite = Sprites.StarFull;
                }
            }
            catch (Exception e)
            {
                Plugin.log.Critical("Unable to create extra icon! Exception: " + e);
            }
            return __result;
        }
    }

    [HarmonyPatch(typeof(LevelPackTableCell))]
    [HarmonyPatch("SetDataFromPack")]
    [HarmonyPatch(new Type[] { typeof(IBeatmapLevelPack) })]
    class LevelPackTableCellSetDataPatch
    {

        static bool Prefix(LevelPackTableCell __instance, IBeatmapLevelPack pack)
        {
            try
            {
                if (pack is PlaylistLevelPackSO)
                {
                    Playlist playlist = ((PlaylistLevelPackSO)pack).playlist;
                    __instance.GetPrivateField<TextMeshProUGUI>("_packNameText").text = pack.packName;
                    __instance.GetPrivateField<TextMeshProUGUI>("_infoText").text = (playlist.songs.Count > pack.beatmapLevelCollection.beatmapLevels.Length) ? string.Format("Songs {0} | Downloaded {1}", playlist.songs.Count, pack.beatmapLevelCollection.beatmapLevels.Length) : string.Format("Songs {0}", pack.beatmapLevelCollection.beatmapLevels.Length);
                    __instance.GetPrivateField<UnityEngine.UI.Image>("_coverImage").sprite = pack.coverImage;

                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Plugin.log.Critical("Exception in LevelPackTableCellSetData patch: " + e);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(LevelPackTableCell))]
    [HarmonyPatch("RefreshAvailabilityAsync")]
    [HarmonyPatch(new Type[] { typeof(AdditionalContentModelSO), typeof(IBeatmapLevelPack) })]
    class LevelPackTableCellRefreshAvailabilityPatch
    {

        static bool Prefix(LevelPackTableCell __instance, AdditionalContentModelSO contentModel, IBeatmapLevelPack pack)
        {
            try
            {
                return !(pack is PlaylistLevelPackSO);
            }
            catch (Exception e)
            {
                Plugin.log.Critical("Exception in LevelPackTableCellRefreshAvailability patch: " + e);
                return true;
            }
        }
    }


}
