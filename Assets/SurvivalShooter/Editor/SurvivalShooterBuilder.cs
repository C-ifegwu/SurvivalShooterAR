using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

using SurvivalShooter.Core;
using SurvivalShooter.AR;
using SurvivalShooter.Player;
using SurvivalShooter.Enemies;
using SurvivalShooter.Pooling;
using SurvivalShooter.Audio;
using SurvivalShooter.Data;
using SurvivalShooter.UI;

namespace SurvivalShooter.Editor
{
    [InitializeOnLoad]
    public static class SurvivalShooterBuilder
    {
        private const string BUILD_KEY = "SurvivalShooter_Build_Executed_v5";

        static SurvivalShooterBuilder()
        {
            EditorApplication.delayCall += AutoRunIfPending;
        }

        private static void AutoRunIfPending()
        {
            if (!SessionState.GetBool(BUILD_KEY, false))
            {
                SessionState.SetBool(BUILD_KEY, true);
                Debug.Log("[SurvivalShooterBuilder] Auto-initiating project build and scene configuration...");
                BuildAll();
            }
        }

        [MenuItem("Survival Shooter AR/Build Full Project", priority = 1)]
        public static void BuildAll()
        {
            try
            {
                AssetDatabase.StartAssetEditing();

                EnsureFolders();
                ConfigureTextureImportSettings();
                CreateMaterials();
                CreateAnimationControllers();
                CreateProjectilePrefabs();
                CreateCustomPlaneTrackerPrefab();
                CreatePlacementIndicatorPrefab();
                CreateEnemyPrefabs();

                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                SetupSurvivalShooterScene();

                Debug.Log("<color=green><b>[SurvivalShooterBuilder] Full Survival Shooter AR Setup Completed Successfully!</b></color>");
            }
            catch (Exception ex)
            {
                AssetDatabase.StopAssetEditing();
                Debug.LogError($"[SurvivalShooterBuilder] Error building project: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void EnsureFolders()
        {
            string[] folders = new string[]
            {
                "Assets/SurvivalShooter",
                "Assets/SurvivalShooter/Materials",
                "Assets/SurvivalShooter/Prefabs",
                "Assets/SurvivalShooter/Prefabs/AR",
                "Assets/SurvivalShooter/Prefabs/Projectiles",
                "Assets/SurvivalShooter/Prefabs/Enemies",
                "Assets/SurvivalShooter/Animations",
                "Assets/SurvivalShooter/Scenes"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                    string child = Path.GetFileName(folder);
                    AssetDatabase.CreateFolder(parent, child);
                }
            }
        }

        private static void ConfigureTextureImportSettings()
        {
            string[] texturePaths = new string[]
            {
                "Assets/SurvivalShooter/Textures/CustomPlane_ChibuezeVictorIfegwu.png",
                "Assets/SurvivalShooter/Textures/PlacementReticle.png",
                "Assets/SurvivalShooter/Textures/Crosshair.png",
                "Assets/SurvivalShooter/Textures/DamageVignette.png",
                "Assets/SurvivalShooter/Textures/LaserGlow.png"
            };

            foreach (var path in texturePaths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = (path.Contains("Crosshair") || path.Contains("DamageVignette")) 
                        ? TextureImporterType.Sprite 
                        : TextureImporterType.Default;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = true;
                    importer.SaveAndReimport();
                }
            }
        }

        private static void CreateMaterials()
        {
            Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpUnlit == null) urpUnlit = Shader.Find("Unlit/Transparent");

            // 1. Custom Plane Material with Student Name
            string planeMatPath = "Assets/SurvivalShooter/Materials/CustomPlane_ChibuezeVictorIfegwu.mat";
            Material planeMat = AssetDatabase.LoadAssetAtPath<Material>(planeMatPath);
            if (planeMat == null)
            {
                planeMat = new Material(urpUnlit);
                AssetDatabase.CreateAsset(planeMat, planeMatPath);
            }
            Texture2D planeTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SurvivalShooter/Textures/CustomPlane_ChibuezeVictorIfegwu.png");
            if (planeTex != null) planeMat.mainTexture = planeTex;
            SetMaterialTransparent(planeMat);

            // 2. Placement Reticle Material
            string reticleMatPath = "Assets/SurvivalShooter/Materials/PlacementReticle.mat";
            Material reticleMat = AssetDatabase.LoadAssetAtPath<Material>(reticleMatPath);
            if (reticleMat == null)
            {
                reticleMat = new Material(urpUnlit);
                AssetDatabase.CreateAsset(reticleMat, reticleMatPath);
            }
            Texture2D reticleTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SurvivalShooter/Textures/PlacementReticle.png");
            if (reticleTex != null) reticleMat.mainTexture = reticleTex;
            SetMaterialTransparent(reticleMat);

            // 3. Player Laser Material (Cyan)
            string playerLaserPath = "Assets/SurvivalShooter/Materials/PlayerLaser.mat";
            Material playerLaserMat = AssetDatabase.LoadAssetAtPath<Material>(playerLaserPath);
            if (playerLaserMat == null)
            {
                playerLaserMat = new Material(urpUnlit);
                AssetDatabase.CreateAsset(playerLaserMat, playerLaserPath);
            }
            Texture2D laserTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SurvivalShooter/Textures/LaserGlow.png");
            if (laserTex != null) playerLaserMat.mainTexture = laserTex;
            playerLaserMat.color = new Color(0f, 0.95f, 1f, 1f);
            SetMaterialAdditive(playerLaserMat);

            // 4. Enemy Laser Material (Red / Orange)
            string enemyLaserPath = "Assets/SurvivalShooter/Materials/EnemyLaser.mat";
            Material enemyLaserMat = AssetDatabase.LoadAssetAtPath<Material>(enemyLaserPath);
            if (enemyLaserMat == null)
            {
                enemyLaserMat = new Material(urpUnlit);
                AssetDatabase.CreateAsset(enemyLaserMat, enemyLaserPath);
            }
            if (laserTex != null) enemyLaserMat.mainTexture = laserTex;
            enemyLaserMat.color = new Color(1f, 0.25f, 0.1f, 1f);
            SetMaterialAdditive(enemyLaserMat);
        }

        private static void SetMaterialTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private static void SetMaterialAdditive(Material mat)
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 1);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private static void CreateAnimationControllers()
        {
            // 1. Zombie Controller
            string zombieCtrlPath = "Assets/SurvivalShooter/Animations/ZombieController.controller";
            AnimatorController zombieCtrl = AnimatorController.CreateAnimatorControllerAtPath(zombieCtrlPath);
            zombieCtrl.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            zombieCtrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            zombieCtrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            AnimationClip zombieIdle = FindClipInAsset("Assets/ARGameAnimation/Scary Zombie Pack/zombie idle.fbx");
            AnimationClip zombieMove = FindClipInAsset("Assets/ARGameAnimation/Scary Zombie Pack/zombie run.fbx") 
                                     ?? FindClipInAsset("Assets/ARGameAnimation/Scary Zombie Pack/zombie walk.fbx");
            AnimationClip zombieAttack = FindClipInAsset("Assets/ARGameAnimation/Scary Zombie Pack/zombie attack.fbx");
            AnimationClip zombieDie = FindClipInAsset("Assets/ARGameAnimation/Scary Zombie Pack/zombie death.fbx");

            var sm = zombieCtrl.layers[0].stateMachine;
            var stateIdle = sm.AddState("Idle");
            stateIdle.motion = zombieIdle;
            sm.defaultState = stateIdle;

            var stateMove = sm.AddState("Move");
            stateMove.motion = zombieMove;

            var stateAttack = sm.AddState("Attack");
            stateAttack.motion = zombieAttack;

            var stateDie = sm.AddState("Die");
            stateDie.motion = zombieDie;

            var toMove = stateIdle.AddTransition(stateMove);
            toMove.hasExitTime = false;
            toMove.duration = 0.2f;
            toMove.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");

            var toIdle = stateMove.AddTransition(stateIdle);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.2f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

            var idleToAtk = stateIdle.AddTransition(stateAttack);
            idleToAtk.hasExitTime = false;
            idleToAtk.AddCondition(AnimatorConditionMode.If, 0, "Attack");

            var moveToAtk = stateMove.AddTransition(stateAttack);
            moveToAtk.hasExitTime = false;
            moveToAtk.AddCondition(AnimatorConditionMode.If, 0, "Attack");

            var atkToIdle = stateAttack.AddTransition(stateIdle);
            atkToIdle.hasExitTime = true;
            atkToIdle.duration = 0.25f;

            var anyToDie = sm.AddAnyStateTransition(stateDie);
            anyToDie.hasExitTime = false;
            anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");

            // 2. Soldier Controller
            string soldierCtrlPath = "Assets/SurvivalShooter/Animations/SoldierController.controller";
            AnimatorController soldierCtrl = AnimatorController.CreateAnimatorControllerAtPath(soldierCtrlPath);
            soldierCtrl.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            soldierCtrl.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            soldierCtrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            AnimationClip soldierIdle = FindClipInAsset("Assets/LowPolySoldiers_demo/animation/demo_combat_idle.FBX");
            AnimationClip soldierRun = FindClipInAsset("Assets/LowPolySoldiers_demo/animation/demo_combat_run.FBX");
            AnimationClip soldierShoot = FindClipInAsset("Assets/LowPolySoldiers_demo/animation/demo_combat_shoot.FBX");

            var sSm = soldierCtrl.layers[0].stateMachine;
            var sStateIdle = sSm.AddState("Idle");
            sStateIdle.motion = soldierIdle;
            sSm.defaultState = sStateIdle;

            var sStateRun = sSm.AddState("Run");
            sStateRun.motion = soldierRun;

            var sStateShoot = sSm.AddState("Shoot");
            sStateShoot.motion = soldierShoot;

            var sToRun = sStateIdle.AddTransition(sStateRun);
            sToRun.hasExitTime = false;
            sToRun.duration = 0.2f;
            sToRun.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");

            var sToIdle = sStateRun.AddTransition(sStateIdle);
            sToIdle.hasExitTime = false;
            sToIdle.duration = 0.2f;
            sToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

            var sIdleToShoot = sStateIdle.AddTransition(sStateShoot);
            sIdleToShoot.hasExitTime = false;
            sIdleToShoot.AddCondition(AnimatorConditionMode.If, 0, "Shoot");

            var sRunToShoot = sStateRun.AddTransition(sStateShoot);
            sRunToShoot.hasExitTime = false;
            sRunToShoot.AddCondition(AnimatorConditionMode.If, 0, "Shoot");

            var sShootToIdle = sStateShoot.AddTransition(sStateIdle);
            sShootToIdle.hasExitTime = true;
            sShootToIdle.duration = 0.2f;
        }

        private static AnimationClip FindClipInAsset(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        private static void CreateProjectilePrefabs()
        {
            Material playerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SurvivalShooter/Materials/PlayerLaser.mat");
            Material enemyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SurvivalShooter/Materials/EnemyLaser.mat");

            // 1. Player Projectile
            GameObject pBullet = new GameObject("PlayerProjectile");
            pBullet.tag = "Projectile";
            var pCol = pBullet.AddComponent<SphereCollider>();
            pCol.isTrigger = true;
            pCol.radius = 0.2f;

            var pVis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pVis.name = "Visual";
            pVis.transform.SetParent(pBullet.transform);
            pVis.transform.localScale = new Vector3(0.12f, 0.12f, 0.35f);
            pVis.GetComponent<MeshRenderer>().material = playerMat;
            UnityEngine.Object.DestroyImmediate(pVis.GetComponent<Collider>());

            var pTrail = pBullet.AddComponent<TrailRenderer>();
            pTrail.material = playerMat;
            pTrail.startWidth = 0.12f;
            pTrail.endWidth = 0.0f;
            pTrail.time = 0.18f;

            var pPooled = pBullet.AddComponent<PooledProjectile>();
            pPooled.IsPlayerProjectile = true;
            pPooled.Damage = 20;
            pPooled.Speed = 24f;

            PrefabUtility.SaveAsPrefabAsset(pBullet, "Assets/SurvivalShooter/Prefabs/Projectiles/PlayerProjectile.prefab");
            UnityEngine.Object.DestroyImmediate(pBullet);

            // 2. Enemy Projectile
            GameObject eBullet = new GameObject("EnemyProjectile");
            eBullet.tag = "Projectile";
            var eCol = eBullet.AddComponent<SphereCollider>();
            eCol.isTrigger = true;
            eCol.radius = 0.25f;

            var eVis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eVis.name = "Visual";
            eVis.transform.SetParent(eBullet.transform);
            eVis.transform.localScale = new Vector3(0.15f, 0.15f, 0.4f);
            eVis.GetComponent<MeshRenderer>().material = enemyMat;
            UnityEngine.Object.DestroyImmediate(eVis.GetComponent<Collider>());

            var eTrail = eBullet.AddComponent<TrailRenderer>();
            eTrail.material = enemyMat;
            eTrail.startWidth = 0.15f;
            eTrail.endWidth = 0.0f;
            eTrail.time = 0.22f;

            var ePooled = eBullet.AddComponent<PooledProjectile>();
            ePooled.IsPlayerProjectile = false;
            ePooled.Damage = 10;
            ePooled.Speed = 13f;

            PrefabUtility.SaveAsPrefabAsset(eBullet, "Assets/SurvivalShooter/Prefabs/Projectiles/EnemyProjectile.prefab");
            UnityEngine.Object.DestroyImmediate(eBullet);
        }

        private static void CreateCustomPlaneTrackerPrefab()
        {
            Material planeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SurvivalShooter/Materials/CustomPlane_ChibuezeVictorIfegwu.mat");

            GameObject planeGo = new GameObject("CustomPlaneTracker");
            planeGo.tag = "ARPlane";
            planeGo.AddComponent<ARPlane>();
            planeGo.AddComponent<MeshFilter>();
            var mr = planeGo.AddComponent<MeshRenderer>();
            mr.material = planeMat;
            planeGo.AddComponent<MeshCollider>();
            planeGo.AddComponent<ARPlaneMeshVisualizer>();

            var customVis = planeGo.AddComponent<CustomPlaneVisualizer>();

            var so = new SerializedObject(customVis);
            so.FindProperty("customPlaneMaterial").objectReferenceValue = planeMat;
            so.FindProperty("studentName").stringValue = "Chibueze Victor Ifegwu";
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(planeGo, "Assets/SurvivalShooter/Prefabs/AR/CustomPlaneTracker.prefab");
            UnityEngine.Object.DestroyImmediate(planeGo);
        }

        private static void CreatePlacementIndicatorPrefab()
        {
            Material reticleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SurvivalShooter/Materials/PlacementReticle.mat");

            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            indicator.name = "PlacementIndicator";
            indicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            indicator.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            indicator.GetComponent<MeshRenderer>().material = reticleMat;
            UnityEngine.Object.DestroyImmediate(indicator.GetComponent<Collider>());

            PrefabUtility.SaveAsPrefabAsset(indicator, "Assets/SurvivalShooter/Prefabs/AR/PlacementIndicator.prefab");
            UnityEngine.Object.DestroyImmediate(indicator);
        }

        private static void CreateEnemyPrefabs()
        {
            // 1. Melee Zombie Prefab
            GameObject zombieFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ARGameAnimation/Scary Zombie Pack/copzombie_l_actisdato.fbx");
            RuntimeAnimatorController zombieCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/SurvivalShooter/Animations/ZombieController.controller");

            if (zombieFbx != null)
            {
                GameObject zombie = PrefabUtility.InstantiatePrefab(zombieFbx) as GameObject;
                zombie.name = "MeleeZombie";

                var col = zombie.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0, 0.9f, 0);
                col.radius = 0.35f;
                col.height = 1.8f;

                var anim = zombie.GetComponent<Animator>();
                if (anim != null) anim.runtimeAnimatorController = zombieCtrl;

                var melee = zombie.AddComponent<MeleeEnemy>();

                var so = new SerializedObject(melee);
                so.FindProperty("maxHealth").intValue = 20;
                so.FindProperty("moveSpeed").floatValue = 1.3f;
                so.FindProperty("attackRange").floatValue = 1.2f;
                so.FindProperty("attackCooldown").floatValue = 1.4f;
                so.FindProperty("attackDamage").intValue = 15;
                so.FindProperty("scoreValue").intValue = 100;
                so.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(zombie, "Assets/SurvivalShooter/Prefabs/Enemies/MeleeZombie.prefab");
                UnityEngine.Object.DestroyImmediate(zombie);
            }

            // 2. Shooter Soldier Prefab
            GameObject soldierFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LowPolySoldiers_demo/Soldier_demo_Prefab.prefab");
            RuntimeAnimatorController soldierCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/SurvivalShooter/Animations/SoldierController.controller");

            if (soldierFbx != null)
            {
                GameObject soldier = PrefabUtility.InstantiatePrefab(soldierFbx) as GameObject;
                soldier.name = "ShooterSoldier";

                var col = soldier.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0, 0.9f, 0);
                col.radius = 0.35f;
                col.height = 1.8f;

                var anim = soldier.GetComponent<Animator>();
                if (anim != null) anim.runtimeAnimatorController = soldierCtrl;

                GameObject muzzle = new GameObject("MuzzlePoint");
                muzzle.transform.SetParent(soldier.transform);
                muzzle.transform.localPosition = new Vector3(0.18f, 1.15f, 0.65f);

                var shooter = soldier.AddComponent<ShooterEnemy>();

                var so = new SerializedObject(shooter);
                so.FindProperty("maxHealth").intValue = 50;
                so.FindProperty("moveSpeed").floatValue = 1.0f;
                so.FindProperty("attackRange").floatValue = 4.0f;
                so.FindProperty("attackCooldown").floatValue = 2.2f;
                so.FindProperty("attackDamage").intValue = 10;
                so.FindProperty("scoreValue").intValue = 250;
                so.FindProperty("projectilePoolTag").stringValue = "EnemyProjectile";
                so.FindProperty("muzzlePoint").objectReferenceValue = muzzle.transform;
                so.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(soldier, "Assets/SurvivalShooter/Prefabs/Enemies/ShooterSoldier.prefab");
                UnityEngine.Object.DestroyImmediate(soldier);
            }
        }

        private static void SetupSurvivalShooterScene()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

            var xrOrigin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            if (xrOrigin != null)
            {
                var planeManager = xrOrigin.GetComponent<ARPlaneManager>();
                if (planeManager != null)
                {
                    GameObject customPlanePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SurvivalShooter/Prefabs/AR/CustomPlaneTracker.prefab");
                    planeManager.planePrefab = customPlanePrefab;
                    planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
                    EditorUtility.SetDirty(planeManager);
                }

                if (xrOrigin.GetComponent<ARRaycastManager>() == null)
                {
                    xrOrigin.gameObject.AddComponent<ARRaycastManager>();
                }

                var cam = xrOrigin.Camera;
                if (cam != null)
                {
                    if (cam.GetComponent<PlayerHealth>() == null) cam.gameObject.AddComponent<PlayerHealth>();
                    if (cam.GetComponent<PlayerShooter>() == null) cam.gameObject.AddComponent<PlayerShooter>();
                }
            }

            GameObject mgrGo = GameObject.Find("[GameSystems]");
            if (mgrGo == null) mgrGo = new GameObject("[GameSystems]");

            var gm = mgrGo.GetComponent<GameManager>() ?? mgrGo.AddComponent<GameManager>();
            var audioMgr = mgrGo.GetComponent<AudioManager>() ?? mgrGo.AddComponent<AudioManager>();
            var poolMgr = mgrGo.GetComponent<ObjectPoolManager>() ?? mgrGo.AddComponent<ObjectPoolManager>();
            var factory = mgrGo.GetComponent<EnemyFactory>() ?? mgrGo.AddComponent<EnemyFactory>();
            var spawner = mgrGo.GetComponent<EnemySpawner>() ?? mgrGo.AddComponent<EnemySpawner>();
            var placement = mgrGo.GetComponent<ARPlacementManager>() ?? mgrGo.AddComponent<ARPlacementManager>();
            var leaderboard = mgrGo.GetComponent<LeaderboardManager>() ?? mgrGo.AddComponent<LeaderboardManager>();

            AudioClip shootClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Laser Weapons Sound Pack/Free/light_blast_1.wav");
            AudioClip deathClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ZombieHorrorPackageFree/WAV/BodyFall/Foley_BodyFall_001.wav");
            AudioClip spawnClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ZombieHorrorPackageFree/WAV/VO/Zombie01/Zombie001_Idle_A_001.wav");
            AudioClip enemyShootClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Laser Weapons Sound Pack/Free/heavy_blast_001.wav");
            AudioClip meleeClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ZombieHorrorPackageFree/WAV/Bite/Zombie_Attack_Bite_001.wav");
            AudioClip hurtClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ZombieHorrorPackageFree/WAV/VO/Zombie01/Zombie001_Hurt_A_001.wav");
            AudioClip enemyDeathClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ZombieHorrorPackageFree/WAV/Impact/Impact_Flesh_001.wav");
            AudioClip buttonClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/DemoSceneAssets/Audio/Button Pop.wav");

            audioMgr.ConfigureClips(shootClip, deathClip, spawnClip, enemyShootClip, meleeClip, hurtClip, enemyDeathClip, buttonClip);

            GameObject playerProjPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SurvivalShooter/Prefabs/Projectiles/PlayerProjectile.prefab");
            GameObject enemyProjPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SurvivalShooter/Prefabs/Projectiles/EnemyProjectile.prefab");

            poolMgr.ConfigureDefaultPools(playerProjPrefab, enemyProjPrefab);

            GameObject meleePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SurvivalShooter/Prefabs/Enemies/MeleeZombie.prefab");
            GameObject shooterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SurvivalShooter/Prefabs/Enemies/ShooterSoldier.prefab");
            factory.SetPrefabs(meleePrefab, shooterPrefab);

            GameObject reticlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SurvivalShooter/Prefabs/AR/PlacementIndicator.prefab");
            placement.ConfigurePrefabs(reticlePrefab, null);

            BuildCanvasUI(mgrGo);

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("[SurvivalShooterBuilder] Scene setup saved successfully.");
        }

        private static void BuildCanvasUI(GameObject managersGo)
        {
            GameObject canvasGo = GameObject.Find("SurvivalShooterCanvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("SurvivalShooterCanvas");
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            var uiMgr = canvasGo.GetComponent<UIManager>() ?? canvasGo.AddComponent<UIManager>();

            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            GameObject vignetteGo = FindOrCreateChild(canvasGo, "DamageVignette");
            var vignetteImg = vignetteGo.GetComponent<Image>() ?? vignetteGo.AddComponent<Image>();
            Sprite vigSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SurvivalShooter/Textures/DamageVignette.png");
            if (vigSprite != null) vignetteImg.sprite = vigSprite;
            vignetteImg.color = new Color(1f, 0f, 0f, 0.9f);
            vignetteImg.raycastTarget = false;
            StretchFull(vignetteGo.GetComponent<RectTransform>());
            vignetteImg.color = new Color(1f, 0f, 0f, 0f);

            // 1. START MENU PANEL
            GameObject startPanel = FindOrCreateChild(canvasGo, "StartMenuPanel");
            StretchFull(startPanel.GetComponent<RectTransform>());
            var startBg = startPanel.GetComponent<Image>() ?? startPanel.AddComponent<Image>();
            startBg.color = new Color(0.04f, 0.08f, 0.14f, 0.92f);

            var titleText = CreateText(startPanel, "Title", "SURVIVAL SHOOTER AR", 64, TextAlignmentOptions.Center, new Vector2(0, 500), new Vector2(900, 120), Color.cyan);
            var subText = CreateText(startPanel, "Subtitle", "COMBAT ZONE TRACKER // CHIBUEZE VICTOR IFEGWU", 30, TextAlignmentOptions.Center, new Vector2(0, 410), new Vector2(950, 70), Color.white);
            var planeStatus = CreateText(startPanel, "PlaneStatus", "SCANNING HORIZONTAL AR PLANES...\nAIM DEVICE AT FLOOR", 32, TextAlignmentOptions.Center, new Vector2(0, 260), new Vector2(900, 120), new Color(1f, 0.85f, 0.2f));

            var diffLabel = CreateText(startPanel, "DiffLabel", "SELECT DIFFICULTY:", 28, TextAlignmentOptions.Center, new Vector2(0, 100), new Vector2(600, 50), Color.gray);
            var btnNormal = CreateButton(startPanel, "Btn_Normal", "CADET (NORMAL)", new Vector2(-180, 20), new Vector2(320, 80), new Color(0f, 0.8f, 1f));
            var btnHard = CreateButton(startPanel, "Btn_Hard", "VETERAN (HARD)", new Vector2(180, 20), new Vector2(320, 80), new Color(0.7f, 0.7f, 0.7f));

            var btnStart = CreateButton(startPanel, "Btn_Start", "ESTABLISH COMBAT ZONE", new Vector2(0, -180), new Vector2(650, 110), new Color(0.1f, 0.9f, 0.4f));
            var btnLeaderboard = CreateButton(startPanel, "Btn_Leaderboard", "VIEW LEADERBOARD", new Vector2(0, -320), new Vector2(500, 85), new Color(0.2f, 0.6f, 1f));

            // 2. IN-GAME HUD PANEL
            GameObject inGamePanel = FindOrCreateChild(canvasGo, "InGamePanel");
            StretchFull(inGamePanel.GetComponent<RectTransform>());
            inGamePanel.SetActive(false);

            GameObject healthGo = FindOrCreateChild(inGamePanel, "HealthBar");
            var hRect = healthGo.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.5f, 1f);
            hRect.anchorMax = new Vector2(0.5f, 1f);
            hRect.anchoredPosition = new Vector2(-200, -80);
            hRect.sizeDelta = new Vector2(400, 45);

            var slider = healthGo.GetComponent<Slider>() ?? healthGo.AddComponent<Slider>();
            var hBg = healthGo.GetComponent<Image>() ?? healthGo.AddComponent<Image>();
            hBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            GameObject fillGo = FindOrCreateChild(healthGo, "Fill");
            var fRect = fillGo.GetComponent<RectTransform>();
            StretchFull(fRect);
            var fImg = fillGo.GetComponent<Image>() ?? fillGo.AddComponent<Image>();
            fImg.color = new Color(0.1f, 0.95f, 0.3f, 0.95f);
            slider.fillRect = fRect;

            var hpText = CreateText(healthGo, "HPText", "HP: 100 / 100", 24, TextAlignmentOptions.Center, Vector2.zero, new Vector2(300, 40), Color.white);

            var scoreTxt = CreateText(inGamePanel, "ScoreText", "SCORE: 0", 38, TextAlignmentOptions.TopRight, new Vector2(460, -60), new Vector2(350, 70), Color.yellow);
            scoreTxt.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            scoreTxt.rectTransform.anchorMax = new Vector2(0.5f, 1f);

            var timeTxt = CreateText(inGamePanel, "TimeText", "TIME: 01:30", 38, TextAlignmentOptions.Center, new Vector2(0, -140), new Vector2(350, 70), Color.cyan);
            timeTxt.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            timeTxt.rectTransform.anchorMax = new Vector2(0.5f, 1f);

            GameObject crosshairGo = FindOrCreateChild(inGamePanel, "Crosshair");
            var cImg = crosshairGo.GetComponent<Image>() ?? crosshairGo.AddComponent<Image>();
            Sprite crosshairSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SurvivalShooter/Textures/Crosshair.png");
            if (crosshairSprite != null) cImg.sprite = crosshairSprite;
            cImg.color = new Color(0f, 1f, 0.9f, 0.85f);
            cImg.raycastTarget = false;
            var cRect = crosshairGo.GetComponent<RectTransform>();
            cRect.anchoredPosition = Vector2.zero;
            cRect.sizeDelta = new Vector2(100, 100);

            var fireBtn = CreateButton(inGamePanel, "Btn_Fire", "FIRE", new Vector2(340, -680), new Vector2(240, 240), new Color(1f, 0.2f, 0.25f, 0.85f));
            fireBtn.image.type = Image.Type.Simple;

            // 3. END GAME PANEL
            GameObject endPanel = FindOrCreateChild(canvasGo, "EndGamePanel");
            StretchFull(endPanel.GetComponent<RectTransform>());
            var endBg = endPanel.GetComponent<Image>() ?? endPanel.AddComponent<Image>();
            endBg.color = new Color(0.05f, 0.05f, 0.12f, 0.95f);
            endPanel.SetActive(false);

            var endTitle = CreateText(endPanel, "EndTitle", "MISSION COMPLETE", 64, TextAlignmentOptions.Center, new Vector2(0, 520), new Vector2(900, 100), Color.green);
            var finalScore = CreateText(endPanel, "FinalScore", "FINAL SCORE: 0", 42, TextAlignmentOptions.Center, new Vector2(0, 360), new Vector2(700, 70), Color.yellow);
            var enemiesKilled = CreateText(endPanel, "EnemiesKilled", "ENEMIES ELIMINATED: 0", 34, TextAlignmentOptions.Center, new Vector2(0, 270), new Vector2(700, 60), Color.white);
            var timeSurv = CreateText(endPanel, "TimeSurvived", "TIME SURVIVED: 00:00", 34, TextAlignmentOptions.Center, new Vector2(0, 190), new Vector2(700, 60), Color.cyan);
            var diffPlayed = CreateText(endPanel, "DiffPlayed", "DIFFICULTY: NORMAL", 30, TextAlignmentOptions.Center, new Vector2(0, 110), new Vector2(700, 60), Color.gray);

            var btnRestart = CreateButton(endPanel, "Btn_Restart", "PLAY AGAIN", new Vector2(0, -80), new Vector2(520, 95), new Color(0.1f, 0.9f, 0.4f));
            var btnMainMenu = CreateButton(endPanel, "Btn_MainMenu", "MAIN MENU", new Vector2(0, -210), new Vector2(520, 95), new Color(0.2f, 0.6f, 1f));
            var btnEndLeaderboard = CreateButton(endPanel, "Btn_EndLeaderboard", "VIEW LEADERBOARD", new Vector2(0, -340), new Vector2(520, 95), new Color(0.8f, 0.5f, 0.1f));

            // 4. LEADERBOARD PANEL
            GameObject leadPanel = FindOrCreateChild(canvasGo, "LeaderboardPanel");
            StretchFull(leadPanel.GetComponent<RectTransform>());
            var leadBg = leadPanel.GetComponent<Image>() ?? leadPanel.AddComponent<Image>();
            leadBg.color = new Color(0.02f, 0.05f, 0.1f, 0.98f);
            leadPanel.SetActive(false);

            CreateText(leadPanel, "LeadTitle", "COMBAT RECORD - TOP 5 SESSIONS", 48, TextAlignmentOptions.Center, new Vector2(0, 520), new Vector2(950, 90), Color.cyan);
            var rawText = CreateText(leadPanel, "RawRecordText", "Loading Sessions...", 26, TextAlignmentOptions.TopLeft, new Vector2(0, 60), new Vector2(850, 700), Color.white);

            var btnCloseLead = CreateButton(leadPanel, "Btn_CloseLeaderboard", "CLOSE", new Vector2(0, -480), new Vector2(400, 85), new Color(0.9f, 0.25f, 0.25f));

            uiMgr.ConfigureUIPanels(
                startPanel, inGamePanel, endPanel, leadPanel,
                btnStart, btnLeaderboard, btnNormal, btnHard, planeStatus,
                slider, hpText, scoreTxt, timeTxt,
                fireBtn, vignetteImg,
                endTitle, finalScore, enemiesKilled,
                timeSurv, diffPlayed, btnRestart, btnMainMenu,
                btnEndLeaderboard, rawText, btnCloseLead
            );
            EditorUtility.SetDirty(uiMgr);
        }

        private static GameObject FindOrCreateChild(GameObject parent, string name)
        {
            Transform t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private static TextMeshProUGUI CreateText(GameObject parent, string name, string text, float size, TextAlignmentOptions align, Vector2 pos, Vector2 sizeDelta, Color color)
        {
            GameObject go = FindOrCreateChild(parent, name);
            var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = color;
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = sizeDelta;
            return tmp;
        }

        private static Button CreateButton(GameObject parent, string name, string label, Vector2 pos, Vector2 sizeDelta, Color bgColor)
        {
            GameObject go = FindOrCreateChild(parent, name);
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = sizeDelta;

            GameObject lblGo = FindOrCreateChild(go, "Label");
            var tmp = lblGo.GetComponent<TextMeshProUGUI>() ?? lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            StretchFull(lblGo.GetComponent<RectTransform>());

            return btn;
        }

        [MenuItem("Survival Shooter AR/Build Android APK", priority = 2)]
        public static void BuildAndroidAPK()
        {
            string buildDir = "Builds";
            if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);
            string apkPath = Path.Combine(buildDir, "SurvivalShooterAR.apk");

            PlayerSettings.companyName = "ChibuezeVictorIfegwu";
            PlayerSettings.productName = "Survival Shooter AR";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.chibueze.survivalshooterar");

            string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log("[SurvivalShooterBuilder] Starting Android APK build...");
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[SurvivalShooterBuilder] Android Build Result: {report.summary.result}, Output: {apkPath}");
        }
    }
}