//SongBrowserSettings by halsafar
using System;
using System.Collections.Generic;

namespace SongBrowserPlugin
{
    [Serializable]
    public enum SongSortMode
    {
        Default,
        Favorites,
        Original,
    }

    [Serializable]
    public class SongBrowserSettings
    {
        public SongSortMode sortMode = default(SongSortMode);
        public List<String> favorites;
        
        /// <summary>
        /// Constructor.
        /// </summary>
        public SongBrowserSettings()
        {
            favorites = new List<String>();
        }
    }
}