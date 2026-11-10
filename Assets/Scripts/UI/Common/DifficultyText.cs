using BarracksAndBolts.Core.Session;

namespace BarracksAndBolts.UI.Common
{
    /// <summary>
    /// What each difficulty means, in words, for the main menu choice and the sandbox button. The numbers behind it
    /// are in the raid and normal mode settings.
    /// </summary>
    public static class DifficultyText
    {
        public static string Summary(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    return "Smaller raids, units eat less, more to start with";
                case Difficulty.Hard:
                    return "Bigger raids, hungry units, less to start with";
                default:
                    return "The game as it is meant to be";
            }
        }

        /// <summary>The name on the first line, the summary small under it. For a button.</summary>
        public static string WithSummary(Difficulty difficulty)
        {
            return $"{difficulty}\n<size=60%>{Summary(difficulty)}</size>";
        }
    }
}
