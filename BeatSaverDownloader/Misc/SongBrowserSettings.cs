using System;
using System.Collections.Generic;

//SongBrowserSettings from halsafar's SongBrowserPlugin
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
        
        public SongBrowserSettings()
        {
            favorites = new List<String>();
        }
    }
}