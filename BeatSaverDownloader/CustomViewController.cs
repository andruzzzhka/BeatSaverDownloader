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

namespace BeatSaverDownloader
{
    


    class CustomViewController : VRUI.VRUIViewController, TableView.IDataSource
    {
        CustomUI ui;

        List<Song> _songs = new List<Song>();

        List<Song> _alreadyDownloadedSongs = new List<Song>();

        TextMeshProUGUI _loadingText;
        TextMeshProUGUI _pageText;

        Button _downloadButton;

        Button _pageUpButton;
        Button _pageDownButton;

        TableView _songsTableView;

        SongLoader _songLoader;

        int _currentPage = 0;

        int _songsPerPage = 6;

        bool _loading = false;

        int _selectedRow = -1;

        SongListTableCell _songListTableCellInstance;

        protected override void DidActivate()
        {
            Debug.Log("Activated!");

            ui = FindObjectOfType<CustomUI>();
            _songLoader = FindObjectOfType<SongLoader>();

            UpdateAlreadyDownloadedSongs();

            try
            {
                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().Where(x => (x.name == "PageUpButton")).First(),rectTransform,false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f,1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f,1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -10f);
                _pageUpButton.interactable = true;
                _pageUpButton.onClick.AddListener(delegate ()
                 {

                     if (_currentPage > 0)
                     {
                         if (!_loading)
                         {
                             _loading = true;
                             _loadingText.text = "Loading...";
                             _selectedRow = -1;
                             _downloadButton.gameObject.SetActive(false);
                             StartCoroutine(GetSongs(_currentPage - 1));
                         }
                     }


                 });


                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().Where(x => (x.name == "PageDownButton")).First(), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 10f);
                _pageDownButton.interactable = true;
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    if (!_loading)
                    {
                        _loading = true;
                        _loadingText.text = "Loading...";
                        _selectedRow = -1;
                        _downloadButton.gameObject.SetActive(false);
                        StartCoroutine(GetSongs(_currentPage + 1));
                    }

                });

                _songListTableCellInstance = Resources.FindObjectsOfTypeAll<SongListTableCell>().Where(x => (x.name == "SongListTableCell")).First();

            }
            catch(Exception e)
            {
                Debug.Log("EXCEPTION IN DidActivate: "+e);
            }

            Button _backButton = ui.CreateBackButton(rectTransform);
            

            _backButton.onClick.AddListener(delegate () {
                try
                {
                    _songLoader.RefreshSongs();
                }catch(Exception e)
                {
                    Debug.Log("Can't refresh songs!");
                }
                DismissModalViewController(null, false);
            });

            _downloadButton = ui.CreateUIButton(rectTransform);

            (_downloadButton.transform as RectTransform).anchorMin = new Vector2(0.5f,1f);
            (_downloadButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
            (_downloadButton.transform as RectTransform).anchoredPosition = new Vector2(-66f, -50f);
            (_downloadButton.transform as RectTransform).sizeDelta = new Vector2(32f, 10f);

            ui.SetButtonText(ref _downloadButton,"Download");
            ui.SetButtonIcon(ref _downloadButton, CustomUI.icons.Where(x => x.name == "PlayIcon").First());

            _downloadButton.onClick.AddListener(delegate() {

                if(_selectedRow != -1 && !_loading)
                {
                    DownloadSong(_selectedRow);
                }

            });

            _downloadButton.gameObject.SetActive(false);

            
            _loadingText = ui.CreateText(rectTransform, "Loading songs from BeatSaver...", new Vector2(-34f, -32f));
            StartCoroutine(GetSongs(0));

            base.DidActivate();

            
        }

        protected override void DidDeactivate()
        {
            
            Destroy(_songsTableView.gameObject);
            Destroy(_loadingText.gameObject);
            Destroy(_pageText.gameObject);

            Destroy(_downloadButton.gameObject);

            Destroy(_pageUpButton.gameObject);
            Destroy(_pageDownButton.gameObject);
            
            base.DidDeactivate();
            

        }

        IEnumerator GetSongs(int page)
        {
            _currentPage = page;

            UnityWebRequest www = UnityWebRequest.Get("https://beatsaver.com/api.php?mode=top&off="+(_currentPage*_songsPerPage));
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
                    OnSongsParsed();

                }catch(Exception e)
                {
                    Debug.Log("EXCEPTION IN GET SONGS: "+e.Message+" | "+e.StackTrace);
                }
            }

        }

        public void OnSongsParsed()
        {

            if (_pageText == null)
            {
                _pageText = ui.CreateText(rectTransform, "PAGE "+(_currentPage+1), new Vector2(-34f, -36f));
                _pageText.fontSize = 8;
            }
            else
            {
                _pageText.text = "PAGE " + (_currentPage + 1);
            }

            RefreshScreen();

        }

        public void RefreshScreen()
        {

            if (_songsTableView == null)
            {
                _songsTableView = new GameObject().AddComponent<TableView>();

                _songsTableView.transform.SetParent(rectTransform, false);

                _songsTableView.dataSource = this;

                (_songsTableView.transform as RectTransform).anchorMin = new Vector2(0.3f, 0.125f);
                (_songsTableView.transform as RectTransform).anchorMax = new Vector2(0.7f, 0.875f);
                (_songsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 0f);
                (_songsTableView.transform as RectTransform).position = new Vector3(0f, 0f, 2.4f);
                (_songsTableView.transform as RectTransform).anchoredPosition = new Vector2(0f, 0f);

                _songsTableView.DidSelectRowEvent += _songsTableView_DidSelectRowEvent;
            }
            else
            {
                _songsTableView.ReloadData();
            }

        }

        private void _songsTableView_DidSelectRowEvent(TableView sender, int row)
        {
            if (!_loading)
            {
                _selectedRow = row;
                _downloadButton.gameObject.SetActive(true);

                if (IsSongAlreadyDownloaded(_songs[row])) {
                    ui.SetButtonText(ref _downloadButton, "Redownload");
                }
                else
                {
                    ui.SetButtonText(ref _downloadButton, "Download");
                }
            }
            else
            {
                if(_selectedRow != -1)
                {
                    _songsTableView.SelectRow(_selectedRow);
                }
            }

        }

        private string HtmlDecode(string songName)
        {
            string buf = songName;

            buf = buf.Replace("&amp;","&").Replace("&period;", ".").Replace("&lpar;","(").Replace("&rpar;",")").Replace("&semi;",";").Replace("&lbrack;","[").Replace("&rsqb;","]").Replace("&apos;","\'");

            return buf;
        }

        public void DownloadSong(int buttonId)
        {
            Debug.Log("Downloading "+_songs[buttonId].beatname);

            StartCoroutine(DownloadSongCoroutine(_songs[buttonId],buttonId));

        }

        public IEnumerator DownloadSongCoroutine(Song songInfo, int row)
        {

            _loading = true;
            _loadingText.text = "Downloading...";
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
                    _songsTableView.ReloadData();
                    _songsTableView.SelectRow(row);
                    if (IsSongAlreadyDownloaded(_songs[row]))
                    {
                        ui.SetButtonText(ref _downloadButton, "Redownload");
                    }
                    else
                    {
                        ui.SetButtonText(ref _downloadButton, "Download");
                    }

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

        public float RowHeight()
        {
            return 10f;
        }

        public int NumberOfRows()
        {
            return Math.Min(_songsPerPage,_songs.Count);
        }

        public TableCell CellForRow(int row)
        {
            SongListTableCell _tableCell = Instantiate(_songListTableCellInstance);

            (_tableCell.transform as RectTransform).anchorMin = new Vector2(0f, 1f);
            (_tableCell.transform as RectTransform).anchorMax = new Vector2(0f, 1f);
            (_tableCell.transform as RectTransform).sizeDelta = new Vector2(0f, 10f);
            
            _tableCell.songName = string.Format("{0}\n<size=80%>{1}</size>", HtmlDecode(_songs[row].songName), HtmlDecode(_songs[row].songSubName));
            _tableCell.author = HtmlDecode(_songs[row].authorName);
            StartCoroutine(LoadSprite("https://beatsaver.com/img/"+ _songs[row].id+ "."+ _songs[row].img, _tableCell));

            bool alreadyDownloaded = IsSongAlreadyDownloaded(_songs[row]);
            
            if (alreadyDownloaded)
            {

                _tableCell.GetComponentsInChildren<UnityEngine.UI.Image>()[0].color = Color.gray;
                _tableCell.GetComponentsInChildren<UnityEngine.UI.Image>()[1].color = Color.gray;
                _tableCell.GetComponentsInChildren<UnityEngine.UI.Image>()[2].color = Color.gray;
                
            }

            return _tableCell;
        }

        private IEnumerator LoadSprite(string spritePath, TableCell obj)
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

        private void UpdateAlreadyDownloadedSongs()
        {
            _alreadyDownloadedSongs.Clear();
            foreach (CustomSongInfo song in RetrieveAllSongs())
            {
                _alreadyDownloadedSongs.Add(new Song(song));
            }
        }







        private bool IsSongAlreadyDownloaded(Song _song)
        {
            bool alreadyDownloaded = false;

            foreach (Song song in _alreadyDownloadedSongs)
            {
                alreadyDownloaded = alreadyDownloaded || song.Compare(_song);
            }

            return alreadyDownloaded;
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
