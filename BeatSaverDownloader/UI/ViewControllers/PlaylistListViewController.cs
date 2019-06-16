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
using TMPro;
using CustomUI.BeatSaber;
namespace BeatSaverDownloader.UI.ViewControllers
{
    class PlaylistListViewController : CustomListViewController, TableView.IDataSource
    {
        public event Action<Playlist> didSelectRow;

        public List<Playlist> playlistList = new List<Playlist>();

        public bool highlightDownloadedPlaylists = false;
        
        private LevelListTableCell _songListTableCellInstance;

        private int _lastSelectedRow;

        private bool initialized = false;
        public override void __Activate(ActivationType activationType)
        {
            base.__Activate(activationType);
            //
            if (!initialized && activationType == ActivationType.AddedToHierarchy)
            {

                _songListTableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));

                RectTransform container = new GameObject("CustomListContainer", typeof(RectTransform)).transform as RectTransform;
                container.SetParent(rectTransform, false);
                container.anchorMin = new Vector2(0f, 0.5f);
                container.anchorMax = new Vector2(1f, 0.5f);
                container.sizeDelta = new Vector2(0f, 0f);
                container.anchoredPosition = new Vector2(0f, 0f);

                _customListTableView.didSelectCellWithIdxEvent += _songsTableView_DidSelectRowEvent;
                initialized = true;
            }
            else
            {
                _customListTableView.ReloadData();
                _customListTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
                _lastSelectedRow = -1;
            }
        }

        internal void Refresh()
        {
            _customListTableView.RefreshTable();
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

            if (_customListTableView != null)
            {
                _customListTableView.ReloadData();
                _customListTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
            }
        }

        private void _songsTableView_DidSelectRowEvent(TableView sender, int row)
        {
            _lastSelectedRow = row;
            didSelectRow?.Invoke(playlistList[row]);
        }

        public override float CellSize()
        {
            return 8.5f;
        }

        public override int NumberOfCells()
        {
            return playlistList.Count;
        }

        public override TableCell CellForIdx(int row)
        {
            LevelListTableCell _tableCell = GetTableCell(false);

            _tableCell.reuseIdentifier = "PlaylistTableCell";
            var songNameText = _tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText");
            songNameText.text = playlistList[row].playlistTitle;
            songNameText.overflowMode = TextOverflowModes.Overflow;
            _tableCell.GetPrivateField<TextMeshProUGUI>("_authorText").text = playlistList[row].playlistAuthor;
            _tableCell.GetPrivateField<UnityEngine.UI.RawImage>("_coverRawImage").texture = playlistList[row].icon.texture;

            _tableCell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
            _tableCell.SetPrivateField("_beatmapCharacteristicImages", new UnityEngine.UI.Image[0]);
            _tableCell.SetPrivateField("_bought", true);

            foreach (var icon in _tableCell.GetComponentsInChildren<UnityEngine.UI.Image>().Where(x => x.name.StartsWith("LevelTypeIcon")))
            {
                Destroy(icon.gameObject);
            }

            if (highlightDownloadedPlaylists)
            {
                if (PlaylistsCollection.loadedPlaylists.Any(x => x.PlaylistEqual(playlistList[row])))
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
            }

            return _tableCell;
        }
    }
}
