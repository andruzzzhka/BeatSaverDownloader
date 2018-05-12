using HMUI;
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

        public bool _loading = false;
        public int _selectedRow = -1;

       

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

            StartCoroutine(GetSongs(0));

            base.DidActivate();

            
        }

        protected override void DidDeactivate()
        {
            _songDetailPushed = false;

            base.DidDeactivate();


        }

        public IEnumerator GetSongs(int page)
        {

            UnityWebRequest www = UnityWebRequest.Get("https://beatsaver.com/api.php?mode=top&off="+(page * _songListViewController._songsPerPage));
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

                    _songs.Clear();

                    for(int i = 0; i < node["songs"].Count; i++)
                    {                        
                        _songs.Add(new Song(node["songs"][i]));
                    }

                    _loading = false;
                    _loadingText.text = "";
                    _songListViewController.RefreshScreen();

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
                try
                {
                    byte[] data = www.downloadHandler.data;

                    
                    string docPath = Application.dataPath;
                    docPath = docPath.Substring(0, docPath.Length - 5);
                    docPath = docPath.Substring(0, docPath.LastIndexOf("/"));
                    string customSongsPath = docPath + "/CustomSongs/";
                    docPath += "/CustomSongs/"+songInfo.beatname+".zip";
                    File.WriteAllBytes(docPath, data);

                    FastZip zip = new FastZip();

                    zip.ExtractZip(docPath, customSongsPath,null);

                    File.Delete(docPath);

                    UpdateAlreadyDownloadedSongs();

                    _songListViewController.RefreshScreen();

                    RefreshDetails(row);

                    _loading = false;
                    _loadingText.text = "";
                    _downloadButton.interactable = true;
                    

                    }
                catch (Exception e)
                {
                    Debug.Log("EXCEPTION IN DOWNLOAD SONG: " + e.Message + " | " + e.StackTrace);
                }
            }
        }

        public void ShowDetails(int row)
        {
            if(_songDetailViewController == null)
            {
                _songDetailViewController = Instantiate(Resources.FindObjectsOfTypeAll<SongDetailViewController>().First(), rectTransform, false);

                RefreshDetails(row);

                PushViewController(_songDetailViewController, false);

                
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
                }
            }
        }

        private void RefreshDetails(int row)
        {
            
            RectTransform _levelDetails = _songDetailViewController.GetComponentsInChildren<RectTransform>().Where(x => x.name == "LevelDetails").First();
            _levelDetails.sizeDelta = new Vector2(44f, 20f);
            RectTransform _yourStats = _songDetailViewController.GetComponentsInChildren<RectTransform>().Where(x => x.name == "YourStats").First();
            _yourStats.sizeDelta = new Vector2(44f, 18f);
             

            TextMeshProUGUI[] _textComponents = _songDetailViewController.GetComponentsInChildren<TextMeshProUGUI>();
            
            _textComponents.Where(x => x.name == "SongNameText").First().text = string.Format("{0}\n<size=80%>{1}</size>", HTML5Decode.HtmlDecode(_songs[row].songName), HTML5Decode.HtmlDecode(_songs[row].songSubName));
            _textComponents.Where(x => x.name == "DurationValueText").First().text = HTML5Decode.HtmlDecode(_songs[row].downloads);
            _textComponents.Where(x => x.name == "DurationText").First().text = "Downloads";

            _textComponents.Where(x => x.name == "BPMText").First().text = "Upvotes";
            _textComponents.Where(x => x.name == "BPMValueText").First().text = HTML5Decode.HtmlDecode(_songs[row].upvotes);

            _textComponents.Where(x => x.name == "NotesCountText").First().text = "Author";
            _textComponents.Where(x => x.name == "NotesCountValueText").First().text = HTML5Decode.HtmlDecode(_songs[row].authorName);

            _textComponents.Where(x => x.name == "NotesCountValueText").First().rectTransform.sizeDelta = new Vector2(16f,3f);
            _textComponents.Where(x => x.name == "NotesCountValueText").First().alignment = TextAlignmentOptions.CaplineRight;

            _textComponents.Where(x => x.name == "Title").First().text = "Difficulties";

            _textComponents.Where(x => x.name == "HighScoreText").First().text = "Expert";
            _textComponents.Where(x => x.name == "HighScoreValueText").First().text = (_songs[row].difficultyLevels.Where(x => x.difficulty == "Expert").Count() > 0) ? "Yes" : "No";

            _textComponents.Where(x => x.name == "MaxComboText").First().text = "Hard";
            _textComponents.Where(x => x.name == "MaxComboValueText").First().text = (_songs[row].difficultyLevels.Where(x => x.difficulty == "Hard").Count() > 0) ? "Yes" : "No";

            _textComponents.Where(x => x.name == "MaxRankText").First().text = "Easy/Normal";
            _textComponents.Where(x => x.name == "MaxRankText").First().rectTransform.sizeDelta = new Vector2(18f,3f);
            _textComponents.Where(x => x.name == "MaxRankValueText").First().text = (_songs[row].difficultyLevels.Where(x => (x.difficulty == "Easy" || x.difficulty == "Normal")).Count() > 0) ? "Yes" : "No";

            
            Destroy(_textComponents.Where(x => x.name == "ObstaclesCountText").First().gameObject);
            Destroy(_textComponents.Where(x => x.name == "ObstaclesCountValueText").First().gameObject);
            
            _downloadButton = _songDetailViewController.GetComponentInChildren<Button>();
            (_downloadButton.transform as RectTransform).sizeDelta = new Vector2(30f,10f);
            (_downloadButton.transform as RectTransform).anchoredPosition = new Vector2(2f, 6f);

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
