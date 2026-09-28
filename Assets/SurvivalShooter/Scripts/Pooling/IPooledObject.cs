namespace SurvivalShooter.Pooling
{
    /// <summary>
    /// Interface implemented by all pooled objects to manage reuse lifecycle.
    /// </summary>
    public interface IPooledObject
    {
        string PoolTag { get; set; }
        void OnObjectSpawn();
        void ReturnToPool();
    }
}
