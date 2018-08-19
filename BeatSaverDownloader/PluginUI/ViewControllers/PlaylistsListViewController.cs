using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaverDownloader.Misc;
using HMUI;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace BeatSaverDownloader.PluginUI.ViewControllers
{
    class PlaylistsListViewController : VRUIViewController, TableView.IDataSource
    {
        public event Action<Playlist> playlistSelected;

        Button _pageUpButton;
        Button _pageDownButton;

        StandardLevelListTableCell _playlistListTableCellInstance;
        TableView _playlistsTableView;

        List<Playlist> _availablePlaylists = new List<Playlist>();

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if(firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                _playlistListTableCellInstance = Resources.FindObjectsOfTypeAll<StandardLevelListTableCell>().First(x => (x.name == "StandardLevelListTableCell"));

                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -14f);
                _pageUpButton.interactable = true;
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    _playlistsTableView.PageScrollUp();
                });
                
                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 8f);
                _pageDownButton.interactable = true;
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    _playlistsTableView.PageScrollDown();
                });

                _playlistsTableView = new GameObject().AddComponent<TableView>();
                _playlistsTableView.transform.SetParent(rectTransform, false);

                Mask viewportMask = Instantiate(Resources.FindObjectsOfTypeAll<Mask>().First(), _playlistsTableView.transform, false);
                viewportMask.transform.DetachChildren();
                _playlistsTableView.GetComponentsInChildren<RectTransform>().First(x => x.name == "Content").transform.SetParent(viewportMask.rectTransform, false);

                (_playlistsTableView.transform as RectTransform).anchorMin = new Vector2(0.0f, 0.5f);
                (_playlistsTableView.transform as RectTransform).anchorMax = new Vector2(1.0f, 0.5f);
                (_playlistsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
                (_playlistsTableView.transform as RectTransform).position = new Vector3(0f, 0f, 2.4f);
                (_playlistsTableView.transform as RectTransform).anchoredPosition = new Vector3(0f, -3f);

                ReflectionUtil.SetPrivateField(_playlistsTableView, "_pageUpButton", _pageUpButton);
                ReflectionUtil.SetPrivateField(_playlistsTableView, "_pageDownButton", _pageDownButton);

                _playlistsTableView.didSelectRowEvent += _playlistsTableView_DidSelectRowEvent;
                _playlistsTableView.dataSource = this;
            }
        }

        private void _playlistsTableView_DidSelectRowEvent(TableView sender, int row)
        {
            playlistSelected?.Invoke(_availablePlaylists[row]);
        }

        public void SetPlaylists(List<Playlist> playlists)
        {
            _availablePlaylists = playlists;

            if(_playlistsTableView != null)
            {
                _playlistsTableView.ReloadData();
            }
        }

        public TableCell CellForRow(int row)
        {
            StandardLevelListTableCell cell = Instantiate(_playlistListTableCellInstance);

            Playlist playlist = _availablePlaylists[row];
            
            cell.coverImage = playlist.icon;
            cell.songName = playlist.playlistTitle;
            cell.author = playlist.playlistAuthor;

            return cell;
        }

        public int NumberOfRows()
        {
            return _availablePlaylists.Count();
        }

        public float RowHeight()
        {
            return 10f;
        }
    }
}
