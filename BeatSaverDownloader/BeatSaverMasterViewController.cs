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
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using VRUI;
using Logger = IllusionPlugin.Logger;

namespace BeatSaverDownloader
{
    class BeatSaverMasterViewController : VRUINavigationController
    {
        BeatSaverUI ui;
        private Logger log = new Logger("BeatSaverDownloader");

        public BeatSaverSongListViewController _songListViewController;
        public SongDetailViewController _songDetailViewController;
        public SearchKeyboardViewController _searchKeyboardViewController;


        public List<Song> _songs = new List<Song>();
        public List<Song> _alreadyDownloadedSongs = new List<Song>();

        public Button _downloadButton;
        Button _backButton;


        public SongLoader _songLoader;

        public string _sortBy = "top";
        private bool isLoading = false;
        public bool _loading { get { return isLoading; } set { isLoading = value; SetLoadingIndicator(isLoading); } }
        public int _selectedRow = -1;

        FastZip.Overwrite _confirmOverwriteState = FastZip.Overwrite.Prompt;

        protected override void DidActivate()
        {
            log.Log("Activated!");
            
            ui = BeatSaverUI._instance;
            _songLoader = FindObjectOfType<SongLoader>();

            UpdateAlreadyDownloadedSongs();

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
                    try
                    {
                        _songLoader.RefreshSongs();
                    }
                    catch (Exception e)
                    {
                        log.Exception("Can't refresh songs! EXCEPTION: " + e);
                    }
                    DismissModalViewController(null, false);
                });
            }

            GetPage(0);

            base.DidActivate();
            
        }

        protected override void DidDeactivate()
        {
            ClearSearchInput();

            base.DidDeactivate();
        }

        public void GetPage(int page)
        {
            _loading = true;
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

            UnityWebRequest www = UnityWebRequest.Get(String.Format("https://beatsaver.com/api.php?mode={0}&off={1}", sortBy, (page * _songListViewController._songsPerPage)));

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                log.Error(www.error);
            }
            else
            {
                try
                {
                    string parse = "{\"songs\": " + www.downloadHandler.text.Replace("][", ",") + "}";

                    JSONNode node = JSON.Parse(parse);



                    for (int i = 0; i < node["songs"].Count; i++)
                    {
                        _songs.Add(new Song(node["songs"][i]));
                    }

                    _loading = false;
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
                    log.Exception("EXCEPTION IN GET SONGS: " + e.Message + " | " + e.StackTrace);
                }
            }

        }

        public IEnumerator GetSearchResults(int page, string search)
        {
            _songs.Clear();
            _songListViewController.RefreshScreen();

            UnityWebRequest www = UnityWebRequest.Get(String.Format("https://beatsaver.com/search.php?q={0}", search));//  (page * _songListViewController._songsPerPage)

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                log.Error(www.error);
            }
            else
            {
                try
                {
                    string parse = www.downloadHandler.text;

                    JSONNode node = JSON.Parse(parse);


                    

                    for (int i = (page * _songListViewController._songsPerPage); i < Math.Min(node["hits"]["hits"].Count, ((page + 1) * _songListViewController._songsPerPage)); i++)
                    {
                        
                        _songs.Add(new Song(node["hits"]["hits"][i]["_source"], JSON.Parse(node["hits"]["hits"][i]["_source"]["difficultyLevels"].Value)));
                    }

                    _loading = false;
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
                    log.Exception("EXCEPTION IN GET SEARCH RESULTS: " + e.Message + " | " + e.StackTrace);
                }
            }

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
            
            UnityWebRequest www = UnityWebRequest.Get("https://beatsaver.com/dl.php?id=" + (songInfo.id));
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                log.Error(www.error);
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
                    customSongsPath = docPath + "/CustomSongs/";
                    zipPath = customSongsPath + songInfo.beatname + ".zip";
                    File.WriteAllBytes(zipPath, data);
                }catch(Exception e)
                {
                    log.Exception("FATAL EXCEPTION: "+e);

                    _songListViewController._songsTableView.SelectRow(row);
                    RefreshDetails(row);
                    _loading = false;
                    _downloadButton.interactable = true;
                    yield break;
                }


                bool isOverwriting = false;
                using (var zf = new ZipFile(zipPath))
                {
                    foreach (ZipEntry ze in zf)
                    {
                        if (ze.IsFile)
                        {
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
                   

                if (_confirmOverwriteState == FastZip.Overwrite.Always || isOverwriting)
                {

                    FastZip zip = new FastZip();

                    zip.ExtractZip(zipPath, customSongsPath, null);
                    
                    UpdateAlreadyDownloadedSongs();
                    _songListViewController.RefreshScreen();
                }
                _confirmOverwriteState = FastZip.Overwrite.Prompt;
                File.Delete(zipPath); 
            }

            _songListViewController._songsTableView.SelectRow(row);
            RefreshDetails(row);
            _loading = false;
            _downloadButton.interactable = true;
        }

        IEnumerator PromptOverwriteFiles(string dirName)
        {

            TextMeshProUGUI _overwriteText = ui.CreateText(_songDetailViewController.rectTransform,String.Format("Overwrite folder \"{0}\"?",dirName), new Vector2(18f,-64f));

            Button _confirmOverwrite = ui.CreateUIButton(_songDetailViewController.rectTransform, "ApplyButton");

            ui.SetButtonText(ref _confirmOverwrite, "Yes");
            (_confirmOverwrite.transform as RectTransform).sizeDelta = new Vector2(15f,10f);
            (_confirmOverwrite.transform as RectTransform).anchoredPosition = new Vector2(-13f, 6f);
            _confirmOverwrite.onClick.AddListener(delegate() { _confirmOverwriteState = FastZip.Overwrite.Always; });

            Button _discardOverwrite = ui.CreateUIButton(_songDetailViewController.rectTransform, "ApplyButton");

            ui.SetButtonText(ref _discardOverwrite, "No");
            (_discardOverwrite.transform as RectTransform).sizeDelta = new Vector2(15f, 10f);
            (_discardOverwrite.transform as RectTransform).anchoredPosition = new Vector2(2f, 6f);
            _discardOverwrite.onClick.AddListener(delegate () { _confirmOverwriteState = FastZip.Overwrite.Never; });

            
            (_downloadButton.transform as RectTransform).anchoredPosition = new Vector2(2f,-10f);

            yield return new WaitUntil(delegate() { return (_confirmOverwriteState == FastZip.Overwrite.Always || _confirmOverwriteState == FastZip.Overwrite.Never); });
            
            (_downloadButton.transform as RectTransform).anchoredPosition = new Vector2(2f,6f);
            
            Destroy(_overwriteText.gameObject);
            Destroy(_confirmOverwrite.gameObject);
            Destroy(_discardOverwrite.gameObject);
            
            

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

                _textComponents.First(x => x.name == "HighScoreText").text = "Expert";
                _textComponents.First(x => x.name == "HighScoreValueText").text = (_songs[row].difficultyLevels.Where(x => x.difficulty == "Expert").Count() > 0) ? "Yes" : "No";

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

            if (IsSongAlreadyDownloaded(_songs[row]))
            {
                ui.SetButtonText(ref _downloadButton, "Redownload");
            }
            else
            {
                ui.SetButtonText(ref _downloadButton, "Download");
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

        public bool IsSongAlreadyDownloaded(Song _song)
        {
            bool alreadyDownloaded = false;

            foreach (Song song in _alreadyDownloadedSongs)
            {
                alreadyDownloaded = alreadyDownloaded || song.Compare(_song);
            }

            return alreadyDownloaded;
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
            else
            {
                Directory.CreateDirectory(path + "/CustomSongs/.cache");
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

        private CustomSongInfo GetCustomSongInfo(string songPath)
        {
            var infoText = File.ReadAllText(songPath + "/info.json");
            CustomSongInfo songInfo;
            try
            {
                songInfo = JsonUtility.FromJson<CustomSongInfo>(infoText);
            }
            catch (Exception e)
            {
                Debug.Log("Error parsing song: " + songPath);
                return null;
            }
            songInfo.path = songPath;

            //Here comes SimpleJSON to the rescue when JSONUtility can't handle an array.
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

    }
}
