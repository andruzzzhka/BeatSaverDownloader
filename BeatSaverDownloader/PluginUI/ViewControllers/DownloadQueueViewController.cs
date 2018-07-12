using HMUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using VRUI;

namespace BeatSaverDownloader.PluginUI
{
    class DownloadQueueViewController : VRUIViewController, TableView.IDataSource
    {
        public BeatSaverMasterViewController _parentMasterViewController;

        public List<Song> _queuedSongs = new List<Song>();

        TextMeshProUGUI _titleText;

        TableView _queuedSongsTableView;
        SongListTableCell _songListTableCellInstance;

        protected override void DidActivate()
        {

            _songListTableCellInstance = Resources.FindObjectsOfTypeAll<SongListTableCell>().First(x => (x.name == "SongListTableCell"));

            if (_titleText == null)
            {
                _titleText = BeatSaberUI.CreateText(rectTransform, "DOWNLOAD QUEUE", new Vector2(0f, -6f));
                _titleText.alignment = TextAlignmentOptions.Top;
                _titleText.fontSize = 8;
            }

            if (_queuedSongsTableView == null)
            {
                _queuedSongsTableView = new GameObject().AddComponent<TableView>();

                _queuedSongsTableView.transform.SetParent(rectTransform, false);

                _queuedSongsTableView.dataSource = this;

                (_queuedSongsTableView.transform as RectTransform).anchorMin = new Vector2(0.3f, 0.5f);
                (_queuedSongsTableView.transform as RectTransform).anchorMax = new Vector2(0.7f, 0.5f);
                (_queuedSongsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
                (_queuedSongsTableView.transform as RectTransform).anchoredPosition = new Vector3(0f, -3f);

                _queuedSongsTableView.DidSelectRowEvent += _queuedSongsTableView_DidSelectRowEvent;
            }
            else
            {
                Refresh();
            }
        }

        private void _queuedSongsTableView_DidSelectRowEvent(TableView arg1, int arg2)
        {

        }

        protected override void DidDeactivate()
        {

        }

        public void EnqueueSong(Song song)
        {
            _queuedSongs.Add(song);
            song.songQueueState = SongQueueState.Queued;

            Refresh();


            StartCoroutine(DownloadSongFromQueue(song));
        }

        IEnumerator DownloadSongFromQueue(Song song)
        {
            yield return _parentMasterViewController.DownloadSongCoroutine(song);

            _queuedSongs.Remove(song);
            song.songQueueState = SongQueueState.Available;
            Refresh();
        }

        public void Refresh()
        {
            int removed = _queuedSongs.RemoveAll(x => x.songQueueState != SongQueueState.Downloading && x.songQueueState != SongQueueState.Queued);

            Logger.StaticLog($"Removed {removed} songs from queue");

            _queuedSongsTableView.ReloadData();
        }

        public float RowHeight()
        {
            return 10f;
        }

        public int NumberOfRows()
        {
            return _queuedSongs.Count;
        }

        public TableCell CellForRow(int row)
        {
            SongListTableCell _tableCell = Instantiate(_songListTableCellInstance);

            DownloadQueueTableCell _queueCell = _tableCell.gameObject.AddComponent<DownloadQueueTableCell>();

            _queueCell.Init(_queuedSongs[row]);
            
            return _queueCell;
        }
    }
}
