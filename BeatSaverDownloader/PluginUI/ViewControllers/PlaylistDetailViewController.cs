using BeatSaverDownloader.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace BeatSaverDownloader.PluginUI.ViewControllers
{
    class PlaylistDetailViewController : VRUIViewController
    {
        public event Action selectPressed;
        public event Action downloadPressed;

        private Logger log = new Logger("BeatSaverDownloader");

        Button _selectButton;
        Button _downloadButton;

        TextMeshProUGUI songNameText;
        TextMeshProUGUI songsText;
        TextMeshProUGUI authorNameText;
        private PlaylistNavigationController _parentMasterViewController;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if(firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                RemoveCustomUIElements(rectTransform);

                RectTransform _levelDetails = GetComponentsInChildren<RectTransform>().First(x => x.name == "LevelDetails");
                _levelDetails.sizeDelta = new Vector2(44f, 20f);
                RectTransform _yourStats = GetComponentsInChildren<RectTransform>(true).First(x => x.name == "YourStats");
                _yourStats.sizeDelta = new Vector2(44f, 18f);
                _yourStats.gameObject.SetActive(true);

                _selectButton = GetComponentInChildren<Button>();
                (_selectButton.transform as RectTransform).sizeDelta = new Vector2(30f, 10f);
                (_selectButton.transform as RectTransform).anchoredPosition = new Vector2(2f, 6f);
                BeatSaberUI.SetButtonText(_selectButton, "Select");
                _selectButton.onClick.AddListener(delegate () { selectPressed?.Invoke(); });

                _downloadButton = BeatSaberUI.CreateUIButton((RectTransform)_selectButton.transform.parent, "PlayButton");
                (_downloadButton.transform as RectTransform).sizeDelta = new Vector2(30f, 10f);
                (_downloadButton.transform as RectTransform).anchoredPosition = new Vector2(2f, 24f);
                BeatSaberUI.SetButtonText(_downloadButton, "Download");
                _downloadButton.onClick.AddListener(delegate () { downloadPressed?.Invoke(); });

                TextMeshProUGUI[] _textComponents = GetComponentsInChildren<TextMeshProUGUI>();

                try
                {
                    songNameText = _textComponents.First(x => x.name == "SongNameText");

                    songsText = _textComponents.First(x => x.name == "DurationValueText");
                    _textComponents.First(x => x.name == "DurationText").text = "Songs";

                    _textComponents.First(x => x.name == "BPMText").text = "Author";
                    authorNameText = _textComponents.First(x => x.name == "BPMValueText");

                    authorNameText.rectTransform.sizeDelta = new Vector2(16f, 3f);
                    authorNameText.rectTransform.anchoredPosition -= new Vector2(6f, 0f);
                    authorNameText.alignment = TextAlignmentOptions.CaplineRight;

                    if (_textComponents.Where(x => x.name == "ObstaclesCountText").Count() != 0)
                    {
                        Destroy(_textComponents.First(x => x.name == "ObstaclesCountText").gameObject);
                        Destroy(_textComponents.First(x => x.name == "ObstaclesCountValueText").gameObject);
                        Destroy(_textComponents.First(x => x.name == "Title").gameObject);
                        Destroy(_textComponents.First(x => x.name == "HighScoreText").gameObject);
                        Destroy(_textComponents.First(x => x.name == "MaxComboText").gameObject);
                        Destroy(_textComponents.First(x => x.name == "MaxRankText").gameObject);
                        Destroy(_textComponents.First(x => x.name == "HighScoreValueText").gameObject);
                        Destroy(_textComponents.First(x => x.name == "MaxComboValueText").gameObject);
                        Destroy(_textComponents.First(x => x.name == "MaxRankValueText").gameObject);
                        Destroy(_textComponents.First(x => x.name == "NotesCountText").gameObject);
                        Destroy(_textComponents.First(x => x.name == "NotesCountValueText").gameObject);
                    }
                }
                catch (Exception e)
                {
                    Logger.Exception("EXCEPTION: " + e);
                }
            }
        }

        public void UpdateContent(Playlist playlist)
        {
            songNameText.text = playlist.playlistTitle;
            authorNameText.text = playlist.playlistAuthor;
            songsText.text = playlist.songs.Count.ToString();
        }

        public void UpdateButtons(bool enableSelect, bool enableDownload)
        {
            _selectButton.interactable = enableSelect;
            _downloadButton.interactable = enableDownload;
        }

        protected override void LeftAndRightScreenViewControllers(out VRUIViewController leftScreenViewController, out VRUIViewController rightScreenViewController)
        {

            if (_parentMasterViewController == null)
            {
                _parentMasterViewController = GetComponentInParent<PlaylistNavigationController>();
            }
            if (_parentMasterViewController.downloadQueueViewController == null)
            {
                _parentMasterViewController.downloadQueueViewController = BeatSaberUI.CreateViewController<DownloadQueueViewController>();
            }
            leftScreenViewController = _parentMasterViewController.downloadQueueViewController;
            rightScreenViewController = null;
        }

        void RemoveCustomUIElements(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                if (child.name.Contains("CustomUI"))
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
