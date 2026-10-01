namespace SurvivalShooter.Core
{
    /// <summary>Identifiers for every state in the game flow (Start → Play → End).</summary>
    public enum GameStateId
    {
        MainMenu,
        Scanning,
        Countdown,
        Playing,
        Paused,
        GameOver
    }

    public enum GameResult
    {
        None,
        Survived,
        Defeated
    }

    public enum DifficultyLevel
    {
        Normal,
        Hard
    }

    public enum EnemyType
    {
        Melee,
        Shooter
    }

    /// <summary>Which side an entity/projectile belongs to (prevents friendly fire).</summary>
    public enum Team
    {
        Player,
        Enemy
    }
}
