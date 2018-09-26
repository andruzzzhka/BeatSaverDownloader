using BeatSaverDownloader.PluginUI.ViewControllers;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace BeatSaverDownloader.PluginUI
{
    class VotingUI : MonoBehaviour
    {
        private Logger log = new Logger("BeatSaverDownloader");

        Button upvoteButton;
        Button downvoteButton;
        TextMeshProUGUI ratingText;

        Song votingSong;

        //BeastSaberReviewViewController reviewViewController;

        private string levelId;
        private bool firstVote;

        public IEnumerator WaitForResults()
        {

            Logger.Log("Waiting for results view controller");
            yield return new WaitUntil(delegate () { return Resources.FindObjectsOfTypeAll<ResultsViewController>().Any(); });

            Logger.Log("Found results view controller!");

            ResultsViewController results = Resources.FindObjectsOfTypeAll<ResultsViewController>().FirstOrDefault();

            if(results == null || results.difficultyLevel == null)
            {
                yield break;
            }

            levelId = results.difficultyLevel.level.levelID;

            Logger.Log($"Player ID: {PluginUI.playerId}");
            Logger.Log($"Level ID: {levelId}");

            if (levelId.Length > 32)
            {
                //reviewViewController = BeatSaberUI.CreateViewController<BeastSaberReviewViewController>(); // NOT FINISHED

                //results.screen.screenSystem.leftScreen.SetRootViewController(reviewViewController);

                #region BeatSaver Integration UI
                ratingText = BeatSaberUI.CreateText(results.rectTransform, "LOADING...", new Vector2(51.5f, -40f));
                ratingText.rectTransform.sizeDelta = new Vector2(100f, 10f);
                ratingText.alignment = TextAlignmentOptions.Center;
                ratingText.fontSize = 7f;

                CreateButtons(results.rectTransform);
                #endregion
                
                UnityWebRequest www = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/songs/search/hash/{levelId.Substring(0, 32)}");
                
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    Logger.Error(www.error);
                    TextMeshProUGUI _errorText = BeatSaberUI.CreateText(results.rectTransform, www.error, new Vector2(40f, -30f));
                    _errorText.alignment = TextAlignmentOptions.Center;
                    Destroy(_errorText.gameObject, 2f);
                }
                else
                {
                    try
                    {
                        firstVote = true;

                        JSONNode node = JSON.Parse(www.downloadHandler.text);

                        votingSong = Song.FromSearchNode(node["songs"][0]);

                        ratingText.text = (int.Parse(votingSong.upvotes) - int.Parse(votingSong.downvotes)).ToString();

                        if (!string.IsNullOrEmpty(PluginConfig.apiAccessToken) && PluginConfig.apiAccessToken != PluginConfig.apiTokenPlaceholder)
                        {
                            CreateButtons(results.rectTransform);
                            upvoteButton.interactable = true;
                            downvoteButton.interactable = true;
                        }
                        else
                        {
                            Logger.Warning("No API Access Token!");
                        }

                    }
                    catch (Exception e)
                    {
                        Logger.Exception("EXCEPTION(GET SONG RATING): " + e);
                    }
                }

            }
        }

        private void CreateButtons(RectTransform parent)
        {
            if (upvoteButton == null)
            {
                upvoteButton = BeatSaberUI.CreateUIButton(parent, "SettingsButton");
                BeatSaberUI.SetButtonText(upvoteButton, "+");
                BeatSaberUI.SetButtonTextSize(upvoteButton, 7f);
                (upvoteButton.transform as RectTransform).anchoredPosition = new Vector2(-14f, 45f);
                upvoteButton.interactable = false;


                upvoteButton.onClick.RemoveAllListeners();
                upvoteButton.onClick.AddListener(delegate ()
                {
                    StartCoroutine(VoteForSong(true));
                });
            }

            if (downvoteButton == null)
            {
                downvoteButton = BeatSaberUI.CreateUIButton(parent, "SettingsButton");
                BeatSaberUI.SetButtonText(downvoteButton, "-");
                BeatSaberUI.SetButtonTextSize(downvoteButton, 7f);
                (downvoteButton.transform as RectTransform).anchoredPosition = new Vector2(-14f, 26f);
                downvoteButton.interactable = false;

                downvoteButton.onClick.RemoveAllListeners();
                downvoteButton.onClick.AddListener(delegate ()
                {
                    StartCoroutine(VoteForSong(false));
                });
            }
        }

        IEnumerator VoteForSong(bool upvote)
        {
            Logger.Log($"Voting...");

            upvoteButton.interactable = false;
            downvoteButton.interactable = false;
            
            UnityWebRequest voteWWW = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/songs/vote/{votingSong.id}/{(upvote ? 1 : 0)}/{PluginConfig.apiAccessToken}");
            voteWWW.timeout = 30;
            yield return voteWWW.SendWebRequest();

            if (voteWWW.isNetworkError || voteWWW.isHttpError)
            {
                Logger.Error(voteWWW.error);
                ratingText.text = voteWWW.error;
            }
            else
            {
                if (!firstVote)
                {
                    yield return new WaitForSecondsRealtime(3f);
                }

                firstVote = false;

                upvoteButton.interactable = true;
                downvoteButton.interactable = true;
                
                switch (voteWWW.responseCode)
                {
                    case 200:
                        {
                            JSONNode node = JSON.Parse(voteWWW.downloadHandler.text);
                            ratingText.text = (int.Parse(node["upVotes"]) - int.Parse(node["downVotes"])).ToString();
                        }; break;
                    case 403:
                        {
                            ratingText.text = "Read-only token";
                        }; break;
                    case 401:
                        {
                            ratingText.text = "Token not found";
                        }; break;
                    case 400:
                        {
                            ratingText.text = "Bad token";
                        };break;
                    default:
                        {
                            ratingText.text = "Error "+voteWWW.responseCode;
                        }; break;
                }
            }

        }

        

    }
}
