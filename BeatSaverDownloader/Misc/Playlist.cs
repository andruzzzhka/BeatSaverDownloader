using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BeatSaverDownloader.Misc
{
    public class PlaylistSong
    {
        public string key { get; set; }
        public string songName { get; set; }
        public string levelId { get; set; }
        
        [NonSerialized]
        public IStandardLevel level;
        [NonSerialized]
        public bool oneSaber;
        [NonSerialized]
        public string path;
    }

    public class Playlist
    {
        public string playlistTitle { get; set; }
        public string playlistAuthor { get; set; }
        public string image { get; set; }
        public List<PlaylistSong> songs { get; set; }
        public string fileLoc { get; set; }
        public string customDetailUrl { get; set; }

        [NonSerialized]
        public Sprite icon;
    }
}
