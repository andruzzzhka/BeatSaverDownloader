using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRUI;
using UnityEngine.UI;
using UnityEngine;

namespace BeatSaverDownloader.PluginUI.ViewControllers
{
    class SubMenuViewController : VRUIViewController
    {

        Button _backButton;
        Button _beatSaverSongsButton;
        Button _beastSaberPlaylistsButton;

        HorizontalLayoutGroup _layoutGroup;

        BeatSaverNavigationController _beatSaverViewController;
        //BeastSaberNavigationController _beastSaberViewController;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (activationType == ActivationType.AddedToHierarchy)
            {
                if (firstActivation)
                {
                    _backButton = BeatSaberUI.CreateBackButton(rectTransform);
                    _backButton.onClick.AddListener(delegate ()
                    {
                        DismissModalViewController(null, false);
                    });

                    _layoutGroup = new GameObject("CustomUILayout").AddComponent<HorizontalLayoutGroup>();
                    _layoutGroup.transform.SetParent(rectTransform, false);

                    (_layoutGroup.transform as RectTransform).anchorMin = new Vector2(0.25f, 0.5f);
                    (_layoutGroup.transform as RectTransform).anchorMin = new Vector2(0.25f, 0.5f);
                    (_layoutGroup.transform as RectTransform).anchoredPosition = new Vector2(0, 4f);
                    (_layoutGroup.transform as RectTransform).sizeDelta = new Vector2(0, 40f);

                    _layoutGroup.childAlignment = TextAnchor.MiddleRight;
                    _layoutGroup.spacing = 2f;
                    _layoutGroup.childControlHeight = false;
                    _layoutGroup.childControlWidth = false;
                    _layoutGroup.childForceExpandHeight = false;
                    _layoutGroup.childForceExpandWidth = false;

                    _beatSaverSongsButton = BeatSaberUI.CreateUIButton((_layoutGroup.transform as RectTransform), "PartyButton", "Songs", PluginUI.Base64ToSprite(Base64Sprites.SongIcon));

                    _beatSaverSongsButton.onClick.AddListener(delegate () {

                        if (_beatSaverViewController == null)
                        {
                            _beatSaverViewController = BeatSaberUI.CreateViewController<BeatSaverNavigationController>();
                        }

                        PresentModalViewController(_beatSaverViewController, null, false);
                    });


                    _beastSaberPlaylistsButton = BeatSaberUI.CreateUIButton((_layoutGroup.transform as RectTransform), "PartyButton", "Playlists", PluginUI.Base64ToSprite(Base64Sprites.PlaylistIcon));

                    _beastSaberPlaylistsButton.onClick.AddListener(delegate () {

                        if (_beatSaverViewController == null)
                        {
                            _beatSaverViewController = BeatSaberUI.CreateViewController<BeatSaverNavigationController>();
                        }

                        PresentModalViewController(_beatSaverViewController, null, false);
                    });

                }
            }
        }


    }
}
