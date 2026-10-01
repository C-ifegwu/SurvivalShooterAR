using UnityEngine;

namespace SurvivalShooter.Core
{
    /// <summary>
    /// Generic MonoBehaviour Singleton (Singleton pattern).
    /// Every manager inherits from this instead of duplicating the same boilerplate,
    /// which demonstrates inheritance + generics and keeps managers encapsulated.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = (T)this;
            OnSingletonAwake();
        }

        /// <summary>Hook for derived classes, called once the instance is registered.</summary>
        protected virtual void OnSingletonAwake() { }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
