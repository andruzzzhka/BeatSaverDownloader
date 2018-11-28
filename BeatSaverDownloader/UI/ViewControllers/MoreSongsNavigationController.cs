using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRUI;
using UnityEngine.UI;
using BeatSaverDownloader.Misc;
using CustomUI.BeatSaber;

namespace BeatSaverDownloader.UI.ViewControllers
{
    class MoreSongsNavigationController : VRUINavigationController
    {
        public event Action didFinishEvent;

        private Button _backButton;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (activationType == ActivationType.AddedToHierarchy) {
                _backButton = BeatSaberUI.CreateBackButton(rectTransform, didFinishEvent.Invoke);
            }
        }
    }
}
