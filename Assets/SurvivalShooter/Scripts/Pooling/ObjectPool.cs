using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalShooter.Pooling
{
    /// <summary>Contract for objects that live in an <see cref="ObjectPool{T}"/>.</summary>
    public interface IPoolable
    {
        /// <summary>Called every time the object is taken from the pool (reset state here).</summary>
        void OnSpawned();
        /// <summary>Called when the object goes back to the pool.</summary>
        void OnDespawned();
    }

    /// <summary>
    /// Generic Object Pool (Object Pooling pattern).
    /// All instances are created up-front in <see cref="Prewarm"/>; during gameplay objects are only
    /// activated / deactivated, never Instantiated or Destroyed.
    /// </summary>
    public class ObjectPool<T> where T : Component, IPoolable
    {
        private readonly T prefab;
        private readonly Transform container;
        private readonly Stack<T> inactive = new Stack<T>();
        private readonly List<T> active = new List<T>();
        private readonly Action<T> onCreate;

        public int CountInactive => inactive.Count;
        public int CountActive => active.Count;
        public int CountAll => inactive.Count + active.Count;
        public int ExpansionCount { get; private set; }

        public ObjectPool(T prefab, Transform container, int prewarmCount, Action<T> onCreate = null)
        {
            this.prefab = prefab;
            this.container = container;
            this.onCreate = onCreate;
            Prewarm(prewarmCount);
        }

        private void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                inactive.Push(CreateInstance());
            }
        }

        private T CreateInstance()
        {
            T instance = UnityEngine.Object.Instantiate(prefab, container);
            instance.gameObject.name = prefab.name;
            instance.gameObject.SetActive(false);
            onCreate?.Invoke(instance);
            return instance;
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            T item;
            if (inactive.Count > 0)
            {
                item = inactive.Pop();
            }
            else
            {
                // Safety net only; the pool is sized so this never happens in normal play.
                item = CreateInstance();
                ExpansionCount++;
            }

            item.transform.SetPositionAndRotation(position, rotation);
            item.gameObject.SetActive(true);
            active.Add(item);
            item.OnSpawned();
            return item;
        }

        public void Release(T item)
        {
            if (item == null || !active.Remove(item)) return;
            item.OnDespawned();
            item.gameObject.SetActive(false);
            item.transform.SetParent(container, false);
            inactive.Push(item);
        }

        public void ReleaseAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Release(active[i]);
            }
        }
    }
}
