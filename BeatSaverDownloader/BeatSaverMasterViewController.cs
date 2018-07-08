using HMUI;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using SimpleJSON;
using SongLoaderPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using VRUI;

namespace BeatSaverDownloader
{
    enum Prompt { NotSelected, Yes, No};

    class BeatSaverMasterViewController : VRUINavigationController
    {
        BeatSaverUI ui;
        private Logger log = new Logger("BeatSaverDownloader");

        public BeatSaverSongListViewController _songListViewController;
        public SongDetailViewController _songDetailViewController;
        public SearchKeyboardViewController _searchKeyboardViewController;


        public List<Song> _songs = new List<Song>();
        public List<Song> _alreadyDownloadedSongs = new List<Song>();

        private List<LevelStaticData> _notUpdatedSongs = new List<LevelStaticData>();

        public Button _downloadButton;
        public Button _deleteButton;
        Button _backButton;

        SongPreviewPlayer _songPreviewPlayer;
        public SongLoader _songLoader;

        public string _sortBy = "top";
        private bool isLoading = false;
        public bool _loading { get { return isLoading; } set { isLoading = value; SetLoadingIndicator(isLoading); } }
        public int _selectedRow = -1;

        Prompt _confirmOverwriteState = Prompt.NotSelected;
        Prompt _confirmDeleteState = Prompt.NotSelected;

        protected override void DidActivate()
        {            
            ui = BeatSaverUI._instance;
            _songLoader = FindObjectOfType<SongLoader>();

            UpdateAlreadyDownloadedSongs();



            if(_songPreviewPlayer == null)
            {
                ObjectProvider[] providers = Resources.FindObjectsOfTypeAll<ObjectProvider>().Where(x => x.name == "SongPreviewPlayerProvider").ToArray();

                if (providers.Length > 0) {
                    _songPreviewPlayer = providers[0].GetProvidedObject<SongPreviewPlayer>();
                }
            }

            if (_songListViewController == null)
            {
                _songListViewController = ui.CreateViewController<BeatSaverSongListViewController>();
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
                _backButton = ui.CreateBackButton(rectTransform);

                _backButton.onClick.AddListener(delegate ()
                {
                    if (!_loading)
                    {
                        if (_songPreviewPlayer != null)
                        {
                            _songPreviewPlayer.CrossfadeToDefault();
                        }
                        try
                        {
                            _songLoader.RefreshSongs();
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

            base.DidActivate();
            
        }

        protected override void DidDeactivate()
        {
            ClearSearchInput();

            base.DidDeactivate();
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
            _songListViewController.RefreshScreen();

            _loading = true;
            
            UnityWebRequest www = UnityWebRequest.Get(String.Format($"{Plugin.beatsaverURL}/api/songs/{0}/{1}", sortBy, (page * _songListViewController._songsPerPage)));
            www.timeout = 10;
            yield return www.SendWebRequest();

            

            if (www.isNetworkError || www.isHttpError)
            {
                log.Error(www.error);
                TextMeshProUGUI _errorText = ui.CreateText(rectTransform, String.Format("Request timed out"), new Vector2(0f, -48f));
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

                    
                    _songListViewController.RefreshScreen();
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
            _songListViewController.RefreshScreen();

            UnityWebRequest www = UnityWebRequest.Get(String.Format($"{Plugin.beatsaverURL}/songs/search/all/{0}", search));
            www.timeout = 10;
            yield return www.SendWebRequest();

            

            if (www.isNetworkError || www.isHttpError)
            {
                log.Error(www.error);
                TextMeshProUGUI _errorText = ui.CreateText(rectTransform, www.error, new Vector2(0f, -48f));
                _errorText.alignment = TextAlignmentOptions.Center;
                Destroy(_errorText.gameObject, 2f);
            }
            else
            {
                try
                {
                    string parse = www.downloadHandler.text;

                    JSONNode node = JSON.Parse(www.downloadHandler.text);

                    for (int i = (page * _songListViewController._songsPerPage); i < Math.Min(node["songs"].Count, ((page + 1) * _songListViewController._songsPerPage)); i++)
                    {
                        _songs.Add(new Song(node["songs"][i]));
                    }

                    _songListViewController.RefreshScreen();
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

            StartCoroutine(DownloadSongCoroutine(_songs[buttonId],buttonId));
        }

        public IEnumerator DownloadSongCoroutine(Song songInfo, int row)
        {

            _loading = true;
            ui.SetButtonText(ref _downloadButton, "Downloading...");
            _downloadButton.interactable = false;
            if (_deleteButton != null)
            {
                _deleteButton.interactable = false;
            }

            string downloadedSongPath = "";

            UnityWebRequest www = UnityWebRequest.Get(songInfo.downloadUrl);
            www.timeout = 10;
            yield return www.SendWebRequest();

            log.Log("Received response from BeatSaver.com...");

            if (www.isNetworkError || www.isHttpError)
            {
                log.Error(www.error);
                TextMeshProUGUI _errorText = ui.CreateText(_songDetailViewController.rectTransform, String.Format(www.error), new Vector2(18f, -64f));
                Destroy(_errorText.gameObject, 2f);
            }
            else
            {
                string zipPath = "";
                string docPath = "";
                string customSongsPath = "";
                try
                {
                    byte[] data = www.downloadHandler.data;

                    docPath = Application.dataPath;
                    docPath = docPath.Substring(0, docPath.Length - 5);
                    docPath = docPath.Substring(0, docPath.LastIndexOf("/"));
                    customSongsPath = docPath + "/CustomSongs/" + songInfo.id +"/";
                    zipPath = customSongsPath + songInfo.beatname + ".zip";
                    if (!Directory.Exists(customSongsPath)) {
                        Directory.CreateDirectory(customSongsPath);
                    }
                    File.WriteAllBytes(zipPath, data);
                    log.Log("Downloaded zip file!");
                }catch(Exception e)
                {
                    log.Exception("EXCEPTION: "+e);

                    _songListViewController._songsTableView.SelectRow(row);
                    RefreshDetails(row);
                    _loading = false;
                    if (_deleteButton != null)
                    {
                        _downloadButton.interactable = true;
                    }
                    yield break;
                }


                bool isOverwriting = false;
                using (var zf = new ZipFile(zipPath))
                {
                    foreach (ZipEntry ze in zf)
                    {
                        if (ze.IsFile)
                        {
                            if (string.IsNullOrEmpty(downloadedSongPath) && ze.Name.IndexOf('/')!= -1)
                            {
                                downloadedSongPath = customSongsPath + ze.Name.Substring(0, ze.Name.IndexOf('/'));
                            }
                            if (Directory.Exists(customSongsPath + ze.Name.Substring(0, ze.Name.IndexOf('/'))))
                            {
                                yield return PromptOverwriteFiles(ze.Name.Substring(0,ze.Name.IndexOf('/')));
                                break;
                            }
                            else
                            {
                                isOverwriting = true;
                            }
                            
                        }
                        else if (ze.IsDirectory)
                        {
                            downloadedSongPath = customSongsPath + ze.Name;
                            if (Directory.Exists(customSongsPath + ze.Name))
                            {

                                yield return PromptOverwriteFiles(ze.Name.Trim('\\','/'));
                                break;
                            }
                            else
                            {
                                isOverwriting = true;
                            }
                        }
                                    
                    }
                    
                }
                   


                if (_confirmOverwriteState == Prompt.Yes || isOverwriting)
                {

                    FastZip zip = new FastZip();

                    log.Log("Extractibg...");
                    zip.ExtractZip(zipPath, customSongsPath, null);

                    
                    try
                    {
                        CustomSongInfo downloadedSong = GetCustomSongInfo(downloadedSongPath);

                        CustomLevelStaticData newLevel = null;
                        try
                        {
                            newLevel = ScriptableObject.CreateInstance<CustomLevelStaticData>();
                        }
                        catch (Exception e)
                        {
                            //LevelStaticData.OnEnable throws null reference exception because we don't have time to set _difficultyLevels
                        }

                        ReflectionUtil.SetPrivateField(newLevel, "_levelId", downloadedSong.GetIdentifier());
                        ReflectionUtil.SetPrivateField(newLevel, "_authorName", downloadedSong.authorName);
                        ReflectionUtil.SetPrivateField(newLevel, "_songName", downloadedSong.songName);
                        ReflectionUtil.SetPrivateField(newLevel, "_songSubName", downloadedSong.songSubName);
                        ReflectionUtil.SetPrivateField(newLevel, "_previewStartTime", downloadedSong.previewStartTime);
                        ReflectionUtil.SetPrivateField(newLevel, "_previewDuration", downloadedSong.previewDuration);
                        ReflectionUtil.SetPrivateField(newLevel, "_beatsPerMinute", downloadedSong.beatsPerMinute);

                        List<LevelStaticData.DifficultyLevel> difficultyLevels = new List<LevelStaticData.DifficultyLevel>();

                        LevelStaticData.DifficultyLevel newDiffLevel = new LevelStaticData.DifficultyLevel();

                        StartCoroutine(LoadAudio("file://" + downloadedSong.path + "/" + downloadedSong.difficultyLevels[0].audioPath, newDiffLevel, "_audioClip"));
                        difficultyLevels.Add(newDiffLevel);

                        ReflectionUtil.SetPrivateField(newLevel, "_difficultyLevels", difficultyLevels.ToArray());

                        newLevel.OnEnable();
                        _notUpdatedSongs.Add(newLevel);
                    }catch(Exception e)
                    {
                        log.Exception("Can't play preview! Exception: "+e);
                    }

                    UpdateAlreadyDownloadedSongs();
                    _songListViewController.RefreshScreen();

                    log.Log("Downloaded!");
                }
                _confirmOverwriteState = Prompt.NotSelected;
                File.Delete(zipPath);
            }
            try
            {
                _songListViewController._songsTableView.SelectRow(row);
                RefreshDetails(row);
            }
            catch (Exception e)
            {
                log.Exception(e.ToString());
            }
            _loading = false;
            if (_deleteButton != null)
            {
                _downloadButton.interactable = true;
            }
            
        }

        IEnumerator PromptOverwriteFiles(string dirName)
        {

            TextMeshProUGUI _overwriteText = ui.CreateText(_songDetailViewController.rectTransform,String.Format("Overwrite folder \"{0}\"?",dirName), new Vector2(18f,-64f));

            Button _confirmOverwrite = ui.CreateUIButton(_songDetailViewController.rectTransform, "ApplyButton");

            ui.SetButtonText(ref _confirmOverwrite, "Yes");
            (_confirmOverwrite.transform as RectTransform).sizeDelta = new Vector2(15f,10f);
            (_confirmOverwrite.transform as RectTransform).anchoredPosition = new Vector2(-13f, 6f);
            _confirmOverwrite.onClick.AddListener(delegate() { _confirmOverwriteState = Prompt.Yes; });

            Button _discardOverwrite = ui.CreateUIButton(_songDetailViewController.rectTransform, "ApplyButton");

            ui.SetButtonText(ref _discardOverwrite, "No");
            (_discardOverwrite.transform as RectTransform).sizeDelta = new Vector2(15f, 10f);
            (_discardOverwrite.transform as RectTransform).anchoredPosition = new Vector2(2f, 6f);
            _discardOverwrite.onClick.AddListener(delegate () { _confirmOverwriteState = Prompt.No; });

            
            (_downloadButton.transform as RectTransform).anchoredPosition = new Vector2(2f,-10f);

            yield return new WaitUntil(delegate() { return (_confirmOverwriteState == Prompt.Yes || _confirmOverwriteState == Prompt.No); });
            
            (_downloadButton.transform as RectTransform).anchoredPosition = new Vector2(2f,6f);
            
            Destroy(_overwriteText.gameObject);
            Destroy(_confirmOverwrite.gameObject);
            Destroy(_discardOverwrite.gameObject);            

        }

        IEnumerator DeleteSong(int row)
        {
            bool zippedSong = false;
            _loading = true;
            _downloadButton.interactable = false;
            _deleteButton.interactable = false;

            string _songPath = GetDownloadedSongPath(_songs[row]);

            if (!string.IsNullOrEmpty(_songPath) && _songPath.Contains("/.cache/"))
            {
                zippedSong = true;
            }

            if (string.IsNullOrEmpty(_songPath))
            {
                log.Error("Song path is null or empty!");
                _loading = false;
                _downloadButton.interactable = true;
                _deleteButton.interactable = true;
                yield break;
            }
            if (!Directory.Exists(_songPath))
            {
                log.Error("Song folder does not exists!");
                _loading = false;
                _downloadButton.interactable = true;
                _deleteButton.interactable = true;
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
                        if(CreateMD5FromFile(file,out hash))
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

            UpdateAlreadyDownloadedSongs();
            _songListViewController.RefreshScreen();
            _songListViewController._songsTableView.SelectRow(row);
            RefreshDetails(row);

            _loading = false;
            _downloadButton.interactable = true;
            _deleteButton.interactable = true;
        }


        IEnumerator PromptDeleteFolder(string dirName)
        {
            TextMeshProUGUI _deleteText = ui.CreateText(_songDetailViewController.rectTransform, String.Format("Delete folder \"{0}\"?", dirName.Substring(dirName.LastIndexOf('/')).Trim('/')), new Vector2(18f, -64f));

            Button _confirmDelete = ui.CreateUIButton(_songDetailViewController.rectTransform, "ApplyButton");

            ui.SetButtonText(ref _confirmDelete, "Yes");
            (_confirmDelete.transform as RectTransform).sizeDelta = new Vector2(15f, 10f);
            (_confirmDelete.transform as RectTransform).anchoredPosition = new Vector2(-13f, 6f);
            _confirmDelete.onClick.AddListener(delegate () { _confirmDeleteState = Prompt.Yes; });

            Button _discardDelete = ui.CreateUIButton(_songDetailViewController.rectTransform, "ApplyButton");

            ui.SetButtonText(ref _discardDelete, "No");
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
                _searchKeyboardViewController = ui.CreateViewController<SearchKeyboardViewController>();
                PresentModalViewController(_searchKeyboardViewController, null);
                
            }
            else
            {
                PresentModalViewController(_searchKeyboardViewController, null);
                
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
                _songDetailViewController = Instantiate(Resources.FindObjectsOfTypeAll<SongDetailViewController>().First(), rectTransform, false);

                RefreshDetails(row);

                PushViewController(_songDetailViewController, false);

            }
            else
            {

                if (_viewControllers.IndexOf(_songDetailViewController) < 0)
                {
                    RefreshDetails(row);
                    PushViewController(_songDetailViewController, true);
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
            
            RectTransform _levelDetails = _songDetailViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "LevelDetails");
            _levelDetails.sizeDelta = new Vector2(44f, 20f);
            RectTransform _yourStats = _songDetailViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "YourStats");
            _yourStats.sizeDelta = new Vector2(44f, 18f);
             

            TextMeshProUGUI[] _textComponents = _songDetailViewController.GetComponentsInChildren<TextMeshProUGUI>();

            try
            {

                _textComponents.First(x => x.name == "SongNameText").text = string.Format("{0}\n<size=80%>{1}</size>", HTML5Decode.HtmlDecode(_songs[row].songName), HTML5Decode.HtmlDecode(_songs[row].songSubName));
                _textComponents.First(x => x.name == "DurationValueText").text = HTML5Decode.HtmlDecode(_songs[row].downloads);
                _textComponents.First(x => x.name == "DurationText").text = "Downloads";

                _textComponents.First(x => x.name == "BPMText").text = "Plays";
                _textComponents.First(x => x.name == "BPMValueText").text = HTML5Decode.HtmlDecode(_songs[row].plays);

                _textComponents.First(x => x.name == "NotesCountText").text = "Author";
                _textComponents.First(x => x.name == "NotesCountValueText").text = HTML5Decode.HtmlDecode(_songs[row].authorName);

                _textComponents.First(x => x.name == "NotesCountValueText").rectTransform.sizeDelta = new Vector2(16f, 3f);
                _textComponents.First(x => x.name == "NotesCountValueText").alignment = TextAlignmentOptions.CaplineRight;

                _textComponents.First(x => x.name == "Title").text = "Difficulties";

                _textComponents.First(x => x.name == "HighScoreText").text = "Expert/+";
                _textComponents.First(x => x.name == "HighScoreValueText").text = (_songs[row].difficultyLevels.Where(x => (x.difficulty == "Expert" || x.difficulty == "ExpertPlus")).Count() > 0) ? "Yes" : "No";

                _textComponents.First(x => x.name == "MaxComboText").text = "Hard";
                _textComponents.First(x => x.name == "MaxComboValueText").text = (_songs[row].difficultyLevels.Where(x => x.difficulty == "Hard").Count() > 0) ? "Yes" : "No";

                _textComponents.First(x => x.name == "MaxRankText").text = "Easy/Normal";
                _textComponents.First(x => x.name == "MaxRankText").rectTransform.sizeDelta = new Vector2(18f, 3f);
                _textComponents.First(x => x.name == "MaxRankValueText").text = (_songs[row].difficultyLevels.Where(x => (x.difficulty == "Easy" || x.difficulty == "Normal")).Count() > 0) ? "Yes" : "No";                

                if (_textComponents.Where(x => x.name == "ObstaclesCountText").Count() != 0)
                {
                    Destroy(_textComponents.First(x => x.name == "ObstaclesCountText").gameObject);
                    Destroy(_textComponents.First(x => x.name == "ObstaclesCountValueText").gameObject);
                }
            }catch(Exception e)
            {
                log.Exception("EXCEPTION: "+e);
            }

            if (_downloadButton == null)
            {
                _downloadButton = _songDetailViewController.GetComponentInChildren<Button>();
                (_downloadButton.transform as RectTransform).sizeDelta = new Vector2(30f, 10f);
                (_downloadButton.transform as RectTransform).anchoredPosition = new Vector2(2f, 6f);

            }
            _downloadButton.onClick.RemoveAllListeners();

            _downloadButton.onClick.AddListener(delegate()
            {
                if (!_loading)
                {
                    DownloadSong(row);
                }

            });

            if(_deleteButton != null)
            {
                Destroy(_deleteButton.gameObject);
            }

            if (IsSongAlreadyDownloaded(_songs[row]))
            {
                ui.SetButtonText(ref _downloadButton, "Redownload");

                _deleteButton = ui.CreateUIButton(_songDetailViewController.GetComponent<RectTransform>(), "ApplyButton");
                ui.SetButtonText(ref _deleteButton, "Delete");
                (_deleteButton.transform as RectTransform).sizeDelta = new Vector2(18f, 10f);
                (_deleteButton.transform as RectTransform).anchoredPosition = new Vector2(18f, 6f);


                _deleteButton.onClick.RemoveAllListeners();

                _deleteButton.onClick.AddListener(delegate ()
                {
                    if (!_loading)
                    {
                        StartCoroutine(DeleteSong(row));
                    }

                });

                LevelStaticData _songData = GetLevelStaticDataForSong(_songs[row]);
                
                PlayPreview(_songData);
                

                string _songPath = GetDownloadedSongPath(_songs[row]);
                
                if (!string.IsNullOrEmpty(_songPath) && _songPath.Contains("/.cache/"))
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
                ui.SetButtonText(ref _downloadButton, "Download");
                _downloadButton.interactable = true;
                if (_songPreviewPlayer != null)
                {
                    _songPreviewPlayer.CrossfadeToDefault();
                }
            }
        }

        void PlayPreview(LevelStaticData _songData)
        {
            log.Log("Playing preview for " + _songData.songName);
            if (_songData.previewAudioClip != null)
            {
                if (_songPreviewPlayer != null && _songData != null)
                {
                    try
                    {
                        _songPreviewPlayer.CrossfadeTo(_songData.previewAudioClip, _songData.previewStartTime, _songData.previewDuration, 1f);
                    }
                    catch (Exception e)
                    {
                        log.Error("Can't play preview! Exception: " + e);
                    }
                }
            }
            else
            {
                StartCoroutine(PlayPreviewCoroutine(_songData));
            }
        }

        IEnumerator PlayPreviewCoroutine(LevelStaticData _songData)
        {
            yield return new WaitWhile(delegate () { return _songData.previewAudioClip != null; });

            if (_songPreviewPlayer != null && _songData != null && _songData.previewAudioClip != null)
            {
                try
                {
                    _songPreviewPlayer.CrossfadeTo(_songData.previewAudioClip, _songData.previewStartTime, _songData.previewDuration, 1f);
                }
                catch (Exception e)
                {
                    log.Error("Can't play preview! Exception: " + e);
                }
            }
        }

        public IEnumerator LoadSprite(string spritePath, TableCell obj)
        {
            Texture2D tex;
            using (WWW www = new WWW(spritePath))
            {
                yield return www;
                tex = www.texture;
                var newSprite = Sprite.Create(tex, new Rect(0, 0, 256, 256), Vector2.one * 0.5f, 100, 1);
                obj.GetComponentsInChildren<UnityEngine.UI.Image>()[2].sprite = newSprite;
            }
        }        

        private IEnumerator LoadAudio(string audioPath, object obj, string fieldName)
        {
            using (var www = new WWW(audioPath))
            {
                yield return www;
                ReflectionUtil.SetPrivateField(obj, fieldName, www.GetAudioClip(true, true, AudioType.UNKNOWN));
            }
        }

        public bool IsSongAlreadyDownloaded(Song _song)
        {
            bool alreadyDownloaded = false;

            foreach (Song song in _alreadyDownloadedSongs)
            {
                alreadyDownloaded = alreadyDownloaded || song.Compare(_song);
            }

            return alreadyDownloaded;
        }

        public LevelStaticData GetLevelStaticDataForSong(Song _song)
        {
            foreach(CustomLevelStaticData data in SongLoader.CustomLevelStaticDatas)
            {
                if ((new Song(data)).Compare(_song))
                {
                    return data;
                }
            }
            
            foreach (CustomLevelStaticData data in _notUpdatedSongs)
            {
                if ((new Song(data)).Compare(_song))
                {
                    return data;
                }
            }
            return null;
        }

        public string GetDownloadedSongPath(Song _song)
        {
            foreach (Song song in _alreadyDownloadedSongs)
            {
                if (song.Compare(_song))
                {
                    return song.path;
                }
            }
            return null;
        }

        private void UpdateAlreadyDownloadedSongs()
        {
            _alreadyDownloadedSongs.Clear();
            foreach (CustomSongInfo song in RetrieveAllSongs())
            {
                _alreadyDownloadedSongs.Add(new Song(song));
            }
        }

        private List<CustomSongInfo> RetrieveAllSongs()
        {
            var customSongInfos = new List<CustomSongInfo>();
            var path = Environment.CurrentDirectory;
            path = path.Replace('\\', '/');

            var currentHashes = new List<string>();
            var cachedSongs = new string[0];
            if (Directory.Exists(path + "/CustomSongs/.cache"))
            {
                cachedSongs = Directory.GetDirectories(path + "/CustomSongs/.cache");
            }
            

            var songFolders = Directory.GetDirectories(path + "/CustomSongs").ToList();
            var songCaches = Directory.GetDirectories(path + "/CustomSongs/.cache");

            foreach (var song in songFolders)
            {
                var results = Directory.GetFiles(song, "info.json", SearchOption.AllDirectories);
                if (results.Length == 0)
                {
                    log.Log("Custom song folder '" + song + "' is missing info.json!");
                    continue;
                }

                foreach (var result in results)
                {
                    var songPath = Path.GetDirectoryName(result).Replace('\\', '/');
                    var customSong = GetCustomSongInfo(songPath);
                    if (customSong == null) continue;
                    customSongInfos.Add(customSong);
                }
            }


            return customSongInfos;
        }

        private CustomSongInfo GetCustomSongInfo(string _songPath)
        {
            string songPath = _songPath;
            if(songPath.Contains("/autosaves"))
            {
                songPath = songPath.Replace("/autosaves","");
            }
            var infoText = File.ReadAllText(songPath + "/info.json");
            CustomSongInfo songInfo;
            try
            {
                songInfo = JsonUtility.FromJson<CustomSongInfo>(infoText);
            }
            catch (Exception e)
            {
                log.Warning("Error parsing song: " + songPath);
                return null;
            }
            songInfo.path = songPath;
            
            var diffLevels = new List<CustomSongInfo.DifficultyLevel>();
            var n = JSON.Parse(infoText);
            var diffs = n["difficultyLevels"];
            for (int i = 0; i < diffs.AsArray.Count; i++)
            {
                n = diffs[i];
                diffLevels.Add(new CustomSongInfo.DifficultyLevel()
                {
                    difficulty = n["difficulty"],
                    difficultyRank = n["difficultyRank"].AsInt,
                    audioPath = n["audioPath"],
                    jsonPath = n["jsonPath"]
                });
            }
            songInfo.difficultyLevels = diffLevels.ToArray();
            return songInfo;
        }

        public static bool CreateMD5FromFile(string path, out string hash)
        {
            hash = "";
            if (!File.Exists(path)) return false;
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    var hashBytes = md5.ComputeHash(stream);
                    
                    var sb = new StringBuilder();
                    foreach (var hashByte in hashBytes)
                    {
                        sb.Append(hashByte.ToString("X2"));
                    }

                    hash = sb.ToString();
                    return true;
                }
            }
        }

    }
}
