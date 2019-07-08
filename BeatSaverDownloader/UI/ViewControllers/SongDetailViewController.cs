using BeatSaverDownloader.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using VRUI;
using UnityEngine.UI;
using BeatSaverDownloader.UI.FlowCoordinators;
using CustomUI.BeatSaber;
using HMUI;

namespace BeatSaverDownloader.UI.ViewControllers
{
    enum DownloadState { Downloaded, Downloading, NotDownloaded};

    class SongDetailViewController : VRUIViewController
    {
        public event Action<Song> downloadButtonPressed;
        public event Action<Song> favoriteButtonPressed;

        private Song _currentSong;

        private TextMeshProUGUI songNameText;
        private IconSegmentedControl _characteristicSegmentedDisplay;
        private TextSegmentedControl _difficultySegmentedDisplay;
        private TextMeshProUGUI difficulty1Text;
        private TextMeshProUGUI difficulty2Text;
        private TextMeshProUGUI difficulty3Text;
        private TextMeshProUGUI difficulty1Title;
        private TextMeshProUGUI difficulty2Title;
        private TextMeshProUGUI difficulty3Title;

        private TextMeshProUGUI downloadsText;
        private TextMeshProUGUI playsText;

        private RawImage coverImage;

        private LevelParamsPanel _levelParams;
        private StandardLevelDetailView _levelDetails;

        private Button _downloadButton;
        private Button _favoriteButton;

        private GameObject _loadingIndicator;

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
                (_levelDetails.transform as RectTransform).anchoredPosition = new Vector2(-40, 0);
                BeatmapDifficultySegmentedControlController beatmapDifficultySegmentedControl = GetComponentsInChildren<BeatmapDifficultySegmentedControlController>(true).First(x => x.name == "BeatmapDifficultySegmentedControl");
                beatmapDifficultySegmentedControl.gameObject.SetActive(false);
                RemoveCustomUIElements(rectTransform);

                _levelParams = GetComponentsInChildren<LevelParamsPanel>().First(x => x.name == "LevelParamsPanel");

                foreach (HoverHint hint in _levelParams.transform.GetComponentsInChildren<HoverHint>())
                {
                    switch (hint.name)
                    {
                        case "Time":
                            {
                                hint.GetComponentInChildren<UnityEngine.UI.Image>().sprite = Sprites.DownloadIcon;
                            }; break;
                        case "BPM":
                            {
                                hint.GetComponentInChildren<UnityEngine.UI.Image>().sprite = Sprites.PlayIcon;
                            }; break;
                        case "NotesCount":
                            {
                                hint.GetComponentInChildren<UnityEngine.UI.Image>().sprite = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "MetronomeIcon");
                            }; break;
                        case "ObstaclesCount":
                            {
                                hint.GetComponentInChildren<UnityEngine.UI.Image>().sprite = Sprites.ThumbUp;
                            }; break;
                        case "BombsCount":
                            {
                                hint.GetComponentInChildren<UnityEngine.UI.Image>().sprite = Sprites.ThumbDown;
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
            //        foreach (var x in _textComponents)
            //        {
            //            Console.WriteLine(x.name);
            //            Console.WriteLine(x.transform.parent.name);
            //        }
                    difficulty1Title = _textComponents.First(x => x.name == "Title" && x.transform.parent.name == "MaxRank"); //.text = "Expert/+";
                    difficulty1Text = _textComponents.First(x => x.name == "Value" && x.transform.parent.name == "MaxRank");

                    difficulty2Title = _textComponents.First(x => x.name == "Title" && x.transform.parent.name == "Highscore");//.text = "Hard";
                    difficulty2Text = _textComponents.First(x => x.name == "Value" && x.transform.parent.name == "Highscore");

                    difficulty3Title = _textComponents.First(x => x.name == "Title" && x.transform.parent.name == "MaxCombo");//.text = "Easy/Normal";
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

                _loadingIndicator = BeatSaberUI.CreateLoadingSpinner(rectTransform);
                (_loadingIndicator.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
                (_loadingIndicator.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
                (_loadingIndicator.transform as RectTransform).anchoredPosition = new Vector2(0f, 0f);
                (_loadingIndicator.transform as RectTransform).anchoredPosition = new Vector2(-40f, 0f);
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
            if (_characteristicSegmentedDisplay == null)
            {
                _characteristicSegmentedDisplay = BeatSaberUI.CreateIconSegmentedControl(rectTransform, new Vector2(-40, .2f), new Vector2(70, 9f),
                    delegate (int value) { if (value != 0) _characteristicSegmentedDisplay.SelectCellWithNumber(0); });
                SetupCharacteristicDisplay(_characteristicSegmentedDisplay, _currentSong);
            }
            else
                SetupCharacteristicDisplay(_characteristicSegmentedDisplay, _currentSong);

            if (_difficultySegmentedDisplay == null)
            {
                _difficultySegmentedDisplay = BeatSaberUI.CreateTextSegmentedControl(rectTransform, new Vector2(-40, -9f), new Vector2(85, 8f),
                    delegate (int value) { if (value != 0) _difficultySegmentedDisplay.SelectCellWithNumber(0); });
                _difficultySegmentedDisplay.transform.localScale = new Vector3(.8f,
                    _difficultySegmentedDisplay.transform.localScale.y, _difficultySegmentedDisplay.transform.localScale.z);
                SetupDifficultyDisplay(_difficultySegmentedDisplay, _currentSong);
            }
            else
                SetupDifficultyDisplay(_difficultySegmentedDisplay, _currentSong);


            downloadsText.text = _currentSong.downloads.ToString();
            _levelParams.bpm = (float)(_currentSong.plays);
            _levelParams.notesCount = (int)_currentSong.bpm;
            _levelParams.obstaclesCount = _currentSong.upVotes;
            _levelParams.bombsCount = _currentSong.downVotes;

            Polyglot.LocalizedTextMeshProUGUI localizer1 = difficulty1Title.GetComponentInChildren<Polyglot.LocalizedTextMeshProUGUI>();
            if (localizer1 != null)
                GameObject.Destroy(localizer1);
            Polyglot.LocalizedTextMeshProUGUI localizer2 = difficulty2Title.GetComponentInChildren<Polyglot.LocalizedTextMeshProUGUI>();
            if (localizer2 != null)
                GameObject.Destroy(localizer2);
            Polyglot.LocalizedTextMeshProUGUI localizer3 = difficulty3Title.GetComponentInChildren<Polyglot.LocalizedTextMeshProUGUI>();
            if (localizer3 != null)
                GameObject.Destroy(localizer3);
            difficulty1Title.text = "";
            difficulty2Title.text = "";
            difficulty3Title.text = "";
            difficulty1Text.text = "";
            difficulty2Text.text = "";
            difficulty3Text.text = "";




     //       difficulty1Text.text = (_currentSong.metadata.difficulties.expert || _currentSong.metadata.difficulties.expertPlus) ? "Yes" : "No";
     //       difficulty2Text.text = (_currentSong.metadata.difficulties.hard) ? "Yes" : "No";
     //       difficulty3Text.text = (_currentSong.metadata.difficulties.easy || _currentSong.metadata.difficulties.normal) ? "Yes" : "No";

            StartCoroutine(LoadScripts.LoadSpriteCoroutine(_currentSong.coverURL, (cover) => { coverImage.texture = cover.texture;}));

            SetFavoriteState(PluginConfig.favoriteSongs.Any(x => x.Contains(_currentSong.hash)));
            SetDownloadState((SongDownloader.Instance.IsSongDownloaded(_currentSong) ? DownloadState.Downloaded : (sender.IsDownloadingSong(_currentSong) ? DownloadState.Downloading : DownloadState.NotDownloaded)));
            SetLoadingState(false);
        }


        public void SetLoadingState(bool isLoading) 
        {
            if (isLoading)
            {
                _downloadButton.SetButtonText("LOADING...");
                songNameText.text = "Loading...";
                downloadsText.text = "0";
                _levelParams.bpm = 0f;
                _levelParams.notesCount = 0;
                _levelParams.obstaclesCount = 0;
                _levelParams.bombsCount = 0;
            }

            if (_loadingIndicator != null)
            {
                _downloadButton.interactable = !isLoading;
                _levelDetails.gameObject.SetActive(!isLoading);
                _loadingIndicator.SetActive(isLoading);
                if(_characteristicSegmentedDisplay)
                _characteristicSegmentedDisplay.gameObject.SetActive(!isLoading);
                if (_difficultySegmentedDisplay)
                    _difficultySegmentedDisplay.gameObject.SetActive(!isLoading);
            }
        }

        void SetupDifficultyDisplay(TextSegmentedControl controller, Song song)
        {
            List<string> Diffs = new List<string>();
            if (song.metadata.difficulties.easy)
                Diffs.Add("Easy");
            if (song.metadata.difficulties.normal)
                Diffs.Add("Normal");
            if (song.metadata.difficulties.hard)
                Diffs.Add("Hard");
            if (song.metadata.difficulties.expert)
                Diffs.Add("Expert");
            if (song.metadata.difficulties.expertPlus)
                Diffs.Add("Expert+");
            controller.SetTexts(Diffs.ToArray());

        }
        void SetupCharacteristicDisplay(IconSegmentedControl controller, Song song)
        {
            string MissingText = "";
            List<IconSegmentedControl.DataItem> characteristics = new List<IconSegmentedControl.DataItem>();
            foreach(var c in song.metadata.characteristics)
            {
                BeatmapCharacteristicSO characteristic = SongCore.Loader.beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerialiedName(c);
                if (characteristic.characteristicName == "Missing Characteristic")
                {
                    MissingText += $" {c}";
                }
                else
                    characteristics.Add(new IconSegmentedControl.DataItem(characteristic.icon, characteristic.hintText));
            }
            if(!string.IsNullOrWhiteSpace(MissingText))
            {
                BeatmapCharacteristicSO characteristic = SongCore.Loader.beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerialiedName("Missing Characteristic");
                characteristics.Add(new IconSegmentedControl.DataItem(characteristic.icon, "Missing Characteristics:" + MissingText));
            }
            controller.SetData(characteristics.ToArray());
           
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
