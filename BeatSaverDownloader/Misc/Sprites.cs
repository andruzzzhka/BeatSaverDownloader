using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BeatSaverDownloader.Misc
{
    class Sprites
    {
        public static Sprite AddToFavorites;
        public static Sprite RemoveFromFavorites;
        public static Sprite StarFull;
        public static Sprite StarEmpty;
        public static Sprite DoubleArrow;

        //by elliotttate#9942
        public static Sprite BeastSaberLogo;
        public static Sprite ReviewIcon;

        //https://www.flaticon.com/free-icon/thumbs-up_70420
        public static Sprite ThumbUp;

        //https://www.flaticon.com/free-icon/dislike-thumb_70485
        public static Sprite ThumbDown;

        //https://www.flaticon.com/free-icon/playlist_727239
        public static Sprite PlaylistIcon;

        //https://www.flaticon.com/free-icon/musical-note_727218
        public static Sprite SongIcon;

        //https://www.flaticon.com/free-icon/download_724933
        public static Sprite DownloadIcon;

        //https://www.flaticon.com/free-icon/media-play-symbol_31128
        public static Sprite PlayIcon;
        
        //https://game-icons.net/1x1/delapouite/perspective-dice-six-faces-three.html
        public static Sprite RandomIcon;

        //https://www.flaticon.com/free-icon/waste-bin_70388
        public static Sprite DeleteIcon;

        public static void ConvertToSprites()
        {
            Plugin.log.Info("Creating sprites...");

            AddToFavorites =        CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("BeatSaverDownloader.Assets.AddToFavorites.png");
            RemoveFromFavorites =   CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("BeatSaverDownloader.Assets.RemoveFromFavorites.png");
            StarFull =              CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("BeatSaverDownloader.Assets.StarFull.png");
            StarEmpty =             CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("BeatSaverDownloader.Assets.StarEmpty.png");
            BeastSaberLogo =        CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("BeatSaverDownloader.Assets.BeastSaberLogo.png");
            ReviewIcon =            CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("BeatSaverDownloader.Assets.ReviewIcon.png");
            ThumbUp =               CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("BeatSaverDownloader.Assets.ThumbUp.png");
            ThumbDown =             CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("BeatSaverDownloader.Assets.ThumbDown.png");
            PlaylistIcon =          CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("BeatSaverDownloader.Assets.PlaylistIcon.png");
            SongIcon =              CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("BeatSaverDownloader.Assets.SongIcon.png");
            DownloadIcon =          CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("BeatSaverDownloader.Assets.DownloadIcon.png");
            PlayIcon =              CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("BeatSaverDownloader.Assets.PlayIcon.png");
            DoubleArrow =           CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("BeatSaverDownloader.Assets.DoubleArrow.png");
            RandomIcon =            CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("BeatSaverDownloader.Assets.RandomIcon.png");
            DeleteIcon =            CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("BeatSaverDownloader.Assets.DeleteIcon.png");

            Plugin.log.Info("Creating sprites... Done!");
        }

        public static string SpriteToBase64(Sprite input)
        {
            return Convert.ToBase64String(input.texture.EncodeToPNG());
        }

        public static Sprite Base64ToSprite(string input)
        {
            string base64 = input;
            if (input.Contains(","))
            {
                base64 = input.Substring(input.IndexOf(','));
            }
            Texture2D tex = Base64ToTexture2D(base64);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), (Vector2.one / 2f));
        }

        public static Texture2D Base64ToTexture2D(string encodedData)
        {
            byte[] imageData = Convert.FromBase64String(encodedData);

            Texture2D texture = new Texture2D(0, 0, TextureFormat.ARGB32, false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Trilinear;
            texture.LoadImage(imageData);
            return texture;
        }
    }
}
