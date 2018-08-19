using BeatSaverDownloader.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace BeatSaverDownloader.PluginUI.ViewControllers
{
    class PlaylistNavigationController : VRUINavigationController
    {
        public event Action<Playlist> finished;

        PlaylistsListViewController _playlistsList;
        PlaylistDetailViewController _playlistDetail;

        Button _backButton;

        Playlist _selectedPlaylist;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if(firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                _backButton = BeatSaberUI.CreateBackButton(rectTransform);

                _backButton.onClick.AddListener(delegate ()
                {
                    DismissModalViewController(null, false);
                    finished?.Invoke(null);
                });

                _playlistsList = BeatSaberUI.CreateViewController<PlaylistsListViewController>();
                _playlistsList.rectTransform.anchorMin = new Vector2(0.3f, 0f);
                _playlistsList.rectTransform.anchorMax = new Vector2(0.7f, 1f);

                _playlistsList.SetPlaylists(PluginConfig.playlists);

                PushViewController(_playlistsList, true);

                _playlistsList.playlistSelected += ShowDetails;
            }
            else
            {
                _playlistsList.SetPlaylists(PluginConfig.playlists);
                PushViewController(_playlistsList, true);
            }
        }

        public void ShowDetails(Playlist playlist)
        {
            _selectedPlaylist = playlist;

            if (_playlistDetail == null)
            {
                GameObject _playlistDetailGameObject = Instantiate(Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First(), rectTransform, false).gameObject;
                Destroy(_playlistDetailGameObject.GetComponent<StandardLevelDetailViewController>());
                _playlistDetail = _playlistDetailGameObject.AddComponent<PlaylistDetailViewController>();
                _playlistDetail.selectPressed += SelectPressed;

                PushViewController(_playlistDetail, false);
                _playlistDetail.UpdateContent(playlist);
            }
            else
            {
                if (_viewControllers.IndexOf(_playlistDetail) < 0)
                {
                    PushViewController(_playlistDetail, true);
                    _playlistDetail.UpdateContent(playlist);
                }
                else
                {
                    _playlistDetail.UpdateContent(playlist);
                }

            }
        }

        private void SelectPressed()
        {
            DismissModalViewController(null, false);
            finished?.Invoke(_selectedPlaylist);
        }
    }
}
