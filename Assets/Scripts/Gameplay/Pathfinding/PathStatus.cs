namespace BarracksAndBolts.Gameplay.Pathfinding
{
    public enum PathStatus
    {
        /// <summary>The path ends at the requested goal.</summary>
        Found,

        /// <summary>The goal can't be reached (blocked or walled off); the path ends at the closest reachable cell.</summary>
        Partial,

        /// <summary>No search was possible, e.g. the start is outside the map.</summary>
        NotFound
    }
}
