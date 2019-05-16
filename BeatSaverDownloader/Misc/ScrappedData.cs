using Newtonsoft.Json;
using SongLoaderPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatSaverDownloader.Misc
{
    public class ScrappedSong
    {
        public string Key { get; set; }
        public string Hash { get; set; }
        public string SongName { get; set; }
        public string SongSubName { get; set; }
        public string AuthorName { get; set; }
        public List<DifficultyStats> Diffs { get; set; }
        public long Bpm { get; set; }
        public long PlayedCount { get; set; }
        public long Upvotes { get; set; }
        public long Downvotes { get; set; }
    }

    public class DifficultyStats
    {
        public string Diff { get; set; }
        public long Scores { get; set; }
        public double Stars { get; set; }
    }

    public class ScrappedData : MonoBehaviour
    {
        private static ScrappedData _instance = null;
        public static ScrappedData Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = new GameObject("ScrappedData").AddComponent<ScrappedData>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        public static List<ScrappedSong> Songs = new List<ScrappedSong>();

        public static string scrappedDataURL = "https://raw.githubusercontent.com/andruzzzhka/BeatSaberScrappedData/master/combinedScrappedData.json";

        public IEnumerator DownloadScrappedData(Action<List<ScrappedSong>> callback)
        {
            Plugin.log.Info("Downloading scrapped data...");

            UnityWebRequest www;
            bool timeout = false;
            float time = 0f;
            UnityWebRequestAsyncOperation asyncRequest;

            try
            {
                www = UnityWebRequest.Get(scrappedDataURL);

                asyncRequest = www.SendWebRequest();
            }
            catch (Exception e)
            {
                Plugin.log.Error(e);
                yield break;
            }

            while (!asyncRequest.isDone)
            {
                yield return null;
                time += Time.deltaTime;
                if (time >= 5f && asyncRequest.progress <= float.Epsilon)
                {
                    www.Abort();
                    timeout = true;
                    Plugin.log.Error("Connection timed out!");
                }
            }


            if (www.isNetworkError || www.isHttpError || timeout)
            {
                Plugin.log.Error("Unable to download scrapped data! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                Plugin.log.Info("Received response from github.com...");

                Songs = JsonConvert.DeserializeObject<List<ScrappedSong>>(www.downloadHandler.text).OrderByDescending(x => x.Diffs.Count > 0 ? x.Diffs.Max(y => y.Stars) : 0).ToList();
                
                callback?.Invoke(Songs);
                Plugin.log.Info("Scrapped data downloaded!");
            }
        }
        
    }
}
