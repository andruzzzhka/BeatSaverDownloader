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

        public DifficultyLevel(LevelStaticData.DifficultyLevel difficultyLevel)
        {
            difficulty = LevelStaticData.GetDifficultyName(difficultyLevel.difficulty);
            difficultyRank = difficultyLevel.difficultyRank;
        }

        public DifficultyLevel(string Difficulty, int DifficultyRank, string AudioPath, string JsonPath, int Offset = 0)
        {
            difficulty = Difficulty;
            difficultyRank = DifficultyRank;
            audioPath = AudioPath;
            jsonPath = JsonPath;
            offset = Offset;

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
        public string downvotes;
        public string plays;
        public string beattext;
        public string uploadtime;
        public string songName;
        public string songSubName;
        public string authorName;
        public string beatsPerMinute;
        public string coverUrl;
        public string downloadUrl;
        public DifficultyLevel[] difficultyLevels;
        public string img;

        public string path;

        public Song(JSONNode jsonNode)
        {
            id = jsonNode["key"];
            beatname = jsonNode["name"];
            ownerid = jsonNode["uploaderId"];
            downloads = jsonNode["downloadCount"];
            upvotes = jsonNode["upVotes"];
            downvotes = jsonNode["downVotes"];
            plays = jsonNode["playedCount"];
            beattext = jsonNode["description"];
            uploadtime = jsonNode["createdAt"];
            songName = jsonNode["songName"];
            songSubName = jsonNode["songSubName"];
            authorName = jsonNode["authorName"];
            beatsPerMinute = jsonNode["bpm"];
            coverUrl = jsonNode["coverUrl"];
            downloadUrl = jsonNode["downloadUrl"];

            var difficultyNode = jsonNode["difficulties"];

            difficultyLevels = new DifficultyLevel[difficultyNode.Count];

            for (int i = 0; i < difficultyNode.Count; i++)
            {
                difficultyLevels[i] = new DifficultyLevel(difficultyNode[i]["difficulty"], difficultyNode[i]["difficultyRank"], difficultyNode[i]["audioPath"], difficultyNode[i]["jsonPath"]);
            }
        }

        public Song(JSONNode jsonNode, JSONNode difficultyNode)
        {

            id = jsonNode["key"];
            beatname = jsonNode["name"];
            ownerid = jsonNode["uploaderId"];
            downloads = jsonNode["downloadCount"];
            upvotes = jsonNode["upVotes"];
            downvotes = jsonNode["downVotes"];
            plays = jsonNode["playedCount"];
            beattext = jsonNode["description"];
            uploadtime = jsonNode["createdAt"];
            songName = jsonNode["songName"];
            songSubName = jsonNode["songSubName"];
            authorName = jsonNode["authorName"];
            beatsPerMinute = jsonNode["bpm"];
            coverUrl = jsonNode["coverUrl"];
            downloadUrl = jsonNode["downloadUrl"];

            difficultyLevels = new DifficultyLevel[difficultyNode.Count];

            for (int i = 0; i < difficultyNode.Count; i++)
            {
                difficultyLevels[i] = new DifficultyLevel(difficultyNode[i]["difficulty"], difficultyNode[i]["difficultyRank"], difficultyNode[i]["audioPath"], difficultyNode[i]["jsonPath"]);
            }

        }

        public bool Compare(Song compareTo)
        {
            if (compareTo != null)
            {
                //Logger.StaticLog("songName is " + string.IsNullOrEmpty(compareTo.songName));
                //Logger.StaticLog("songSubName is " + string.IsNullOrEmpty(compareTo.songSubName));
                //Logger.StaticLog("authorName is " + string.IsNullOrEmpty(compareTo.authorName));
                //Logger.StaticLog("authorName is " + string.IsNullOrEmpty(compareTo.authorName));

                if (HTML5Decode.HtmlDecode(songName) == HTML5Decode.HtmlDecode(compareTo.songName))
                {
                    if (difficultyLevels != null && compareTo.difficultyLevels != null)
                    {
                        return (HTML5Decode.HtmlDecode(songSubName) == HTML5Decode.HtmlDecode(compareTo.songSubName) && HTML5Decode.HtmlDecode(authorName) == HTML5Decode.HtmlDecode(compareTo.authorName) && difficultyLevels.Length == compareTo.difficultyLevels.Length);
                    }
                    else
                    {
                        return (HTML5Decode.HtmlDecode(songSubName) == HTML5Decode.HtmlDecode(compareTo.songSubName) && HTML5Decode.HtmlDecode(authorName) == HTML5Decode.HtmlDecode(compareTo.authorName));
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }



        public Song(CustomLevelStaticData _data)
        {
            songName = _data.songName;
            songSubName = _data.songSubName;
            authorName = _data.authorName;
            difficultyLevels = ConvertDifficultyLevels(_data.difficultyLevels);
        }

        public Song(CustomSongInfo _song)
        {

            songName = _song.songName;
            songSubName = _song.songSubName;
            authorName = _song.authorName;
            difficultyLevels = ConvertDifficultyLevels(_song.difficultyLevels);
            path = _song.path;
        }

        public DifficultyLevel[] ConvertDifficultyLevels(CustomSongInfo.DifficultyLevel[] _difficultyLevels)
        {
            if (_difficultyLevels != null && _difficultyLevels.Length > 0)
            {
                DifficultyLevel[] buffer = new DifficultyLevel[_difficultyLevels.Length];

                for (int i = 0; i < _difficultyLevels.Length; i++)
                {
                    buffer[i] = new DifficultyLevel(_difficultyLevels[i]);
                }


                return buffer;
            }
            else
            {
                return null;
            }
        }

        public DifficultyLevel[] ConvertDifficultyLevels(LevelStaticData.DifficultyLevel[] _difficultyLevels)
        {
            if (_difficultyLevels != null && _difficultyLevels.Length > 0)
            {
                DifficultyLevel[] buffer = new DifficultyLevel[_difficultyLevels.Length];

                for (int i = 0; i < _difficultyLevels.Length; i++)
                {
                    buffer[i] = new DifficultyLevel(_difficultyLevels[i]);
                }


                return buffer;
            }
            else
            {
                return null;
            }
        }

    }
    [Serializable]
    public class RootObject
    {
        public Song[] songs;
    }
}
