using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaverDownloader.PluginUI
{
    class SongListUITweaks : MonoBehaviour
    {
        SearchKeyboardViewController _searchViewController;

        SongSelectionMasterViewController _songSelectionMasterViewController;
        SongListViewController _songListViewController;

        RectTransform _tableViewRectTransform;

        Button _sortByButton;
        Button _favButton;
        Button _allButton;
        Button _searchButton;

        public void SongListUIFound()
        {
            if (_songSelectionMasterViewController == null)
            {
                _songSelectionMasterViewController = Resources.FindObjectsOfTypeAll<SongSelectionMasterViewController>().First();
            }

            if (_songListViewController == null)
            {
                _songListViewController = ReflectionUtil.GetPrivateField<SongListViewController>(_songSelectionMasterViewController, "_songListViewController");
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
                        

            if (_sortByButton == null)
            {
                _sortByButton = BeatSaberUI.CreateUIButton((_tableViewRectTransform.parent as RectTransform), "ApplyButton");
                BeatSaberUI.SetButtonText(_sortByButton, "Sort by");
                BeatSaberUI.SetButtonTextSize(_sortByButton, 3f);
                (_sortByButton.transform as RectTransform).sizeDelta = new Vector2(30f, 6f);
                (_sortByButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 73f);
                _sortByButton.onClick.RemoveAllListeners();
                _sortByButton.onClick.AddListener(delegate ()
                {
                    SelectTopButtons(TopButtonsState.SortBy);
                });
            }

            if (_favButton == null)
            {
                _favButton = BeatSaberUI.CreateUIButton((_tableViewRectTransform.parent as RectTransform), "ApplyButton");
                BeatSaberUI.SetButtonText(_favButton, "Favorites");
                BeatSaberUI.SetButtonTextSize(_favButton, 3f);
                (_favButton.transform as RectTransform).sizeDelta = new Vector2(30f, 6f);
                (_favButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 73f);
                _favButton.onClick.RemoveAllListeners();
                _favButton.onClick.AddListener(delegate ()
                {
                    ShowLevels(true);
                    SelectTopButtons(TopButtonsState.Select);
                });
                _favButton.gameObject.SetActive(false);
            }

            if (_allButton == null)
            {
                _allButton = BeatSaberUI.CreateUIButton((_tableViewRectTransform.parent as RectTransform), "ApplyButton");
                BeatSaberUI.SetButtonText(_allButton, "All");
                BeatSaberUI.SetButtonTextSize(_allButton, 3f);
                (_allButton.transform as RectTransform).sizeDelta = new Vector2(30f, 6f);
                (_allButton.transform as RectTransform).anchoredPosition = new Vector2(-30f, 73f);
                _allButton.onClick.RemoveAllListeners();
                _allButton.onClick.AddListener(delegate ()
                {
                    ShowLevels(false);
                    SelectTopButtons(TopButtonsState.Select);
                });
                _allButton.gameObject.SetActive(false);

            }

            if (_searchButton == null)
            {
                _searchButton = BeatSaberUI.CreateUIButton((_tableViewRectTransform.parent as RectTransform), "ApplyButton");
                BeatSaberUI.SetButtonText(_searchButton, "Search");
                BeatSaberUI.SetButtonTextSize(_searchButton, 3f);
                (_searchButton.transform as RectTransform).sizeDelta = new Vector2(30f, 6f);
                (_searchButton.transform as RectTransform).anchoredPosition = new Vector2(-30f, 73f);
                _searchButton.onClick.RemoveAllListeners();
                _searchButton.onClick.AddListener(delegate ()
                {
                    ShowSearchKeyboard();
                    SelectTopButtons(TopButtonsState.Search);

                });
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
                        
                        _favButton.gameObject.SetActive(false);
                        _allButton.gameObject.SetActive(false);
                    }; break;
                case TopButtonsState.SortBy:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);
                        
                        _favButton.gameObject.SetActive(true);
                        _allButton.gameObject.SetActive(true);
                    }; break;
                case TopButtonsState.Search:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);
                        
                        _favButton.gameObject.SetActive(false);
                        _allButton.gameObject.SetActive(false);

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

        void ShowLevels(bool onlyFavorites)
        {
            GameplayMode gameplayMode = ReflectionUtil.GetPrivateField<GameplayMode>(_songSelectionMasterViewController, "_gameplayMode");

            if (onlyFavorites)
            {
                SetSongListLevels(PluginUI.GetLevels(gameplayMode).Where(x => PluginConfig.favoriteSongs.Contains(x.levelId)).ToArray());
            }
            else
            {
                SetSongListLevels(PluginUI.GetLevels(gameplayMode));
            }
        }

        void SearchForLevels(string searchFor)
        {
            GameplayMode gameplayMode = ReflectionUtil.GetPrivateField<GameplayMode>(_songSelectionMasterViewController, "_gameplayMode");

            SetSongListLevels(PluginUI.GetLevels(gameplayMode).Where(x => $"{x.songName} {x.songSubName} {x.authorName}".ToLower().Contains(searchFor)).ToArray());
        }

        void SetSongListLevels(LevelStaticData[] levels)
        {
            SongListViewController songListViewController = ReflectionUtil.GetPrivateField<SongListViewController>(_songSelectionMasterViewController, "_songListViewController");

            ReflectionUtil.SetPrivateField(songListViewController.GetComponentInChildren<SongListTableView>(), "_levels", levels);
            ReflectionUtil.SetPrivateField(songListViewController, "_levelsStaticData", levels);

            TableView _songListTableView = songListViewController.GetComponentInChildren<TableView>();
            _songListTableView.ReloadData();
        }

    }
}
