using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace BeatSaverDownloader.PluginUI
{
    class SearchKeyboardViewController : VRUIViewController
    {
        GameObject _searchKeyboardGO;

        CustomUIKeyboard _searchKeyboard;

        Button _searchButton;
        Button _backButton;

        TextMeshProUGUI _inputText;
        public string _inputString = "";

        public event Action<string> searchButtonPressed;
        public event Action backButtonPressed;

        protected override void DidActivate()
        {
            if (_searchKeyboard == null)
            {
                _searchKeyboardGO = Instantiate(Resources.FindObjectsOfTypeAll<UIKeyboard>().First(x => x.name != "CustomUIKeyboard"), rectTransform, false).gameObject;

                _searchKeyboard = _searchKeyboardGO.AddComponent<CustomUIKeyboard>();

                _searchKeyboard.uiKeyboardKeyEvent = delegate (char input) { _inputString += input; UpdateInputText(); };
                _searchKeyboard.uiKeyboardDeleteEvent = delegate () { _inputString = _inputString.Substring(0, _inputString.Length - 1); UpdateInputText(); };
            }

            if(_inputText == null)
            {
                _inputText = BeatSaberUI.CreateText(rectTransform,"Search...", new Vector2(0f,-11.5f));
                _inputText.alignment = TextAlignmentOptions.Center;
                _inputText.fontSize = 6f;
            }
            else
            {
                _inputString = "";
                UpdateInputText();
            }

            if(_searchButton == null)
            {
                _searchButton = BeatSaberUI.CreateUIButton(rectTransform, "ApplyButton");
                BeatSaberUI.SetButtonText(_searchButton, "Search");
                (_searchButton.transform as RectTransform).sizeDelta = new Vector2(30f, 10f);
                (_searchButton.transform as RectTransform).anchoredPosition = new Vector2(-15f, 1.5f);
                _searchButton.onClick.RemoveAllListeners();
                _searchButton.onClick.AddListener(delegate() {
                    searchButtonPressed?.Invoke(_inputString);
                    DismissModalViewController(null, false);
                });
            }

            if (_backButton == null)
            {
                _backButton = BeatSaberUI.CreateBackButton(rectTransform);

                _backButton.onClick.AddListener(delegate ()
                {
                    _inputString = "";
                    backButtonPressed?.Invoke();
                    DismissModalViewController(null, false);
                });
            }

        }

        void UpdateInputText()
        {
            if (_inputText != null)
            {
                _inputText.text = _inputString.ToUpper();
            }
        }

        void ClearInput()
        {
            _inputString = "";
        }
        

    }
}
