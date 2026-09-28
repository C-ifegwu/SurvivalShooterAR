namespace SurvivalShooter.Core
{
    /// <summary>
    /// Explicit game state machine states.
    /// </summary>
    public enum GameState
    {
        ScanningPlanes,
        PlacementReady,
        Playing,
        GameOver,
        Victory
    }

    /// <summary>
    /// Gameplay difficulty levels.
    /// </summary>
    public enum DifficultyLevel
    {
        Normal,
        Hard
    }

    /// <summary>
    /// Type of enemy entities.
    /// </summary>
    public enum EnemyType
    {
        MeleeZombie,
        ShooterSoldier
    }
}
