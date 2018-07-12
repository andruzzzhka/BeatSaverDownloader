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

        public event Action<string> continuePressed;

        Button upvoteButton;
        Button downvoteButton;
        TextMeshProUGUI ratingText;

        Song votingSong;

        //BeastSaberReviewViewController reviewViewController;

        private string levelId;

        public IEnumerator WaitForResults()
        {
            
            log.Log("Waiting for results view controller");
            yield return new WaitUntil(delegate () { return Resources.FindObjectsOfTypeAll<ResultsViewController>().Count() > 0; });

            log.Log("Found results view controller!");

            ResultsViewController results = Resources.FindObjectsOfTypeAll<ResultsViewController>().First();
            
            levelId = ReflectionUtil.GetPrivateField<string>(results, "_levelId");

            results.resultsViewControllerDidPressContinueButtonEvent += Results_resultsViewControllerDidPressContinueButtonEvent;

            log.Log($"Player ID: {PluginUI.playerId}");
            log.Log($"Level ID: {levelId}");

            if (levelId.Length > 32)
            {
                //reviewViewController = BeatSaberUI.CreateViewController<BeastSaberReviewViewController>(); // NOT FINISHED

                //results.screen.screenSystem.leftScreen.SetRootViewController(reviewViewController);

                #region BeatSaver Integration UI
                ratingText = BeatSaberUI.CreateText(results.rectTransform, "LOADING...", new Vector2(51.5f, -40f));
                ratingText.rectTransform.sizeDelta = new Vector2(100f, 10f);
                ratingText.alignment = TextAlignmentOptions.Center;
                ratingText.fontSize = 7f;

                upvoteButton = BeatSaberUI.CreateUIButton(results.rectTransform, "ApplyButton");
                BeatSaberUI.SetButtonText(upvoteButton, "+");
                BeatSaberUI.SetButtonTextSize(upvoteButton, 7f);
                (upvoteButton.transform as RectTransform).anchoredPosition = new Vector2(40f, 45f);
                upvoteButton.interactable = false;


                upvoteButton.onClick.RemoveAllListeners();
                upvoteButton.onClick.AddListener(delegate ()
                {
                    StartCoroutine(VoteForSong(true));
                });

                downvoteButton = BeatSaberUI.CreateUIButton(results.rectTransform, "ApplyButton");
                BeatSaberUI.SetButtonText(downvoteButton, "-");
                BeatSaberUI.SetButtonTextSize(downvoteButton, 7f);
                (downvoteButton.transform as RectTransform).anchoredPosition = new Vector2(40f, 26f);
                downvoteButton.interactable = false;

                downvoteButton.onClick.RemoveAllListeners();
                downvoteButton.onClick.AddListener(delegate ()
                {
                    StartCoroutine(VoteForSong(false));
                });
                #endregion

                log.Log($"{PluginConfig.beatsaverURL}/api/search/hash/{levelId.Substring(0, 32)}");
                UnityWebRequest www = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/songs/search/hash/{levelId.Substring(0, 32)}");
                
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    log.Error(www.error);
                    TextMeshProUGUI _errorText = BeatSaberUI.CreateText(results.rectTransform, www.error, new Vector2(40f, -30f));
                    _errorText.alignment = TextAlignmentOptions.Center;
                    Destroy(_errorText.gameObject, 2f);
                }
                else
                {
                    try
                    {
                        JSONNode node = JSON.Parse(www.downloadHandler.text);

                        votingSong = Song.FromSearchNode(node["songs"][0]);

                        ratingText.text = (int.Parse(votingSong.upvotes) - int.Parse(votingSong.downvotes)).ToString();

                        if (!string.IsNullOrEmpty(PluginConfig.apiAccessToken))
                        {
                            upvoteButton.interactable = true;
                            downvoteButton.interactable = true;
                        }
                        else
                        {
                            log.Warning("API Access Token is empty!");
                        }

                    }
                    catch (Exception e)
                    {
                        log.Exception("EXCEPTION(GET SONG RATING): " + e);
                    }
                }

            }
        }

        private void Results_resultsViewControllerDidPressContinueButtonEvent(ResultsViewController obj)
        {
            continuePressed.Invoke(levelId);
        }

        IEnumerator VoteForSong(bool upvote)
        {
            log.Log($"Voting...");

            upvoteButton.interactable = false;
            downvoteButton.interactable = false;

            UnityWebRequest voteWWW = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/vote/{votingSong.id}/{(upvote ? 1 : 0)}/{PluginConfig.apiAccessToken}");
            voteWWW.timeout = 30;
            yield return voteWWW.SendWebRequest();

            if (voteWWW.isNetworkError || voteWWW.isHttpError)
            {
                log.Error(voteWWW.error);
                ratingText.text = voteWWW.error;
            }
            else
            {
                upvoteButton.interactable = true;
                downvoteButton.interactable = true;

                int parsedInt;

                if (int.TryParse(voteWWW.downloadHandler.text, out parsedInt))
                {
                    switch (parsedInt)
                    {
                        case 0:
                            {
                                ratingText.text = ((int.Parse(votingSong.upvotes) - int.Parse(votingSong.downvotes)) + (upvote ? 1 : -1)).ToString();
                            }; break;
                        case 2: break;
                        case 1:
                            {
                                ratingText.text = "Error";
                            }; break;
                        default:
                            {
                                ratingText.text = "Fatal error";
                            }; break;
                    }
                }
                else
                {
                    ratingText.text = voteWWW.downloadHandler.text;
                }
            }

        }

        

    }
}
