using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Gameplay.Teams;
using NUnit.Framework;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class TeamServiceTests
    {
        [TestCase(TeamColor.Red, TeamColor.Yellow)]
        [TestCase(TeamColor.Yellow, TeamColor.Red)]
        public void GetColor_EnemyTakesAColorThePlayerDidNotPick(TeamColor playerColor, TeamColor expectedEnemyColor)
        {
            var settings = new GameSessionSettings { PlayerColor = playerColor };

            var teams = new TeamService(settings, null);

            Assert.AreEqual(playerColor, teams.GetColor(Faction.Player));
            Assert.AreEqual(expectedEnemyColor, teams.GetColor(Faction.Enemy));
        }
    }
}
