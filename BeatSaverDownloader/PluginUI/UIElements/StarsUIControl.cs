using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeatSaverDownloader.PluginUI
{
    class StarsUIControl : MonoBehaviour
    {
        public int currentValue;
        public event Action<int> starPressed;

        RectTransform rect;

        Button[] _starButtons = new Button[5];

        public void Init(RectTransform parent, Vector2 position)
        {
            rect = parent ?? throw new ArgumentNullException("parent");
            
            _starButtons[0] = BeatSaberUI.CreateUIButton(rect, "ApplyButton");
            TransformButton(_starButtons[0], position, 0);

            _starButtons[1] = BeatSaberUI.CreateUIButton(rect, "ApplyButton");
            TransformButton(_starButtons[1], position, 1);

            _starButtons[2] = BeatSaberUI.CreateUIButton(rect, "ApplyButton");
            TransformButton(_starButtons[2], position, 2);

            _starButtons[3] = BeatSaberUI.CreateUIButton(rect, "ApplyButton");
            TransformButton(_starButtons[3], position, 3);

            _starButtons[4] = BeatSaberUI.CreateUIButton(rect, "ApplyButton");
            TransformButton(_starButtons[4], position, 4);

            starPressed += StarsUIControl_starPressed;

        }

        private void StarsUIControl_starPressed(int index)
        {
            currentValue = index;

            for(int i = 0; i < index; i++)
            {
                BeatSaberUI.SetButtonIcon(_starButtons[i], PluginUI.Base64ToSprite(Base64Sprites.StarFull));
            }
            
            for(int i = index; index < 5; i++)
            {
                if(i<=_starButtons.Length)
                    BeatSaberUI.SetButtonIcon(_starButtons[i], PluginUI.Base64ToSprite(Base64Sprites.StarEmpty));
            }
        }

        private void TransformButton(Button btn, Vector2 position, int index)
        {
            RectTransform iconTransform = btn.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "Icon");
            iconTransform.gameObject.SetActive(true);
            Destroy(iconTransform.parent.GetComponent<HorizontalLayoutGroup>());
            iconTransform.sizeDelta = new Vector2(8f, 8f);

            Destroy(btn.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "Text").gameObject);

            BeatSaberUI.SetButtonText(btn, "");
            btn.GetComponentsInChildren<Image>().First(x => x.name == "Stroke").enabled = false;

            BeatSaberUI.SetButtonIcon(btn, PluginUI.Base64ToSprite(Base64Sprites.StarEmpty));

            (btn.transform as RectTransform).anchoredPosition = position + new Vector2(8f * index, 0f);
            (btn.transform as RectTransform).sizeDelta = new Vector2(10f, 10f);

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(delegate ()
            {
                starPressed.Invoke(index+1);
            });
            
        }

    }
}
