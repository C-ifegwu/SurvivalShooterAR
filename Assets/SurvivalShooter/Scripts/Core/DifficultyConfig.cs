using System;
using UnityEngine;

namespace SurvivalShooter.Core
{
    /// <summary>
    /// Tunable gameplay variables for a difficulty mode (bonus requirement: 2 difficulty modes).
    /// </summary>
    [Serializable]
    public class DifficultyConfig
    {
        public DifficultyLevel level = DifficultyLevel.Normal;
        public string displayName = "NORMAL";
        [TextArea] public string description = "90s • Standard horde";

        [Header("Round")]
        public float survivalTime = 90f;

        [Header("Spawning")]
        public float startSpawnInterval = 3.0f;
        public float endSpawnInterval = 1.5f;
        public int maxAliveEnemies = 7;
        [Range(0f, 1f)] public float shooterChance = 0.35f;

        [Header("Enemy Scalers")]
        public float enemySpeedMultiplier = 1f;
        public float enemyHealthMultiplier = 1f;
        public float enemyDamageMultiplier = 1f;

        [Header("Scoring")]
        public float scoreMultiplier = 1f;

        public static DifficultyConfig Normal() => new DifficultyConfig
        {
            level = DifficultyLevel.Normal,
            displayName = "NORMAL",
            description = "90s  •  Standard horde",
            survivalTime = 90f,
            startSpawnInterval = 3.0f,
            endSpawnInterval = 1.6f,
            maxAliveEnemies = 7,
            shooterChance = 0.35f,
            enemySpeedMultiplier = 1f,
            enemyHealthMultiplier = 1f,
            enemyDamageMultiplier = 1f,
            scoreMultiplier = 1f
        };

        public static DifficultyConfig Hard() => new DifficultyConfig
        {
            level = DifficultyLevel.Hard,
            displayName = "HARD",
            description = "120s  •  Faster, tougher, deadlier",
            survivalTime = 120f,
            startSpawnInterval = 2.2f,
            endSpawnInterval = 1.0f,
            maxAliveEnemies = 11,
            shooterChance = 0.45f,
            enemySpeedMultiplier = 1.3f,
            enemyHealthMultiplier = 1.5f,
            enemyDamageMultiplier = 1.5f,
            scoreMultiplier = 1.5f
        };
    }
}
