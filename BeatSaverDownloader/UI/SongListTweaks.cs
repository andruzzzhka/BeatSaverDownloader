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

namespace BeatSaverDownloader.UI
{
    public enum SortMode { Default, Difficulty, Newest };

    class SongListTweaks : MonoBehaviour
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

        private LevelCollectionSO _levelCollection;
        private BeatmapCharacteristicSO[] _beatmapCharacteristics;

        private BeatmapCharacteristicSO _lastCharacteristic;

        private PlaylistsFlowCoordinator _playlistsFlowCoordinator;
        private MainFlowCoordinator _mainFlowCoordinator;
        private FlowCoordinator _freePlayFlowCoordinator;
        private LevelListViewController _levelListViewController;
        private BeatmapDifficultyViewController _difficultyViewController;
        private StandardLevelDetailViewController _detailViewController;
        private SearchKeyboardViewController _searchViewController;
        private SimpleDialogPromptViewController _simpleDialog;

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
            if (initialized || PluginConfig.disableSongListTweaks) return;

            Logger.Log("Setting up song list tweaks...");

            _playlistsFlowCoordinator = (new GameObject("PlaylistsFlowCoordinator")).AddComponent<PlaylistsFlowCoordinator>();
            _playlistsFlowCoordinator.didFinishEvent += _playlistsFlowCoordinator_didFinishEvent;

            _beatmapCharacteristics = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSO>();
            _lastCharacteristic = _beatmapCharacteristics.First(x => x.characteristicName == "Standard");

            Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSelectionViewController>().First().didSelectBeatmapCharacteristicEvent += (BeatmapCharacteristicSelectionViewController sender, BeatmapCharacteristicSO selected) => { _lastCharacteristic = selected; };

            if (SongLoader.AreSongsLoaded)
            {
                _levelCollection = SongLoader.CustomLevelCollectionSO;
            }
            else
            {
                SongLoader.SongsLoadedEvent += (SongLoader sender, List<CustomLevel> levels) => {
                    _levelCollection = SongLoader.CustomLevelCollectionSO;
                };
            }

            _mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().FirstOrDefault();
            _mainFlowCoordinator.GetPrivateField<MainMenuViewController>("_mainMenuViewController").didFinishEvent += SongListTweaks_didFinishEvent;

            _simpleDialog = ReflectionUtil.GetPrivateField<SimpleDialogPromptViewController>(_mainFlowCoordinator, "_simpleDialogPromptViewController");
            _simpleDialog = Instantiate(_simpleDialog.gameObject, _simpleDialog.transform.parent).GetComponent<SimpleDialogPromptViewController>();

            _difficultyViewController = Resources.FindObjectsOfTypeAll<BeatmapDifficultyViewController>().FirstOrDefault();
            _difficultyViewController.didSelectDifficultyEvent += _difficultyViewController_didSelectDifficultyEvent;

            _levelListViewController = Resources.FindObjectsOfTypeAll<LevelListViewController>().FirstOrDefault();
            _levelListViewController.didSelectLevelEvent += _levelListViewController_didSelectLevelEvent; ;

            RectTransform _tableViewRectTransform = _levelListViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "TableViewContainer");

            _tableViewRectTransform.sizeDelta = new Vector2(0f, -20f);
            _tableViewRectTransform.anchoredPosition = new Vector2(0f, -2.5f);

            RectTransform _pageUp = _tableViewRectTransform.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "PageUpButton");
            _pageUp.anchoredPosition = new Vector2(0f, -1f);

            RectTransform _pageDown = _tableViewRectTransform.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "PageDownButton");
            _pageDown.anchoredPosition = new Vector2(0f, 1f);
            
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
            _favoriteButton.onClick.AddListener(() => {
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
                    PlaylistsCollection.AddSongToPlaylist(PlaylistsCollection.loadedPlaylists.First(x => x.playlistTitle == "Your favorite songs"), new PlaylistSong() { levelId = _detailViewController.difficultyBeatmap.level.levelID, songName = _detailViewController.difficultyBeatmap.level.songName, level = SongDownloader.GetLevel(_detailViewController.difficultyBeatmap.level.levelID)});
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

            initialized = true;
        }

        public void AddDefaultPlaylists()
        {
            Logger.Log("Creating default playlists...");
                        
            List<LevelSO> levels = _levelCollection.levels.ToList();

            Playlist _allPlaylist = new Playlist() { playlistTitle = "All songs", playlistAuthor = "", image = Base64Sprites.BeastSaberLogoB64, icon = Base64Sprites.BeastSaberLogo, fileLoc = "" };
            _allPlaylist.songs = new List<PlaylistSong>();
            _allPlaylist.songs.AddRange(levels.Select(x => new PlaylistSong() { songName = $"{x.songName} {x.songSubName}", level = x, oneSaber = x.beatmapCharacteristics.Any(y => y.characteristicName == "One Saber"), path = "", key = "", levelId = x.levelID }));
            Logger.Log($"Created \"{_allPlaylist.playlistTitle}\" playlist with {_allPlaylist.songs.Count} songs!");

            Playlist _favPlaylist = new Playlist() { playlistTitle = "Your favorite songs", playlistAuthor = "", image = Base64Sprites.BeastSaberLogoB64, icon = Base64Sprites.BeastSaberLogo, fileLoc = "" };
            _favPlaylist.songs = new List<PlaylistSong>();
            _favPlaylist.songs.AddRange(levels.Where(x => PluginConfig.favoriteSongs.Contains(x.levelID)).Select(x => new PlaylistSong() { songName = $"{x.songName} {x.songSubName}", level = x, oneSaber = x.beatmapCharacteristics.Any(y => y.characteristicName == "One Saber"), path = "", key = "", levelId = x.levelID }));
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
            if(result == MainMenuViewController.MenuButton.SoloFreePlay)
            {
                _freePlayFlowCoordinator = FindObjectOfType<SoloFreePlayFlowCoordinator>();
                _lastCharacteristic = _beatmapCharacteristics.First(x => x.characteristicName == "Standard");
                lastPlaylist = null;
            }
            else if(result == MainMenuViewController.MenuButton.Party)
            {
                _freePlayFlowCoordinator = FindObjectOfType<PartyFreePlayFlowCoordinator>();
                _lastCharacteristic = _beatmapCharacteristics.First(x => x.characteristicName == "Standard");
                lastPlaylist = null;
            }
            else
            {
                _freePlayFlowCoordinator = null;
                lastPlaylist = null;
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
            if (beatmap.level.levelID.Length >= 32) {
                ScrappedSong song = ScrappedData.Songs.FirstOrDefault(x => x.Hash == beatmap.level.levelID.Substring(0, 32));
                if(song != null && song.Diffs.Any(x => x.Diff == beatmap.difficulty.ToString()))
                    _starStatText.text = (song == null ? "--" : song.Diffs.First(x => x.Diff == beatmap.difficulty.ToString()).Stars.ToString());
            }
            else
            {
                _starStatText.text = "--";
            }
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
            _playlistsFlowCoordinator.parentFlowCoordinator = _freePlayFlowCoordinator;
            _freePlayFlowCoordinator.InvokePrivateMethod("PresentFlowCoordinator", new object[] { _playlistsFlowCoordinator , null, false, false});
        }

        private void DeletePressed()
        {
            IBeatmapLevel level = _detailViewController.difficultyBeatmap.level;
            _simpleDialog.Init("Delete song", $"Do you really want to delete \"{ level.songName} {level.songSubName}\"?", "Delete", "Cancel");
            _simpleDialog.didFinishEvent -= _simpleDialog_didFinishEvent;
            _simpleDialog.didFinishEvent += _simpleDialog_didFinishEvent;
            _freePlayFlowCoordinator.InvokePrivateMethod("PresentViewController", new object[] { _simpleDialog, null, false });
        }

        private void _simpleDialog_didFinishEvent(SimpleDialogPromptViewController sender, bool delete)
        {
            _freePlayFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _simpleDialog, null, false });
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
                        Logger.Log("Removed "+removedLevels+" level(s) from song list!");

                        if (selectedIndex > 0)
                            selectedIndex--;

                        _levelListViewController.SetLevels(levels.ToArray());
                        TableView listTableView = _levelListViewController.GetPrivateField<LevelListTableView>("_levelListTableView").GetPrivateField<TableView>("_tableView");
                        listTableView.ScrollToRow(selectedIndex, false);
                        listTableView.SelectRow(selectedIndex, true);
                    }
                }catch(Exception e)
                {
                    Logger.Error("Unable to delete song! Exception: "+e);
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

            _freePlayFlowCoordinator.InvokePrivateMethod("PresentViewController", new object[] { _searchViewController, null, false });
        }

        private void _searchViewController_searchButtonPressed(string request)
        {
            _freePlayFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _searchViewController, null, false });
            SetLevels(_lastCharacteristic, SortMode.Default, request);
        }

        private void _searchViewController_backButtonPressed()
        {
            _freePlayFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _searchViewController, null, false });
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
                    case SortMode.Difficulty: {
                            levels = levels.AsParallel().OrderBy(x => { int index = ScrappedData.Songs.FindIndex(y => x.levelID.StartsWith(y.Hash)); return (index == -1 ? (x.levelID.Length < 32 ? int.MaxValue : int.MaxValue-1) : index); }).ToArray();
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

        LevelSO[] SortLevelsByCreationTime(LevelSO[] levels)
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
            bool isSolo = (_freePlayFlowCoordinator is SoloFreePlayFlowCoordinator);

            if (isSolo)
            {
                SoloFreePlayFlowCoordinator soloCoordinator = _freePlayFlowCoordinator as SoloFreePlayFlowCoordinator;
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
                PartyFreePlayFlowCoordinator partyCoordinator = _freePlayFlowCoordinator as PartyFreePlayFlowCoordinator;
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
    }
}
