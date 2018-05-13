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

namespace BeatSaverDownloader
{
    class BeatSaverMasterViewController : VRUINavigationController
    {
        BeatSaverUI ui;

        BeatSaverSongListViewController _songListViewController;
        SongDetailViewController _songDetailViewController;

        bool _songDetailPushed = false;

        public List<Song> _songs = new List<Song>();
        public List<Song> _alreadyDownloadedSongs = new List<Song>();

        public TextMeshProUGUI _loadingText;
        public Button _downloadButton;
        Button _backButton;
        

        public SongLoader _songLoader;

        public string _sortBy = "top";
        public bool _loading = false;
        public int _selectedRow = -1;

        FastZip.Overwrite _confirmOverwriteState = FastZip.Overwrite.Prompt;

        protected override void DidActivate()
        {
            Debug.Log("Activated!");

            ui = FindObjectOfType<BeatSaverUI>();
            _songLoader = FindObjectOfType<SongLoader>();

            UpdateAlreadyDownloadedSongs();

            if(_songListViewController == null)
            {
                _songListViewController = ui.CreateViewController<BeatSaverSongListViewController>();
                _songListViewController.rectTransform.anchorMin = new Vector2(0.3f, 0f);
                _songListViewController.rectTransform.anchorMax = new Vector2(0.7f, 1f);

                PushViewController(_songListViewController,true);

            }
            else
            {
                PushViewController(_songListViewController,true);
            }

            

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
                        Debug.Log("Can't refresh songs! Exception: " + e);
                    }
                    DismissModalViewController(null, false);
                });
            }
           

            if (_loadingText == null)
            {
                _loadingText = ui.CreateText(rectTransform, "Loading...", new Vector2(-52f, -8f));
                _loadingText.rectTransform.sizeDelta = new Vector2(12.5f, 10f);
            }
            else
            {
                _loadingText.text = "Loading...";
            }

            StartCoroutine(GetSongs(0, _sortBy));

            base.DidActivate();

            
        }

        protected override void DidDeactivate()
        {
            _songDetailPushed = false;

            base.DidDeactivate();


        }

        public IEnumerator GetSongs(int page, string sortBy)
        {
            _songs.Clear();
            _songListViewController.RefreshScreen();
            

            UnityWebRequest www = UnityWebRequest.Get(String.Format("https://beatsaver.com/api.php?mode={0}&off={1}", sortBy, (page * _songListViewController._songsPerPage)));
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                try
                {
                    string parse = "{\"songs\": " + www.downloadHandler.text.Replace("][", ",") + "}";
                    
                    JSONNode node = JSON.Parse(parse);

                    

                    for(int i = 0; i < node["songs"].Count; i++)
                    {                        
                        _songs.Add(new Song(node["songs"][i]));
                    }

                    _loading = false;
                    _loadingText.text = "";
                    _songListViewController.RefreshScreen();
                    if (_selectedRow != -1)
                    {
                        _songListViewController._songsTableView.SelectRow(_selectedRow);
                        ShowDetails(_selectedRow);
                    }

                    _songListViewController._pageUpButton.interactable = (page == 0) ? false : true;
                    _songListViewController._pageDownButton.interactable = (_songs.Count < _songListViewController._songsPerPage) ? false : true;

                }
                catch(Exception e)
                {
                    Debug.Log("EXCEPTION IN GET SONGS: "+e.Message+" | "+e.StackTrace);
                }
            }

        }

        public void DownloadSong(int buttonId)
        {
            Debug.Log("Downloading "+_songs[buttonId].beatname);

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
                Debug.Log(www.error);
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
                    Debug.Log("FATAL EXCEPTION: "+e);

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

                _songListViewController._songsTableView.SelectRow(row);
                RefreshDetails(row);
                _loading = false;
                _loadingText.text = "";
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

        public void ShowDetails(int row)
        {
            _selectedRow = row;
            if(_songDetailViewController == null)
            {
                _songDetailViewController = Instantiate(Resources.FindObjectsOfTypeAll<SongDetailViewController>().First(), rectTransform, false);

                RefreshDetails(row);

                PushViewController(_songDetailViewController, false);
                _songDetailPushed = true;

            }
            else
            {
                if (_songDetailPushed)
                {
                    RefreshDetails(row);
                }
                else
                {
                    RefreshDetails(row);

                    PushViewController(_songDetailViewController, false);
                    _songDetailPushed = true;
                }
            }
        }

        private void RefreshDetails(int row)
        {
            
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

                _textComponents.First(x => x.name == "BPMText").text = "Upvotes";
                _textComponents.First(x => x.name == "BPMValueText").text = HTML5Decode.HtmlDecode(_songs[row].upvotes);

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
                Debug.Log("EXCEPTION: "+e);
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
                    Debug.Log("Custom song folder '" + song + "' is missing info.json!");
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
