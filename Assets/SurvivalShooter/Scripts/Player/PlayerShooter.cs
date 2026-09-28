using UnityEngine;
using UnityEngine.EventSystems;
using SurvivalShooter.Core;
using SurvivalShooter.Audio;
using SurvivalShooter.Pooling;

namespace SurvivalShooter.Player
{
    /// <summary>
    /// Player Shooting System utilizing Object Pooling for zero runtime garbage collection.
    /// Supports both UI fire button triggers and screen touch shooting in AR space.
    /// </summary>
    public class PlayerShooter : MonoBehaviour
    {
        public static PlayerShooter Instance { get; private set; }

        [Header("Shooting Configuration")]
        [SerializeField] private string projectilePoolTag = "PlayerProjectile";
        [SerializeField] private float fireRate = 0.22f;
        [SerializeField] private int bulletDamage = 20;
        [SerializeField] private float bulletSpeed = 22f;

        [Header("Fire Origin")]
        [SerializeField] private Transform firePoint;

        private float nextFireTime;
        private Camera arCamera;
        private bool isCombatActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            arCamera = GetComponent<Camera>();
            if (arCamera == null)
            {
                arCamera = Camera.main;
            }

            if (firePoint == null && arCamera != null)
            {
                firePoint = arCamera.transform;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameState state)
        {
            isCombatActive = (state == GameState.Playing);
        }

        private void Update()
        {
            if (!isCombatActive) return;

            // Handle touch or mouse click shooting in editor or on mobile device
            #if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0))
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    Shoot();
                }
            }
            #else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        Shoot();
                    }
                }
            }
            #endif
        }

        public void Shoot()
        {
            if (!isCombatActive) return;
            if (Time.time < nextFireTime) return;

            nextFireTime = Time.time + fireRate;

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Quaternion spawnRot = firePoint != null ? firePoint.rotation : transform.rotation;

            // Offset slightly forward from camera lens
            spawnPos += (firePoint != null ? firePoint.forward : transform.forward) * 0.25f;

            // Spawn from pre-allocated object pool
            GameObject bulletObj = ObjectPoolManager.Instance.SpawnFromPool(projectilePoolTag, spawnPos, spawnRot);
            if (bulletObj != null)
            {
                PooledProjectile bullet = bulletObj.GetComponent<PooledProjectile>();
                if (bullet != null)
                {
                    bullet.IsPlayerProjectile = true;
                    bullet.Damage = bulletDamage;
                    bullet.Speed = bulletSpeed;
                }
            }

            // Audio feedback
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPlayerShoot();
            }
        }

        public void SetDamage(int damage)
        {
            bulletDamage = damage;
        }
    }
}
