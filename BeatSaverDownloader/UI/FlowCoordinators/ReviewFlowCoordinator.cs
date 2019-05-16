using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRUI;
using UnityEngine.UI;
using TMPro;
using CustomUI.BeatSaber;
using UnityEngine;
using BeatSaverDownloader.UI.ViewControllers;
using BeatSaverDownloader.Misc;
using System.Collections;
using UnityEngine.Networking;
using BS_Utils.Gameplay;
using SimpleJSON;
using BS_Utils.Utilities;
using System.Diagnostics;

namespace BeatSaverDownloader.UI.FlowCoordinators
{
    public struct BeastSaberReview
    {
        public string username;
        public string title;
        public string comment;
        public float fun_factor;
        public float rhythm;
        public float flow;
        public float pattern_quality;
        public float readability;
        public float level_quality;

        public BeastSaberReview(string username, float fun_factor, float rhythm, float flow, float pattern_quality, float readability, float level_quality)
        {
            this.username = username;
            title = "";
            comment = "";
            this.fun_factor = fun_factor;
            this.rhythm = rhythm;
            this.flow = flow;
            this.pattern_quality = pattern_quality;
            this.readability = readability;
            this.level_quality = level_quality;
        }
    }

    class ReviewFlowCoordinator : FlowCoordinator
    {
        public event Action didFinishEvent;

        public string songkey;
        public string levelId;

        public FlowCoordinator parentFlowCoordinator;

        private BackButtonNavigationController _navigationController;
        private ReviewViewController _reviewViewController;
        private SimpleDialogPromptViewController _simpleDialog;

        private SongReview _lastReview;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                title = "BeastSaber Review";

                _simpleDialog = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().FirstOrDefault().GetPrivateField<SimpleDialogPromptViewController>("_simpleDialogPromptViewController");
                _simpleDialog = Instantiate(_simpleDialog.gameObject, _simpleDialog.transform.parent).GetComponent<SimpleDialogPromptViewController>();

                _navigationController = BeatSaberUI.CreateViewController<BackButtonNavigationController>();
                _navigationController.didFinishEvent += () => { didFinishEvent?.Invoke();};

                _reviewViewController = BeatSaberUI.CreateViewController<ReviewViewController>();
                _reviewViewController.didPressSubmit += delegate (float funFactor, float rhythm, float flow, float patternQuality, float readability, float levelQuality)
                {
                    if (songkey.Contains("-"))
                    {
                        songkey = songkey.Substring(0, songkey.IndexOf("-"));
                    }

                    _lastReview = new SongReview(songkey, funFactor, rhythm, flow, patternQuality, readability, levelQuality); SubmitPressed();
                };
                _reviewViewController.didPressOpenInBrowser += delegate ()
                {
                    if (songkey.Contains("-"))
                    {
                        songkey = songkey.Substring(0, songkey.IndexOf("-"));
                    }

                    Application.OpenURL($"https://bsaber.com/songs/{songkey}/");
                };

                _reviewViewController.didActivateEvent += (arg0, arg1) =>
                {
                    if (arg1 == VRUIViewController.ActivationType.AddedToHierarchy)
                    {
                        if (PluginConfig.reviewedSongs.ContainsKey(levelId.Substring(0, 32)))
                        {
                            _reviewViewController.SetSubmitButtonState(false, true);
                            _reviewViewController.SetStatusText(true, "<color=red>You have already left a review about this song!");

                            _reviewViewController.SetReviewValues(
                                PluginConfig.reviewedSongs[levelId.Substring(0, 32)].fun_factor,
                                PluginConfig.reviewedSongs[levelId.Substring(0, 32)].rhythm,
                                PluginConfig.reviewedSongs[levelId.Substring(0, 32)].flow,
                                PluginConfig.reviewedSongs[levelId.Substring(0, 32)].pattern_quality,
                                PluginConfig.reviewedSongs[levelId.Substring(0, 32)].readability,
                                PluginConfig.reviewedSongs[levelId.Substring(0, 32)].level_quality
                                );
                        }
                        else
                        {
                            _reviewViewController.SetSubmitButtonState(true, true);
                            _reviewViewController.SetStatusText(false, "");

                            _reviewViewController.SetReviewValues(0f, 0f, 0f, 0f, 0f, 0f);
                        }
                    }
                };
            }

            SetViewControllersToNavigationConctroller(_navigationController, _reviewViewController);
            ProvideInitialViewControllers(_navigationController, null, null);
        }

        public void SubmitPressed()
        {
            _simpleDialog.Init("Post a review?", "All reviews are final and you will no longer be able to leave a review about this song!\n\nAre you sure you want to continue?", "Yes", "No", 
                (selectedIndex) => 
                {
                    DismissViewController(_simpleDialog, null, false);

                    if (selectedIndex == 0)
                        StartCoroutine(PostReview(_lastReview.fun_factor, _lastReview.rhythm, _lastReview.flow, _lastReview.pattern_quality, _lastReview.readability, _lastReview.level_quality));
                });
            PresentViewController(_simpleDialog, null, false);
        }

        public IEnumerator PostReview(float funFactor, float rhythm, float flow, float patternQuality, float readability, float levelQuality)
        {
            yield return null;

            BeastSaberReview review = new BeastSaberReview(GetUserInfo.GetUserName(), funFactor, rhythm, flow, patternQuality, readability, levelQuality);

            _reviewViewController.SetSubmitButtonState(true, false);
            _reviewViewController.SetStatusText(false, "");

            UnityWebRequest voteWWW = new UnityWebRequest($"https://bsaber.com/wp-json/bsaber-api/songs/{songkey}/reviews");
            voteWWW.method = "POST";
            voteWWW.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(review)));
            voteWWW.downloadHandler = new DownloadHandlerBuffer();
            voteWWW.SetRequestHeader("Content-Type", "application/json");
            voteWWW.timeout = 30;
            yield return voteWWW.SendWebRequest();

            if (voteWWW.isNetworkError)
            {
                Plugin.log.Error(voteWWW.error);
                _reviewViewController.SetSubmitButtonState(true, true);
                _reviewViewController.SetStatusText(false, "");
            }
            else
            {

                switch (voteWWW.responseCode)
                {
                    case 200:
                        {
                            JSONNode node = JSON.Parse(voteWWW.downloadHandler.text);

                            if (node["success"])
                            {
                                Plugin.log.Info("Success!");

                                if (!PluginConfig.reviewedSongs.ContainsKey(levelId.Substring(0, 32)))
                                {
                                    PluginConfig.reviewedSongs.Add(levelId.Substring(0, 32), new SongReview(songkey, funFactor, rhythm, flow, patternQuality, readability, levelQuality));
                                    PluginConfig.SaveConfig();
                                }
                                else
                                {
                                    PluginConfig.reviewedSongs[levelId.Substring(0, 32)] = new SongReview(songkey, funFactor, rhythm, flow, patternQuality, readability, levelQuality);
                                    PluginConfig.SaveConfig();
                                }

                                _reviewViewController.SetSubmitButtonState(false, false);
                                _reviewViewController.SetStatusText(true, "<color=green>Success!");
                            }
                            else
                            {
                                Plugin.log.Error("Something went wrong...\n Response: "+ voteWWW.downloadHandler.text);
                                _reviewViewController.SetSubmitButtonState(true, true);
                                _reviewViewController.SetStatusText(false, "");
                            }
                        }; break;
                    default:
                        {
                            Plugin.log.Error("Error: " + voteWWW.responseCode+"\nResponse: "+ voteWWW.downloadHandler.text);
                            _reviewViewController.SetSubmitButtonState(true, true);
                            _reviewViewController.SetStatusText(false, "");
                        }; break;
                }
            }
        }

    }
}
