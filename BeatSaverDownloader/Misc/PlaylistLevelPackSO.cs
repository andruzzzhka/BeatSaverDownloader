using BS_Utils.Utilities;
using Harmony;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BeatSaverDownloader.Misc
{
    class PlaylistLevelPackSO : CustomBeatmapLevelPackSO
    {
        public Playlist playlist { get { return _playlist; } set { _playlist = value;  UpdateDataFromPlaylist(); } }

        private Playlist _playlist;

        public void UpdateDataFromPlaylist()
        {
            _packName = _playlist.playlistTitle;
            _coverImage = _playlist.icon;
            _packID = $"Playlist_{playlist.playlistTitle}_{playlist.playlistAuthor}";
            _isPackAlwaysOwned = false;

            PlaylistsCollection.MatchSongsForPlaylist(playlist);

            IPreviewBeatmapLevel[] levels = playlist.songs.Where(x => x.level != null).Select(x => x.level).ToArray();

            CustomLevelCollectionSO levelCollection = ScriptableObject.CreateInstance<CustomLevelCollectionSO>();
            levelCollection.SetPrivateField("_levelList", levels.Where(x => x is BeatmapLevelSO).Cast<BeatmapLevelSO>().ToList());
            levelCollection.SetPrivateField("_beatmapLevels", levels);

            _beatmapLevelCollection = levelCollection;
        }

    }
}
