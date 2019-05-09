using BeatSaverDownloader.Misc;
using CustomUI.BeatSaber;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeatSaverDownloader.UI.UIElements
{
    class StarsUIControl : MonoBehaviour
    {
        public int value { get { return _currentValue;  } set { HandleStarPressedEvent(value, false); } }
        public event Action<int> starPressed;

        private int _currentValue;

        Button[] _starButtons = new Button[5];

        private static RectTransform viewControllersContainer;
        private static Button practiceButtonTemplate;

        public void Init(RectTransform parent, Vector2 position)
        {
            transform.SetParent(parent, false);

            (transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
            (transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
            (transform as RectTransform).sizeDelta = new Vector2(50f, 10f);
            (transform as RectTransform).anchoredPosition = position;

            for (int i = 0; i < 5; i++)
                TransformButton(i);
        }

        private void HandleStarPressedEvent(int index, bool callbackAction = true)
        {
            if(_currentValue == index && _currentValue == 1)
            {
                _currentValue = 0;
            }
            else
            {
                _currentValue = index;
            }

            if(_currentValue > 5)
            {
                _currentValue = 5;
            }
            else if(_currentValue < 0)
            {
                _currentValue = 0;
            }

            for (int i = 0; i < _currentValue; i++)
            {
                if (_starButtons.Length > i)
                    _starButtons[i].SetButtonIcon(Sprites.StarFull);
                else
                    Plugin.log.Info("Index out of bounds! (1) Items: " + _starButtons.Length + ", Index: "+i);
            }

            for (int i = _currentValue; i < _starButtons.Length; i++)
            {
                if (_starButtons.Length > i)
                    _starButtons[i].SetButtonIcon(Sprites.StarEmpty);
                else
                    Plugin.log.Info("Index out of bounds! (2) Items: " + _starButtons.Length + ", Index: " + i);
            }

            if(callbackAction)
                starPressed?.Invoke(_currentValue);
        }

        private void TransformButton(int index)
        {
            
            if (viewControllersContainer == null)
            {
                viewControllersContainer = FindObjectsOfType<RectTransform>().First(x => x.name == "ViewControllers");
                practiceButtonTemplate = viewControllersContainer.GetComponentsInChildren<Button>(true).First(x => x.name == "PracticeButton");
            }

            _starButtons[index] = Instantiate(practiceButtonTemplate, transform as RectTransform, false);
            _starButtons[index].gameObject.SetActive(true);
            _starButtons[index].onClick = new Button.ButtonClickedEvent();
            _starButtons[index].onClick.AddListener(() => { HandleStarPressedEvent(index + 1); });
            _starButtons[index].name = "CustomUIButton";
            _starButtons[index].SetButtonText("");
            _starButtons[index].SetButtonIcon(Sprites.StarEmpty);

            (_starButtons[index].transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
            (_starButtons[index].transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
            (_starButtons[index].transform as RectTransform).anchoredPosition = new Vector2(10f * index, 0f);
            (_starButtons[index].transform as RectTransform).sizeDelta = new Vector2(10f, 10f);

            RectTransform iconTransform = _starButtons[index].GetComponentsInChildren<RectTransform>(true).First(x => x.name == "Icon");
            iconTransform.gameObject.SetActive(true);
            Destroy(iconTransform.parent.GetComponent<HorizontalLayoutGroup>());
            iconTransform.sizeDelta = new Vector2(8f, 8f);
            iconTransform.anchoredPosition = new Vector2(5f, -4.8f);

            _starButtons[index].GetComponentsInChildren<Image>().First(x => x.name == "Stroke").enabled = false;
            
        }

    }
}