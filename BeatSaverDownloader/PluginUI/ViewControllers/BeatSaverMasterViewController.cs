using SimpleJSON;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using VRUI;

namespace BeatSaverDownloader.PluginUI
{
    enum Prompt { NotSelected, Yes, No};

    class BeatSaverMasterViewController : VRUINavigationController
    {
        private Logger log = new Logger("BeatSaverDownloader");

        public BeatSaverSongListViewController _songListViewController;
        public BeatSaverSongDetailViewController _songDetailViewController;
        public SearchKeyboardViewController _searchKeyboardViewController;
        public DownloadQueueViewController _downloadQueueViewController;

        public List<Song> _songs = new List<Song>();
        public List<Song> _alreadyDownloadedSongs = new List<Song>();

        private List<CustomLevel> _notUpdatedSongs = new List<CustomLevel>();

        public Button _downloadButton;
        Button _backButton;

        SongPreviewPlayer _songPreviewPlayer;

        public string _sortBy = "top";
        private bool isLoading = false;
        public bool _loading { get { return isLoading; } set { isLoading = value; SetLoadingIndicator(isLoading); } }
        public int _selectedRow = -1;
        
        Prompt _confirmDeleteState = Prompt.NotSelected;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            _alreadyDownloadedSongs = SongLoader.CustomLevels.Select(x => new Song(x)).ToList();
            
            if (_songPreviewPlayer == null)
            {
                _songPreviewPlayer = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().First();
            }

            if (_songListViewController == null)
            {
                _songListViewController = BeatSaberUI.CreateViewController<BeatSaverSongListViewController>();
                _songListViewController.rectTransform.anchorMin = new Vector2(0.3f, 0f);
                _songListViewController.rectTransform.anchorMax = new Vector2(0.7f, 1f);

                PushViewController(_songListViewController, true);

            }
            else
            {
                if (_viewControllers.IndexOf(_songListViewController) < 0)
                {
                    PushViewController(_songListViewController, true);
                }
                 
            }
            _songListViewController.SelectTopButtons(TopButtonsState.Select);

            if (_backButton == null)
            {
                _backButton = BeatSaberUI.CreateBackButton(rectTransform);

                _backButton.onClick.AddListener(delegate ()
                {
                    if (!_loading && (_downloadQueueViewController == null || _downloadQueueViewController._queuedSongs.Count == 0))
                    {
                        if (_songPreviewPlayer != null)
                        {
                            _songPreviewPlayer.CrossfadeToDefault();
                        }
                        try
                        {
                            SongLoader.Instance.RefreshSongs(false);
                            _notUpdatedSongs.Clear();
                        }
                        catch (Exception e)
                        {
                            log.Exception("Can't refresh songs! EXCEPTION: " + e);
                        }
                        DismissModalViewController(null, false);
                    }
                });
            }


            GetPage(_songListViewController._currentPage);
            
        }

        protected override void DidDeactivate(DeactivationType type)
        {
            ClearSearchInput();
        }

        public void GetPage(int page)
        {
            
            if (IsSearching())
            {
                StartCoroutine(GetSearchResults(page, _searchKeyboardViewController._inputString));
            }
            else
            {
                StartCoroutine(GetSongs(page, _sortBy));
            }
        }

        public IEnumerator GetSongs(int page, string sortBy)
        {
            _songs.Clear();
            _songListViewController._songsTableView.ReloadData();

            _loading = true;
            
            UnityWebRequest www = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/songs/{sortBy}/{(page * _songListViewController._songsPerPage)}");
            www.timeout = 30;
            yield return www.SendWebRequest();
            
            if (www.isNetworkError || www.isHttpError)
            {
                log.Error(www.error);
                TextMeshProUGUI _errorText = BeatSaberUI.CreateText(rectTransform, www.error, new Vector2(0f, -48f));
                _errorText.alignment = TextAlignmentOptions.Center;
                Destroy(_errorText.gameObject, 2f);
            }
            else
            {
                try
                {
                    JSONNode node = JSON.Parse(www.downloadHandler.text);
                    
                    for (int i = 0; i < node["songs"].Count; i++)
                    {
                        _songs.Add(new Song(node["songs"][i]));
                    }


                    _songListViewController._songsTableView.ReloadData();
                    if (_selectedRow != -1 && _songs.Count > 0)
                    {
                        _songListViewController._songsTableView.SelectRow(Math.Min(_selectedRow, _songs.Count-1));
                        ShowDetails(Math.Min(_selectedRow, _songs.Count-1));
                    }

                    _songListViewController._pageUpButton.interactable = (page == 0) ? false : true;
                    _songListViewController._pageDownButton.interactable = (_songs.Count < _songListViewController._songsPerPage) ? false : true;

                }
                catch (Exception e)
                {
                    log.Exception("EXCEPTION(GET SONGS): " + e);
                }
            }
            _loading = false;
        }

        public IEnumerator GetSearchResults(int page, string search)
        {
            _songs.Clear();
            _songListViewController._songsTableView.ReloadData();
            
            UnityWebRequest www = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/songs/search/all/{search}");

            www.timeout = 30;
            yield return www.SendWebRequest();

            

            if (www.isNetworkError || www.isHttpError)
            {
                log.Error(www.error);
                TextMeshProUGUI _errorText = BeatSaberUI.CreateText(rectTransform, www.error, new Vector2(0f, -48f));
                _errorText.alignment = TextAlignmentOptions.Center;
                Destroy(_errorText.gameObject, 2f);
            }
            else
            {
                try
                {
                    JSONNode node = JSON.Parse(www.downloadHandler.text);
                    
                    for (int i = (page * _songListViewController._songsPerPage); i < Math.Min(node["songs"].Count, ((page + 1) * _songListViewController._songsPerPage)); i++)
                    {
                        _songs.Add(Song.FromSearchNode(node["songs"][i]));
                    }

                    _songListViewController._songsTableView.ReloadData();
                    if (_selectedRow != -1 && _songs.Count > 0)
                    {
                        _songListViewController._songsTableView.SelectRow(Math.Min(_selectedRow,_songs.Count-1));
                        ShowDetails(Math.Min(_selectedRow, _songs.Count-1));
                    }

                    _songListViewController._pageUpButton.interactable = (page == 0) ? false : true;
                    _songListViewController._pageDownButton.interactable = (_songs.Count < _songListViewController._songsPerPage) ? false : true;

                }
                catch (Exception e)
                {
                    log.Exception("EXCEPTION(GET SEARCH RESULTS): " + e);
                }
            }
            _loading = false;
        }

        public void DownloadSong(int buttonId)
        {
            log.Log("Downloading "+_songs[buttonId].beatname);

            if (!_downloadQueueViewController._queuedSongs.Contains(_songs[buttonId]))
            {
                _downloadQueueViewController.EnqueueSong(_songs[buttonId]);
            }
        }

        public IEnumerator DownloadSongCoroutine(Song songInfo)
        {
            if(_songs[_selectedRow].Compare(songInfo))
            {
                RefreshDetails(_selectedRow);
            }

            songInfo.songQueueState = SongQueueState.Downloading;
            
            UnityWebRequest www = UnityWebRequest.Get(songInfo.downloadUrl);

            bool timeout = false;
            float time = 0f;

            UnityWebRequestAsyncOperation asyncRequest = www.SendWebRequest();

            while (!asyncRequest.isDone || asyncRequest.progress < 1f)
            {
                yield return null;

                time += Time.deltaTime;

                if(time >= 15f && asyncRequest.progress == 0f)
                {
                    www.Abort();
                    timeout = true;
                }

                songInfo.downloadingProgress = asyncRequest.progress;
            }


            if (www.isNetworkError || www.isHttpError || timeout)
            {
                if (timeout)
                {
                    songInfo.songQueueState = SongQueueState.Error;
                    TextMeshProUGUI _errorText = BeatSaberUI.CreateText(_songDetailViewController.rectTransform, "Request timeout", new Vector2(18f, -64f));
                    Destroy(_errorText.gameObject, 2f);
                }
                else
                {
                    songInfo.songQueueState = SongQueueState.Error;
                    log.Error($"Downloading error: {www.error}");
                    TextMeshProUGUI _errorText = BeatSaberUI.CreateText(_songDetailViewController.rectTransform, www.error, new Vector2(18f, -64f));
                    Destroy(_errorText.gameObject, 2f);
                }
                
            }
            else
            {

                log.Log("Received response from BeatSaver.com...");

                string zipPath = "";
                string docPath = "";
                string customSongsPath = "";

                byte[] data = www.downloadHandler.data;

                try
                {

                    docPath = Application.dataPath;
                    docPath = docPath.Substring(0, docPath.Length - 5);
                    docPath = docPath.Substring(0, docPath.LastIndexOf("/"));
                    customSongsPath = docPath + "/CustomSongs/" + songInfo.id +"/";
                    zipPath = customSongsPath + songInfo.id + ".zip";
                    if (!Directory.Exists(customSongsPath)) {
                        Directory.CreateDirectory(customSongsPath);
                    }
                    File.WriteAllBytes(zipPath, data);
                    log.Log("Downloaded zip file!");
                }catch(Exception e)
                {
                    log.Exception("EXCEPTION: "+e);
                    songInfo.songQueueState = SongQueueState.Error;
                    yield break;
                }

                log.Log("Extracting...");

                try
                {
                    ZipFile.ExtractToDirectory(zipPath, customSongsPath);
                }
                catch(Exception e)
                {
                    log.Exception($"Can't extract ZIP! Exception: {e}");
                }

                songInfo.path = Directory.GetDirectories(customSongsPath).FirstOrDefault();

                try
                {
                    File.Delete(zipPath);
                }catch(IOException e)
                {
                    log.Warning($"Can't delete zip! Exception: {e}");
                }
                
                songInfo.songQueueState = SongQueueState.Downloaded;

                _alreadyDownloadedSongs.Add(songInfo);

                log.Log("Downloaded!");

                _downloadQueueViewController.Refresh();
                _songListViewController._songsTableView.ReloadData();
                _songListViewController._songsTableView.SelectRow(_selectedRow);
            }

            if (_songs[_selectedRow].Compare(songInfo))
            {
                RefreshDetails(_selectedRow);
            }

        }

        IEnumerator DeleteSong(Song _songInfo)
        {
            bool zippedSong = false;
            _loading = true;
            _downloadButton.interactable = false;

            string _songPath = GetDownloadedSongPath(_songInfo);

            if (!string.IsNullOrEmpty(_songPath) && _songPath.Contains("/.cache/"))
            {
                zippedSong = true;
            }

            if (string.IsNullOrEmpty(_songPath))
            {
                log.Error("Song path is null or empty!");
                _loading = false;
                _downloadButton.interactable = true;
                yield break;
            }
            if (!Directory.Exists(_songPath))
            {
                log.Error("Song folder does not exists!");
                _loading = false;
                _downloadButton.interactable = true;
                yield break;
            }

            yield return PromptDeleteFolder(_songPath);

            if(_confirmDeleteState == Prompt.Yes)
            {
                if (zippedSong)
                {
                    log.Log("Deleting \"" + _songPath.Substring(_songPath.LastIndexOf('/')) + "\"...");
                    Directory.Delete(_songPath, true);

                    string songHash = Directory.GetParent(_songPath).Name;

                    if (Directory.GetFileSystemEntries(_songPath.Substring(0, _songPath.LastIndexOf('/'))).Length == 0)
                    {
                        log.Log("Deleting empty folder \"" + _songPath.Substring(0, _songPath.LastIndexOf('/')) + "\"...");
                        Directory.Delete(_songPath.Substring(0, _songPath.LastIndexOf('/')), false);
                    }

                    string docPath = Application.dataPath;
                    docPath = docPath.Substring(0, docPath.Length - 5);
                    docPath = docPath.Substring(0, docPath.LastIndexOf("/"));
                    string customSongsPath = docPath + "/CustomSongs/";

                    string hash = "";

                    foreach (string file in Directory.GetFiles(customSongsPath, "*.zip"))
                    {
                        if(PluginUI.CreateMD5FromFile(file,out hash))
                        {
                            if (hash == songHash)
                            {
                                File.Delete(file);
                                break;
                            }
                        }
                    }

                }
                else
                {
                    log.Log("Deleting \"" + _songPath.Substring(_songPath.LastIndexOf('/')) + "\"...");
                    Directory.Delete(_songPath, true);
                    if (Directory.GetFileSystemEntries(_songPath.Substring(0, _songPath.LastIndexOf('/'))).Length == 0)
                    {
                        log.Log("Deleting empty folder \"" + _songPath.Substring(0, _songPath.LastIndexOf('/')) + "\"...");
                        Directory.Delete(_songPath.Substring(0, _songPath.LastIndexOf('/')), false);
                    }
                }
            }
            _confirmDeleteState = Prompt.NotSelected;



            log.Log($"{_alreadyDownloadedSongs.RemoveAll(x => x.Compare(_songInfo))} song removed");


            _songListViewController._songsTableView.ReloadData();
            _songListViewController._songsTableView.SelectRow(_selectedRow);
            RefreshDetails(_selectedRow);

            _loading = false;
            _downloadButton.interactable = true;
        }


        IEnumerator PromptDeleteFolder(string dirName)
        {
            TextMeshProUGUI _deleteText = BeatSaberUI.CreateText(_songDetailViewController.rectTransform, String.Format("Delete folder \"{0}\"?", dirName.Substring(dirName.LastIndexOf('/')).Trim('/')), new Vector2(18f, -64f));

            Button _confirmDelete = BeatSaberUI.CreateUIButton(_songDetailViewController.rectTransform, "SettingsButton");

            BeatSaberUI.SetButtonText(_confirmDelete, "Yes");
            (_confirmDelete.transform as RectTransform).sizeDelta = new Vector2(15f, 10f);
            (_confirmDelete.transform as RectTransform).anchoredPosition = new Vector2(-13f, 6f);
            _confirmDelete.onClick.AddListener(delegate () { _confirmDeleteState = Prompt.Yes; });

            Button _discardDelete = BeatSaberUI.CreateUIButton(_songDetailViewController.rectTransform, "SettingsButton");

            BeatSaberUI.SetButtonText(_discardDelete, "No");
            (_discardDelete.transform as RectTransform).sizeDelta = new Vector2(15f, 10f);
            (_discardDelete.transform as RectTransform).anchoredPosition = new Vector2(2f, 6f);
            _discardDelete.onClick.AddListener(delegate () { _confirmDeleteState = Prompt.No; });


            (_downloadButton.transform as RectTransform).anchoredPosition = new Vector2(2f, -10f);
            
            yield return new WaitUntil(delegate () { return (_confirmDeleteState == Prompt.Yes || _confirmDeleteState == Prompt.No); });

            (_downloadButton.transform as RectTransform).anchoredPosition = new Vector2(2f, 6f);

            Destroy(_deleteText.gameObject);
            Destroy(_confirmDelete.gameObject);
            Destroy(_discardDelete.gameObject);
            
        }

        public void ClearSearchInput()
        {
            if(_searchKeyboardViewController != null)
            {
                _searchKeyboardViewController._inputString = "";
            }
        }

        public bool IsSearching()
        {
            return (_searchKeyboardViewController != null && !String.IsNullOrEmpty(_searchKeyboardViewController._inputString));
        }

        public void ShowSearchKeyboard()
        {
            if (_searchKeyboardViewController == null)
            {
                _searchKeyboardViewController = BeatSaberUI.CreateViewController<SearchKeyboardViewController>();
                PresentModalViewController(_searchKeyboardViewController, null, false);
                
            }
            else
            {
                PresentModalViewController(_searchKeyboardViewController, null, false);
                
            }
        }

        void SetLoadingIndicator(bool loading)
        {
            if(_songListViewController != null && _songListViewController._loadingIndicator)
            {
                _songListViewController._loadingIndicator.SetActive(loading);
            }
        }

        public void ShowDetails(int row)
        {
            _selectedRow = row;
            
            if (_songDetailViewController == null)
            {
                GameObject _songDetailGameObject = Instantiate(Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First(), rectTransform, false).gameObject;
                Destroy(_songDetailGameObject.GetComponent<StandardLevelDetailViewController>());
                _songDetailViewController = _songDetailGameObject.AddComponent<BeatSaverSongDetailViewController>();
                
                PushViewController(_songDetailViewController, false);
                RefreshDetails(row);
            }
            else
            {
                if (_viewControllers.IndexOf(_songDetailViewController) < 0)
                {
                    PushViewController(_songDetailViewController, true);
                    RefreshDetails(row);
                }
                else
                {
                    RefreshDetails(row);
                }
                
            }
        }

        private void RefreshDetails(int row)
        {
            if(_songs.Count<=row)
            {
                return;
            }

            _songDetailViewController.UpdateContent(_songs[row]);
            
            if (_downloadButton == null)
            {
                _downloadButton = _songDetailViewController.GetComponentInChildren<Button>();
                (_downloadButton.transform as RectTransform).sizeDelta = new Vector2(30f, 10f);
                (_downloadButton.transform as RectTransform).anchoredPosition = new Vector2(2f, 6f);

            }

            if (IsSongAlreadyDownloaded(_songs[row]))
            {
                BeatSaberUI.SetButtonText(_downloadButton, "Delete");

                _downloadButton.onClick.RemoveAllListeners();

                _downloadButton.onClick.AddListener(delegate ()
                {
                    if (!_loading)
                    {
                        StartCoroutine(DeleteSong(_songs[row]));
                    }
                });                

                string _songPath = GetDownloadedSongPath(_songs[row]);
                
                if (string.IsNullOrEmpty(_songPath))
                {
                    _downloadButton.interactable = false;
                }
                else
                {
                    _downloadButton.interactable = true;
                }
            }
            else
            {
                BeatSaberUI.SetButtonText(_downloadButton, "Download");
                _downloadButton.interactable = true;
                
                _downloadButton.onClick.RemoveAllListeners();

                _downloadButton.onClick.AddListener(delegate ()
                {
                    if (!_loading)
                    {
                        DownloadSong(row);
                    }

                });

                if (_songPreviewPlayer != null)
                {
                    _songPreviewPlayer.CrossfadeToDefault();
                }
            }

            if (_downloadQueueViewController != null && _downloadQueueViewController._queuedSongs.Contains(_songs[row]) && !IsSongAlreadyDownloaded(_songs[row]))
            {
                BeatSaberUI.SetButtonText(_downloadButton, "Queued...");
                _downloadButton.interactable = false;
            }
        }

        public bool IsSongAlreadyDownloaded(Song _song)
        {
            return _alreadyDownloadedSongs.Any(x => x.Compare(_song));
        }

        public string GetDownloadedSongPath(Song _song)
        {
            foreach (Song song in _alreadyDownloadedSongs)
            {
                if (song.Compare(_song))
                {
                    if (Directory.Exists(song.path))
                    {
                        return song.path;
                    }
                }
            }

            return null;
        }

    }
}
