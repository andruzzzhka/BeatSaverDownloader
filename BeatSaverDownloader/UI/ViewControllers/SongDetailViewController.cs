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
using BeatSaverDownloader.UI.FlowCoordinators;
using CustomUI.BeatSaber;

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

        private RawImage coverImage;

        private LevelParamsPanel _levelParams;
        private StandardLevelDetailView _levelDetails;

        private Button _downloadButton;
        private Button _favoriteButton;

        //Time      - Downloads
        //BPM       - Plays
        //Notes     - BPM
        //Obstacles - Upvotes
        //Bombs     - Downvotes

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation)
            {
                gameObject.SetActive(true);
                _levelDetails = GetComponentsInChildren<StandardLevelDetailView>(true).First(x => x.name == "LevelDetail");
                _levelDetails.gameObject.SetActive(true);

                RemoveCustomUIElements(rectTransform);

                _levelParams = GetComponentsInChildren<LevelParamsPanel>().First(x => x.name == "LevelParamsPanel");

                foreach (HoverHint hint in _levelParams.transform.GetComponentsInChildren<HoverHint>())
                {
                    switch (hint.name)
                    {
                        case "Time":
                            {
                                hint.GetComponentInChildren<Image>().sprite = Sprites.DownloadIcon;
                            }; break;
                        case "BPM":
                            {
                                hint.GetComponentInChildren<Image>().sprite = Sprites.PlayIcon;
                            }; break;
                        case "NotesCount":
                            {
                                hint.GetComponentInChildren<Image>().sprite = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "MetronomeIcon");
                            }; break;
                        case "ObstaclesCount":
                            {
                                hint.GetComponentInChildren<Image>().sprite = Sprites.ThumbUp;
                            }; break;
                        case "BombsCount":
                            {
                                hint.GetComponentInChildren<Image>().sprite = Sprites.ThumbDown;
                            }; break;
                    }

                    Destroy(hint);
                }
                
                RectTransform yourStats = GetComponentsInChildren<RectTransform>().First(x => x.name == "Stats");
                yourStats.gameObject.SetActive(true);
                
                TextMeshProUGUI[] _textComponents = GetComponentsInChildren<TextMeshProUGUI>();
                
                try
                {
                    songNameText = _textComponents.First(x => x.name == "SongNameText");

                    downloadsText = _textComponents.First(x => x.name == "ValueText" && x.transform.parent.name == "Time");
                    downloadsText.fontSize = 3f;

                    playsText = _textComponents.First(x => x.name == "ValueText" && x.transform.parent.name == "BPM");
                    playsText.fontSize = 3f;
                    
                    _textComponents.First(x => x.name == "Title" && x.transform.parent.name == "MaxRank").text = "Expert/+";
                    difficulty1Text = _textComponents.First(x => x.name == "Value" && x.transform.parent.name == "MaxRank");

                    _textComponents.First(x => x.name == "Title" && x.transform.parent.name == "Highscore").text = "Hard";
                    difficulty2Text = _textComponents.First(x => x.name == "Value" && x.transform.parent.name == "Highscore");

                    _textComponents.First(x => x.name == "Title" && x.transform.parent.name == "MaxCombo").text = "Easy/Normal";
                    difficulty3Text = _textComponents.First(x => x.name == "Value" && x.transform.parent.name == "MaxCombo");
                }
                catch (Exception e)
                {
                    Plugin.log.Critical("Unable to convert detail view controller! Exception:  " + e);
                }

                _downloadButton = _levelDetails.playButton;
                _downloadButton.SetButtonText("DOWNLOAD");
                _downloadButton.ToggleWordWrapping(false);
                _downloadButton.onClick.RemoveAllListeners();
                _downloadButton.onClick.AddListener(() => { downloadButtonPressed?.Invoke(_currentSong); });
                (_downloadButton.transform as RectTransform).sizeDelta = new Vector2(26f, 8.8f);

                _favoriteButton = _levelDetails.practiceButton;
                _favoriteButton.SetButtonIcon(Sprites.AddToFavorites);
                _favoriteButton.onClick.RemoveAllListeners();
                _favoriteButton.onClick.AddListener(() => { favoriteButtonPressed?.Invoke(_currentSong); });

                coverImage = _levelDetails.GetPrivateField<RawImage>("_coverImage");
            }
        }

        public void SetFavoriteState(bool favorited)
        {
            _favoriteButton.SetButtonIcon(favorited ? Sprites.RemoveFromFavorites : Sprites.AddToFavorites);
        }

        public void SetDownloadState(DownloadState state)
        {
            _downloadButton.SetButtonText(state == DownloadState.Downloading ? "QUEUED..." : (state == DownloadState.Downloaded ? "DELETE" : "DOWNLOAD"));
            _downloadButton.interactable = state != DownloadState.Downloading;
        }

        public void SetContent(MoreSongsFlowCoordinator sender, Song newSongInfo)
        {
            _currentSong = newSongInfo;

            songNameText.text = _currentSong.songName;

            downloadsText.text = _currentSong.downloads;
            _levelParams.bpm = float.Parse(_currentSong.plays);
            _levelParams.notesCount = int.Parse(_currentSong.beatsPerMinute);
            _levelParams.obstaclesCount = int.Parse(_currentSong.upvotes);
            _levelParams.bombsCount = int.Parse(_currentSong.downvotes);

            difficulty1Text.text = (_currentSong.difficultyLevels.Where(x => (x.difficulty == "Expert" || x.difficulty == "ExpertPlus")).Count() > 0) ? "Yes" : "No";
            difficulty2Text.text = (_currentSong.difficultyLevels.Where(x => x.difficulty == "Hard").Count() > 0) ? "Yes" : "No";
            difficulty3Text.text = (_currentSong.difficultyLevels.Where(x => (x.difficulty == "Easy" || x.difficulty == "Normal")).Count() > 0) ? "Yes" : "No";

            StartCoroutine(LoadScripts.LoadSpriteCoroutine(_currentSong.coverUrl, (cover) => { coverImage.texture = cover.texture;}));

            SetFavoriteState(PluginConfig.favoriteSongs.Any(x => x.Contains(_currentSong.hash)));
            SetDownloadState((SongDownloader.Instance.IsSongDownloaded(_currentSong) ? DownloadState.Downloaded : (sender.IsDownloadingSong(_currentSong) ? DownloadState.Downloading : DownloadState.NotDownloaded)));
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
