using BeatSaverDownloader.Misc;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using VRUI;
using UnityEngine.UI;
using UnityEngine;
using BeatSaverDownloader.UI.UIElements;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using System.Collections;

namespace BeatSaverDownloader.UI.ViewControllers
{
    class DownloadQueueViewController : CustomListViewController, TableView.IDataSource
    {
        public List<Song> queuedSongs = new List<Song>();

        TextMeshProUGUI _titleText;

        Button _abortButton;
        LevelListTableCell _songListTableCellInstance;
        private bool initialized = false;
        public override void __Activate(ActivationType activationType)
        {
            base.__Activate(activationType);
            //
            if (!initialized && activationType == ActivationType.AddedToHierarchy)
            {
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0, 25);
                SongDownloader.Instance.songDownloaded -= SongDownloaded;
                SongDownloader.Instance.songDownloaded += SongDownloaded;
                _songListTableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));

                RectTransform viewControllersContainer = FindObjectsOfType<RectTransform>().First(x => x.name == "ViewControllers");

                var headerPanelRectTransform = Instantiate(viewControllersContainer.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "HeaderPanel" && x.parent.name == "PlayerSettingsViewController"), rectTransform);
                headerPanelRectTransform.gameObject.SetActive(true);

                _titleText = headerPanelRectTransform.GetComponentInChildren<TextMeshProUGUI>();
                _titleText.text = "DOWNLOAD QUEUE";

                _customListTableView.selectionType = TableViewSelectionType.None;

                _abortButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton", new Vector2(36f, -30f), new Vector2(20f, 10f), AbortDownloads, "Abort All");
                _abortButton.ToggleWordWrapping(false);
                initialized = true;
            }
            else
            {
                _titleText.text = "DOWNLOAD QUEUE";
            }
        }

        public void AbortDownloads()
        {
            if (queuedSongs.Count > 0)
            {
                Plugin.log.Info("Cancelling downloads...");
                foreach (Song song in queuedSongs.Where(x => x.songQueueState == SongQueueState.Downloading || x.songQueueState == SongQueueState.Queued))
                {
                    song.songQueueState = SongQueueState.Error;
                }
                queuedSongs.Clear();
                SongCore.Loader.Instance.RefreshSongs(false);
                Refresh();
            }
        }

        protected override void DidDeactivate(DeactivationType type)
        {
            SongDownloader.Instance.songDownloaded -= SongDownloaded;
        }

        public void EnqueueSong(Song song, bool startDownload = true)
        {

            queuedSongs.Add(song);
            song.songQueueState = SongQueueState.Queued;
            if (startDownload && queuedSongs.Count(x => x.songQueueState == SongQueueState.Downloading) < PluginConfig.maxSimultaneousDownloads)
            {
                StartCoroutine(DownloadSong(song));
                Refresh();
            }
            else
                RefreshVisuals();

        }

        public void EnqueueSongAtStart(Song song, bool startDownload = true)
        {
            queuedSongs.Insert(0, song);
            song.songQueueState = SongQueueState.Queued;
            if (startDownload && queuedSongs.Count(x => x.songQueueState == SongQueueState.Downloading) < PluginConfig.maxSimultaneousDownloads)
            {
                StartCoroutine(DownloadSong(song));
                Refresh();
            }
            else
                RefreshVisuals();
        }
        public void DownloadAllSongsFromQueue()
        {
            Plugin.log.Info("Downloading all songs from queue...");

            for (int i = 0; i < Math.Min(PluginConfig.maxSimultaneousDownloads, queuedSongs.Count); i++)
            {
                StartCoroutine(DownloadSong(queuedSongs[i]));
            }
            Refresh();
        }

        IEnumerator DownloadSong(Song song)
        {
            yield return SongDownloader.Instance.DownloadSongCoroutine(song);
            Refresh();
        }

        private void SongDownloaded(Song obj)
        {
            Refresh();
            if (queuedSongs.Count(x => x.songQueueState == SongQueueState.Downloading) < PluginConfig.maxSimultaneousDownloads && queuedSongs.Any(x => x.songQueueState == SongQueueState.Queued))
            {
                StartCoroutine(DownloadSong(queuedSongs.First(x => x.songQueueState == SongQueueState.Queued)));
            }
        }

        public void Refresh()
        {
            int removed = queuedSongs.RemoveAll(x => x.songQueueState == SongQueueState.Downloaded || x.songQueueState == SongQueueState.Error);

            Plugin.log.Info($"Removed {removed} songs from queue");

            _customListTableView.ReloadData();
            _customListTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, true);

            if (queuedSongs.Count(x => x.songQueueState == SongQueueState.Downloading || x.songQueueState == SongQueueState.Queued) == 0)
            {
                Plugin.log.Info("All songs downloaded!");
                SongCore.Loader.Instance.RefreshSongs(false);
            }

            if (queuedSongs.Count(x => x.songQueueState == SongQueueState.Downloading) < PluginConfig.maxSimultaneousDownloads && queuedSongs.Any(x => x.songQueueState == SongQueueState.Queued))
                StartCoroutine(DownloadSong(queuedSongs.First(x => x.songQueueState == SongQueueState.Queued)));
        }

        public void RefreshVisuals()
        {
            int removed = queuedSongs.RemoveAll(x => x.songQueueState == SongQueueState.Downloaded || x.songQueueState == SongQueueState.Error);
            if (removed > 0)
                Plugin.log.Info($"Removed {removed} songs from queue");

            _customListTableView.ReloadData();
            _customListTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, true);

            if (queuedSongs.Count(x => x.songQueueState == SongQueueState.Downloading || x.songQueueState == SongQueueState.Queued) == 0)
            {
                Plugin.log.Info("All songs downloaded!");
                SongCore.Loader.Instance.RefreshSongs(false);
            }

    //        if (queuedSongs.Count(x => x.songQueueState == SongQueueState.Downloading) < PluginConfig.maxSimultaneousDownloads && queuedSongs.Any(x => x.songQueueState == SongQueueState.Queued))
    //            StartCoroutine(DownloadSong(queuedSongs.First(x => x.songQueueState == SongQueueState.Queued)));
        }
        public override float CellSize()
        {
            return 8.5f;
        }

        public override int NumberOfCells()
        {
            return queuedSongs.Count;
        }

        public override TableCell CellForIdx(int row)
        {
            LevelListTableCell _tableCell = GetTableCell(false);

            DownloadQueueTableCell _queueCell = _tableCell.gameObject.AddComponent<DownloadQueueTableCell>();

            _queueCell.Init(queuedSongs[row]);

            return _queueCell;
        }
    }
}
