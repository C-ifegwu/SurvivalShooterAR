using UnityEngine;

namespace SurvivalShooter.Core
{
    /// <summary>Data describing a single hit.</summary>
    public readonly struct DamageInfo
    {
        public readonly int Amount;
        public readonly Vector3 Point;
        public readonly Vector3 Direction;
        public readonly Team SourceTeam;

        public DamageInfo(int amount, Vector3 point, Vector3 direction, Team sourceTeam)
        {
            Amount = amount;
            Point = point;
            Direction = direction;
            SourceTeam = sourceTeam;
        }
    }

    /// <summary>
    /// Abstraction for anything that can be hit (player and every enemy).
    /// Projectiles only talk to this interface, so they never need to know concrete types (polymorphism).
    /// </summary>
    public interface IDamageable
    {
        Team Team { get; }
        bool IsAlive { get; }
        void TakeDamage(DamageInfo info);
    }
}
