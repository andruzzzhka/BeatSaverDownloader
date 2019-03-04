using CustomUI.BeatSaber;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using VRUI;
using UnityEngine.UI;
using System.Collections;
using BeatSaverDownloader.UI.UIElements;
using CustomUI.Settings;

namespace BeatSaverDownloader.UI.ViewControllers
{
    class ReviewViewController : VRUIViewController
    {
        public event Action<float, float, float, float, float, float> didPressSubmit;

        private StarsUIControl _funFactorControl;
        private StarsUIControl _flowControl;
        private StarsUIControl _rhythmControl;
        private StarsUIControl _readabilityControl;
        private StarsUIControl _patternQualityControl;
        private StarsUIControl _levelDesignControl;

        Button _submitButton;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                TextMeshProUGUI _funFactorText = BeatSaberUI.CreateText(rectTransform, "Fun Factor", new Vector2(-29f, 28f));
                _funFactorText.fontSize = 7f;
                _funFactorText.alignment = TextAlignmentOptions.Center;
                _funFactorControl = new GameObject("FunFactorUIControl", typeof(RectTransform)).AddComponent<StarsUIControl>();
                _funFactorControl.Init(rectTransform, new Vector2(-50f, 19.5f));

                TextMeshProUGUI _flowText = BeatSaberUI.CreateText(rectTransform, "Flow", new Vector2(31f, 28f));
                _flowText.fontSize = 7f;
                _flowText.alignment = TextAlignmentOptions.Center;
                _flowControl = new GameObject("FlowUIControl", typeof(RectTransform)).AddComponent<StarsUIControl>();
                _flowControl.Init(rectTransform, new Vector2(10f, 19.5f));

                TextMeshProUGUI _rhythmText = BeatSaberUI.CreateText(rectTransform, "Rhythm", new Vector2(-29f, 8f));
                _rhythmText.fontSize = 7f;
                _rhythmText.alignment = TextAlignmentOptions.Center;
                _rhythmControl = new GameObject("RhythmUIControl", typeof(RectTransform)).AddComponent<StarsUIControl>();
                _rhythmControl.Init(rectTransform, new Vector2(-50f, -0.5f));

                TextMeshProUGUI _readabilityText = BeatSaberUI.CreateText(rectTransform, "Readability", new Vector2(31f, 8f));
                _readabilityText.fontSize = 7f;
                _readabilityText.alignment = TextAlignmentOptions.Center;
                _readabilityControl = new GameObject("ReadabilityUIControl", typeof(RectTransform)).AddComponent<StarsUIControl>();
                _readabilityControl.Init(rectTransform, new Vector2(10f, -0.5f));

                TextMeshProUGUI _patternQualityText = BeatSaberUI.CreateText(rectTransform, "Pattern quality", new Vector2(-29f, -12f));
                _patternQualityText.fontSize = 7f;
                _patternQualityText.alignment = TextAlignmentOptions.Center;
                _patternQualityControl = new GameObject("PatternQualityUIControl", typeof(RectTransform)).AddComponent<StarsUIControl>();
                _patternQualityControl.Init(rectTransform, new Vector2(-50f, -20.5f));

                TextMeshProUGUI _levelDesignText = BeatSaberUI.CreateText(rectTransform, "Level quality", new Vector2(31f, -12f));
                _levelDesignText.fontSize = 7f;
                _levelDesignText.alignment = TextAlignmentOptions.Center;
                _levelDesignControl = new GameObject("LevelDesignUIControl", typeof(RectTransform)).AddComponent<StarsUIControl>();
                _levelDesignControl.Init(rectTransform, new Vector2(10f, -20.5f));

                _submitButton = this.CreateUIButton("CreditsButton", new Vector2(3f, -31f), new Vector2(30f, 10f), () => { didPressSubmit?.Invoke(_funFactorControl.currentValue, _rhythmControl.currentValue, _flowControl.currentValue, _patternQualityControl.currentValue, _readabilityControl.currentValue, _levelDesignControl.currentValue); }, "Submit");
            }
        }
       
        public void Update()
        {
            if (_funFactorControl != null &&
                _flowControl != null &&
                _rhythmControl != null &&
                _readabilityControl != null &&
                _patternQualityControl != null &&
                _levelDesignControl != null &&
                _submitButton != null)
            {
                _submitButton.interactable = true;
            }
        }

        public void SetSubmitButtonState(bool enabled)
        {
            _submitButton.interactable = enabled;
        }
    }
}
