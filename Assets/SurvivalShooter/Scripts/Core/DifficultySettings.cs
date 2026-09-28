using System;
using UnityEngine;

namespace SurvivalShooter.Core
{
    [Serializable]
    public class DifficultyConfig
    {
        public DifficultyLevel level;
        public float survivalTime = 90f;
        public float spawnInterval = 3.2f;
        public float enemySpeedMultiplier = 1.0f;
        public float enemyHealthMultiplier = 1.0f;
        public float enemyDamageMultiplier = 1.0f;
        public int playerBulletDamage = 20;

        public static DifficultyConfig CreateDefaultNormal()
        {
            return new DifficultyConfig
            {
                level = DifficultyLevel.Normal,
                survivalTime = 90f,
                spawnInterval = 3.2f,
                enemySpeedMultiplier = 1.0f,
                enemyHealthMultiplier = 1.0f,
                enemyDamageMultiplier = 1.0f,
                playerBulletDamage = 20
            };
        }

        public static DifficultyConfig CreateDefaultHard()
        {
            return new DifficultyConfig
            {
                level = DifficultyLevel.Hard,
                survivalTime = 120f,
                spawnInterval = 2.0f,
                enemySpeedMultiplier = 1.35f,
                enemyHealthMultiplier = 1.4f,
                enemyDamageMultiplier = 1.5f,
                playerBulletDamage = 20
            };
        }
    }
}
