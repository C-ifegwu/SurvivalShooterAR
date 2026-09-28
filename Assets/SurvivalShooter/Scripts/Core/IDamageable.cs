using UnityEngine;

namespace SurvivalShooter.Core
{
    /// <summary>
    /// Interface for any entity capable of receiving damage.
    /// Demonstrates Abstraction and enables Polymorphic damage delivery.
    /// </summary>
    public interface IDamageable
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        bool IsDead { get; }

        void TakeDamage(int damageAmount, Vector3 hitPoint, Vector3 hitNormal);
    }
}
