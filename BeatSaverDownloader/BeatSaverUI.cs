using HMUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRUI;
using Image = UnityEngine.UI.Image;

namespace BeatSaverDownloader
{
    class BeatSaverUI : MonoBehaviour
    {
        private Logger log = new Logger("BeatSaverDownloader");

        private RectTransform _mainMenuRectTransform;
        private MainMenuViewController _mainMenuViewController;

        private Button _buttonInstance;
        private Button _backButtonInstance;
        private GameObject _loadingIndicatorInstance;

        public static BeatSaverUI _instance;

        public static List<Sprite> icons = new List<Sprite>();

        public BeatSaverMasterViewController _beatSaverViewController;
        
        internal static void OnLoad()
        {
            if (_instance != null)
            {
                return;
            }
            new GameObject("BeatSaver UI").AddComponent<BeatSaverUI>();

        }

        private void Awake()
        {
            _instance = this;
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                icons.Add(sprite);
            }
            try
            {
                _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "QuitButton"));
                _backButtonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "BackArrowButton"));
                _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                _mainMenuRectTransform = _buttonInstance.transform.parent as RectTransform;
                _loadingIndicatorInstance = Resources.FindObjectsOfTypeAll<GameObject>().Where(x => x.name == "LoadingIndicator").First();
            }
            catch(Exception e)
            {
                log.Exception("EXCEPTION ON AWAKE(TRY FIND BUTTONS): "+e);
            }

            try
            {
                CreateBeatSaverButton();
            }
            catch (Exception e)
            {
                log.Exception("EXCEPTION ON AWAKE(TRY CREATE BUTTON): " + e);
            }
            
        }        

        private void CreateBeatSaverButton()
        {
            
            Button _beatSaverButton = CreateUIButton(_mainMenuRectTransform, "QuitButton");
            
            try
            {
                (_beatSaverButton.transform as RectTransform).anchoredPosition = new Vector2(30f, 7f);
                (_beatSaverButton.transform as RectTransform).sizeDelta = new Vector2(28f, 10f);

                SetButtonText(ref _beatSaverButton, "BeatSaver");

                SetButtonIcon(ref _beatSaverButton, icons.First(x => (x.name == "SettingsIcon")));
                
                _beatSaverButton.onClick.AddListener(delegate () {

                    try
                    {
                        if (_beatSaverViewController == null)
                        {
                            _beatSaverViewController = CreateViewController<BeatSaverMasterViewController>();    
                        }
                        _mainMenuViewController.PresentModalViewController(_beatSaverViewController, null, false);
                        
                    }
                    catch (Exception e)
                    {
                        log.Exception("EXCETPION IN BUTTON: "+e.Message);
                    }

                });

            }
            catch(Exception e)
            {
                log.Exception("EXCEPTION: "+e.Message);
            }

        }

       

        public Button CreateUIButton(RectTransform parent, string buttonTemplate)
        {
            if (_buttonInstance == null)
            {
                return null;
            }

            Button btn = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == buttonTemplate)), parent, false);
            DestroyImmediate(btn.GetComponent<GameEventOnUIButtonClick>());
            btn.onClick = new Button.ButtonClickedEvent();

            return btn;
        }

        public Button CreateBackButton(RectTransform parent)
        {
            if (_backButtonInstance == null)
            {
                return null;
            }

            Button _button = Instantiate(_backButtonInstance, parent, false);
            DestroyImmediate(_button.GetComponent<GameEventOnUIButtonClick>());
            _button.onClick = new Button.ButtonClickedEvent();

            return _button;
        }

        public T CreateViewController<T>() where T : VRUIViewController
        {
            T vc = new GameObject("CreatedViewController").AddComponent<T>();

            vc.rectTransform.anchorMin = new Vector2(0f, 0f);
            vc.rectTransform.anchorMax = new Vector2(1f, 1f);
            vc.rectTransform.sizeDelta = new Vector2(0f, 0f);
            vc.rectTransform.anchoredPosition = new Vector2(0f, 0f);

            return vc;
        }

        public GameObject CreateLoadingIndicator(Transform parent)
        {
            GameObject indicator = Instantiate(_loadingIndicatorInstance, parent, false);
            return indicator;
        }

        public TextMeshProUGUI CreateText(RectTransform parent, string text, Vector2 position)
        {
            TextMeshProUGUI textMesh = new GameObject("TextMeshProUGUI_GO").AddComponent<TextMeshProUGUI>();
            textMesh.rectTransform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = 4;
            textMesh.color = Color.white;
            textMesh.font = Resources.Load<TMP_FontAsset>("Teko-Medium SDF No Glow");
            textMesh.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            textMesh.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            textMesh.rectTransform.sizeDelta = new Vector2(60f, 10f);
            textMesh.rectTransform.anchoredPosition = position;

            return textMesh;
        }

        public void SetButtonText(ref Button _button, string _text)
        {
            if (_button.GetComponentInChildren<TextMeshProUGUI>() != null)
            {

                _button.GetComponentInChildren<TextMeshProUGUI>().text = _text;
            }

        }

        public void SetButtonTextSize(ref Button _button, float _fontSize)
        {
            if (_button.GetComponentInChildren<TextMeshProUGUI>() != null)
            {
                _button.GetComponentInChildren<TextMeshProUGUI>().fontSize = _fontSize;
            }
            

        }

        public void SetButtonIcon(ref Button _button, Sprite _icon)
        {
            if (_button.GetComponentsInChildren<UnityEngine.UI.Image>().Count() > 1)
            {

                _button.GetComponentsInChildren<UnityEngine.UI.Image>()[1].sprite = _icon;
            }

        }

        public void SetButtonBackground(ref Button _button, Sprite _background)
        {
            if (_button.GetComponentsInChildren<Image>().Any())
            {

                _button.GetComponentsInChildren<UnityEngine.UI.Image>()[0].sprite = _background;
            }

        }


    }
}
