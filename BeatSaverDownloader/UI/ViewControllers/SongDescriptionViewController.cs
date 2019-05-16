using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI.UIElements;
using BS_Utils.Utilities;
using CustomUI.BeatSaber;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace BeatSaverDownloader.UI.ViewControllers
{
    class SongDescriptionViewController : VRUIViewController
    {
        public event Action<string> linkClicked;

        private Button _pageUpButton;
        private Button _pageDownButton;

        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _descriptionText;
        private TextMeshProHyperlinkHandler _linkHandler;
        private RectTransform _textViewport;
        private RectTransform _descriptionContainer;
        private float _dstPosY;
        private float _smooth = 8f;
        private float _contentHeight;
        private float _scrollPageHeight;

        private Regex linksParser = new Regex(@"(http|ftp|https):\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.ECMAScript);


        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                var headerPanelRectTransform = Instantiate(Resources.FindObjectsOfTypeAll<RectTransform>().First(x => x.name == "HeaderPanel" && x.parent.name == "PlayerSettingsViewController"), rectTransform);
                headerPanelRectTransform.gameObject.SetActive(true);

                _titleText = headerPanelRectTransform.GetComponentInChildren<TextMeshProUGUI>();
                _titleText.text = "SONG DESCRIPTION";

                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "PageUpButton"), rectTransform, false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(52f, -12f);
                (_pageUpButton.transform as RectTransform).sizeDelta = new Vector2(8f, 6f);
                _pageUpButton.GetComponentsInChildren<RectTransform>().First(x => x.name == "BG").sizeDelta = new Vector2(8f, 6f);
                _pageUpButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Arrow").sprite = Sprites.DoubleArrow;
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    PageUpButtonPressed();
                });

                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "PageDownButton"), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(52f, 10f);
                (_pageDownButton.transform as RectTransform).sizeDelta = new Vector2(8f, 6f);
                _pageDownButton.GetComponentsInChildren<RectTransform>().First(x => x.name == "BG").sizeDelta = new Vector2(8f, 6f);
                _pageDownButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Arrow").sprite = Sprites.DoubleArrow;
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    PageDownButtonPressed();
                });

                _textViewport = new GameObject("DescriptionViewport", typeof(RectTransform), typeof(RectMask2D)).transform as RectTransform;
                _textViewport.SetParent(rectTransform, false);
                _textViewport.anchorMin = new Vector2(0.05f, 0.025f);
                _textViewport.anchorMax = new Vector2(0.9f, 0.85f);
                _textViewport.sizeDelta = new Vector2(0f, 0f);
                _textViewport.anchoredPosition = new Vector2(0f, 0f);
                
                _descriptionText = BeatSaberUI.CreateText(_textViewport, "", new Vector2(0f, 0f));
                _descriptionText.fontSize = 4f;
                _descriptionText.enableWordWrapping = true;
                _descriptionText.overflowMode = TextOverflowModes.Overflow;
                _descriptionText.lineSpacing = -40f;
                _descriptionText.ignoreRectMaskCulling = true;
                _descriptionText.rectTransform.anchorMin = new Vector2(0f, 0f);
                _descriptionText.rectTransform.anchorMax = new Vector2(1f, 1f);
                _descriptionText.rectTransform.sizeDelta = new Vector2(0f, 0f);
                _descriptionText.rectTransform.anchoredPosition = new Vector2(0f, 0f);

                _linkHandler = _descriptionText.gameObject.AddComponent<TextMeshProHyperlinkHandler>();
                _linkHandler.linkClicked += linkClicked;
            }
            SetDescription("");
        }
        
        public void Update()
        {
            float position = Mathf.Lerp(_descriptionContainer.anchoredPosition.y, _dstPosY, Time.deltaTime * _smooth);
            if (Mathf.Abs(position - _dstPosY) < 0.01f)
            {
                position = _dstPosY;
            }
            _descriptionContainer.anchoredPosition = new Vector2(0f, position);
        }

        public void PageUpButtonPressed()
        {
            _dstPosY = Mathf.Max(0f, _dstPosY - _scrollPageHeight);
            RefreshButtonsInteractibility();
        }
        
        public void PageDownButtonPressed()
        {
            _dstPosY = Mathf.Min(_contentHeight - _textViewport.rect.height, _dstPosY + _scrollPageHeight);
            RefreshButtonsInteractibility();
        }

        public void RefreshButtonsInteractibility()
        {
            _pageUpButton.interactable = (_dstPosY > 0.01f);
            _pageDownButton.interactable = (_dstPosY < _contentHeight - _textViewport.rect.height);
        }

        public void SetDescription(string text)
        {
            _descriptionText.text = linksParser.Replace(text, ReplaceLink);
            _descriptionText.ForceMeshUpdate();
            _contentHeight = _descriptionText.preferredHeight;
            _scrollPageHeight = _textViewport.rect.height * 0.8f;
            _descriptionContainer = _descriptionText.rectTransform;
            _descriptionContainer.sizeDelta = new Vector2(0f, _contentHeight - _textViewport.rect.height);
            _descriptionContainer.pivot = new Vector2(0.5f, 1f);
            _descriptionContainer.anchoredPosition = Vector2.zero;
            _dstPosY = 0f;
            bool active = _contentHeight > _textViewport.rect.height;
            _pageUpButton.gameObject.SetActive(active);
            _pageDownButton.gameObject.SetActive(active);
            RefreshButtonsInteractibility();
        }

        private string ReplaceLink(Match match)
        {
            return $"<link=\"{match.Value}\"><color=blue>{match.Value}</color></link>";
        }
    }
}
