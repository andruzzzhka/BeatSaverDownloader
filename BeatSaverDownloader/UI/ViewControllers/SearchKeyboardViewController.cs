using BeatSaverDownloader.UI.UIElements;
using CustomUI.BeatSaber;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using VRUI;

namespace BeatSaverDownloader.UI.ViewControllers
{
    class SearchKeyboardViewController : CustomViewController
    {

        KEYBOARD _searchKeyboard;
        public event Action<string> searchButtonPressed;

        private bool initialized = false;
        public override void __Activate(ActivationType activationType)
        {
            base.__Activate(activationType);
            //
            if (!initialized && activationType == ActivationType.AddedToHierarchy)
            {
                RectTransform keyboardRect = new GameObject("SearchKeyboard").AddComponent<RectTransform>();
                keyboardRect.gameObject.transform.SetParent(rectTransform);
                keyboardRect.position = rectTransform.position;
                keyboardRect.localScale = rectTransform.localScale;
                _searchKeyboard = new KEYBOARD(keyboardRect, KEYBOARD.QWERTY);
                _searchKeyboard.EnterPressed += delegate (string value) { searchButtonPressed?.Invoke(value); };
                keyboardRect.localScale *= 1.6f;
                keyboardRect.anchoredPosition = new Vector2(6, -10);
                initialized = true;
            }

        }

    }
}