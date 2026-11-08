using System;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Gameplay.Data;

namespace BarracksAndBolts.Gameplay.Teams
{
    /// <summary>
    /// Maps a faction to its color. The player picks a color; the enemy gets the first one left.
    /// </summary>
    public sealed class TeamService
    {
        private readonly GameDatabase _database;
        private readonly TeamColor _playerColor;
        private readonly TeamColor _enemyColor;

        public TeamService(GameSessionSettings sessionSettings, GameDatabase database)
        {
            _database = database;
            _playerColor = sessionSettings.PlayerColor;
            _enemyColor = FindEnemyColor(_playerColor);
        }

        public TeamColor GetColor(Faction faction)
        {
            return faction == Faction.Player ? _playerColor : _enemyColor;
        }

        public TeamColorDefinition GetColorSet(Faction faction)
        {
            return _database.GetTeamColor(GetColor(faction));
        }

        private static TeamColor FindEnemyColor(TeamColor playerColor)
        {
            foreach (TeamColor color in Enum.GetValues(typeof(TeamColor)))
            {
                if (color != playerColor)
                    return color;
            }

            throw new InvalidOperationException("At least two team colors are needed.");
        }
    }
}
