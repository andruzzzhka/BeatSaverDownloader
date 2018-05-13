using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace BeatSaverDownloader
{
    class BeatSaverSongListViewController : VRUIViewController, TableView.IDataSource
    {
        new BeatSaverMasterViewController _parentViewController;
        BeatSaverUI ui;

        public Button _pageUpButton;
        public Button _pageDownButton;
        Button _topButton;
        Button _newButton;
        Button _starButton;
        TextMeshProUGUI _sortByText;

        public TableView _songsTableView;
        SongListTableCell _songListTableCellInstance;

        int _currentPage = 0;

        public int _songsPerPage = 6;


        protected override void DidActivate()
        {
            ui = FindObjectOfType<BeatSaverUI>();
            _parentViewController = transform.parent.GetComponent<BeatSaverMasterViewController>();

            try
            {
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
                             if (!_parentViewController._loading)
                             {
                                 _parentViewController._loading = true;
                                 _parentViewController._loadingText.text = "Loading...";
                                 _currentPage -= 1;
                                 StartCoroutine(_parentViewController.GetSongs(_currentPage,_parentViewController._sortBy));
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
                        if (!_parentViewController._loading)
                        {
                            _parentViewController._loading = true;
                            _parentViewController._loadingText.text = "Loading...";
                            _currentPage += 1;
                            StartCoroutine(_parentViewController.GetSongs(_currentPage, _parentViewController._sortBy));
                        }

                    });
                }

                if(_sortByText == null)
                {
                    _sortByText = ui.CreateText(rectTransform,"SORT BY", new Vector2(-36f,-4.75f));
                    _sortByText.fontSize = 3.5f;
                    _sortByText.rectTransform.sizeDelta = new Vector2(10f,6f);
                }

                if (_topButton == null)
                {
                    _topButton = ui.CreateUIButton(rectTransform, "ApplyButton");
                    ui.SetButtonText(ref _topButton, "Downloads");
                    ui.SetButtonTextSize(ref _topButton, 3f);
                    (_topButton.transform as RectTransform).sizeDelta = new Vector2(20f, 6f);
                    (_topButton.transform as RectTransform).anchoredPosition = new Vector2(-30f, 73f);
                    _topButton.onClick.RemoveAllListeners();
                    _topButton.onClick.AddListener(delegate() {
                        if (!_parentViewController._loading)
                        {
                            _parentViewController._loading = true;
                            _parentViewController._loadingText.text = "Loading...";
                            _parentViewController._sortBy = "top";
                            StartCoroutine(_parentViewController.GetSongs(_currentPage, _parentViewController._sortBy));
                        }
                    });
                }

                if (_newButton == null)
                {
                    _newButton = ui.CreateUIButton(rectTransform, "ApplyButton");
                    ui.SetButtonText(ref _newButton, "Upload Time");
                    ui.SetButtonTextSize(ref _newButton, 3f);
                    (_newButton.transform as RectTransform).sizeDelta = new Vector2(20f, 6f);
                    (_newButton.transform as RectTransform).anchoredPosition = new Vector2(-10f, 73f);
                    _newButton.onClick.RemoveAllListeners();
                    _newButton.onClick.AddListener(delegate () {
                        if (!_parentViewController._loading)
                        {
                            _parentViewController._loading = true;
                            _parentViewController._loadingText.text = "Loading...";
                            _parentViewController._sortBy = "new";
                            StartCoroutine(_parentViewController.GetSongs(_currentPage, _parentViewController._sortBy));
                        }
                    });

                }

                if (_starButton == null)
                {
                    _starButton = ui.CreateUIButton(rectTransform, "ApplyButton");
                    ui.SetButtonText(ref _starButton, "Upvotes");
                    ui.SetButtonTextSize(ref _starButton, 3f);
                    (_starButton.transform as RectTransform).sizeDelta = new Vector2(20f, 6f);
                    (_starButton.transform as RectTransform).anchoredPosition = new Vector2(10f, 73f);
                    _starButton.onClick.RemoveAllListeners();
                    _starButton.onClick.AddListener(delegate () {
                        if (!_parentViewController._loading)
                        {
                            _parentViewController._loading = true;
                            _parentViewController._loadingText.text = "Loading...";
                            _parentViewController._sortBy = "star";
                            StartCoroutine(_parentViewController.GetSongs(_currentPage, _parentViewController._sortBy));
                        }
                    });
                }

                _songListTableCellInstance = Resources.FindObjectsOfTypeAll<SongListTableCell>().First(x => (x.name == "SongListTableCell"));

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

                    _songsTableView.DidSelectRowEvent += _songsTableView_DidSelectRowEvent;
                    
                }
                else
                {
                    _songsTableView.ReloadData();
                }
            }
            catch (Exception e)
            {
                Debug.Log("EXCEPTION IN DidActivate: " + e);
            }

        }

        protected override void DidDeactivate()
        {
            
            
            base.DidDeactivate();


        }

        public void RefreshScreen()
        {
            _songsTableView.ReloadData();
            
        }


        private void _songsTableView_DidSelectRowEvent(TableView sender, int row)
        {
            if (_parentViewController._loading)
            {
                if (_parentViewController._selectedRow != -1)
                {
                    _songsTableView.SelectRow(_parentViewController._selectedRow);
                }
                else
                {
                    _songsTableView.ClearSelection();
                }
            }
            else
            {
                _parentViewController.ShowDetails(row);
            }

        }

        public float RowHeight()
        {
            return 10f;
        }

        public int NumberOfRows()
        {
            return Math.Min(_songsPerPage, _parentViewController._songs.Count);
        }

        public TableCell CellForRow(int row)
        {
            SongListTableCell _tableCell = Instantiate(_songListTableCellInstance);

            _tableCell.songName = string.Format("{0}\n<size=80%>{1}</size>", HTML5Decode.HtmlDecode(_parentViewController._songs[row].songName), HTML5Decode.HtmlDecode(_parentViewController._songs[row].songSubName));
            _tableCell.author = HTML5Decode.HtmlDecode(_parentViewController._songs[row].authorName);
            StartCoroutine(_parentViewController.LoadSprite("https://beatsaver.com/img/" + _parentViewController._songs[row].id + "." + _parentViewController._songs[row].img, _tableCell));

            bool alreadyDownloaded = _parentViewController.IsSongAlreadyDownloaded(_parentViewController._songs[row]);

            if (alreadyDownloaded)
            {

                foreach(UnityEngine.UI.Image img in _tableCell.GetComponentsInChildren<UnityEngine.UI.Image>())
                {
                    img.color = new Color(1f,1f,1f,0.2f);
                }
                foreach (TextMeshProUGUI text in _tableCell.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    text.faceColor = new Color32(255,255,255,50);
                }



            }

            return _tableCell;
        }
    }
}
