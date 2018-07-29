using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using VRUI;

namespace BeatSaverDownloader.PluginUI.ViewControllers
{
    class BeastSaberReviewViewController : VRUIViewController
    {

        Logger log = new Logger("BeatSaverDownloader");

        private StarsUIControl _funFactorControl;
        private StarsUIControl _flowControl;
        private StarsUIControl _rhythmControl;
        private StarsUIControl _readabilityControl;
        private StarsUIControl _patternQualityControl;
        private StarsUIControl _levelDesignControl;

        Button submitButton;

        bool loading;


        public void Update()
        {
            if (_funFactorControl.currentValue == 0 ||
               _flowControl.currentValue == 0 ||
               _rhythmControl.currentValue == 0 ||
               _readabilityControl.currentValue == 0 ||
               _patternQualityControl.currentValue == 0 ||
               _levelDesignControl.currentValue == 0)
            {
                submitButton.interactable = false;
            }
            else
            {
                if (!loading)
                {
                    submitButton.interactable = true;
                }
            }
        }

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {

            TextMeshProUGUI _funFactorText = BeatSaberUI.CreateText(rectTransform, "Fun Factor", new Vector2(-29f, -10f));
            _funFactorText.fontSize = 7f;
            _funFactorText.alignment = TextAlignmentOptions.Center;
            _funFactorControl = new GameObject("FunFactorUIControl").AddComponent<StarsUIControl>();
            _funFactorControl.Init(rectTransform, new Vector2(-50f, 57.5f));

            TextMeshProUGUI _flowText = BeatSaberUI.CreateText(rectTransform, "Flow", new Vector2(31f, -10f));
            _flowText.fontSize = 7f;
            _flowText.alignment = TextAlignmentOptions.Center;
            _flowControl = new GameObject("FlowUIControl").AddComponent<StarsUIControl>();
            _flowControl.Init(rectTransform, new Vector2(10f, 57.5f));

            TextMeshProUGUI _rhythmText = BeatSaberUI.CreateText(rectTransform, "Rhythm", new Vector2(-29f, -30f));
            _rhythmText.fontSize = 7f;
            _rhythmText.alignment = TextAlignmentOptions.Center;
            _rhythmControl = new GameObject("RhythmUIControl").AddComponent<StarsUIControl>();
            _rhythmControl.Init(rectTransform, new Vector2(-50f, 37.5f));

            TextMeshProUGUI _readabilityText = BeatSaberUI.CreateText(rectTransform, "Readability", new Vector2(31f, -30f));
            _readabilityText.fontSize = 7f;
            _readabilityText.alignment = TextAlignmentOptions.Center;
            _readabilityControl = new GameObject("ReadabilityUIControl").AddComponent<StarsUIControl>();
            _readabilityControl.Init(rectTransform, new Vector2(10f, 37.5f));

            TextMeshProUGUI _patternQualityText = BeatSaberUI.CreateText(rectTransform, "Pattern quality", new Vector2(-29f, -50f));
            _patternQualityText.fontSize = 7f;
            _patternQualityText.alignment = TextAlignmentOptions.Center;
            _patternQualityControl = new GameObject("PatternQualityUIControl").AddComponent<StarsUIControl>();
            _patternQualityControl.Init(rectTransform, new Vector2(-50f, 17.5f));

            TextMeshProUGUI _levelDesignText = BeatSaberUI.CreateText(rectTransform, "Level design", new Vector2(31f, -50f));
            _levelDesignText.fontSize = 7f;
            _levelDesignText.alignment = TextAlignmentOptions.Center;
            _levelDesignControl = new GameObject("LevelDesignUIControl").AddComponent<StarsUIControl>();
            _levelDesignControl.Init(rectTransform, new Vector2(10f, 17.5f));

            submitButton = BeatSaberUI.CreateUIButton(rectTransform, "SettingsButton");
            (submitButton.transform as RectTransform).anchoredPosition = new Vector2(-15f, 5f);
            (submitButton.transform as RectTransform).sizeDelta = new Vector2(30f, 10f);
            BeatSaberUI.SetButtonText(submitButton, "Submit");
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(delegate ()
            {
                StartCoroutine(SubmitReview());
            });

        }


        IEnumerator SubmitReview()
        {
            Logger.StaticLog($"Submiting...\nFunFactor: {_funFactorControl.currentValue}, Flow: {_flowControl.currentValue}, \nRhythm: {_rhythmControl.currentValue}, Readability: {_readabilityControl.currentValue}, \nPatternQuality: {_patternQualityControl.currentValue}, LevelDesign: {_levelDesignControl.currentValue}");

            loading = true;
            submitButton.interactable = false;

            UnityWebRequest voteWWW = UnityWebRequest.Get($"TODO");
            voteWWW.timeout = 30;
            yield return voteWWW.SendWebRequest();

            loading = false;

            if (voteWWW.isHttpError || voteWWW.isNetworkError)
            {
                submitButton.interactable = true;
                log.Error($"{(voteWWW.isHttpError ? "HTTP Error" : "Network Error")}: {voteWWW.error}");
            }
        }
    }
}
