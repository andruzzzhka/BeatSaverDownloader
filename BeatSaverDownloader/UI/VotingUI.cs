using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI.FlowCoordinators;
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
using Steamworks;

namespace BeatSaverDownloader.UI
{
    public class VotingUI : MonoBehaviour
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
        private Button _reviewButton;

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

            _upvoteButton = _standardLevelResultsViewController.CreateUIButton("PracticeButton", new Vector2(65f, 10f), () => { VoteForSong(true); }, "", Sprites.ThumbUp);
            _downvoteButton = _standardLevelResultsViewController.CreateUIButton("PracticeButton", new Vector2(65f, -10f), () => { VoteForSong(false); }, "", Sprites.ThumbDown);
            _ratingText = _standardLevelResultsViewController.CreateText("LOADING", new Vector2(65f, 0f));
            _ratingText.alignment = TextAlignmentOptions.Center;
            _ratingText.fontSize = 7f;
            _ratingText.lineSpacing = -38f;

            _reviewButton = _standardLevelResultsViewController.CreateUIButton("PracticeButton", new Vector2(65f, -20f), () => { ShowReviewScreen(); }, "", Sprites.ReviewIcon);

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
                _reviewButton.gameObject.SetActive(false);
            }
            else
            {
                _upvoteButton.gameObject.SetActive(true);
                _downvoteButton.gameObject.SetActive(true);
                _ratingText.gameObject.SetActive(true);
                _ratingText.alignment = TextAlignmentOptions.Center;
                _reviewButton.gameObject.SetActive(true);

                _upvoteButton.interactable = false;
                _downvoteButton.interactable = false;
                _reviewButton.interactable = false;
                _ratingText.text = "LOADING...";

                StartCoroutine(GetRatingForSong(_lastLevel));
            }
        }

        private void ShowReviewScreen()
        {
            if (PluginUI.Instance.reviewFlowCoordinator == null)
            {
                PluginUI.Instance.reviewFlowCoordinator = new GameObject("ReviewFlow").AddComponent<ReviewFlowCoordinator>();
                PluginUI.Instance.reviewFlowCoordinator.didFinishEvent += () =>
                {
                    SongListTweaks.Instance.freePlayFlowCoordinator.InvokePrivateMethod("DismissFlowCoordinator", new object[] { PluginUI.Instance.reviewFlowCoordinator, null, false });
                };
            }

            PluginUI.Instance.reviewFlowCoordinator.parentFlowCoordinator = SongListTweaks.Instance.freePlayFlowCoordinator;
            PluginUI.Instance.reviewFlowCoordinator.songkey = _lastBeatSaverSong.id;
            PluginUI.Instance.reviewFlowCoordinator.levelId = _lastLevel.levelID;
            SongListTweaks.Instance.freePlayFlowCoordinator.InvokePrivateMethod("PresentFlowCoordinator", new object[] { PluginUI.Instance.reviewFlowCoordinator, null, false, false });
        }

        private IEnumerator GetRatingForSong(IBeatmapLevel level)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/songs/search/hash/{level.levelID.Substring(0, 32)}");

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Plugin.log.Error($"Unable to connect to {PluginConfig.beatsaverURL}! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                try
                {
                    _firstVote = true;

                    JSONNode node = JSON.Parse(www.downloadHandler.text);

                    if (node["songs"].Count > 0)
                    {
                        _lastBeatSaverSong = Song.FromSearchNode(node["songs"][0]);

                        _ratingText.text = (int.Parse(_lastBeatSaverSong.upvotes) - int.Parse(_lastBeatSaverSong.downvotes)).ToString();

                        bool canVote = (PluginConfig.apiAccessToken != PluginConfig.apiTokenPlaceholder || (VRPlatformHelper.instance.vrPlatformSDK == VRPlatformHelper.VRPlatformSDK.OpenVR || Environment.CommandLine.ToLower().Contains("-vrmode oculus") || Environment.CommandLine.ToLower().Contains("fpfc")));

                        _upvoteButton.interactable = canVote;
                        _downvoteButton.interactable = canVote;

                        _reviewButton.interactable = true;

                        if (PluginConfig.votedSongs.ContainsKey(_lastLevel.levelID.Substring(0, 32)))
                        {
                            switch (PluginConfig.votedSongs[_lastLevel.levelID.Substring(0, 32)].voteType)
                            {
                                case VoteType.Upvote: { _upvoteButton.interactable = false; } break;
                                case VoteType.Downvote: { _downvoteButton.interactable = false; } break;
                            }
                        }
                    }
                    else
                    {
                        Plugin.log.Error("Song doesn't exist on BeatSaver!");
                    }
                }
                catch (Exception e)
                {
                    Plugin.log.Critical("Unable to get song rating! Excpetion: " + e);
                }
            }
        }

        private void VoteForSong(bool upvote)
        {
            if(PluginConfig.apiAccessToken != PluginConfig.apiTokenPlaceholder || string.IsNullOrWhiteSpace(PluginConfig.apiAccessToken))
            {
                StartCoroutine(VoteWithAccessToken(upvote));
            }
            else if((VRPlatformHelper.instance.vrPlatformSDK == VRPlatformHelper.VRPlatformSDK.OpenVR || Environment.CommandLine.ToLower().Contains("-vrmode oculus") || Environment.CommandLine.ToLower().Contains("fpfc")))
            {
                StartCoroutine(VoteWithSteamID(upvote));
            }
        }

        private IEnumerator VoteWithAccessToken(bool upvote)
        {
            Plugin.log.Info($"Voting...");

            _upvoteButton.interactable = false;
            _downvoteButton.interactable = false;

            UnityWebRequest voteWWW = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/songs/vote/{_lastBeatSaverSong.id}/{(upvote ? 1 : 0)}/{PluginConfig.apiAccessToken}");
            voteWWW.timeout = 30;
            yield return voteWWW.SendWebRequest();

            if (voteWWW.isNetworkError)
            {
                Plugin.log.Error(voteWWW.error);
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
                            _ratingText.text = "Read-only\ntoken";
                        }; break;
                    case 401:
                        {
                            _upvoteButton.interactable = false;
                            _downvoteButton.interactable = false;
                            _ratingText.text = "Token\nnot found";
                        }; break;
                    case 400:
                        {
                            _upvoteButton.interactable = false;
                            _downvoteButton.interactable = false;
                            _ratingText.text = "Bad\ntoken";
                        }; break;
                    default:
                        {
                            _upvoteButton.interactable = true;
                            _downvoteButton.interactable = true;
                            _ratingText.text = "Error\n" + voteWWW.responseCode;
                        }; break;
                }
            }
        }

        private IEnumerator VoteWithSteamID(bool upvote)
        {
            if (!SteamManager.Initialized)
            {
                Plugin.log.Error($"SteamManager is not initialized!");
            }
            
            _upvoteButton.interactable = false;
            _downvoteButton.interactable = false;

            Plugin.log.Info($"Getting a ticket...");

            var steamId = SteamUser.GetSteamID();
            string authTicketHexString = "";

            byte[] authTicket = new byte[1024];
            var authTicketResult = SteamUser.GetAuthSessionTicket(authTicket, 1024, out var length);
            if (authTicketResult != HAuthTicket.Invalid)
            {
                var beginAuthSessionResult = SteamUser.BeginAuthSession(authTicket, (int)length, steamId);
                switch (beginAuthSessionResult)
                {
                    case EBeginAuthSessionResult.k_EBeginAuthSessionResultOK:
                        var result = SteamUser.UserHasLicenseForApp(steamId, new AppId_t(620980));

                        SteamUser.EndAuthSession(steamId);

                        switch (result)
                        {
                            case EUserHasLicenseForAppResult.k_EUserHasLicenseResultDoesNotHaveLicense:
                                _upvoteButton.interactable = false;
                                _downvoteButton.interactable = false;
                                _ratingText.text = "User does not\nhave license";
                                yield break;
                            case EUserHasLicenseForAppResult.k_EUserHasLicenseResultHasLicense:
                                if(SteamHelper.m_GetAuthSessionTicketResponse == null)
                                    SteamHelper.m_GetAuthSessionTicketResponse = Callback<GetAuthSessionTicketResponse_t>.Create(OnAuthTicketResponse);

                                SteamHelper.lastTicket = SteamUser.GetAuthSessionTicket(authTicket, 1024, out length);
                                if (SteamHelper.lastTicket != HAuthTicket.Invalid)
                                {
                                    Array.Resize(ref authTicket, (int)length);
                                    authTicketHexString = BitConverter.ToString(authTicket).Replace("-", "");
                                }

                                break;
                            case EUserHasLicenseForAppResult.k_EUserHasLicenseResultNoAuth:
                                _upvoteButton.interactable = false;
                                _downvoteButton.interactable = false;
                                _ratingText.text = "User is not\nauthenticated";
                                yield break;
                        }
                        break;
                    default:
                        _upvoteButton.interactable = false;
                        _downvoteButton.interactable = false;
                        _ratingText.text = "Auth\nfailed";
                        yield break;
                }
            }

            Plugin.log.Info("Waiting for Steam callback...");

            float startTime = Time.time;
            yield return new WaitWhile(() => { return SteamHelper.lastTicketResult != EResult.k_EResultOK && (Time.time - startTime) < 20f; });

            if(SteamHelper.lastTicketResult != EResult.k_EResultOK)
            {
                Plugin.log.Error($"Auth ticket callback timeout");
                _upvoteButton.interactable = true;
                _downvoteButton.interactable = true;
                _ratingText.text = "Callback\ntimeout";
                yield break;
            }

            SteamHelper.lastTicketResult = EResult.k_EResultRevoked;

            Plugin.log.Info($"Voting...");

            Dictionary<string, string> formData = new Dictionary<string, string> ();
            formData.Add("id", steamId.m_SteamID.ToString());
            formData.Add("ticket", authTicketHexString);

            UnityWebRequest voteWWW = UnityWebRequest.Post($"{PluginConfig.beatsaverURL}/api/songs/voteById/{_lastBeatSaverSong.id}/{(upvote ? 1 : 0)}", formData);
            voteWWW.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            voteWWW.timeout = 30;
            yield return voteWWW.SendWebRequest();

            if (voteWWW.isNetworkError)
            {
                Plugin.log.Error(voteWWW.error);
                _ratingText.text = voteWWW.error;
            }
            else
            {
                if (!_firstVote)
                {
                    yield return new WaitForSecondsRealtime(2f);
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
                    case 500:
                        {
                            _upvoteButton.interactable = false;
                            _downvoteButton.interactable = false;
                            _ratingText.text = "Steam API\nerror";
                            Plugin.log.Error("Error: " + voteWWW.downloadHandler.text);
                        }; break;
                    case 401:
                        {
                            _upvoteButton.interactable = false;
                            _downvoteButton.interactable = false;
                            _ratingText.text = "Invalid\nauth ticket";
                            Plugin.log.Error("Error: " + voteWWW.downloadHandler.text);
                        }; break;
                    case 403:
                        {
                            _upvoteButton.interactable = false;
                            _downvoteButton.interactable = false;
                            _ratingText.text = "Steam ID\nmismatch";
                            Plugin.log.Error("Error: " + voteWWW.downloadHandler.text);
                        }; break;
                    case 400:
                        {
                            _upvoteButton.interactable = false;
                            _downvoteButton.interactable = false;
                            _ratingText.text = "Bad\nrequest";
                            Plugin.log.Error("Error: "+voteWWW.downloadHandler.text);
                        }; break;
                    default:
                        {
                            _upvoteButton.interactable = true;
                            _downvoteButton.interactable = true;
                            _ratingText.text = "Error\n" + voteWWW.responseCode;
                            Plugin.log.Error("Error: " + voteWWW.downloadHandler.text);
                        }; break;
                }
            }
        }

        private void OnAuthTicketResponse(GetAuthSessionTicketResponse_t response)
        {
            if(SteamHelper.lastTicket == response.m_hAuthTicket)
            {
                SteamHelper.lastTicketResult = response.m_eResult;
            }
        }
    }
}
