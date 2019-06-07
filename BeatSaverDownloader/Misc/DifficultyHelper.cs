using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaverDownloader.Misc {
    public static class DifficultyHelper {

        const string kDifficultyEasySerializedName = "Easy";
        const string kDifficultyNormalSerializedName = "Normal";
        const string kDifficultyHardSerializedName = "Hard";
        const string kDifficultyExpertSerializedName = "Expert";
        const string kDifficultyExpertPlusNameSerializedLegacy = "Expert+";
        const string kDifficultyExpertPlusSerializedName = "ExpertPlus";
        const string kDifficultyUnknownSerializedName = "Unknown";

        public static BeatmapDifficulty BeatmapDifficultyFromSerializedName(string name) {

            if (name == kDifficultyEasySerializedName) {
                return BeatmapDifficulty.Easy;
            } else if (name == kDifficultyNormalSerializedName) {
                return BeatmapDifficulty.Normal;
            } else if (name == kDifficultyHardSerializedName) {
                return BeatmapDifficulty.Hard;
            } else if (name == kDifficultyExpertSerializedName) {
                return BeatmapDifficulty.Expert;
            } else if (name == kDifficultyExpertPlusNameSerializedLegacy || name == kDifficultyExpertPlusSerializedName) {
                return BeatmapDifficulty.ExpertPlus;
            }

            return BeatmapDifficulty.Normal;
        }
    }
}
