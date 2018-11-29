using BeatSaverDownloader.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using VRUI;
using UnityEngine.UI;
using Logger = BeatSaverDownloader.Misc.Logger;
using SongLoaderPlugin;
using BeatSaverDownloader.UI.FlowCoordinators;

namespace BeatSaverDownloader.UI.ViewControllers
{
    enum DownloadState { Downloaded, Downloading, NotDownloaded};

    class SongDetailViewController : VRUIViewController
    {
        public event Action<Song> downloadButtonPressed;
        public event Action<Song> favoriteButtonPressed;

        private Song _currentSong;

        private TextMeshProUGUI songNameText;

        private TextMeshProUGUI difficulty1Text;
        private TextMeshProUGUI difficulty2Text;
        private TextMeshProUGUI difficulty3Text;

        private TextMeshProUGUI downloadsText;
        private TextMeshProUGUI playsText;
        private LevelParamsPanel _levelDetails;

        private Button _downloadButton;
        private Button _favoriteButton;

        //Time      - Downloads
        //BPM       - Plays
        //Notes     - BPM
        //Obstacles - Upvotes
        //Bombs     - Downvotes

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                RemoveCustomUIElements(rectTransform);

                _levelDetails = GetComponentsInChildren<LevelParamsPanel>().First(x => x.name == "LevelParamsPanel");
                foreach (HoverHint hint in _levelDetails.transform.GetComponentsInChildren<HoverHint>())
                {
                    switch (hint.name)
                    {
                        case "Time":
                            {
                                hint.GetComponentInChildren<Image>().sprite = Base64Sprites.DownloadIcon;
                            }; break;
                        case "BPM":
                            {
                                hint.GetComponentInChildren<Image>().sprite = Base64Sprites.PlayIcon;
                            }; break;
                        case "NotesCount":
                            {
                                hint.GetComponentInChildren<Image>().sprite = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "MetronomeIcon");
                            }; break;
                        case "ObstaclesCount":
                            {
                                hint.GetComponentInChildren<Image>().sprite = Base64Sprites.ThumbUp;
                            }; break;
                        case "BombsCount":
                            {
                                hint.GetComponentInChildren<Image>().sprite = Base64Sprites.ThumbDown;
                            }; break;
                    }

                    Destroy(hint);
                }

                RectTransform yourStats = GetComponentsInChildren<RectTransform>(true).First(x => x.name == "YourStats");
                yourStats.gameObject.SetActive(true);

                RectTransform buttonsRect = GetComponentsInChildren<RectTransform>().First(x => x.name == "Buttons");
                buttonsRect.anchoredPosition = new Vector2(0f, 6f);

                TextMeshProUGUI[] _textComponents = GetComponentsInChildren<TextMeshProUGUI>();

                try
                {
                    songNameText = _textComponents.First(x => x.name == "SongNameText");

                    downloadsText = _textComponents.First(x => x.name == "ValueText" && x.transform.parent.name == "Time");
                    downloadsText.fontSize = 3f;

                    playsText = _textComponents.First(x => x.name == "ValueText" && x.transform.parent.name == "BPM");
                    playsText.fontSize = 3f;

                    _textComponents.First(x => x.name == "YourStatsTitle").text = "Difficulties";

                    _textComponents.First(x => x.name == "HighScoreText").text = "Expert/+";
                    difficulty1Text = _textComponents.First(x => x.name == "HighScoreValueText");

                    _textComponents.First(x => x.name == "MaxComboText").text = "Hard";
                    difficulty2Text = _textComponents.First(x => x.name == "MaxComboValueText");

                    _textComponents.First(x => x.name == "MaxRankText").text = "Easy/Normal";
                    _textComponents.First(x => x.name == "MaxRankText").rectTransform.sizeDelta = new Vector2(18f, 3f);
                    difficulty3Text = _textComponents.First(x => x.name == "MaxRankValueText");
                }
                catch (Exception e)
                {
                    Logger.Exception("Unable to convert detail view controller! Exception:  " + e);
                }

                _downloadButton = GetComponentsInChildren<Button>().First(x => x.name == "PlayButton");
                _downloadButton.GetComponentsInChildren<TextMeshProUGUI>().First().text = "DOWNLOAD";
                _downloadButton.onClick.RemoveAllListeners();
                _downloadButton.onClick.AddListener(() => { downloadButtonPressed?.Invoke(_currentSong); });

                _favoriteButton = GetComponentsInChildren<Button>().First(x => x.name == "PracticeButton");
                _favoriteButton.GetComponentsInChildren<Image>().First(x => x.name == "Icon").sprite = Base64Sprites.AddToFavorites;
                _favoriteButton.onClick.RemoveAllListeners();
                _favoriteButton.onClick.AddListener(() => { favoriteButtonPressed?.Invoke(_currentSong); });

            }
        }

        public void SetFavoriteState(bool favorited)
        {
            _favoriteButton.GetComponentsInChildren<Image>().First(x => x.name == "Icon").sprite = (favorited ? Base64Sprites.RemoveFromFavorites : Base64Sprites.AddToFavorites);
        }

        public void SetDownloadState(DownloadState state)
        {
            _downloadButton.GetComponentsInChildren<TextMeshProUGUI>().First().text = (state == DownloadState.Downloading ? "QUEUED..." : (state == DownloadState.Downloaded ? "DELETE" : "DOWNLOAD"));
            _downloadButton.interactable = state != DownloadState.Downloading;
        }

        public void SetContent(MoreSongsFlowCoordinator sender, Song newSongInfo)
        {
            _currentSong = newSongInfo;

            songNameText.text = newSongInfo.songName;

            downloadsText.text = newSongInfo.downloads;
            _levelDetails.bpm = float.Parse(newSongInfo.plays);
            _levelDetails.notesCount = int.Parse(newSongInfo.beatsPerMinute);
            _levelDetails.obstaclesCount = int.Parse(newSongInfo.upvotes);
            _levelDetails.bombsCount = int.Parse(newSongInfo.downvotes);

            difficulty1Text.text = (newSongInfo.difficultyLevels.Where(x => (x.difficulty == "Expert" || x.difficulty == "ExpertPlus")).Count() > 0) ? "Yes" : "No";
            difficulty2Text.text = (newSongInfo.difficultyLevels.Where(x => x.difficulty == "Hard").Count() > 0) ? "Yes" : "No";
            difficulty3Text.text = (newSongInfo.difficultyLevels.Where(x => (x.difficulty == "Easy" || x.difficulty == "Normal")).Count() > 0) ? "Yes" : "No";

            SetFavoriteState(PluginConfig.favoriteSongs.Any(x => x.Contains(newSongInfo.hash)));
            SetDownloadState((SongDownloader.Instance.IsSongDownloaded(newSongInfo) ? DownloadState.Downloaded : (sender.IsDownloadingSong(newSongInfo) ? DownloadState.Downloading : DownloadState.NotDownloaded)));
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
