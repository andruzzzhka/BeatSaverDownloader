using BeatSaverDownloader.Misc;
using SimpleJSON;
using SongLoaderPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using VRUI;

namespace BeatSaverDownloader.PluginUI.ViewControllers
{
    class PlaylistNavigationController : VRUINavigationController
    {
        public event Action<Playlist> finished;

        public DownloadQueueViewController downloadQueueViewController;

        PlaylistsListViewController _playlistsList;
        PlaylistDetailViewController _playlistDetail;

        Button _backButton;

        Playlist _selectedPlaylist;

        bool _downloadingPlaylist;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if(firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                _backButton = BeatSaberUI.CreateBackButton(rectTransform);

                _backButton.onClick.AddListener(delegate ()
                {
                    if (!downloadQueueViewController._queuedSongs.Any(x => x.songQueueState == SongQueueState.Queued || x.songQueueState == SongQueueState.Downloading) && !_downloadingPlaylist)
                    {
                        DismissModalViewController(null, false);
                        finished?.Invoke(null);
                    }
                });

                _playlistsList = BeatSaberUI.CreateViewController<PlaylistsListViewController>();
                _playlistsList.rectTransform.anchorMin = new Vector2(0.3f, 0f);
                _playlistsList.rectTransform.anchorMax = new Vector2(0.7f, 1f);

                _playlistsList.SetPlaylists(PluginConfig.playlists);

                PushViewController(_playlistsList, true);

                _playlistsList.playlistSelected += ShowDetails;
            }
            else
            {
                _playlistsList.SetPlaylists(PluginConfig.playlists);
                PushViewController(_playlistsList, true);
            }
        }

        public void ShowDetails(Playlist playlist)
        {
            _selectedPlaylist = playlist;

            if (_playlistDetail == null)
            {
                GameObject _playlistDetailGameObject = Instantiate(Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First(), rectTransform, false).gameObject;
                Destroy(_playlistDetailGameObject.GetComponent<StandardLevelDetailViewController>());
                _playlistDetail = _playlistDetailGameObject.AddComponent<PlaylistDetailViewController>();
                _playlistDetail.selectPressed += SelectPressed;
                _playlistDetail.downloadPressed += DownloadPressed;

                PushViewController(_playlistDetail, false);
                _playlistDetail.UpdateContent(playlist);
            }
            else
            {
                if (_viewControllers.IndexOf(_playlistDetail) < 0)
                {
                    PushViewController(_playlistDetail, true);
                    _playlistDetail.UpdateContent(playlist);
                    _playlistDetail.UpdateButtons(!_downloadingPlaylist, !_downloadingPlaylist);
                }
                else
                {
                    _playlistDetail.UpdateContent(playlist);
                    _playlistDetail.UpdateButtons(!_downloadingPlaylist, !_downloadingPlaylist);
                }

            }
        }

        private void DownloadPressed()
        {
            if (!_downloadingPlaylist)
                StartCoroutine(DownloadPlaylist(_selectedPlaylist));
            else
                Logger.Log("Already downloading playlist!");
        }

        public IEnumerator DownloadPlaylist(Playlist playlist)
        {
            SongListUITweaks.MatchSongsForPlaylist(_selectedPlaylist);
            List<PlaylistSong> playlistSongsToDownload = _selectedPlaylist.songs.Where(x => x.level == null).ToList();

            List<Song> beatSaverSongs = new List<Song>();

            downloadQueueViewController.AbortDownloads();
            _downloadingPlaylist = true;
            _playlistDetail.UpdateButtons(!_downloadingPlaylist, !_downloadingPlaylist);

            foreach (var item in playlistSongsToDownload)
            {
                Logger.Log("Obtaining hash and url for " + item.key + ": " + item.songName);
                yield return GetSongByPlaylistSong(item);

                Logger.Log("Song is null: "+(_lastRequestedSong==null)+"\n Level is downloaded: "+(SongLoader.CustomLevels.Any(x => x.levelID.Substring(0, 32) == _lastRequestedSong.hash.ToUpper())));

                if (_lastRequestedSong != null && !SongLoader.CustomLevels.Any(x => x.levelID.Substring(0, 32) == _lastRequestedSong.hash.ToUpper()))
                {
                    Logger.Log(item.key + ": " + item.songName+"  -  "+ _lastRequestedSong.hash);
                    beatSaverSongs.Add(_lastRequestedSong);
                    downloadQueueViewController.EnqueueSong(_lastRequestedSong, false);
                }
            }

            Logger.Log($"Need to download {beatSaverSongs.Count(x=>x.songQueueState == SongQueueState.Queued)} songs:");

            if(!beatSaverSongs.Any(x => x.songQueueState == SongQueueState.Queued))
            {
                _downloadingPlaylist = false;
                _playlistDetail.UpdateButtons(!_downloadingPlaylist, !_downloadingPlaylist);
            }

            foreach (var item in beatSaverSongs.Where(x => x.songQueueState == SongQueueState.Queued))
            {
                Logger.Log(item.songName);
            }

            downloadQueueViewController.allSongsDownloaded -= AllSongsDownloaded;
            downloadQueueViewController.allSongsDownloaded += AllSongsDownloaded;

            downloadQueueViewController.DownloadAllSongsFromQueue();

        }

        private void AllSongsDownloaded()
        {
            SongLoader.Instance.RefreshSongs(false);

            foreach(Playlist playlist in PluginConfig.playlists)
                SongListUITweaks.MatchSongsForPlaylist(playlist);

            _downloadingPlaylist = false;
            _playlistDetail.UpdateButtons(!_downloadingPlaylist, !_downloadingPlaylist);
        }

        private Song _lastRequestedSong;

        public IEnumerator GetSongByPlaylistSong(PlaylistSong song)
        {
            UnityWebRequest wwwId = null;
            try
            {
                wwwId = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/songs/detail/" + song.key);
                wwwId.timeout = 10;
            }
            catch
            {
                _lastRequestedSong = new Song() { songName = song.songName, songQueueState = SongQueueState.Error, downloadingProgress = 1f, hash = "" };

                yield break;
            }

            yield return wwwId.SendWebRequest();


            if (wwwId.isNetworkError || wwwId.isHttpError)
            {
                Logger.Error(wwwId.error);
                Logger.Error($"Song {song.songName} doesn't exist on BeatSaver!");
                _lastRequestedSong = new Song() { songName = song.songName, songQueueState = SongQueueState.Error, downloadingProgress = 1f, hash = "" };
            }
            else
            {
                JSONNode node = JSON.Parse(wwwId.downloadHandler.text);
                Song _tempSong = new Song(node["song"]);

                _lastRequestedSong = _tempSong;
            }
            
        }

        private void SelectPressed()
        {
            DismissModalViewController(null, false);
            finished?.Invoke(_selectedPlaylist);
        }
    }
}
