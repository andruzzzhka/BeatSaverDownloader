using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BeatSaverDownloader.UI.UIElements
{
    //https://deltadreamgames.com/unity-tmp-hyperlinks/
    [RequireComponent(typeof(TextMeshProUGUI))]
    class TextMeshProHyperlinkHandler : MonoBehaviour, IPointerClickHandler
    {
        public event Action<string> linkClicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            TextMeshProUGUI textMesh = GetComponent<TextMeshProUGUI>();
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(textMesh, eventData.pressPosition, Camera.main);
            if (linkIndex != -1)
            {
                TMP_LinkInfo linkInfo = textMesh.textInfo.linkInfo[linkIndex];
                linkClicked?.Invoke(linkInfo.GetLinkID());
            }
        }
    }
}
