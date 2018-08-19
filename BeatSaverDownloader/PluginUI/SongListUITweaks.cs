using BeatSaverDownloader.Misc;
using BeatSaverDownloader.PluginUI.ViewControllers;
using HMUI;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaverDownloader.PluginUI
{
    public enum SortMode { Default, Author, Newest};

    class SongListUITweaks : MonoBehaviour
    {
        public static SortMode lastSortMode = SortMode.Default;
        public static Playlist lastPlaylist;

        SearchKeyboardViewController _searchViewController;

        StandardLevelSelectionFlowCoordinator _songSelectionMasterViewController;
        StandardLevelListViewController _songListViewController;
        StandardLevelSelectionNavigationController _levelSelectionNavController;

        PlaylistNavigationController _playlistNavController;

        RectTransform _tableViewRectTransform;

        Button _searchButton;
        Button _sortByButton;
        Button _playlistsButton;
        Button _authorButton;
        Button _defButton;
        Button _newButton;


        public void SongListUIFound()
        {
            if (_songSelectionMasterViewController == null)
            {
                _songSelectionMasterViewController = Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().First();
            }

            if (_songListViewController == null)
            {
                _songListViewController = ReflectionUtil.GetPrivateField<StandardLevelListViewController>(_songSelectionMasterViewController, "_levelListViewController");
            }

            if (_levelSelectionNavController == null)
            {
                _levelSelectionNavController = ReflectionUtil.GetPrivateField<StandardLevelSelectionNavigationController>(_songSelectionMasterViewController, "_levelSelectionNavigationController");
            }

            if (_playlistNavController == null)
            {
                _playlistNavController = BeatSaberUI.CreateViewController<PlaylistNavigationController>();
            }

            if (_tableViewRectTransform == null)
            {
                _tableViewRectTransform = _songListViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "TableViewContainer");

                _tableViewRectTransform.sizeDelta = new Vector2(0f , -20f);
                _tableViewRectTransform.anchoredPosition = new Vector2(0f, -2.5f);

                RectTransform _pageUp = _tableViewRectTransform.GetComponentsInChildren<RectTransform>().First(x => x.name == "PageUpButton");
                _pageUp.anchoredPosition = new Vector2(0f, -1f);

                RectTransform _pageDown = _tableViewRectTransform.GetComponentsInChildren<RectTransform>().First(x => x.name == "PageDownButton");
                _pageDown.anchoredPosition = new Vector2(0f, 1f);
            }

            if (_searchButton == null)
            {
                _searchButton = BeatSaberUI.CreateUIButton((_tableViewRectTransform.parent as RectTransform), "SettingsButton");
                BeatSaberUI.SetButtonText(_searchButton, "Search");
                BeatSaberUI.SetButtonTextSize(_searchButton, 3f);
                (_searchButton.transform as RectTransform).sizeDelta = new Vector2(20f, 6f);
                (_searchButton.transform as RectTransform).anchoredPosition = new Vector2(-40f, 73f);
                _searchButton.onClick.RemoveAllListeners();
                _searchButton.onClick.AddListener(delegate ()
                {
                    ShowSearchKeyboard();
                    SelectTopButtons(TopButtonsState.Search);

                });
            }

            if (_sortByButton == null)
            {
                _sortByButton = BeatSaberUI.CreateUIButton((_tableViewRectTransform.parent as RectTransform), "SettingsButton");
                BeatSaberUI.SetButtonText(_sortByButton, "Sort by");
                BeatSaberUI.SetButtonTextSize(_sortByButton, 3f);
                (_sortByButton.transform as RectTransform).sizeDelta = new Vector2(20f, 6f);
                (_sortByButton.transform as RectTransform).anchoredPosition = new Vector2(-20f, 73f);
                _sortByButton.onClick.RemoveAllListeners();
                _sortByButton.onClick.AddListener(delegate ()
                {
                    SelectTopButtons(TopButtonsState.SortBy);
                });
            }

            if (_playlistsButton == null)
            {
                _playlistsButton = BeatSaberUI.CreateUIButton((_tableViewRectTransform.parent as RectTransform), "SettingsButton");
                BeatSaberUI.SetButtonText(_playlistsButton, "Playlists");
                BeatSaberUI.SetButtonTextSize(_playlistsButton, 3f);
                (_playlistsButton.transform as RectTransform).sizeDelta = new Vector2(20f, 6f);
                (_playlistsButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 73f);
                _playlistsButton.onClick.RemoveAllListeners();
                _playlistsButton.onClick.AddListener(delegate ()
                {
                    SelectTopButtons(TopButtonsState.Playlists);
                    _levelSelectionNavController.PresentModalViewController(_playlistNavController, null);
                });
                _playlistNavController.finished += SelectedPlaylist;
            }

            if (_authorButton == null)
            {
                _authorButton = BeatSaberUI.CreateUIButton((_tableViewRectTransform.parent as RectTransform), "SettingsButton");
                BeatSaberUI.SetButtonText(_authorButton, "Author");
                BeatSaberUI.SetButtonTextSize(_authorButton, 3f);
                (_authorButton.transform as RectTransform).sizeDelta = new Vector2(20f, 6f);
                (_authorButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 73f);
                _authorButton.onClick.RemoveAllListeners();
                _authorButton.onClick.AddListener(delegate ()
                {
                    ShowLevels(SortMode.Author);
                    SelectTopButtons(TopButtonsState.Select);
                });
                _authorButton.gameObject.SetActive(false);
            }

            if (_defButton == null)
            {
                _defButton = BeatSaberUI.CreateUIButton((_tableViewRectTransform.parent as RectTransform), "SettingsButton");
                BeatSaberUI.SetButtonText(_defButton, "Default");
                BeatSaberUI.SetButtonTextSize(_defButton, 3f);
                (_defButton.transform as RectTransform).sizeDelta = new Vector2(20f, 6f);
                (_defButton.transform as RectTransform).anchoredPosition = new Vector2(-40f, 73f);
                _defButton.onClick.RemoveAllListeners();
                _defButton.onClick.AddListener(delegate ()
                {
                    ShowLevels(SortMode.Default);
                    SelectTopButtons(TopButtonsState.Select);
                });
                _defButton.gameObject.SetActive(false);

            }

            if (_newButton == null)
            {
                _newButton = BeatSaberUI.CreateUIButton((_tableViewRectTransform.parent as RectTransform), "SettingsButton");
                BeatSaberUI.SetButtonText(_newButton, "Newest");
                BeatSaberUI.SetButtonTextSize(_newButton, 3f);
                (_newButton.transform as RectTransform).sizeDelta = new Vector2(20f, 6f);
                (_newButton.transform as RectTransform).anchoredPosition = new Vector2(-20f, 73f);
                _newButton.onClick.RemoveAllListeners();
                _newButton.onClick.AddListener(delegate ()
                {
                    ShowLevels(SortMode.Newest);
                    SelectTopButtons(TopButtonsState.Select);
                });
                _newButton.gameObject.SetActive(false);

            }

            if(lastPlaylist != null)
            {
                SelectedPlaylist(lastPlaylist);
            }
        }

        private void SelectedPlaylist(Playlist playlist)
        {
            lastPlaylist = playlist;
            SelectTopButtons(TopButtonsState.Select);

            if(!lastPlaylist.songs.All(x => x.level != null))
            {
                lastPlaylist.songs.ForEach(x => x.level = SongLoader.CustomLevels.FirstOrDefault(y => y.customSongInfo.path.Contains(x.key)));
            }

            ShowLevels(SortMode.Default);
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

                        _authorButton.gameObject.SetActive(false);
                        _defButton.gameObject.SetActive(false);
                        _newButton.gameObject.SetActive(false);
                    }; break;
                case TopButtonsState.SortBy:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);
                        _playlistsButton.gameObject.SetActive(false);

                        _authorButton.gameObject.SetActive(true);
                        _defButton.gameObject.SetActive(true);
                        _newButton.gameObject.SetActive(true);
                    }; break;
                case TopButtonsState.Search:
                case TopButtonsState.Playlists:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);
                        _playlistsButton.gameObject.SetActive(false);

                        _authorButton.gameObject.SetActive(false);
                        _defButton.gameObject.SetActive(false);
                        _newButton.gameObject.SetActive(false);
                    }; break;
            }

        }

        void ShowSearchKeyboard()
        {
            if(_searchViewController == null)
            {
                _searchViewController = BeatSaberUI.CreateViewController<SearchKeyboardViewController>();
                _searchViewController.searchButtonPressed += _searchViewController_searchButtonPressed;
                _searchViewController.backButtonPressed += _searchViewController_backButtonPressed;
            }
            
            _songListViewController.navigationController.PresentModalViewController(_searchViewController, null, false);
        }

        private void _searchViewController_backButtonPressed()
        {
            SelectTopButtons(TopButtonsState.Select);
        }

        private void _searchViewController_searchButtonPressed(string searchFor)
        {
            Logger.StaticLog($"Searching for \"{searchFor}\"...");
            
            SelectTopButtons(TopButtonsState.Select);
            SearchForLevels(searchFor);
        }

        public void ShowLevels(SortMode mode)
        {
            if (_songListViewController != null && _songListViewController.selectedLevel != null)
            {
                SetSongListLevels(GetSortedLevels(mode), _songListViewController.selectedLevel.levelID);
            }
            else
            {
                SetSongListLevels(GetSortedLevels(mode));
            }
        }

        public IStandardLevel[] GetSortedLevels(SortMode mode)
        {
            lastSortMode = mode;

            GameplayMode gameplayMode = GetCurrentGameplayMode();

            switch (mode)
            {
                case SortMode.Author:
                    return (GetLevels(gameplayMode).OrderBy(x => x.songAuthorName).ThenBy(x => x.songName + " " + x.songSubName).ToArray());
                case SortMode.Default:
                    return (GetLevels(gameplayMode).ToArray());
                case SortMode.Newest:
                    return (SortLevelsByCreationTime(gameplayMode));
            }
            return null;
        }

        void SearchForLevels(string searchFor)
        {
            GameplayMode gameplayMode = GetCurrentGameplayMode();

            SetSongListLevels(GetLevels(gameplayMode).Where(x => $"{x.songName} {x.songSubName} {x.songAuthorName}".ToLower().IndexOf(searchFor) >= 0).ToArray());
        }

        IStandardLevel[] SortLevelsByCreationTime(GameplayMode gameplayMode)
        {
            DirectoryInfo customSongsFolder = new DirectoryInfo(Environment.CurrentDirectory.Replace('\\', '/') + "/CustomSongs/");

            List<string> sortedFolders = customSongsFolder.GetDirectories().OrderByDescending(x => x.CreationTime.Ticks).Select(x => x.FullName.Replace('\\','/')).ToList();
            
            List<string> sortedLevelIDs = new List<string>();

            foreach(string path in sortedFolders)
            {
                CustomLevel song = SongLoader.CustomLevels.FirstOrDefault(x => x.customSongInfo.path.StartsWith(path));
                if (song != null)
                {
                    sortedLevelIDs.Add(song.levelID);
                }
            }

            List<IStandardLevel> notSorted = GetLevels(gameplayMode).Select(x => (IStandardLevel)x).ToList();

            List<IStandardLevel> sortedLevels = new List<IStandardLevel>();

            foreach(string levelId in sortedLevelIDs)
            {
                IStandardLevel data = notSorted.FirstOrDefault(x => x.levelID == levelId);
                if (data != null)
                {
                    sortedLevels.Add(data);
                }
            }

            sortedLevels.AddRange(notSorted.Except(sortedLevels));

            return sortedLevels.ToArray();
        }


        void SetSongListLevels(IStandardLevel[] levels, string selectedLevelID = "")
        {
            StandardLevelListViewController songListViewController = ReflectionUtil.GetPrivateField<StandardLevelListViewController>(_songSelectionMasterViewController, "_levelListViewController");
            
            StandardLevelListTableView _songListTableView = songListViewController.GetComponentInChildren<StandardLevelListTableView>();

            ReflectionUtil.SetPrivateField(_songListTableView, "_levels", levels);
            ReflectionUtil.SetPrivateField(songListViewController, "_levels", levels);
            
            ReflectionUtil.GetPrivateField<TableView>(_songListTableView, "_tableView").ReloadData();

            if (!string.IsNullOrEmpty(selectedLevelID))
            {
                if (levels.Any(x => x.levelID == selectedLevelID))
                {
                    SelectAndScrollToLevel(_songListTableView, selectedLevelID);
                }
                else
                {
                    SelectAndScrollToLevel(_songListTableView, levels.FirstOrDefault().levelID);
                }
            }
        }

        void SelectAndScrollToLevel(StandardLevelListTableView table, string levelID)
        {
            int row = table.RowNumberForLevelID(levelID);
            TableView _tableView = table.GetComponentInChildren<TableView>();
            _tableView.SelectRow(row, true);
            _tableView.ScrollToRow(row, true);
        }

        public GameplayMode GetCurrentGameplayMode()
        {
            if (_songSelectionMasterViewController == null)
            {
                _songSelectionMasterViewController = Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().First();
            }
            return ReflectionUtil.GetPrivateField<GameplayMode>(_songSelectionMasterViewController, "_gameplayMode");
        }

        public List<IStandardLevel> GetLevels(GameplayMode mode)
        {
            if(lastPlaylist != null)
            {
                return lastPlaylist.songs.Where(x => (x.level != null) && (x.oneSaber == (mode == GameplayMode.SoloOneSaber))).Select(x => x.level).ToList();
            }
            else
            {
                return PluginUI._instance._levelCollections.GetLevels(mode).Cast<IStandardLevel>().ToList();
            }
        }

    }
}
