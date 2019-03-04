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

        public FlowCoordinator parentFlowCoordinator;

        private BackButtonNavigationController _navigationController;
        private ReviewViewController _reviewViewController;
        
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                title = "BeastSaber Review";

                _navigationController = BeatSaberUI.CreateViewController<BackButtonNavigationController>();
                _navigationController.didFinishEvent += () => { didFinishEvent?.Invoke();};

                _reviewViewController = BeatSaberUI.CreateViewController<ReviewViewController>();
                _reviewViewController.didPressSubmit += delegate (float funFactor, float rhythm, float flow, float patternQuality, float readability, float levelQuality) { StartCoroutine(PostReview(funFactor, rhythm, flow, patternQuality, readability, levelQuality)); };
            }

            SetViewControllersToNavigationConctroller(_navigationController, _reviewViewController);
            ProvideInitialViewControllers(_navigationController, null, null);
        }

        public IEnumerator PostReview(float funFactor, float rhythm, float flow, float patternQuality, float readability, float levelQuality)
        {
            yield return null;

            if (songkey.Contains("-"))
            {
                songkey = songkey.Substring(0, songkey.IndexOf("-"));
            }

            BeastSaberReview review = new BeastSaberReview(GetUserInfo.GetUserName(), funFactor, rhythm, flow, patternQuality, readability, levelQuality);

            _reviewViewController.SetSubmitButtonState(false);

            UnityWebRequest voteWWW = new UnityWebRequest($"https://bsaber.com/wp-json/bsaber-api/songs/{songkey}/reviews");
            voteWWW.method = "POST";
            voteWWW.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(review)));
            voteWWW.downloadHandler = new DownloadHandlerBuffer();
            voteWWW.SetRequestHeader("Content-Type", "application/json");
            voteWWW.timeout = 30;
            yield return voteWWW.SendWebRequest();

            if (voteWWW.isNetworkError)
            {
                Misc.Logger.Error(voteWWW.error);
                _reviewViewController.SetSubmitButtonState(true);
            }
            else
            {
                _reviewViewController.SetSubmitButtonState(true);

                switch (voteWWW.responseCode)
                {
                    case 200:
                        {
                            JSONNode node = JSON.Parse(voteWWW.downloadHandler.text);

                            if (node["success"])
                            {
                                Misc.Logger.Log("Success!");
                            }
                            else
                            {
                                Misc.Logger.Error("Something went wrong...\n Error: "+node["data"]);
                            }
                        }; break;
                    default:
                        {
                            Misc.Logger.Error("Error: " + voteWWW.responseCode+"\nResponse: "+ voteWWW.downloadHandler.text);
                        }; break;
                }
            }
        }

    }
}
