using CustomUI.BeatSaber;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = BeatSaverDownloader.Misc.Logger;
using UnityEngine.UI;
using BeatSaverDownloader.UI.ViewControllers;
using VRUI;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System.IO;
using BeatSaverDownloader.Misc;
using HMUI;
using BeatSaverDownloader.UI.FlowCoordinators;
using TMPro;
using Harmony;
using System.Reflection;

namespace BeatSaverDownloader.UI
{
    public enum SortMode { Default, Difficulty, Newest };

    public class SongListTweaks : MonoBehaviour
    {

        public bool initialized = false;

        public static SortMode lastSortMode = SortMode.Default;
        public static Playlist lastPlaylist;

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

        private LevelCollectionSO _levelCollection;
        private BeatmapCharacteristicSO[] _beatmapCharacteristics;

        private BeatmapCharacteristicSO _lastCharacteristic;
        private PlaylistsFlowCoordinator _playlistsFlowCoordinator;
        private MainFlowCoordinator _mainFlowCoordinator;
        private LevelListViewController _levelListViewController;
        private BeatmapDifficultyViewController _difficultyViewController;
        private StandardLevelDetailViewController _detailViewController;
        private SearchKeyboardViewController _searchViewController;
        private SimpleDialogPromptViewController _simpleDialog;

        private Button _fastPageUpButton;
        private Button _fastPageDownButton;

        private Button _randomButton;
        private Button _searchButton;
        private Button _sortByButton;
        private Button _playlistsButton;

        private Button _defButton;
        private Button _newButton;
        private Button _authorButton;

        private Button _favoriteButton;
        private Button _deleteButton;

        private TextMeshProUGUI _starStatText;

        public void OnLoad()
        {
            initialized = false;
            SetupTweaks();

            if (PluginConfig.disableSongListTweaks)
                return;

            if (SongLoader.AreSongsLoaded)
            {
                AddDefaultPlaylists();
            }
            else
            {
                SongLoader.SongsLoadedEvent += (SongLoader arg1, List<CustomLevel> arg2) => { AddDefaultPlaylists(); };
            }
        }

        private void SetupTweaks()
        {
            _mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().FirstOrDefault();
            _mainFlowCoordinator.GetPrivateField<MainMenuViewController>("_mainMenuViewController").didFinishEvent += SongListTweaks_didFinishEvent;

            _beatmapCharacteristics = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSO>();
            _lastCharacteristic = _beatmapCharacteristics.First(x => x.characteristicName == "Standard");

            if (initialized || PluginConfig.disableSongListTweaks) return;

            Logger.Log("Setting up song list tweaks...");

            try
            {
                var harmony = HarmonyInstance.Create("BeatSaverDownloaderHarmonyInstance");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch(Exception e)
            {
                Logger.Log("Unable to patch level list! Exception: "+e);
            }

            _playlistsFlowCoordinator = new GameObject("PlaylistsFlowCoordinator").AddComponent<PlaylistsFlowCoordinator>();
            _playlistsFlowCoordinator.didFinishEvent += _playlistsFlowCoordinator_didFinishEvent;
            
            Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSelectionViewController>().First().didSelectBeatmapCharacteristicEvent += (BeatmapCharacteristicSelectionViewController sender, BeatmapCharacteristicSO selected) => { _lastCharacteristic = selected; };

            if (SongLoader.AreSongsLoaded)
            {
                _levelCollection = SongLoader.CustomLevelCollectionSO;
            }
            else
            {
                SongLoader.SongsLoadedEvent += (SongLoader sender, List<CustomLevel> levels) =>
                {
                    _levelCollection = SongLoader.CustomLevelCollectionSO;
                };
            }

            _simpleDialog = ReflectionUtil.GetPrivateField<SimpleDialogPromptViewController>(_mainFlowCoordinator, "_simpleDialogPromptViewController");
            _simpleDialog = Instantiate(_simpleDialog.gameObject, _simpleDialog.transform.parent).GetComponent<SimpleDialogPromptViewController>();

            _difficultyViewController = Resources.FindObjectsOfTypeAll<BeatmapDifficultyViewController>().FirstOrDefault();
            _difficultyViewController.didSelectDifficultyEvent += _difficultyViewController_didSelectDifficultyEvent;

            _levelListViewController = Resources.FindObjectsOfTypeAll<LevelListViewController>().FirstOrDefault();
            _levelListViewController.didSelectLevelEvent += _levelListViewController_didSelectLevelEvent;

            TableView _songSelectionTableView = _levelListViewController.GetComponentsInChildren<TableView>().First();
            RectTransform _tableViewRectTransform = _levelListViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "TableViewContainer");

            _tableViewRectTransform.sizeDelta = new Vector2(0f, -20f);
            _tableViewRectTransform.anchoredPosition = new Vector2(0f, -2.5f);

            Button _pageUp = _tableViewRectTransform.GetComponentsInChildren<Button>(true).First(x => x.name == "PageUpButton");
            (_pageUp.transform as RectTransform).anchoredPosition = new Vector2(0f, -1f);

            Button _pageDown = _tableViewRectTransform.GetComponentsInChildren<Button>(true).First(x => x.name == "PageDownButton");
            (_pageDown.transform as RectTransform).anchoredPosition = new Vector2(0f, 1f);

            _fastPageUpButton = Instantiate(_pageUp, _tableViewRectTransform, false);
            (_fastPageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
            (_fastPageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
            (_fastPageUpButton.transform as RectTransform).anchoredPosition = new Vector2(-26f, 1f);
            (_fastPageUpButton.transform as RectTransform).sizeDelta = new Vector2(8f, 6f);
            _fastPageUpButton.GetComponentsInChildren<RectTransform>().First(x => x.name == "BG").sizeDelta = new Vector2(8f, 6f);
            _fastPageUpButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Arrow").sprite = Base64Sprites.DoubleArrow;
            _fastPageUpButton.onClick.AddListener(delegate ()
            {
                for (int i = 0; i < 10; i++)
                {
                    _songSelectionTableView.PageScrollUp();
                }
            });

            _fastPageDownButton = Instantiate(_pageDown, _tableViewRectTransform, false);
            (_fastPageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
            (_fastPageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
            (_fastPageDownButton.transform as RectTransform).anchoredPosition = new Vector2(-26f, -1f);
            (_fastPageDownButton.transform as RectTransform).sizeDelta = new Vector2(8f, 6f);
            _fastPageDownButton.GetComponentsInChildren<RectTransform>().First(x => x.name == "BG").sizeDelta = new Vector2(8f, 6f);
            _fastPageDownButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Arrow").sprite = Base64Sprites.DoubleArrow;
            _fastPageDownButton.onClick.AddListener(delegate ()
            {
                for (int i = 0; i < 10; i++)
                {
                    _songSelectionTableView.PageScrollDown();
                }
            });

            _randomButton = _levelListViewController.CreateUIButton("PracticeButton", new Vector2(-35f, 36.25f), new Vector2(8.8f, 6f), () => 
            {
                int randomRow = UnityEngine.Random.Range(0, _songSelectionTableView.dataSource.NumberOfRows());
                _songSelectionTableView.ScrollToRow(randomRow, false);
                _songSelectionTableView.SelectRow(randomRow, true);
            }
            , "", Base64Sprites.RandomIcon);
            var _randomIconLayour = _randomButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content");
            _randomIconLayour.padding = new RectOffset(0, 0, 0, 0);

            _searchButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(-20f, 36.25f), new Vector2(20f, 6f), SearchPressed, "Search");
            _searchButton.SetButtonTextSize(3f);
            _searchButton.ToggleWordWrapping(false);

            _sortByButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(0f, 36.25f), new Vector2(20f, 6f), () =>
            {
                SelectTopButtons(TopButtonsState.SortBy);
            }, "Sort By");
            _sortByButton.SetButtonTextSize(3f);
            _sortByButton.ToggleWordWrapping(false);

            _playlistsButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(20f, 36.25f), new Vector2(20f, 6f), PlaylistsButtonPressed, "Playlists");
            _playlistsButton.SetButtonTextSize(3f);
            _playlistsButton.ToggleWordWrapping(false);

            _defButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(-20f, 36.25f), new Vector2(20f, 6f), () =>
            {
                SelectTopButtons(TopButtonsState.Select);
                SetLevels(_lastCharacteristic, SortMode.Default, "");
            },
                "Default");

            _defButton.SetButtonTextSize(3f);
            _defButton.ToggleWordWrapping(false);
            _defButton.gameObject.SetActive(false);

            _newButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(0f, 36.25f), new Vector2(20f, 6f), () =>
            {
                SelectTopButtons(TopButtonsState.Select);
                SetLevels(_lastCharacteristic, SortMode.Newest, "");
            }, "Newest");

            _newButton.SetButtonTextSize(3f);
            _newButton.ToggleWordWrapping(false);
            _newButton.gameObject.SetActive(false);


            _authorButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(20f, 36.25f), new Vector2(20f, 6f), () =>
            {
                SelectTopButtons(TopButtonsState.Select);
                SetLevels(_lastCharacteristic, SortMode.Difficulty, "");
            }, "Difficulty");

            _authorButton.SetButtonTextSize(3f);
            _authorButton.ToggleWordWrapping(false);
            _authorButton.gameObject.SetActive(false);

            _detailViewController = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First(x => x.name == "StandardLevelDetailViewController");
            RectTransform buttonsRect = _detailViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "Buttons");

            buttonsRect.anchoredPosition = new Vector2(0f, 10.75f);

            RectTransform customButtonsRect = Instantiate(buttonsRect, buttonsRect.parent, true);

            Destroy(customButtonsRect.GetComponent<ContentSizeFitter>());
            Destroy(customButtonsRect.GetComponent<HorizontalLayoutGroup>());

            customButtonsRect.name = "CustomUIButtonsHolder";
            customButtonsRect.anchoredPosition = new Vector2(0f, 1.25f);

            _favoriteButton = customButtonsRect.GetComponentsInChildren<Button>().First(x => x.name == "PracticeButton");
            _favoriteButton.SetButtonIcon(Base64Sprites.AddToFavorites);
            _favoriteButton.onClick.AddListener(() =>
            {
                if (PluginConfig.favoriteSongs.Any(x => x.Contains(_detailViewController.difficultyBeatmap.level.levelID)))
                {
                    PluginConfig.favoriteSongs.Remove(_detailViewController.difficultyBeatmap.level.levelID);
                    PluginConfig.SaveConfig();
                    _favoriteButton.SetButtonIcon(Base64Sprites.AddToFavorites);
                    PlaylistsCollection.RemoveLevelFromPlaylist(PlaylistsCollection.loadedPlaylists.First(x => x.playlistTitle == "Your favorite songs"), _detailViewController.difficultyBeatmap.level.levelID);
                }
                else
                {
                    PluginConfig.favoriteSongs.Add(_detailViewController.difficultyBeatmap.level.levelID);
                    PluginConfig.SaveConfig();
                    _favoriteButton.SetButtonIcon(Base64Sprites.RemoveFromFavorites);
                    PlaylistsCollection.AddSongToPlaylist(PlaylistsCollection.loadedPlaylists.First(x => x.playlistTitle == "Your favorite songs"), new PlaylistSong() { levelId = _detailViewController.difficultyBeatmap.level.levelID, songName = _detailViewController.difficultyBeatmap.level.songName, level = SongDownloader.GetLevel(_detailViewController.difficultyBeatmap.level.levelID) });
                }
            });

            _deleteButton = customButtonsRect.GetComponentsInChildren<Button>().First(x => x.name == "PlayButton");
            _deleteButton.SetButtonText("Delete");
            _deleteButton.ToggleWordWrapping(false);
            _deleteButton.onClick.AddListener(DeletePressed);
            _deleteButton.GetComponentsInChildren<RectTransform>().First(x => x.name == "GlowContainer").gameObject.SetActive(false);
            _deleteButton.interactable = !PluginConfig.disableDeleteButton;

            //based on https://github.com/halsafar/BeatSaberSongBrowser/blob/master/SongBrowserPlugin/UI/Browser/SongBrowserUI.cs#L192
            var statsPanel = _detailViewController.GetComponentsInChildren<CanvasRenderer>(true).First(x => x.name == "LevelParamsPanel");
            var statTransforms = statsPanel.GetComponentsInChildren<RectTransform>();
            var valueTexts = statsPanel.GetComponentsInChildren<TextMeshProUGUI>().Where(x => x.name == "ValueText").ToList();

            foreach (RectTransform r in statTransforms)
            {
                if (r.name == "Separator")
                {
                    continue;
                }
                r.sizeDelta = new Vector2(r.sizeDelta.x * 0.85f, r.sizeDelta.y * 0.85f);
            }

            var _starStatTransform = Instantiate(statTransforms[1], statsPanel.transform, false);
            _starStatText = _starStatTransform.GetComponentInChildren<TextMeshProUGUI>();
            _starStatTransform.GetComponentInChildren<UnityEngine.UI.Image>().sprite = Base64Sprites.StarFull;
            _starStatText.text = "--";

            ResultsViewController _standardLevelResultsViewController = Resources.FindObjectsOfTypeAll<ResultsViewController>().First(x => x.name == "StandardLevelResultsViewController");
            _standardLevelResultsViewController.continueButtonPressedEvent += _standardLevelResultsViewController_continueButtonPressedEvent;
            
            initialized = true;
        }
        
        public void AddDefaultPlaylists()
        {
            Logger.Log("Creating default playlists...");

            List<LevelSO> levels = _levelCollection.levels.ToList();

            Playlist _allPlaylist = new Playlist() { playlistTitle = "All songs", playlistAuthor = "", image = Base64Sprites.BeastSaberLogoB64, icon = Base64Sprites.BeastSaberLogo, fileLoc = "" };
            _allPlaylist.songs = new List<PlaylistSong>();
            _allPlaylist.songs.AddRange(levels.Select(x => new PlaylistSong() { songName = $"{x.songName} {x.songSubName}", level = x, oneSaber = x.beatmapCharacteristics.Any(y => y.characteristicName == "One Saber"), path = "", key = "", levelId = x.levelID, hash = CustomHelpers.CheckHex(x.levelID.Substring(0, Math.Min(32, x.levelID.Length))) }));
            Logger.Log($"Created \"{_allPlaylist.playlistTitle}\" playlist with {_allPlaylist.songs.Count} songs!");

            Playlist _favPlaylist = new Playlist() { playlistTitle = "Your favorite songs", playlistAuthor = "", image = Base64Sprites.BeastSaberLogoB64, icon = Base64Sprites.BeastSaberLogo, fileLoc = "" };
            _favPlaylist.songs = new List<PlaylistSong>();
            _favPlaylist.songs.AddRange(levels.Where(x => PluginConfig.favoriteSongs.Contains(x.levelID)).Select(x => new PlaylistSong() { songName = $"{x.songName} {x.songSubName}", level = x, oneSaber = x.beatmapCharacteristics.Any(y => y.characteristicName == "One Saber"), path = "", key = "", levelId = x.levelID, hash = CustomHelpers.CheckHex(x.levelID.Substring(0, Math.Min(32, x.levelID.Length))) }));
            Logger.Log($"Created \"{_favPlaylist.playlistTitle}\" playlist with {_favPlaylist.songs.Count} songs!");

            if (PlaylistsCollection.loadedPlaylists.Any(x => x.playlistTitle == "All songs" || x.playlistTitle == "Your favorite songs"))
            {
                PlaylistsCollection.loadedPlaylists.RemoveAll(x => x.playlistTitle == "All songs" || x.playlistTitle == "Your favorite songs");
            }

            PlaylistsCollection.loadedPlaylists.Insert(0, _favPlaylist);
            PlaylistsCollection.loadedPlaylists.Insert(0, _allPlaylist);

            _favPlaylist.SavePlaylist("Playlists\\favorites.json");
        }

        private void _playlistsFlowCoordinator_didFinishEvent(Playlist playlist)
        {
            if (playlist != null)
            {
                lastPlaylist = playlist;

                SetLevels(_lastCharacteristic, lastSortMode, "");
            }
        }

        private void SongListTweaks_didFinishEvent(MainMenuViewController sender, MainMenuViewController.MenuButton result)
        {
            _lastCharacteristic = _beatmapCharacteristics.First(x => x.characteristicName == "Standard");
            if (result == MainMenuViewController.MenuButton.SoloFreePlay)
            {
                freePlayFlowCoordinator = FindObjectOfType<SoloFreePlayFlowCoordinator>();
            }
            else if (result == MainMenuViewController.MenuButton.Party)
            {
                freePlayFlowCoordinator = FindObjectOfType<PartyFreePlayFlowCoordinator>();
            }
            else
            {
                freePlayFlowCoordinator = null;
            }
            lastPlaylist = null;
        }

        public void SelectTopButtons(TopButtonsState _newState)
        {
            switch (_newState)
            {
                case TopButtonsState.Select:
                    {
                        _sortByButton.gameObject.SetActive(true);
                        _searchButton.gameObject.SetActive(true);
                        _playlistsButton.gameObject.SetActive(true);

                        _defButton.gameObject.SetActive(false);
                        _newButton.gameObject.SetActive(false);
                        _authorButton.gameObject.SetActive(false);
                    }; break;
                case TopButtonsState.SortBy:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);
                        _playlistsButton.gameObject.SetActive(false);

                        _defButton.gameObject.SetActive(true);
                        _newButton.gameObject.SetActive(true);
                        _authorButton.gameObject.SetActive(true);
                    }; break;
                case TopButtonsState.Search:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);
                        _playlistsButton.gameObject.SetActive(false);

                        _defButton.gameObject.SetActive(false);
                        _newButton.gameObject.SetActive(false);
                        _authorButton.gameObject.SetActive(false);

                    }; break;
            }
        }

        private void _difficultyViewController_didSelectDifficultyEvent(BeatmapDifficultyViewController sender, IDifficultyBeatmap beatmap)
        {
            _favoriteButton.SetButtonIcon(PluginConfig.favoriteSongs.Any(x => x.Contains(beatmap.level.levelID)) ? Base64Sprites.RemoveFromFavorites : Base64Sprites.AddToFavorites);
            _deleteButton.interactable = !PluginConfig.disableDeleteButton && (beatmap.level.levelID.Length >= 32);
            if (beatmap.level.levelID.Length >= 32)
            {
                ScrappedSong song = ScrappedData.Songs.FirstOrDefault(x => x.Hash == beatmap.level.levelID.Substring(0, 32));
                if (song != null && song.Diffs.Any(x => x.Diff == beatmap.difficulty.ToString()))
                    _starStatText.text = song.Diffs.First(x => x.Diff == beatmap.difficulty.ToString()).Stars.ToString();
                else
                    _starStatText.text = "--";
            }
            else
                _starStatText.text = "--";
        }

        private void _levelListViewController_didSelectLevelEvent(LevelListViewController sender, IBeatmapLevel beatmap)
        {
            if (_difficultyViewController.isInViewControllerHierarchy && _difficultyViewController.selectedDifficultyBeatmap != null && beatmap.levelID.Length >= 32)
            {
                ScrappedSong song = ScrappedData.Songs.FirstOrDefault(x => x.Hash == beatmap.levelID.Substring(0, 32));
                if (song != null && song.Diffs.Any(x => x.Diff == _difficultyViewController.selectedDifficultyBeatmap.difficulty.ToString()))
                    _starStatText.text = song.Diffs.First(x => x.Diff == _difficultyViewController.selectedDifficultyBeatmap.difficulty.ToString()).Stars.ToString();
                else
                {
                    _starStatText.text = "--";
                }
            }
            else
            {
                _starStatText.text = "--";
            }
        }

        private void PlaylistsButtonPressed()
        {
            _playlistsFlowCoordinator.parentFlowCoordinator = freePlayFlowCoordinator;
            freePlayFlowCoordinator.InvokePrivateMethod("PresentFlowCoordinator", new object[] { _playlistsFlowCoordinator, null, false, false });
        }

        private void DeletePressed()
        {
            IBeatmapLevel level = _detailViewController.difficultyBeatmap.level;
            _simpleDialog.Init("Delete song", $"Do you really want to delete \"{ level.songName} {level.songSubName}\"?", "Delete", "Cancel");
            _simpleDialog.didFinishEvent -= _simpleDialog_didFinishEvent;
            _simpleDialog.didFinishEvent += _simpleDialog_didFinishEvent;
            freePlayFlowCoordinator.InvokePrivateMethod("PresentViewController", new object[] { _simpleDialog, null, false });
        }

        private void _simpleDialog_didFinishEvent(SimpleDialogPromptViewController sender, bool delete)
        {
            freePlayFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _simpleDialog, null, false });
            if (delete)
            {
                try
                {
                    SongDownloader.Instance.DeleteSong(new Song(SongLoader.CustomLevels.First(x => x.levelID == _detailViewController.difficultyBeatmap.level.levelID)));

                    List<IBeatmapLevel> levels = _levelListViewController.GetPrivateField<IBeatmapLevel[]>("_levels").ToList();
                    int selectedIndex = levels.IndexOf(_detailViewController.difficultyBeatmap.level);

                    if (selectedIndex > -1)
                    {
                        int removedLevels = levels.RemoveAll(x => x == _detailViewController.difficultyBeatmap.level);
                        Logger.Log("Removed " + removedLevels + " level(s) from song list!");

                        if (selectedIndex > 0)
                            selectedIndex--;

                        _levelListViewController.SetLevels(levels.ToArray());
                        TableView listTableView = _levelListViewController.GetPrivateField<LevelListTableView>("_levelListTableView").GetPrivateField<TableView>("_tableView");
                        listTableView.ScrollToRow(selectedIndex, false);
                        listTableView.SelectRow(selectedIndex, true);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Unable to delete song! Exception: " + e);
                }
            }
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
            SetLevels(_lastCharacteristic, SortMode.Default, request);
        }

        private void _searchViewController_backButtonPressed()
        {
            freePlayFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _searchViewController, null, false });
        }

        public void SetLevels(BeatmapCharacteristicSO characteristic, SortMode sortMode, string searchRequest)
        {
            LevelSO[] levels = null;
            if (lastPlaylist != null)
            {
                levels = lastPlaylist.songs.Where(x => x.level != null && x.level.beatmapCharacteristics.Contains(characteristic)).Select(x => x.level).ToArray();
            }
            else
            {
                levels = _levelCollection.GetLevelsWithBeatmapCharacteristic(characteristic);
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

            _levelListViewController.SetLevels(levels);
            PopDifficultyAndDetails();
        }

        public LevelSO[] SortLevelsByCreationTime(LevelSO[] levels)
        {
            DirectoryInfo customSongsFolder = new DirectoryInfo(Environment.CurrentDirectory.Replace('\\', '/') + "/CustomSongs/");

            List<string> sortedFolders = customSongsFolder.GetDirectories().OrderByDescending(x => x.CreationTime.Ticks).Select(x => x.FullName.Replace('\\', '/')).ToList();

            List<string> sortedLevelIDs = new List<string>();

            foreach (string path in sortedFolders)
            {
                CustomLevel song = SongLoader.CustomLevels.FirstOrDefault(x => x.customSongInfo.path.StartsWith(path));
                if (song != null)
                {
                    sortedLevelIDs.Add(song.levelID);
                }
            }

            List<LevelSO> notSorted = new List<LevelSO>(levels);

            List<LevelSO> sortedLevels = new List<LevelSO>();

            foreach (string levelId in sortedLevelIDs)
            {
                LevelSO data = notSorted.FirstOrDefault(x => x.levelID == levelId);
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
                if (soloCoordinator.GetPrivateField<BeatmapDifficultyViewController>("_beatmapDifficultyViewControllerViewController").isInViewControllerHierarchy)
                {
                    controllers++;
                }
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
                if (partyCoordinator.GetPrivateField<BeatmapDifficultyViewController>("_beatmapDifficultyViewControllerViewController").isInViewControllerHierarchy)
                {
                    controllers++;
                }
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
                Logger.Warning("Unable to refresh song list! Exception: "+e);
            }
        }
    }

    [HarmonyPatch(typeof(LevelListTableView))]
    [HarmonyPatch("CellForRow")]
    [HarmonyPatch(new Type[] { typeof(int) })]
    class LevelListTableViewPatch
    {
        static Material noGlow;

        static TableCell Postfix(TableCell __result, LevelListTableView __instance, int row)
        {
            try
            {
                if (!PluginConfig.enableSongIcons) return __result;

                string levelId = __instance.GetPrivateField<IBeatmapLevel[]>("_levels")[row].levelID;
                levelId = levelId.Substring(0, Math.Min(32, levelId.Length));

                UnityEngine.UI.Image[] images = __result.transform.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                UnityEngine.UI.Image icon = null;

                if (images.Any(x => x.name == "ExtraIcon"))
                {
                    icon = images.First(x => x.name == "ExtraIcon");
                }
                else
                {
                    RectTransform iconRT = new GameObject("ExtraIcon", typeof(RectTransform)).GetComponent<RectTransform>();
                    iconRT.SetParent(__result.transform, false);

                    iconRT.anchorMin = new Vector2(0.95f, 0.25f);
                    iconRT.anchorMax = new Vector2(0.95f, 0.25f);
                    iconRT.anchoredPosition = new Vector2(0f, 0f);
                    iconRT.sizeDelta = new Vector2(4f, 4f);

                    icon = iconRT.gameObject.AddComponent<UnityEngine.UI.Image>();

                    if (noGlow == null)
                    {
                        noGlow = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "UINoGlow");
                    }

                    icon.material = noGlow;
                }

                if (PluginConfig.favoriteSongs.Any(x => x.StartsWith(levelId)))
                {
                    icon.enabled = true;
                    icon.sprite = Base64Sprites.StarFull;
                }
                else if (PluginConfig.votedSongs.ContainsKey(levelId))
                {
                    switch (PluginConfig.votedSongs[levelId].voteType)
                    {
                        case VoteType.Upvote:
                            {
                                icon.enabled = true;
                                icon.sprite = Base64Sprites.ThumbUp;
                            }
                            break;
                        case VoteType.Downvote:
                            {
                                icon.enabled = true;
                                icon.sprite = Base64Sprites.ThumbDown;
                            }
                            break;
                    }
                }
                else
                {
                    icon.enabled = false;
                }
            }
            catch
            {

            }
            return __result;
        }
    }

    [HarmonyPatch(typeof(LevelListTableCell))]
    [HarmonyPatch("SelectionDidChange")]
    [HarmonyPatch(new Type[] { typeof(TableCell.TransitionType) })]
    class LevelListTableCellPatch
    {
        static void Postfix(LevelListTableCell __instance, TableCell.TransitionType transitionType)
        {
            try
            {
                if (!PluginConfig.enableSongIcons) return;

                UnityEngine.UI.Image[] images = __instance.transform.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                UnityEngine.UI.Image icon = null;

                if (images.Any(x => x.name == "ExtraIcon"))
                {
                    icon = images.First(x => x.name == "ExtraIcon");
                    if (icon.enabled)
                    {
                        if (__instance.selected)
                        {
                            icon.color = Color.black;
                        }
                        else
                        {
                            icon.color = Color.white;
                        }
                    }
                }
            }
            catch
            {

            }
            
        }
    }
}
