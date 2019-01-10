using BeatSaverDownloader.Misc;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Logger = BeatSaverDownloader.Misc.Logger;

namespace BeatSaverDownloader.UI
{
    class VotingUI : MonoBehaviour
    {
        public bool initialized = false;

        private static VotingUI _instance = null;
        public static VotingUI Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = new GameObject("VotingUI").AddComponent<VotingUI>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        private ResultsViewController _standardLevelResultsViewController;

        private TextMeshProUGUI _ratingText;
        private Button _upvoteButton;
        private Button _downvoteButton;

        private IBeatmapLevel _lastLevel;
        private Song _lastBeatSaverSong;

        private bool _firstVote;

        public void OnLoad()
        {
            initialized = false;
            SetupUI();
        }

        public void SetupUI()
        {
            if (initialized) return;

            _standardLevelResultsViewController = Resources.FindObjectsOfTypeAll<ResultsViewController>().First(x => x.name == "StandardLevelResultsViewController");
            _standardLevelResultsViewController.didActivateEvent += _standardLevelResultsViewController_didActivateEvent;

            _upvoteButton = _standardLevelResultsViewController.CreateUIButton("PracticeButton", new Vector2(65f, 10f), () => { StartCoroutine(VoteForSong(true)); }, "", Base64Sprites.ThumbUp);
            _downvoteButton = _standardLevelResultsViewController.CreateUIButton("PracticeButton", new Vector2(65f, -10f), () => { StartCoroutine(VoteForSong(false)); }, "", Base64Sprites.ThumbDown);
            _ratingText = _standardLevelResultsViewController.CreateText("LOADING...", new Vector2(65f, 0f));
            _ratingText.alignment = TextAlignmentOptions.Center;
            _ratingText.fontSize = 7f;

            initialized = true;
        }


        private void _standardLevelResultsViewController_didActivateEvent(bool firstActivation, VRUI.VRUIViewController.ActivationType activationType)
        {
            IDifficultyBeatmap diffBeatmap = _standardLevelResultsViewController.GetPrivateField<IDifficultyBeatmap>("_difficultyBeatmap");
            _lastLevel = diffBeatmap.level;

            if (_lastLevel.levelID.Length < 32)
            {
                _upvoteButton.gameObject.SetActive(false);
                _downvoteButton.gameObject.SetActive(false);
                _ratingText.gameObject.SetActive(false);
            }
            else
            {
                _upvoteButton.gameObject.SetActive(true);
                _downvoteButton.gameObject.SetActive(true);
                _ratingText.gameObject.SetActive(true);

                _upvoteButton.interactable = false;
                _downvoteButton.interactable = false;
                _ratingText.text = "LOADING...";

                StartCoroutine(GetRatingForSong(_lastLevel));
            }
        }

        private IEnumerator GetRatingForSong(IBeatmapLevel level)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/songs/search/hash/{level.levelID.Substring(0, 32)}");

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Unable to connect to {PluginConfig.beatsaverURL}! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                try
                {
                    _firstVote = true;

                    JSONNode node = JSON.Parse(www.downloadHandler.text);

                    _lastBeatSaverSong = Song.FromSearchNode(node["songs"][0]);

                    _ratingText.text = (int.Parse(_lastBeatSaverSong.upvotes) - int.Parse(_lastBeatSaverSong.downvotes)).ToString();

                    _upvoteButton.interactable = (PluginConfig.apiAccessToken != PluginConfig.apiTokenPlaceholder);
                    _downvoteButton.interactable = (PluginConfig.apiAccessToken != PluginConfig.apiTokenPlaceholder);

                    if (PluginConfig.votedSongs.ContainsKey(_lastLevel.levelID.Substring(0, 32)))
                    {
                        switch (PluginConfig.votedSongs[_lastLevel.levelID.Substring(0, 32)].voteType)
                        {
                            case VoteType.Upvote: { _upvoteButton.interactable = false; } break;
                            case VoteType.Downvote: { _downvoteButton.interactable = false; } break;
                        }
                    }

                }
                catch (Exception e)
                {
                    Logger.Exception("Unable to get song rating! Excpetion: " + e);
                }
            }
        }


        private IEnumerator VoteForSong(bool upvote)
        {
            Logger.Log($"Voting...");

            _upvoteButton.interactable = false;
            _downvoteButton.interactable = false;

            UnityWebRequest voteWWW = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/songs/vote/{_lastBeatSaverSong.id}/{(upvote ? 1 : 0)}/{PluginConfig.apiAccessToken}");
            voteWWW.timeout = 30;
            yield return voteWWW.SendWebRequest();

            if (voteWWW.isNetworkError || voteWWW.isHttpError)
            {
                Logger.Error(voteWWW.error);
                _ratingText.text = voteWWW.error;
            }
            else
            {
                if (!_firstVote)
                {
                    yield return new WaitForSecondsRealtime(3f);
                }

                _firstVote = false;
                
                switch (voteWWW.responseCode)
                {
                    case 200:
                        {
                            JSONNode node = JSON.Parse(voteWWW.downloadHandler.text);
                            _ratingText.text = (int.Parse(node["upVotes"]) - int.Parse(node["downVotes"])).ToString();

                            if (upvote)
                            {
                                _upvoteButton.interactable = false;
                                _downvoteButton.interactable = true;
                            }
                            else
                            {
                                _downvoteButton.interactable = false;
                                _upvoteButton.interactable = true;
                            }

                            if (!PluginConfig.votedSongs.ContainsKey(_lastLevel.levelID.Substring(0, 32)))
                            {
                                PluginConfig.votedSongs.Add(_lastLevel.levelID.Substring(0, 32), new SongVote(_lastBeatSaverSong.id, upvote ? VoteType.Upvote : VoteType.Downvote));
                                PluginConfig.SaveConfig();
                            }
                            else if (PluginConfig.votedSongs[_lastLevel.levelID.Substring(0, 32)].voteType != (upvote ? VoteType.Upvote : VoteType.Downvote))
                            {
                                PluginConfig.votedSongs[_lastLevel.levelID.Substring(0, 32)] = new SongVote(_lastBeatSaverSong.id, upvote ? VoteType.Upvote : VoteType.Downvote);
                                PluginConfig.SaveConfig();
                            }
                        }; break;
                    case 403:
                        {
                            _upvoteButton.interactable = false;
                            _downvoteButton.interactable = false;
                            _ratingText.text = "Read-only token";
                        }; break;
                    case 401:
                        {
                            _upvoteButton.interactable = false;
                            _downvoteButton.interactable = false;
                            _ratingText.text = "Token not found";
                        }; break;
                    case 400:
                        {
                            _upvoteButton.interactable = false;
                            _downvoteButton.interactable = false;
                            _ratingText.text = "Bad token";
                        }; break;
                    default:
                        {
                            _upvoteButton.interactable = true;
                            _downvoteButton.interactable = true;
                            _ratingText.text = "Error " + voteWWW.responseCode;
                        }; break;
                }
            }
        }
    }
}
