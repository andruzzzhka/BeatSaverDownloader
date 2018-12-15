using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VRUI;
using UnityEngine.UI;
using BeatSaverDownloader.Misc;
using CustomUI.Utilities;

namespace BeatSaverDownloader.UI.ViewControllers
{
    class PlaylistListViewController : VRUIViewController, TableView.IDataSource
    {
        public event Action<Playlist> didSelectRow;

        public List<Playlist> playlistList = new List<Playlist>();

        private Button _pageUpButton;
        private Button _pageDownButton;
        
        private TableView _songsTableView;
        private LevelListTableCell _songListTableCellInstance;

        private int _lastSelectedRow;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {


            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                rectTransform.anchorMin = new Vector2(0.3f, 0f);
                rectTransform.anchorMax = new Vector2(0.7f, 1f);

                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -14f);
                (_pageUpButton.transform as RectTransform).sizeDelta = new Vector2(40f, 10f);
                _pageUpButton.interactable = true;
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    _songsTableView.PageScrollUp();
                });

                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 11f);
                (_pageDownButton.transform as RectTransform).sizeDelta = new Vector2(40f, 10f);
                _pageDownButton.interactable = true;
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    _songsTableView.PageScrollDown();
                });

                _songListTableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));
                _songsTableView = new GameObject().AddComponent<TableView>();
                _songsTableView.transform.SetParent(rectTransform, false);

                _songsTableView.SetPrivateField("_isInitialized", false);
                _songsTableView.SetPrivateField("_preallocatedCells", new TableView.CellsGroup[0]);
                _songsTableView.Init();

                RectMask2D viewportMask = Instantiate(Resources.FindObjectsOfTypeAll<RectMask2D>().First(), _songsTableView.transform, false);
                viewportMask.transform.DetachChildren();
                _songsTableView.GetComponentsInChildren<RectTransform>().First(x => x.name == "Content").transform.SetParent(viewportMask.rectTransform, false);

                (_songsTableView.transform as RectTransform).anchorMin = new Vector2(0f, 0.5f);
                (_songsTableView.transform as RectTransform).anchorMax = new Vector2(1f, 0.5f);
                (_songsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
                (_songsTableView.transform as RectTransform).position = new Vector3(0f, 0f, 2.4f);
                (_songsTableView.transform as RectTransform).anchoredPosition = new Vector3(0f, -3f);

                _songsTableView.SetPrivateField("_pageUpButton", _pageUpButton);
                _songsTableView.SetPrivateField("_pageDownButton", _pageDownButton);

                _songsTableView.dataSource = this;
                _songsTableView.ScrollToRow(0, false);
                _lastSelectedRow = -1;
                _songsTableView.didSelectRowEvent += _songsTableView_DidSelectRowEvent;
            }
            else
            {
                _songsTableView.ReloadData();
                _songsTableView.ScrollToRow(0, false);
                _lastSelectedRow = -1;
            }
        }

        internal void Refresh()
        {
            _songsTableView.ReloadData();
            if (_lastSelectedRow > -1)
                _songsTableView.SelectRow(_lastSelectedRow);
        }

        protected override void DidDeactivate(DeactivationType type)
        {
            _lastSelectedRow = -1;
        }
        
        public void SetContent(List<Playlist> playlists)
        {
            if (playlists == null && playlistList != null)
                playlistList.Clear();
            else
                playlistList = new List<Playlist>(playlists);

            if (_songsTableView != null)
            {
                _songsTableView.ReloadData();
                _songsTableView.ScrollToRow(0, false);
            }
        }

        private void _songsTableView_DidSelectRowEvent(TableView sender, int row)
        {
            _lastSelectedRow = row;
            didSelectRow?.Invoke(playlistList[row]);
        }

        public float RowHeight()
        {
            return 10f;
        }

        public int NumberOfRows()
        {
            return playlistList.Count;
        }

        public TableCell CellForRow(int row)
        {
            LevelListTableCell _tableCell = Instantiate(_songListTableCellInstance);

            _tableCell.reuseIdentifier = "PlaylistTableCell";
            _tableCell.songName = playlistList[row].playlistTitle;
            _tableCell.author = playlistList[row].playlistAuthor;
            _tableCell.coverImage = playlistList[row].icon;

            return _tableCell;
        }
    }
}
