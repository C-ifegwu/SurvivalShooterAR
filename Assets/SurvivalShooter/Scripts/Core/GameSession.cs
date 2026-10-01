using UnityEngine;

namespace SurvivalShooter.Core
{
    /// <summary>
    /// Plain C# model of one play session (encapsulation: state can only change through methods).
    /// </summary>
    public class GameSession
    {
        public DifficultyConfig Config { get; private set; }
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int MeleeKills { get; private set; }
        public int ShooterKills { get; private set; }
        public float TimeSurvived { get; private set; }
        public float TimeRemaining => Mathf.Max(0f, Config.survivalTime - TimeSurvived);
        public float Progress01 => Config.survivalTime <= 0f ? 1f : Mathf.Clamp01(TimeSurvived / Config.survivalTime);
        public GameResult Result { get; private set; }
        public int SurvivalBonus { get; private set; }
        public bool IsFinished => Result != GameResult.None;

        public GameSession(DifficultyConfig config)
        {
            Config = config;
            Result = GameResult.None;
        }

        public void Tick(float deltaTime)
        {
            if (IsFinished) return;
            TimeSurvived = Mathf.Min(Config.survivalTime, TimeSurvived + deltaTime);
        }

        public int RegisterKill(EnemyType type, int baseScore)
        {
            int awarded = Mathf.RoundToInt(baseScore * Config.scoreMultiplier);
            Kills++;
            if (type == EnemyType.Melee) MeleeKills++; else ShooterKills++;
            Score += awarded;
            return awarded;
        }

        public void Finish(GameResult result, int remainingHealth)
        {
            if (IsFinished) return;
            Result = result;
            if (result == GameResult.Survived)
            {
                SurvivalBonus = Mathf.RoundToInt((500 + remainingHealth * 5) * Config.scoreMultiplier);
                Score += SurvivalBonus;
            }
        }
    }
}
