using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI.FlowCoordinators;
using CustomUI.BeatSaber;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using Logger = BeatSaverDownloader.Misc.Logger;

namespace BeatSaverDownloader.UI.ViewControllers
{
    class PlaylistDetailViewController : VRUIViewController
    {

        public event Action<Playlist> downloadButtonPressed;
        public event Action<Playlist> selectButtonPressed;

        private Playlist _currentPlaylist;

        private TextMeshProUGUI songNameText;
        
        private Button _downloadButton;
        private Button _selectButton;
        private string _selectButtonText = "Select";

        private TextMeshProUGUI authorText;
        private TextMeshProUGUI totalSongsText;
        private TextMeshProUGUI downloadedSongsText;

        public bool addDownloadButton = true;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {

            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                RemoveCustomUIElements(rectTransform);

                Destroy(GetComponentsInChildren<LevelParamsPanel>().First(x => x.name == "LevelParamsPanel").gameObject);
                
                RectTransform yourStats = GetComponentsInChildren<RectTransform>(true).First(x => x.name == "YourStats");
                yourStats.gameObject.SetActive(true);

                RectTransform buttonsRect = GetComponentsInChildren<RectTransform>().First(x => x.name == "Buttons");
                buttonsRect.anchoredPosition = new Vector2(0f, 6f);

                TextMeshProUGUI[] _textComponents = GetComponentsInChildren<TextMeshProUGUI>();

                try
                {
                    songNameText = _textComponents.First(x => x.name == "SongNameText");
                    _textComponents.First(x => x.name == "Title").text = "Playlist";

                    _textComponents.First(x => x.name == "YourStatsTitle").text = "Playlist Info";

                    _textComponents.First(x => x.name == "HighScoreText").text = "Author";
                    authorText = _textComponents.First(x => x.name == "HighScoreValueText");
                    authorText.rectTransform.sizeDelta = new Vector2(24f, 0f);

                    _textComponents.First(x => x.name == "MaxComboText").text = "Total songs";
                    totalSongsText = _textComponents.First(x => x.name == "MaxComboValueText");

                    _textComponents.First(x => x.name == "MaxRankText").text = "Downloaded";
                    _textComponents.First(x => x.name == "MaxRankText").rectTransform.sizeDelta = new Vector2(18f, 3f);
                    downloadedSongsText = _textComponents.First(x => x.name == "MaxRankValueText");
                }
                catch (Exception e)
                {
                    Logger.Exception("Unable to convert detail view controller! Exception:  " + e);
                }

                _selectButton = GetComponentsInChildren<Button>().First(x => x.name == "PlayButton");
                _selectButton.SetButtonText(_selectButtonText);
                _selectButton.onClick.RemoveAllListeners();
                _selectButton.onClick.AddListener(() => { selectButtonPressed?.Invoke(_currentPlaylist); });

                if (addDownloadButton)
                {
                    _downloadButton = GetComponentsInChildren<Button>().First(x => x.name == "PracticeButton");
                    _downloadButton.GetComponentsInChildren<Image>().First(x => x.name == "Icon").sprite = Base64Sprites.DownloadIcon;
                    _downloadButton.onClick.RemoveAllListeners();
                    _downloadButton.onClick.AddListener(() => { downloadButtonPressed?.Invoke(_currentPlaylist); });
                }
                else
                {
                    Destroy(GetComponentsInChildren<Button>().First(x => x.name == "PracticeButton").gameObject);
                }
            }
        }

        public void SetDownloadState(bool downloaded)
        {
            _downloadButton.interactable = !downloaded;
        }

        public void SetSelectButtonState(bool enabled)
        {
            _selectButton.interactable = enabled;
        }

        public void SetSelectButtonText(string text)
        {
            _selectButtonText = text;
            if (_selectButton != null)
            {
                _selectButton.SetButtonText(_selectButtonText);
            }
        }

        public void SetContent(Playlist newPlaylist)
        {
            _currentPlaylist = newPlaylist;

            songNameText.text = newPlaylist.playlistTitle;

            authorText.text = newPlaylist.playlistAuthor;

            if (newPlaylist.songs.Count > 0)
            {
                totalSongsText.text = newPlaylist.songs.Count.ToString();
                downloadedSongsText.text = newPlaylist.songs.Where(x => x.level != null).Count().ToString();
                SetDownloadState(newPlaylist.songs.All(x => x.level != null));
            }
            else
            {
                totalSongsText.text = newPlaylist.playlistSongCount.ToString();
                downloadedSongsText.text = "??";
            }
        }

        void RemoveCustomUIElements(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                if (child.name.StartsWith("CustomUI"))
                {
                    Destroy(child.gameObject);
                }
                if (child.childCount > 0)
                {
                    RemoveCustomUIElements(child);
                }
            }
        }
    }
}
