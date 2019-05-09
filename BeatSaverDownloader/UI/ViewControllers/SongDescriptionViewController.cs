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
using VRUI;

namespace BeatSaverDownloader.UI.ViewControllers
{
    class SongDescriptionViewController : VRUIViewController
    {
        public event Action<string> linkClicked;

        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _descriptionText;
        private TextMeshProHyperlinkHandler _linkHandler;

        private Regex linksParser = new Regex(@"(http|ftp|https):\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.ECMAScript);
        
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                var headerPanelRectTransform = Instantiate(Resources.FindObjectsOfTypeAll<RectTransform>().First(x => x.name == "HeaderPanel" && x.parent.name == "PlayerSettingsViewController"), rectTransform);
                headerPanelRectTransform.gameObject.SetActive(true);

                _titleText = headerPanelRectTransform.GetComponentInChildren<TextMeshProUGUI>();
                _titleText.text = "SONG DESCRIPTION";
                
                _descriptionText = BeatSaberUI.CreateText(rectTransform, "", new Vector2(0f, 0f));
                _descriptionText.fontSize = 4f;
                _descriptionText.enableWordWrapping = true;
                _descriptionText.overflowMode = TextOverflowModes.Masking;
                _descriptionText.rectTransform.anchorMin = new Vector2(0.05f, 0f);
                _descriptionText.rectTransform.anchorMax = new Vector2(0.95f, 0.85f);
                _descriptionText.rectTransform.sizeDelta = new Vector2(0f, 0f);
                _descriptionText.rectTransform.anchoredPosition = new Vector2(0f, 0f);

                _linkHandler = _descriptionText.gameObject.AddComponent<TextMeshProHyperlinkHandler>();
                _linkHandler.linkClicked += linkClicked;
            }
            else
            {
                _descriptionText.text = "";
            }
        }
        
        public void SetDescription(string text)
        {
            _descriptionText.text = linksParser.Replace(text, ReplaceLink);
        }

        private string ReplaceLink(Match match)
        {
            return $"<link=\"{match.Value}\"><color=blue>{match.Value}</color></link>";
        }
    }
}
