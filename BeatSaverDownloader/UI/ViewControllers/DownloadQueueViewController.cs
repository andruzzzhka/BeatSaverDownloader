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
    class DownloadQueueViewController : VRUIViewController, TableView.IDataSource
    {
        public List<Song> queuedSongs = new List<Song>();

        TextMeshProUGUI _titleText;

        Button _abortButton;
        TableView _queuedSongsTableView;
        LevelListTableCell _songListTableCellInstance;
        private Button _pageUpButton;
        private Button _pageDownButton;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                SongDownloader.Instance.songDownloaded -= SongDownloaded;
                SongDownloader.Instance.songDownloaded += SongDownloaded;
                _songListTableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));

                RectTransform viewControllersContainer = FindObjectsOfType<RectTransform>().First(x => x.name == "ViewControllers");

                var headerPanelRectTransform = Instantiate(viewControllersContainer.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "HeaderPanel" && x.parent.name == "PlayerSettingsViewController"), rectTransform);
                headerPanelRectTransform.gameObject.SetActive(true);

                _titleText = headerPanelRectTransform.GetComponentInChildren<TextMeshProUGUI>();
                _titleText.text = "DOWNLOAD QUEUE";

                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -18f);
                (_pageUpButton.transform as RectTransform).sizeDelta = new Vector2(40f, 10f);
                _pageUpButton.interactable = true;
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    _queuedSongsTableView.PageScrollUp();
                });

                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 9f);
                (_pageDownButton.transform as RectTransform).sizeDelta = new Vector2(40f, 10f);
                _pageDownButton.interactable = true;
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    _queuedSongsTableView.PageScrollDown();
                });

                RectTransform container = new GameObject("CustomListContainer", typeof(RectTransform)).transform as RectTransform;
                container.SetParent(rectTransform, false);
                container.anchorMin = new Vector2(0.2f, 0.5f);
                container.anchorMax = new Vector2(0.8f, 0.5f);
                container.sizeDelta = new Vector2(0f, 0f);
                container.anchoredPosition = new Vector2(0f, -4f);

                _queuedSongsTableView = new GameObject("CustomTableView", typeof(RectTransform)).AddComponent<TableView>();
                _queuedSongsTableView.gameObject.AddComponent<RectMask2D>();
                _queuedSongsTableView.transform.SetParent(container, false);

                _queuedSongsTableView.SetPrivateField("_isInitialized", false);
                _queuedSongsTableView.SetPrivateField("_preallocatedCells", new TableView.CellsGroup[0]);
                _queuedSongsTableView.Init();

                (_queuedSongsTableView.transform as RectTransform).anchorMin = new Vector2(0f, 0f);
                (_queuedSongsTableView.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                (_queuedSongsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 54f);
                (_queuedSongsTableView.transform as RectTransform).anchoredPosition = new Vector2(0f, 0f);
                
                ReflectionUtil.SetPrivateField(_queuedSongsTableView, "_pageUpButton", _pageUpButton);
                ReflectionUtil.SetPrivateField(_queuedSongsTableView, "_pageDownButton", _pageDownButton);

                _queuedSongsTableView.selectionType = TableViewSelectionType.None;
                _queuedSongsTableView.dataSource = this;

                _abortButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton", new Vector2(36f, -30f), new Vector2(20f, 10f), AbortDownloads, "Abort All");
                _abortButton.ToggleWordWrapping(false);
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
        }

        public void DownloadAllSongsFromQueue()
        {
            Plugin.log.Info("Downloading all songs from queue...");
            
            for(int i = 0; i < Math.Min(PluginConfig.maxSimultaneousDownloads, queuedSongs.Count); i++)
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

            _queuedSongsTableView.ReloadData();
            _queuedSongsTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, true);

            if (queuedSongs.Count(x => x.songQueueState == SongQueueState.Downloading || x.songQueueState == SongQueueState.Queued) == 0)
            {
                Plugin.log.Info("All songs downloaded!");
                SongCore.Loader.Instance.RefreshSongs(false);
            }

            if (queuedSongs.Count(x => x.songQueueState == SongQueueState.Downloading) < PluginConfig.maxSimultaneousDownloads && queuedSongs.Any(x => x.songQueueState == SongQueueState.Queued))
                StartCoroutine(DownloadSong(queuedSongs.First(x => x.songQueueState == SongQueueState.Queued)));
        }

        public float CellSize()
        {
            return 8.5f;
        }

        public int NumberOfCells()
        {
            return queuedSongs.Count;
        }

        public TableCell CellForIdx(int row)
        {
            LevelListTableCell _tableCell = Instantiate(_songListTableCellInstance);

            DownloadQueueTableCell _queueCell = _tableCell.gameObject.AddComponent<DownloadQueueTableCell>();

            _queueCell.Init(queuedSongs[row]);

            return _queueCell;
        }
    }
}
