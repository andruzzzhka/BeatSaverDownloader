using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleJSON;
using SongLoaderPlugin;

namespace BeatSaverDownloader
{

    [Serializable]
    public class DifficultyLevel
    {
        public string difficulty;
        public int difficultyRank;
        public string audioPath;
        public string jsonPath;
        public int? offset;

        public DifficultyLevel(CustomSongInfo.DifficultyLevel difficultyLevel)
        {
            difficulty = difficultyLevel.difficulty;
            difficultyRank = difficultyLevel.difficultyRank;
            audioPath = difficultyLevel.audioPath;
            jsonPath = difficultyLevel.jsonPath;
        }

        public DifficultyLevel(string Difficulty, int DifficultyRank, string AudioPath, string JsonPath, int Offset = 0)
        {
            difficulty = Difficulty;
            difficultyRank = DifficultyRank;
            audioPath = AudioPath;
            jsonPath = JsonPath;
            offset = Offset;

        }

        public bool Compare(DifficultyLevel compareTo)
        {

            return (difficulty==compareTo.difficulty && difficultyRank == compareTo.difficultyRank && audioPath == compareTo.audioPath && jsonPath == compareTo.jsonPath);
        }

    }
    [Serializable]
    public class Song
    {
        public string id;
        public string beatname;
        public string ownerid;
        public string downloads;
        public string upvotes;
        public string beattext;
        public string uploadtime;
        public string songName;
        public string songSubName;
        public string authorName;
        public string beatsPerMinute;
        public DifficultyLevel[] difficultyLevels;
        public string img;

        public Song(JSONNode jsonNode)
        {
            
        id = jsonNode["id"];
        beatname = jsonNode["beatname"];
        ownerid = jsonNode["ownerid"];
        downloads = jsonNode["downloads"];
        upvotes = jsonNode["upvotes"];
        beattext = jsonNode["beattext"];
        uploadtime = jsonNode["uploadtime"];
        songName = jsonNode["songName"];
        songSubName = jsonNode["songSubName"];
        authorName = jsonNode["authorName"];
        beatsPerMinute = jsonNode["beatsPerMinute"];
        img = jsonNode["img"];

        difficultyLevels = new DifficultyLevel[jsonNode["difficultyLevels"].Count];

        for (int i = 0; i < jsonNode["difficultyLevels"].Count; i++)
        {
            difficultyLevels[i] = new DifficultyLevel(jsonNode["difficultyLevels"][i]["difficulty"], jsonNode["difficultyLevels"][i]["difficultyRank"], jsonNode["difficultyLevels"][i]["audioPath"], jsonNode["difficultyLevels"][i]["jsonPath"]);
        }

        }

        public bool Compare(Song compareTo)
        {
            bool diffisIdentical = true;

            if (difficultyLevels.Length != compareTo.difficultyLevels.Length)
            {
                diffisIdentical = false;

            }
            else
            {

                for (int i = 0; i < difficultyLevels.Length; i++)
                {
                    diffisIdentical = diffisIdentical && difficultyLevels[i].Compare(compareTo.difficultyLevels[i]);
                }
            }
            



            return (HtmlDecode(songName) == HtmlDecode(compareTo.songName) && HtmlDecode(songSubName) == HtmlDecode(compareTo.songSubName) && HtmlDecode(authorName) == HtmlDecode(compareTo.authorName) && diffisIdentical );
        }

        private string HtmlDecode(string songName)
        {
            string buf = songName;

            buf = buf.Replace("&amp;", "&").Replace("&period;", ".").Replace("&lpar;", "(").Replace("&rpar;", ")").Replace("&semi;", ";").Replace("&lbrack;", "[").Replace("&rsqb;", "]").Replace("&apos;", "\'");

            return buf;
        }



        public Song(CustomSongInfo _song)
        {

            songName = _song.songName;
            songSubName = _song.songSubName;
            authorName = _song.authorName;
            difficultyLevels = ConvertDifficultyLevels(_song.difficultyLevels);
            
        }

        public DifficultyLevel[] ConvertDifficultyLevels(CustomSongInfo.DifficultyLevel[] _difficultyLevels)
        {
            DifficultyLevel[] buffer = new DifficultyLevel[_difficultyLevels.Length];

            for(int i = 0; i < _difficultyLevels.Length; i++)
            {
                buffer[i] = new DifficultyLevel(_difficultyLevels[i]);
            }


            return buffer;
        }

        public Song(string ID, string BeatName, string OwnerID, string Downloads, string UpVotes, string BeatText, string UploadTime, string SongName, string SongSubName, string AuthorName, string BeatsPerMinute, string Img, DifficultyLevel[] DifficultyLevels)
        {
            id = ID;
            beatname = BeatName;
            ownerid = OwnerID;
            downloads = Downloads;
            upvotes = UpVotes;
            beattext = BeatText;
            uploadtime = UploadTime;
            songName = SongName;
            songSubName = SongSubName;
            authorName = AuthorName;
            beatsPerMinute = BeatsPerMinute;
            img = Img;
            difficultyLevels = DifficultyLevels;
        }
        public Song(string ID, string BeatName, string OwnerID, string Downloads, string UpVotes, string BeatText, string UploadTime, string SongName, string SongSubName, string AuthorName, string BeatsPerMinute, string Img)
        {
            id = ID;
            beatname = BeatName;
            ownerid = OwnerID;
            downloads = Downloads;
            upvotes = UpVotes;
            beattext = BeatText;
            uploadtime = UploadTime;
            songName = SongName;
            songSubName = SongSubName;
            authorName = AuthorName;
            beatsPerMinute = BeatsPerMinute;
            img = Img;
        }
    }
    [Serializable]
    public class RootObject
    {
        public Song[] songs;
    }
}
