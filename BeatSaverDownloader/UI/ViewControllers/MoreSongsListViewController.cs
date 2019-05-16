using System;
using System.Collections.Generic;
using System.Linq;
using VRUI;
using UnityEngine.UI;
using HMUI;
using TMPro;
using UnityEngine;
using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI.FlowCoordinators;
using CustomUI.BeatSaber;
using CustomUI.Utilities;

namespace BeatSaverDownloader.UI.ViewControllers
{
    public enum TopButtonsState { Select, SortBy, Search, Playlists };

    class MoreSongsListViewController : VRUIViewController, TableView.IDataSource
    {
        public event Action<int> didSelectRow;

        public event Action searchButtonPressed;

        public event Action sortByTop;
        public event Action sortByNew;
        public event Action sortByPlays;

        public event Action pageUpPressed;
        public event Action pageDownPressed;

        public List<Song> songsList = new List<Song>();

        private Button _pageUpButton;
        private Button _pageDownButton;

        private Button _sortByButton;

        private Button _topButton;
        private Button _newButton;
        private Button _starButton;

        private Button _searchButton;

        private GameObject _loadingIndicator;

        private TableView _songsTableView;
        private LevelListTableCell _songListTableCellInstance;

        private int _lastSelectedRow;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {

            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0f);
                rectTransform.anchorMax = new Vector2(0.5f, 1f);
                rectTransform.sizeDelta = new Vector2(74f, 0f);
                rectTransform.pivot = new Vector2(0.4f, 0.5f);

                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -14.75f);
                (_pageUpButton.transform as RectTransform).sizeDelta = new Vector2(40f, 10f);
                _pageUpButton.interactable = true;
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    pageUpPressed?.Invoke();
                });
                _pageUpButton.interactable = false;

                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 9f);
                (_pageDownButton.transform as RectTransform).sizeDelta = new Vector2(40f, 10f);
                _pageDownButton.interactable = true;
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    pageDownPressed?.Invoke();
                });

                _sortByButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton", new Vector2(15f, 36.5f), new Vector2(30f, 6f), () => { SelectTopButtons(TopButtonsState.SortBy); }, "Sort by");
                _sortByButton.SetButtonTextSize(3f);

                _topButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton", new Vector2(-20f, 36.5f), new Vector2(20f, 6f), () =>
                {
                    sortByTop?.Invoke();
                    SelectTopButtons(TopButtonsState.Select);
                },
                "Downloads");

                _topButton.SetButtonTextSize(3f);
                _topButton.ToggleWordWrapping(false);
                _topButton.gameObject.SetActive(false);

                _newButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton", new Vector2(0f, 36.5f), new Vector2(20f, 6f), () =>
                {
                    sortByNew?.Invoke();
                    SelectTopButtons(TopButtonsState.Select);
                }, "Newest");

                _newButton.SetButtonTextSize(3f);
                _newButton.ToggleWordWrapping(false);
                _newButton.gameObject.SetActive(false);


                _starButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton", new Vector2(20f, 36.5f), new Vector2(20f, 6f), () =>
                {
                    sortByPlays?.Invoke();
                    SelectTopButtons(TopButtonsState.Select);
                }, "Plays");

                _starButton.SetButtonTextSize(3f);
                _starButton.ToggleWordWrapping(false);
                _starButton.gameObject.SetActive(false);

                _searchButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton", new Vector2(-15, 36.5f), new Vector2(30f, 6f), () =>
                {
                    searchButtonPressed?.Invoke();
                    SelectTopButtons(TopButtonsState.Search);
                }, "Search");
                _searchButton.SetButtonTextSize(3f);

                _loadingIndicator = BeatSaberUI.CreateLoadingSpinner(rectTransform);
                (_loadingIndicator.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
                (_loadingIndicator.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
                (_loadingIndicator.transform as RectTransform).anchoredPosition = new Vector2(0f, 0f);
                _loadingIndicator.SetActive(true);
                
                _songListTableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));

                RectTransform container = new GameObject("CustomListContainer", typeof(RectTransform)).transform as RectTransform;
                container.SetParent(rectTransform, false);
                container.sizeDelta = new Vector2(60f, 0f);

                _songsTableView = new GameObject("CustomTableView", typeof(RectTransform)).AddComponent<TableView>();
                _songsTableView.gameObject.AddComponent<RectMask2D>();
                _songsTableView.transform.SetParent(container, false);

                _songsTableView.SetPrivateField("_isInitialized", false);
                _songsTableView.SetPrivateField("_preallocatedCells", new TableView.CellsGroup[0]);
                _songsTableView.Init();

                (_songsTableView.transform as RectTransform).anchorMin = new Vector2(0f, 0f);
                (_songsTableView.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                (_songsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
                (_songsTableView.transform as RectTransform).anchoredPosition = new Vector2(0f, -3f);
                
                _songsTableView.dataSource = this;
                _songsTableView.didSelectCellWithIdxEvent += _songsTableView_DidSelectRowEvent;
            }
            else
            {
                _songsTableView.ReloadData();
            }
        }

        internal void Refresh()
        {
            _songsTableView.ReloadData();
            if(_lastSelectedRow > -1)
                _songsTableView.SelectCellWithIdx(_lastSelectedRow);
        }

        protected override void DidDeactivate(DeactivationType type)
        {
            _lastSelectedRow = -1;
        }

        public void SelectTopButtons(TopButtonsState _newState)
        {
            switch (_newState)
            {
                case TopButtonsState.Select:
                    {
                        _sortByButton.gameObject.SetActive(true);
                        _searchButton.gameObject.SetActive(true);

                        _topButton.gameObject.SetActive(false);
                        _newButton.gameObject.SetActive(false);
                        _starButton.gameObject.SetActive(false);
                    }; break;
                case TopButtonsState.SortBy:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);

                        _topButton.gameObject.SetActive(true);
                        _newButton.gameObject.SetActive(true);
                        _starButton.gameObject.SetActive(true);
                    }; break;
                case TopButtonsState.Search:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);

                        _topButton.gameObject.SetActive(false);
                        _newButton.gameObject.SetActive(false);
                        _starButton.gameObject.SetActive(false);

                    }; break;
            }
        }

        public void SetContent(List<Song> songs)
        {
            if(songs == null && songsList != null)
                songsList.Clear();
            else
                songsList = new List<Song>(songs);

            if (_songsTableView != null)
            {
                _songsTableView.ReloadData();
                _songsTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
                _lastSelectedRow = -1;
            }
        }

        public void SetLoadingState(bool isLoading)
        {
            if (_loadingIndicator != null)
            {
                _loadingIndicator.SetActive(isLoading);
            }
        }

        public void TogglePageUpDownButtons(bool pageUpEnabled, bool pageDownEnabled)
        {
            _pageUpButton.interactable = pageUpEnabled;
            _pageDownButton.interactable = pageDownEnabled;
        }

        private void _songsTableView_DidSelectRowEvent(TableView sender, int row)
        {
            _lastSelectedRow = row;
            didSelectRow?.Invoke(row);
        }

        public float CellSize()
        {
            return 10f;
        }

        public int NumberOfCells()
        {
            return Math.Min(songsList.Count, MoreSongsFlowCoordinator.songsPerPage);
        }

        public TableCell CellForIdx(int row)
        {
            LevelListTableCell _tableCell = Instantiate(_songListTableCellInstance);
            
            _tableCell.reuseIdentifier = "MoreSongsTableCell";
            _tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText").text = string.Format("{0} <size=80%>{1}</size>", songsList[row].songName, songsList[row].songSubName);
            _tableCell.GetPrivateField<TextMeshProUGUI>("_authorText").text = songsList[row].authorName;
            _tableCell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
            _tableCell.SetPrivateField("_beatmapCharacteristicImages", new UnityEngine.UI.Image[0]);
            _tableCell.SetPrivateField("_bought", true);

            foreach (var icon in _tableCell.GetComponentsInChildren<UnityEngine.UI.Image>().Where(x => x.name.StartsWith("LevelTypeIcon")))
            {
                Destroy(icon.gameObject);
            }

            StartCoroutine(LoadScripts.LoadSpriteCoroutine(songsList[row].coverUrl, (cover) => { _tableCell.GetPrivateField<UnityEngine.UI.RawImage>("_coverRawImage").texture = cover.texture; }));
            bool alreadyDownloaded = SongDownloader.Instance.IsSongDownloaded(songsList[row]);
            
            if (alreadyDownloaded)
            {
                foreach (UnityEngine.UI.Image img in _tableCell.GetComponentsInChildren<UnityEngine.UI.Image>())
                {
                    img.color = new Color(1f, 1f, 1f, 0.2f);
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
