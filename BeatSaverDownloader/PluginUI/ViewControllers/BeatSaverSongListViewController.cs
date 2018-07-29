using BeatSaverDownloader.Misc;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace BeatSaverDownloader.PluginUI
{
    enum TopButtonsState { Select, SortBy, Search };
    class BeatSaverSongListViewController : VRUIViewController, TableView.IDataSource
    {
        BeatSaverMasterViewController _parentMasterViewController;
        
        private Logger log = new Logger("BeatSaverDownloader");

        public Button _pageUpButton;
        public Button _pageDownButton;
        
        Button _sortByButton;

        Button _topButton;
        Button _newButton;
        Button _starButton;
        TextMeshProUGUI _sortByText;

        Button _searchButton;

        public GameObject _loadingIndicator;

        public TableView _songsTableView;
        StandardLevelListTableCell _songListTableCellInstance;

        public int _currentPage = 0;

        public int _songsPerPage = 6;


        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            _parentMasterViewController = GetComponentInParent<BeatSaverMasterViewController>();

            if (_pageUpButton == null)
            {
                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -14f);
                _pageUpButton.interactable = true;
                _pageUpButton.onClick.AddListener(delegate ()
                 {

                     if (_currentPage > 0)
                     {
                         if (!_parentMasterViewController._loading)
                         {
                             _parentMasterViewController._loading = true;
                             _currentPage -= 1;
                             _parentMasterViewController.GetPage(_currentPage);
                         }
                     }


                 });
            }

            if (_pageDownButton == null)
            {
                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 8f);
                _pageDownButton.interactable = true;
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    if (!_parentMasterViewController._loading)
                    {
                        _parentMasterViewController._loading = true;
                        _currentPage += 1;
                        _parentMasterViewController.GetPage(_currentPage);
                    }

                });
            }

            if (_sortByButton == null)
            {
                _sortByButton = BeatSaberUI.CreateUIButton(rectTransform, "SettingsButton");
                BeatSaberUI.SetButtonText(_sortByButton, "Sort by");
                BeatSaberUI.SetButtonTextSize(_sortByButton, 3f);
                (_sortByButton.transform as RectTransform).sizeDelta = new Vector2(30f, 6f);
                (_sortByButton.transform as RectTransform).anchoredPosition = new Vector2(-2f, 73f);
                _sortByButton.onClick.RemoveAllListeners();
                _sortByButton.onClick.AddListener(delegate ()
                {
                    SelectTopButtons(TopButtonsState.SortBy);
                });

            }

            if (_sortByText == null)
            {
                _sortByText = BeatSaberUI.CreateText(rectTransform, "SORT BY", new Vector2(-38f, -4.75f));
                _sortByText.fontSize = 3.5f;
                _sortByText.rectTransform.sizeDelta = new Vector2(10f, 6f);
                _sortByText.gameObject.SetActive(false);
            }

            if (_topButton == null)
            {
                _topButton = BeatSaberUI.CreateUIButton(rectTransform, "SettingsButton");
                BeatSaberUI.SetButtonText(_topButton, "Downloads");
                BeatSaberUI.SetButtonTextSize(_topButton, 3f);
                (_topButton.transform as RectTransform).sizeDelta = new Vector2(20f, 6f);
                (_topButton.transform as RectTransform).anchoredPosition = new Vector2(-42f, 73f);
                _topButton.onClick.RemoveAllListeners();
                _topButton.onClick.AddListener(delegate ()
                {
                    if (!_parentMasterViewController._loading)
                    {
                        _parentMasterViewController._loading = true;
                        _parentMasterViewController._sortBy = "top";
                        _currentPage = 0;
                        _parentMasterViewController.ClearSearchInput();
                        _parentMasterViewController.GetPage(_currentPage);
                        SelectTopButtons(TopButtonsState.Select);
                    }
                });
                _topButton.gameObject.SetActive(false);
            }

            if (_newButton == null)
            {
                _newButton = BeatSaberUI.CreateUIButton(rectTransform, "SettingsButton");
                BeatSaberUI.SetButtonText(_newButton, "Upload Time");
                BeatSaberUI.SetButtonTextSize(_newButton, 3f);
                (_newButton.transform as RectTransform).sizeDelta = new Vector2(20f, 6f);
                (_newButton.transform as RectTransform).anchoredPosition = new Vector2(-22f, 73f);
                _newButton.onClick.RemoveAllListeners();
                _newButton.onClick.AddListener(delegate ()
                {
                    if (!_parentMasterViewController._loading)
                    {
                        _parentMasterViewController._loading = true;
                        _parentMasterViewController._sortBy = "new";
                        _currentPage = 0;
                        _parentMasterViewController.ClearSearchInput();
                        _parentMasterViewController.GetPage(_currentPage);
                        SelectTopButtons(TopButtonsState.Select);
                    }
                });
                _newButton.gameObject.SetActive(false);

            }

            if (_starButton == null)
            {
                _starButton = BeatSaberUI.CreateUIButton(rectTransform, "SettingsButton");
                BeatSaberUI.SetButtonText(_starButton, "Plays");
                BeatSaberUI.SetButtonTextSize(_starButton, 3f);
                (_starButton.transform as RectTransform).sizeDelta = new Vector2(20f, 6f);
                (_starButton.transform as RectTransform).anchoredPosition = new Vector2(-2f, 73f);
                _starButton.onClick.RemoveAllListeners();
                _starButton.onClick.AddListener(delegate ()
                {
                    if (!_parentMasterViewController._loading)
                    {
                        _parentMasterViewController._sortBy = "plays";
                        _currentPage = 0;
                        _parentMasterViewController.ClearSearchInput();
                        _parentMasterViewController.GetPage(_currentPage);
                        SelectTopButtons(TopButtonsState.Select);
                    }
                });
                _starButton.gameObject.SetActive(false);
            }

            if (_searchButton == null)
            {
                _searchButton = BeatSaberUI.CreateUIButton(rectTransform, "SettingsButton");
                BeatSaberUI.SetButtonText(_searchButton, "Search");
                BeatSaberUI.SetButtonTextSize(_searchButton, 3f);
                (_searchButton.transform as RectTransform).sizeDelta = new Vector2(30f, 6f);
                (_searchButton.transform as RectTransform).anchoredPosition = new Vector2(-32f, 73f);
                _searchButton.onClick.RemoveAllListeners();
                _searchButton.onClick.AddListener(delegate ()
                {
                    _parentMasterViewController.ShowSearchKeyboard();
                    SelectTopButtons(TopButtonsState.Search);
                    _currentPage = 0;

                });
            }


            if (_loadingIndicator == null)
            {
                try
                {
                    _loadingIndicator = BeatSaberUI.CreateLoadingIndicator(rectTransform);
                    (_loadingIndicator.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
                    (_loadingIndicator.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
                    (_loadingIndicator.transform as RectTransform).anchoredPosition = new Vector2(0f, 0f);
                    _loadingIndicator.SetActive(true);

                }
                catch (Exception e)
                {
                    log.Exception("EXCEPTION: " + e);
                }
            }


            _songListTableCellInstance = Resources.FindObjectsOfTypeAll<StandardLevelListTableCell>().First(x => (x.name == "StandardLevelListTableCell"));

            if (_songsTableView == null)
            {
                _songsTableView = new GameObject().AddComponent<TableView>();

                _songsTableView.transform.SetParent(rectTransform, false);

                _songsTableView.dataSource = this;

                (_songsTableView.transform as RectTransform).anchorMin = new Vector2(0f, 0.5f);
                (_songsTableView.transform as RectTransform).anchorMax = new Vector2(1f, 0.5f);
                (_songsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
                (_songsTableView.transform as RectTransform).position = new Vector3(0f, 0f, 2.4f);
                (_songsTableView.transform as RectTransform).anchoredPosition = new Vector3(0f, -3f);

                _songsTableView.didSelectRowEvent += _songsTableView_DidSelectRowEvent;

            }
            else
            {
                _songsTableView.ReloadData();
            }




        }

        protected override void DidDeactivate(DeactivationType type)
        {            
        }

        public void SelectTopButtons(TopButtonsState _newState)
        {
            switch (_newState)
            {
                case TopButtonsState.Select:
                    {
                        _sortByButton.gameObject.SetActive(true);
                        _searchButton.gameObject.SetActive(true);

                        _sortByText.gameObject.SetActive(false);
                        _topButton.gameObject.SetActive(false);
                        _newButton.gameObject.SetActive(false);
                        _starButton.gameObject.SetActive(false);
                    }; break;
                case TopButtonsState.SortBy:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);

                        _sortByText.gameObject.SetActive(true);
                        _topButton.gameObject.SetActive(true);
                        _newButton.gameObject.SetActive(true);
                        _starButton.gameObject.SetActive(true);
                    }; break;
                case TopButtonsState.Search:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);

                        _sortByText.gameObject.SetActive(false);
                        _topButton.gameObject.SetActive(false);
                        _newButton.gameObject.SetActive(false);
                        _starButton.gameObject.SetActive(false);
                        
                    }; break;


            }

        }

        private void _songsTableView_DidSelectRowEvent(TableView sender, int row)
        {
            if (_parentMasterViewController._loading)
            {
                if (_parentMasterViewController._selectedRow != -1)
                {
                    _songsTableView.SelectRow(_parentMasterViewController._selectedRow);
                }
                else
                {
                    _songsTableView.ClearSelection();
                }
            }
            else
            {
                _parentMasterViewController.ShowDetails(row);
            }

        }

        public float RowHeight()
        {
            return 10f;
        }

        public int NumberOfRows()
        {
            return Math.Min(_songsPerPage, _parentMasterViewController._songs.Count);
        }

        public TableCell CellForRow(int row)
        {
            StandardLevelListTableCell _tableCell = Instantiate(_songListTableCellInstance);

            _tableCell.songName = string.Format("{0}\n<size=80%>{1}</size>", _parentMasterViewController._songs[row].songName, _parentMasterViewController._songs[row].songSubName);
            _tableCell.author = _parentMasterViewController._songs[row].authorName;
            StartCoroutine(LoadScripts.LoadSprite(_parentMasterViewController._songs[row].coverUrl, _tableCell));
            bool alreadyDownloaded = _parentMasterViewController.IsSongAlreadyDownloaded(_parentMasterViewController._songs[row]);

            if (alreadyDownloaded)
            {

                foreach(UnityEngine.UI.Image img in _tableCell.GetComponentsInChildren<UnityEngine.UI.Image>())
                {
                    img.color = new Color(1f,1f,1f,0.2f);
                }
                foreach (TextMeshProUGUI text in _tableCell.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    text.faceColor = new Color(1f, 1f, 1f, 0.2f);
                }



            }

            return _tableCell;
        }
    }
}
