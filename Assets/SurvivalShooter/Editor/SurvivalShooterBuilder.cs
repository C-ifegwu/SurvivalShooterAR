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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
        private const string BUILD_KEY = "SurvivalShooter_Build_Executed_v6";

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

                Debug.Log("<color=green><b>[SurvivalShooterBuilder] Full Survival Shooter AR Setup with DOTween and Audio Completed Successfully!</b></color>");
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
                "Assets/SurvivalShooter/Scenes",
                "Assets/SurvivalShooter/PostProcessing"
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

            if (Directory.Exists("Assets/SurvivalShooter/Textures/UI"))
            {
                string[] uiFiles = Directory.GetFiles("Assets/SurvivalShooter/Textures/UI", "*.png", SearchOption.AllDirectories);
                foreach (var file in uiFiles)
                {
                    string unityPath = file.Replace('\\', '/');
                    TextureImporter importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
                    if (importer != null && importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.alphaIsTransparency = true;
                        importer.mipmapEnabled = false;

                        if (unityPath.Contains("Status_") || unityPath.Contains("Progress_Bar_") || unityPath.Contains("Frame_"))
                        {
                            importer.spriteBorder = new Vector4(16, 16, 16, 16);
                        }

                        importer.SaveAndReimport();
                    }
                }
            }
        }

        private static void CreateMaterials()
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");

            Texture2D planeTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SurvivalShooter/Textures/CustomPlane_ChibuezeVictorIfegwu.png");
            Material planeMat = new Material(unlitShader);
            planeMat.name = "CustomPlane_ChibuezeVictorIfegwu";
            if (planeTex != null)
            {
                planeMat.mainTexture = planeTex;
                planeMat.SetTexture("_BaseMap", planeTex);
            }
            planeMat.SetFloat("_Surface", 1);
            planeMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            planeMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            planeMat.SetInt("_ZWrite", 0);
            planeMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            AssetDatabase.CreateAsset(planeMat, "Assets/SurvivalShooter/Materials/CustomPlane_ChibuezeVictorIfegwu.mat");

            Texture2D reticleTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SurvivalShooter/Textures/PlacementReticle.png");
            Material reticleMat = new Material(unlitShader);
            reticleMat.name = "PlacementReticle";
            if (reticleTex != null)
            {
                reticleMat.mainTexture = reticleTex;
                reticleMat.SetTexture("_BaseMap", reticleTex);
            }
            reticleMat.color = new Color(0f, 1f, 0.85f, 0.9f);
            reticleMat.SetFloat("_Surface", 1);
            reticleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            reticleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            reticleMat.SetInt("_ZWrite", 0);
            reticleMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            AssetDatabase.CreateAsset(reticleMat, "Assets/SurvivalShooter/Materials/PlacementReticle.mat");

            Material playerMat = new Material(unlitShader);
            playerMat.name = "PlayerProjectileMat";
            playerMat.color = new Color(0f, 1f, 1f, 1f);
            playerMat.EnableKeyword("_EMISSION");
            playerMat.SetColor("_EmissionColor", new Color(0f, 2.5f, 2.5f) * 2f);
            AssetDatabase.CreateAsset(playerMat, "Assets/SurvivalShooter/Materials/PlayerProjectileMat.mat");

            Material enemyMat = new Material(unlitShader);
            enemyMat.name = "EnemyProjectileMat";
            enemyMat.color = new Color(1f, 0.15f, 0.15f, 1f);
            enemyMat.EnableKeyword("_EMISSION");
            enemyMat.SetColor("_EmissionColor", new Color(2.5f, 0.2f, 0.2f) * 2f);
            AssetDatabase.CreateAsset(enemyMat, "Assets/SurvivalShooter/Materials/EnemyProjectileMat.mat");
        }
        private static void CreateAnimationControllers()
        {
            var zCtrl = AnimatorController.CreateAnimatorControllerAtPath("Assets/SurvivalShooter/Animations/ZombieController.controller");
            AnimationClip zIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ARGameAnimation/Scary Zombie Pack/Animations/Zombie_Idle.fbx");
            AnimationClip zWalk = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ARGameAnimation/Scary Zombie Pack/Animations/Zombie_Walk.fbx");
            AnimationClip zAttack = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ARGameAnimation/Scary Zombie Pack/Animations/Zombie_Attack.fbx");
            AnimationClip zDeath = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ARGameAnimation/Scary Zombie Pack/Animations/Zombie_Death.fbx");

            zCtrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            zCtrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            zCtrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            var rootSm = zCtrl.layers[0].stateMachine;
            var stateIdle = rootSm.AddState("Idle");
            var stateWalk = rootSm.AddState("Walk");
            var stateAttack = rootSm.AddState("Attack");
            var stateDeath = rootSm.AddState("Death");

            if (zIdle != null) stateIdle.motion = zIdle;
            if (zWalk != null) stateWalk.motion = zWalk;
            if (zAttack != null) stateAttack.motion = zAttack;
            if (zDeath != null) stateDeath.motion = zDeath;

            var t1 = stateIdle.AddTransition(stateWalk);
            t1.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t1.hasExitTime = false;

            var t2 = stateWalk.AddTransition(stateIdle);
            t2.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            t2.hasExitTime = false;

            var t3 = rootSm.AddAnyStateTransition(stateAttack);
            t3.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            t3.hasExitTime = false;
            var t4 = stateAttack.AddTransition(stateIdle);
            t4.hasExitTime = true;
            t4.exitTime = 0.85f;

            var t5 = rootSm.AddAnyStateTransition(stateDeath);
            t5.AddCondition(AnimatorConditionMode.If, 0, "Die");
            t5.hasExitTime = false;

            var sCtrl = AnimatorController.CreateAnimatorControllerAtPath("Assets/SurvivalShooter/Animations/SoldierController.controller");
            AnimationClip sShoot = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/LowPolySoldiers_demo/Animation/Shoot.anim");

            sCtrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            sCtrl.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            sCtrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            var sSm = sCtrl.layers[0].stateMachine;
            var sIdle = sSm.AddState("Idle");
            var sShootState = sSm.AddState("Shoot");
            var sDieState = sSm.AddState("Death");

            if (sShoot != null) sShootState.motion = sShoot;

            var st1 = sSm.AddAnyStateTransition(sShootState);
            st1.AddCondition(AnimatorConditionMode.If, 0, "Shoot");
            st1.hasExitTime = false;
            var st2 = sShootState.AddTransition(sIdle);
            st2.hasExitTime = true;
            st2.exitTime = 0.75f;

            var st3 = sSm.AddAnyStateTransition(sDieState);
            st3.AddCondition(AnimatorConditionMode.If, 0, "Die");
            st3.hasExitTime = false;
        }

        private static void CreateProjectilePrefabs()
        {
            Material playerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SurvivalShooter/Materials/PlayerProjectileMat.mat");
            Material enemyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SurvivalShooter/Materials/EnemyProjectileMat.mat");

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

            SetupLightingAndPostProcessing();

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

                    var uCam = cam.GetComponent<UniversalAdditionalCameraData>();
                    if (uCam != null)
                    {
                        uCam.renderPostProcessing = true;
                    }
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

            AudioClip buttonClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SurvivalShooter/Audio/UI/Click Button SFX.wav");
            AudioClip hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SurvivalShooter/Audio/UI/Hover Button SFX.wav");
            AudioClip menuBgm = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SurvivalShooter/Audio/Music/Ambient 1.wav");
            AudioClip combatBgm = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SurvivalShooter/Audio/Music/Action 1 (Loop).wav");
            AudioClip victory = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SurvivalShooter/Audio/Music/Victory.wav");
            AudioClip defeat = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SurvivalShooter/Audio/Music/Death.wav");
            AudioClip whoosh = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SurvivalShooter/Audio/UI/SFX_Click_Whoosh.mp3");
            AudioClip punch = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SurvivalShooter/Audio/UI/SFX_Click_Punch.ogg");
            AudioClip mechanical = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SurvivalShooter/Audio/UI/SFX_Click_Mechanical.mp3");

            audioMgr.ConfigureClips(
                shootClip, deathClip, spawnClip, enemyShootClip, meleeClip, hurtClip, enemyDeathClip,
                buttonClip, hoverClip, menuBgm, combatBgm, victory, defeat, whoosh, punch, mechanical
            );

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
            Debug.Log("[SurvivalShooterBuilder] Scene setup saved successfully with full polish.");
        }

        private static void SetupLightingAndPostProcessing()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.46f, 0.58f);
            RenderSettings.ambientEquatorColor = new Color(0.24f, 0.28f, 0.34f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.11f, 0.10f);

            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional) { sun = l; break; }
                }
            }
            if (sun != null)
            {
                sun.color = new Color(1.0f, 0.96f, 0.90f);
                sun.intensity = 1.35f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.82f;
                EditorUtility.SetDirty(sun);
            }

            GameObject volumeGo = GameObject.Find("Global PostProcessing Volume");
            if (volumeGo == null) volumeGo = new GameObject("Global PostProcessing Volume");
            var volume = volumeGo.GetComponent<Volume>() ?? volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;

            string profilePath = "Assets/SurvivalShooter/PostProcessing/PostProcessProfile.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            if (!profile.Has<Bloom>())
            {
                var bloom = profile.Add<Bloom>();
                bloom.intensity.Override(1.25f);
                bloom.threshold.Override(0.9f);
                bloom.scatter.Override(0.7f);
            }
            if (!profile.Has<Tonemapping>())
            {
                var tonemap = profile.Add<Tonemapping>();
                tonemap.mode.Override(TonemappingMode.ACES);
            }
            if (!profile.Has<Vignette>())
            {
                var vig = profile.Add<Vignette>();
                vig.intensity.Override(0.24f);
                vig.smoothness.Override(0.4f);
            }
            if (!profile.Has<ColorAdjustments>())
            {
                var colorAdj = profile.Add<ColorAdjustments>();
                colorAdj.postExposure.Override(0.15f);
                colorAdj.contrast.Override(15f);
                colorAdj.saturation.Override(12f);
            }

            EditorUtility.SetDirty(profile);
            volume.profile = profile;
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

            Sprite btnRedDefault = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SurvivalShooter/Textures/UI/Status_Red_Default.png");
            Sprite btnGreyDefault = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SurvivalShooter/Textures/UI/Status_Grey_Default.png");
            Sprite frameBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SurvivalShooter/Textures/UI/Frame_background.png");
            Sprite hpEmpty = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SurvivalShooter/Textures/UI/Progress_Bar_Rectangle_empty_v1.png");
            Sprite hpFull = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SurvivalShooter/Textures/UI/Progress_Bar_Rectangle_full_v1.png");

            GameObject vignetteGo = FindOrCreateChild(canvasGo, "DamageVignette");
            var vignetteImg = vignetteGo.GetComponent<Image>() ?? vignetteGo.AddComponent<Image>();
            Sprite vigSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SurvivalShooter/Textures/DamageVignette.png");
            if (vigSprite != null) vignetteImg.sprite = vigSprite;
            vignetteImg.color = new Color(1f, 0f, 0f, 0f);
            vignetteImg.raycastTarget = false;
            StretchFull(vignetteGo.GetComponent<RectTransform>());

            // 1. START MENU PANEL
            GameObject startPanel = FindOrCreateChild(canvasGo, "StartMenuPanel");
            StretchFull(startPanel.GetComponent<RectTransform>());
            var startBg = startPanel.GetComponent<Image>() ?? startPanel.AddComponent<Image>();
            if (frameBg != null) { startBg.sprite = frameBg; startBg.type = Image.Type.Sliced; }
            startBg.color = new Color(0.04f, 0.08f, 0.16f, 0.94f);

            var titleText = CreateText(startPanel, "Title", "SURVIVAL SHOOTER AR", 64, TextAlignmentOptions.Center, new Vector2(0, 520), new Vector2(950, 120), new Color(0f, 0.95f, 1f));
            var subText = CreateText(startPanel, "Subtitle", "COMBAT ZONE TRACKER // CHIBUEZE VICTOR IFEGWU", 30, TextAlignmentOptions.Center, new Vector2(0, 420), new Vector2(950, 70), new Color(0.8f, 0.85f, 0.95f));
            var planeStatus = CreateText(startPanel, "PlaneStatus", "SCANNING HORIZONTAL AR PLANES...\nAIM DEVICE AT FLOOR", 32, TextAlignmentOptions.Center, new Vector2(0, 260), new Vector2(900, 120), new Color(1f, 0.85f, 0.2f));

            CreateText(startPanel, "DiffLabel", "SELECT DIFFICULTY", 28, TextAlignmentOptions.Center, new Vector2(0, 70), new Vector2(500, 50), Color.white);
            var btnNormal = CreateButton(startPanel, "Btn_DiffNormal", "NORMAL", new Vector2(-190, -10), new Vector2(340, 85), new Color(0f, 0.9f, 1f), btnRedDefault);
            var btnHard = CreateButton(startPanel, "Btn_DiffHard", "HARD", new Vector2(190, -10), new Vector2(340, 85), new Color(0.55f, 0.55f, 0.6f), btnGreyDefault);

            var btnStart = CreateButton(startPanel, "Btn_Start", "START COMBAT", new Vector2(0, -160), new Vector2(620, 115), new Color(0.1f, 0.95f, 0.45f), btnRedDefault);
            var btnLeaderboard = CreateButton(startPanel, "Btn_Leaderboard", "VIEW LEADERBOARD", new Vector2(0, -310), new Vector2(520, 95), new Color(0.95f, 0.65f, 0.1f), btnGreyDefault);

            // 2. IN-GAME HUD PANEL
            GameObject inGamePanel = FindOrCreateChild(canvasGo, "InGamePanel");
            StretchFull(inGamePanel.GetComponent<RectTransform>());
            inGamePanel.SetActive(false);

            GameObject healthGo = FindOrCreateChild(inGamePanel, "HealthSlider");
            var hRect = healthGo.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 1f);
            hRect.anchorMax = new Vector2(0f, 1f);
            hRect.anchoredPosition = new Vector2(340, -80);
            hRect.sizeDelta = new Vector2(550, 50);

            var slider = healthGo.GetComponent<Slider>() ?? healthGo.AddComponent<Slider>();
            var hBg = healthGo.GetComponent<Image>() ?? healthGo.AddComponent<Image>();
            if (hpEmpty != null) { hBg.sprite = hpEmpty; hBg.type = Image.Type.Sliced; }
            hBg.color = new Color(0.2f, 0.2f, 0.25f, 0.85f);

            GameObject fillGo = FindOrCreateChild(healthGo, "Fill");
            var fRect = fillGo.GetComponent<RectTransform>();
            StretchFull(fRect);
            var fImg = fillGo.GetComponent<Image>() ?? fillGo.AddComponent<Image>();
            if (hpFull != null) { fImg.sprite = hpFull; fImg.type = Image.Type.Sliced; }
            fImg.color = new Color(0f, 1f, 0.55f, 0.95f);
            slider.fillRect = fRect;

            var hpText = CreateText(healthGo, "HPText", "HP: 100 / 100", 24, TextAlignmentOptions.Center, Vector2.zero, new Vector2(350, 45), Color.white);

            var scoreTxt = CreateText(inGamePanel, "ScoreText", "SCORE: 0", 38, TextAlignmentOptions.TopRight, new Vector2(460, -60), new Vector2(350, 70), new Color(1f, 0.85f, 0.1f));
            scoreTxt.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            scoreTxt.rectTransform.anchorMax = new Vector2(0.5f, 1f);

            var timeTxt = CreateText(inGamePanel, "TimeText", "TIME: 01:30", 38, TextAlignmentOptions.Center, new Vector2(0, -140), new Vector2(350, 70), new Color(0f, 0.95f, 1f));
            timeTxt.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            timeTxt.rectTransform.anchorMax = new Vector2(0.5f, 1f);

            GameObject crosshairGo = FindOrCreateChild(inGamePanel, "Crosshair");
            var cImg = crosshairGo.GetComponent<Image>() ?? crosshairGo.AddComponent<Image>();
            Sprite crosshairSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SurvivalShooter/Textures/Crosshair.png");
            if (crosshairSprite != null) cImg.sprite = crosshairSprite;
            cImg.color = new Color(0f, 1f, 0.95f, 0.9f);
            cImg.raycastTarget = false;
            var cRect = crosshairGo.GetComponent<RectTransform>();
            cRect.anchoredPosition = Vector2.zero;
            cRect.sizeDelta = new Vector2(100, 100);

            var fireBtn = CreateButton(inGamePanel, "Btn_Fire", "FIRE", new Vector2(340, -680), new Vector2(250, 250), new Color(1f, 0.2f, 0.25f, 0.9f), btnRedDefault);

            // 3. END GAME PANEL
            GameObject endPanel = FindOrCreateChild(canvasGo, "EndGamePanel");
            StretchFull(endPanel.GetComponent<RectTransform>());
            var endBg = endPanel.GetComponent<Image>() ?? endPanel.AddComponent<Image>();
            if (frameBg != null) { endBg.sprite = frameBg; endBg.type = Image.Type.Sliced; }
            endBg.color = new Color(0.04f, 0.06f, 0.14f, 0.96f);
            endPanel.SetActive(false);

            var endTitle = CreateText(endPanel, "EndTitle", "MISSION COMPLETE", 64, TextAlignmentOptions.Center, new Vector2(0, 520), new Vector2(900, 100), new Color(0.2f, 1f, 0.4f));
            var finalScore = CreateText(endPanel, "FinalScore", "FINAL SCORE: 0", 42, TextAlignmentOptions.Center, new Vector2(0, 360), new Vector2(700, 70), new Color(1f, 0.85f, 0.1f));
            var enemiesKilled = CreateText(endPanel, "EnemiesKilled", "ENEMIES ELIMINATED: 0", 34, TextAlignmentOptions.Center, new Vector2(0, 270), new Vector2(700, 60), Color.white);
            var timeSurv = CreateText(endPanel, "TimeSurvived", "TIME SURVIVED: 00:00", 34, TextAlignmentOptions.Center, new Vector2(0, 190), new Vector2(700, 60), new Color(0f, 0.95f, 1f));
            var diffPlayed = CreateText(endPanel, "DiffPlayed", "DIFFICULTY: NORMAL", 30, TextAlignmentOptions.Center, new Vector2(0, 110), new Vector2(700, 60), Color.gray);

            var btnRestart = CreateButton(endPanel, "Btn_Restart", "PLAY AGAIN", new Vector2(0, -80), new Vector2(540, 100), new Color(0.1f, 0.95f, 0.45f), btnRedDefault);
            var btnMainMenu = CreateButton(endPanel, "Btn_MainMenu", "MAIN MENU", new Vector2(0, -210), new Vector2(540, 100), new Color(0.2f, 0.65f, 1f), btnGreyDefault);
            var btnEndLeaderboard = CreateButton(endPanel, "Btn_EndLeaderboard", "VIEW LEADERBOARD", new Vector2(0, -340), new Vector2(540, 100), new Color(0.95f, 0.65f, 0.1f), btnGreyDefault);

            // 4. LEADERBOARD PANEL
            GameObject leadPanel = FindOrCreateChild(canvasGo, "LeaderboardPanel");
            StretchFull(leadPanel.GetComponent<RectTransform>());
            var leadBg = leadPanel.GetComponent<Image>() ?? leadPanel.AddComponent<Image>();
            if (frameBg != null) { leadBg.sprite = frameBg; leadBg.type = Image.Type.Sliced; }
            leadBg.color = new Color(0.02f, 0.05f, 0.12f, 0.98f);
            leadPanel.SetActive(false);

            CreateText(leadPanel, "LeadTitle", "COMBAT RECORD - TOP 5 SESSIONS", 48, TextAlignmentOptions.Center, new Vector2(0, 520), new Vector2(950, 90), new Color(0f, 0.95f, 1f));
            var rawText = CreateText(leadPanel, "RawRecordText", "Loading Sessions...", 26, TextAlignmentOptions.TopLeft, new Vector2(0, 60), new Vector2(850, 700), Color.white);

            var btnCloseLead = CreateButton(leadPanel, "Btn_CloseLeaderboard", "CLOSE", new Vector2(0, -480), new Vector2(420, 90), new Color(0.95f, 0.25f, 0.25f), btnRedDefault);

            uiMgr.ConfigureUIPanels(
                startPanel, inGamePanel, endPanel, leadPanel,
                btnStart, btnLeaderboard, btnNormal, btnHard, planeStatus,
                slider, hpText, scoreTxt, timeTxt,
                fireBtn, vignetteImg,
                endTitle, finalScore, enemiesKilled,
                timeSurv, diffPlayed, btnRestart, btnMainMenu,
                btnEndLeaderboard, rawText, btnCloseLead,
                titleText
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
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = align;
            tmp.color = color;
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = sizeDelta;
            return tmp;
        }

        private static Button CreateButton(GameObject parent, string name, string label, Vector2 pos, Vector2 sizeDelta, Color bgColor, Sprite btnSprite = null)
        {
            GameObject go = FindOrCreateChild(parent, name);
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            if (btnSprite != null)
            {
                img.sprite = btnSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = bgColor;
            var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = sizeDelta;

            GameObject lblGo = FindOrCreateChild(go, "Label");
            var tmp = lblGo.GetComponent<TextMeshProUGUI>() ?? lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            StretchFull(lblGo.GetComponent<RectTransform>());

            if (go.GetComponent<UIButtonPolish>() == null)
            {
                go.AddComponent<UIButtonPolish>();
            }

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