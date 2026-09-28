using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using SurvivalShooter.Core;
using SurvivalShooter.Audio;
using SurvivalShooter.Pooling;

namespace SurvivalShooter.Player
{
    /// <summary>
    /// Player Shooting System utilizing Object Pooling for zero runtime garbage collection.
    /// Incorporates dynamic weapon recoil camera feedback via DOTween, UI fire buttons,
    /// screen touch shooting in AR space, and editor mouse look controls for testing.
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

        [Header("Recoil Polish")]
        [SerializeField] private float recoilPitch = -1.6f;
        [SerializeField] private float recoilDuration = 0.12f;

        private float nextFireTime;
        private Camera arCamera;
        private bool isCombatActive;
        private Tween recoilTween;

        #if UNITY_EDITOR
        private float yaw = 0f;
        private float pitch = 0f;
        [SerializeField] private float mouseSensitivity = 2.5f;
        #endif

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
            recoilTween?.Kill();
        }

        private void HandleGameStateChanged(GameState state)
        {
            isCombatActive = (state == GameState.Playing);
        }

        private void Update()
        {
            #if UNITY_EDITOR
            HandleEditorCameraLook();
            #endif

            if (!isCombatActive) return;

            // Handle touch or mouse click shooting in editor or on mobile device
            #if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    Shoot();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                Shoot();
            }
            #else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        Shoot();
                    }
                }
            }
            #endif
        }

        #if UNITY_EDITOR
        private void HandleEditorCameraLook()
        {
            // Allow aiming camera when holding right mouse button or during combat
            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * mouseSensitivity * 1.5f;
                pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * 1.5f;
                pitch = Mathf.Clamp(pitch, -60f, 60f);

                if (arCamera != null)
                {
                    arCamera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
                }
            }

            // Keyboard strafe for testing
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                Vector3 moveDir = (arCamera.transform.forward * v + arCamera.transform.right * h);
                moveDir.y = 0;
                transform.position += moveDir.normalized * (3.5f * Time.deltaTime);
            }
        }
        #endif

        public void Shoot()
        {
            if (!isCombatActive) return;
            if (Time.time < nextFireTime) return;

            nextFireTime = Time.time + fireRate;

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Quaternion spawnRot = firePoint != null ? firePoint.rotation : transform.rotation;

            // Offset slightly forward from camera lens
            spawnPos += (firePoint != null ? firePoint.forward : transform.forward) * 0.25f;

            // Spawn from pre-allocated object pool (Zero GC)
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

            // Apply procedural recoil kick to camera via DOTween
            if (arCamera != null)
            {
                recoilTween?.Kill();
                recoilTween = arCamera.transform.DOPunchRotation(
                    new Vector3(recoilPitch, Random.Range(-0.4f, 0.4f), 0f),
                    recoilDuration,
                    4,
                    0.5f
                );
            }

            // Crisp audio feedback
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