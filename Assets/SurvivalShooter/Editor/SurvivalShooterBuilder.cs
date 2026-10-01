using System;
using System.IO;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using SurvivalShooter.AR;
using SurvivalShooter.Audio;
using SurvivalShooter.Core;
using SurvivalShooter.Data;
using SurvivalShooter.Enemies;
using SurvivalShooter.FX;
using SurvivalShooter.Player;
using SurvivalShooter.Pooling;
using static SurvivalShooter.EditorTools.BuilderUtil;

namespace SurvivalShooter.EditorTools
{
    /// <summary>
    /// One-click, repeatable project builder. Generates every material, animator controller,
    /// prefab and the complete AR scene from code so the project is reproducible from the repo.
    /// Menu: Survival Shooter AR ▸ Build Everything
    /// </summary>
    public static class SurvivalShooterBuilder
    {
        public static readonly Color Accent = new Color(0.25f, 0.91f, 1f);
        public static readonly Color Danger = new Color(1f, 0.25f, 0.38f);
        public static readonly Color Amber = new Color(1f, 0.7f, 0.22f);

        private const string ZombieFolder = "Assets/ARGameAnimation/Scary Zombie Pack";
        private const string SoldierFolder = "Assets/LowPolySoldiers_demo";
        private const string LogPath = "Logs/ssar_builder.log";

        [MenuItem("Survival Shooter AR/Build Everything", priority = 1)]
        public static void BuildEverything()
        {
            File.WriteAllText(LogPath, $"BUILD START {DateTime.Now}\n");
            Application.logMessageReceived += LogToFile;
            try
            {
                Step("Project settings", ConfigureProjectSettings);
                Step("Import settings", ConfigureImportSettings);
                Step("Fonts", SurvivalShooterUIBuilder.BuildFonts);
                Step("Materials", BuildMaterials);
                Step("Animators", BuildAnimators);
                Step("Prefabs", BuildPrefabs);
                Step("Scene", BuildScene);
                Log("<b>BUILD COMPLETE</b>");
            }
            catch (Exception e)
            {
                Log("BUILD FAILED: " + e);
                Debug.LogException(e);
            }
            finally
            {
                Application.logMessageReceived -= LogToFile;
                AssetDatabase.SaveAssets();
            }
        }

        private static void Step(string name, Action a)
        {
            Log($"-- {name}");
            a();
            AssetDatabase.SaveAssets();
        }

        public static void Log(string msg)
        {
            Debug.Log("[SSAR Builder] " + msg);
        }

        private static void LogToFile(string condition, string stack, LogType type)
        {
            if (!condition.StartsWith("[SSAR") && type != LogType.Error && type != LogType.Exception) return;
            File.AppendAllText(LogPath, $"{type}: {condition}\n" + (type == LogType.Exception ? stack + "\n" : ""));
        }

        // =================================================================== Settings

        private static void ConfigureProjectSettings()
        {
            PlayerSettings.productName = "Survival Shooter AR";
            PlayerSettings.companyName = "ChibuezeVictorIfegwu";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.chibueze.survivalshooterar");

            // HDR so emissive projectiles glow with bloom
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null) continue;
                asset.supportsHDR = true;
                EditorUtility.SetDirty(asset);
            }
        }

        private static void ConfigureImportSettings()
        {
            // UI sprites & icons
            foreach (string folder in new[] { Art + "/UI", Art + "/Icons" })
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                    Vector4 border = Vector4.zero;
                    string file = Path.GetFileNameWithoutExtension(path);
                    if (file == "UI_RoundRect" || file == "UI_RoundRectOutline") border = new Vector4(40, 40, 40, 40);
                    if (file == "UI_Pill" || file == "UI_PillOutline") border = new Vector4(63, 63, 63, 63);
                    if (file == "UI_RoundRectSmall") border = new Vector4(18, 18, 18, 18);
                    bool changed = ti.textureType != TextureImporterType.Sprite || ti.spriteBorder != border || ti.mipmapEnabled;
                    if (!changed) continue;
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.alphaIsTransparency = true;
                    ti.mipmapEnabled = false;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.spriteBorder = border;
                    ti.SaveAndReimport();
                }
            }

            // World textures
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Art + "/Textures" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                bool plane = path.Contains("CustomPlane");
                TextureWrapMode wrap = plane ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                if (ti.textureType == TextureImporterType.Default && ti.wrapMode == wrap && ti.alphaIsTransparency && ti.mipmapEnabled) continue;
                ti.textureType = TextureImporterType.Default;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = true;
                ti.wrapMode = wrap;
                ti.anisoLevel = plane ? 4 : 1;
                ti.SaveAndReimport();
            }

            // Mixamo zombie: Humanoid rig so all clips retarget, loop locomotion clips
            foreach (string path in Directory.GetFiles(ZombieFolder, "*.fbx").Select(p => p.Replace('\\', '/')))
            {
                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi == null || mi.userData == "ssar_v2") continue;
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                mi.SaveAndReimport();

                mi = (ModelImporter)AssetImporter.GetAtPath(path);
                var clips = mi.defaultClipAnimations;
                string lower = Path.GetFileName(path).ToLowerInvariant();
                bool loop = lower.Contains("walk") || lower.Contains("run") || lower.Contains("idle") || lower.Contains("crawl");
                foreach (var c in clips)
                {
                    c.loopTime = loop;
                    c.loopPose = loop;
                    c.lockRootRotation = true;
                    c.lockRootHeightY = true;
                    c.lockRootPositionXZ = true;
                    c.keepOriginalOrientation = true;
                    c.keepOriginalPositionY = true;
                    c.keepOriginalPositionXZ = true;
                }
                mi.clipAnimations = clips;
                mi.userData = "ssar_v2";
                mi.SaveAndReimport();
                Log("Humanoid rig: " + path);
            }

            // Extract the zombie's embedded textures so we can build proper URP materials
            string texDir = ZombieFolder + "/Textures";
            if (!AssetDatabase.IsValidFolder(texDir))
            {
                EnsureFolder(texDir);
                var zmi = (ModelImporter)AssetImporter.GetAtPath($"{ZombieFolder}/copzombie_l_actisdato.fbx");
                bool ok = zmi.ExtractTextures(texDir);
                AssetDatabase.Refresh();
                Log("Extracted zombie textures: " + ok);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { texDir }))
            {
                string tp = AssetDatabase.GUIDToAssetPath(guid);
                var ti = (TextureImporter)AssetImporter.GetAtPath(tp);
                bool normal = tp.ToLowerInvariant().Contains("normal");
                if (normal && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
                if (!normal && ti.maxTextureSize > 1024) { ti.maxTextureSize = 1024; ti.SaveAndReimport(); }
            }

            // Soldier locomotion loops
            foreach (string name in new[] { "demo_combat_idle", "demo_combat_run" })
            {
                string path = $"{SoldierFolder}/animation/{name}.FBX";
                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi == null || mi.userData == "ssar_v2") continue;
                var clips = mi.clipAnimations.Length > 0 ? mi.clipAnimations : mi.defaultClipAnimations;
                foreach (var c in clips) { c.loopTime = true; c.loopPose = true; }
                mi.clipAnimations = clips;
                mi.userData = "ssar_v2";
                mi.SaveAndReimport();
            }
        }

        // =================================================================== Materials

        private static void BuildMaterials()
        {
            EnsureFolder(Mats);
            var ring = UITex("UI_Ring");
            var glow = Tex("FX_SoftGlow");

            var plane = Transparent("M_CustomPlane", UnlitShader, Tex("CustomPlane_ChibuezeVictorIfegwu"), Color.white, false);
            plane.renderQueue = (int)RenderQueue.Transparent - 10;
            Transparent("M_PlaneBoundary", ParticleShader, null, new Color(0.35f, 0.95f, 1f, 1f), true);
            Transparent("M_Reticle", UnlitShader, Tex("PlacementReticle"), new Color(1f, 1f, 1f, 1f), true);
            Transparent("M_ArenaRing", UnlitShader, Tex("PlacementReticle"), new Color(0.3f, 0.9f, 1f, 0.55f), true);
            Transparent("M_ArenaPulse", UnlitShader, ring, new Color(0.3f, 0.95f, 1f, 0.9f), true);
            Transparent("M_BlobShadow", UnlitShader, Tex("FX_BlobShadow"), new Color(1f, 1f, 1f, 0.85f), false);
            Transparent("M_RingMelee", UnlitShader, ring, new Color(1f, 0.25f, 0.35f, 0.75f), true);
            Transparent("M_RingShooter", UnlitShader, ring, new Color(1f, 0.7f, 0.2f, 0.75f), true);
            Transparent("M_HealthBg", UnlitShader, null, new Color(0.02f, 0.03f, 0.05f, 0.75f), false);
            Transparent("M_HealthLag", UnlitShader, null, new Color(1f, 1f, 1f, 0.85f), false);
            UnlitOpaque("M_HealthMelee", new Color(1f, 0.28f, 0.38f));
            UnlitOpaque("M_HealthShooter", new Color(1f, 0.72f, 0.22f));
            UnlitOpaque("M_ProjectilePlayer", new Color(0.6f, 3.2f, 4.2f));
            UnlitOpaque("M_ProjectileEnemy", new Color(4.5f, 0.6f, 0.8f));
            Transparent("M_TrailPlayer", ParticleShader, glow, new Color(0.35f, 1.1f, 1.4f, 0.9f), true);
            Transparent("M_TrailEnemy", ParticleShader, glow, new Color(1.6f, 0.3f, 0.4f, 1f), true);
            Transparent("M_MuzzleGlow", ParticleShader, glow, new Color(2.4f, 0.7f, 0.45f, 1f), true);
            Transparent("M_FX_Spark", ParticleShader, glow, new Color(1f, 1f, 1f, 0.85f), true);
            Transparent("M_FX_Ring", ParticleShader, ring, Color.white, true);
        }

        private static Material Mat(string name) => Load<Material>($"{Mats}/{name}.mat");

        // =================================================================== Animators

        private static void BuildAnimators()
        {
            EnsureFolder(Anims);

            // ---- Zombie (Melee)
            string zPath = $"{Anims}/AC_Zombie.controller";
            AssetDatabase.DeleteAsset(zPath);
            var z = AnimatorController.CreateAnimatorControllerAtPath(zPath);
            z.AddParameter("Speed", AnimatorControllerParameterType.Float);
            z.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            z.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            var zsm = z.layers[0].stateMachine;
            var zIdle = zsm.AddState("Idle"); zIdle.motion = FirstClip($"{ZombieFolder}/zombie idle.fbx");
            var zWalk = zsm.AddState("Walk"); zWalk.motion = FirstClip($"{ZombieFolder}/zombie walk.fbx"); zWalk.speed = 1.25f;
            var zAtk = zsm.AddState("Attack"); zAtk.motion = FirstClip($"{ZombieFolder}/zombie attack.fbx"); zAtk.speed = 1.3f;
            var zDie = zsm.AddState("Death"); zDie.motion = FirstClip($"{ZombieFolder}/zombie death.fbx"); zDie.speed = 1.2f;
            zsm.defaultState = zIdle;
            Transition(zIdle, zWalk, 0.15f, ("Speed", AnimatorConditionMode.Greater, 0.1f));
            Transition(zWalk, zIdle, 0.15f, ("Speed", AnimatorConditionMode.Less, 0.1f));
            TriggerTransition(zIdle, zAtk, "Attack", 0.08f);
            TriggerTransition(zWalk, zAtk, "Attack", 0.08f);
            var back = zAtk.AddTransition(zIdle); back.hasExitTime = true; back.exitTime = 0.9f; back.duration = 0.15f;
            var toDie = zsm.AddAnyStateTransition(zDie);
            toDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
            toDie.duration = 0.1f; toDie.canTransitionToSelf = false; toDie.hasExitTime = false;
            Log($"Zombie clips: idle={zIdle.motion != null} walk={zWalk.motion != null} attack={zAtk.motion != null} death={zDie.motion != null}");

            // ---- Soldier (Shooter)
            string sPath = $"{Anims}/AC_Soldier.controller";
            AssetDatabase.DeleteAsset(sPath);
            var s = AnimatorController.CreateAnimatorControllerAtPath(sPath);
            s.AddParameter("Speed", AnimatorControllerParameterType.Float);
            s.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            s.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            s.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            var ssm = s.layers[0].stateMachine;
            var sIdle = ssm.AddState("Idle"); sIdle.motion = FirstClip($"{SoldierFolder}/animation/demo_combat_idle.FBX");
            var sRun = ssm.AddState("Run"); sRun.motion = FirstClip($"{SoldierFolder}/animation/demo_combat_run.FBX"); sRun.speed = 0.75f;
            var sShoot = ssm.AddState("Shoot"); sShoot.motion = FirstClip($"{SoldierFolder}/animation/demo_combat_shoot.FBX");
            ssm.defaultState = sIdle;
            Transition(sIdle, sRun, 0.15f, ("Speed", AnimatorConditionMode.Greater, 0.1f));
            Transition(sRun, sIdle, 0.15f, ("Speed", AnimatorConditionMode.Less, 0.1f));
            TriggerTransition(sIdle, sShoot, "Shoot", 0.05f);
            TriggerTransition(sRun, sShoot, "Shoot", 0.05f);
            var sBack = sShoot.AddTransition(sIdle); sBack.hasExitTime = true; sBack.exitTime = 0.85f; sBack.duration = 0.12f;
            Log($"Soldier clips: idle={sIdle.motion != null} run={sRun.motion != null} shoot={sShoot.motion != null}");
            AssetDatabase.SaveAssets();
        }

        private static void Transition(AnimatorState from, AnimatorState to, float duration, (string p, AnimatorConditionMode m, float t) cond)
        {
            var tr = from.AddTransition(to);
            tr.hasExitTime = false;
            tr.duration = duration;
            tr.AddCondition(cond.m, cond.t, cond.p);
        }

        private static void TriggerTransition(AnimatorState from, AnimatorState to, string trigger, float duration)
        {
            var tr = from.AddTransition(to);
            tr.hasExitTime = false;
            tr.duration = duration;
            tr.AddCondition(AnimatorConditionMode.If, 0, trigger);
        }

        // =================================================================== Prefabs

        private static void BuildPrefabs()
        {
            EnsureFolder(Prefabs + "/AR");
            EnsureFolder(Prefabs + "/Projectiles");
            EnsureFolder(Prefabs + "/Enemies");
            EnsureFolder(Prefabs + "/FX");
            EnsureFolder(Prefabs + "/Audio");

            BuildPlanePrefab();
            BuildReticlePrefab();
            BuildArenaPrefab();
            BuildProjectile("PlayerProjectile", Mat("M_ProjectilePlayer"), Mat("M_TrailPlayer"), new Color(0.4f, 1f, 1f), 16f, 0.05f, new Vector3(0.035f, 0.035f, 0.16f));
            BuildProjectile("EnemyProjectile", Mat("M_ProjectileEnemy"), Mat("M_TrailEnemy"), new Color(1f, 0.3f, 0.35f), 4.5f, 0.07f, new Vector3(0.07f, 0.07f, 0.14f));
            BuildEffects();
            BuildAudioEmitter();
            BuildEnemy("MeleeZombie", EnemyType.Melee);
            BuildEnemy("ShooterSoldier", EnemyType.Shooter);
        }

        private static void SavePrefab(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static void BuildPlanePrefab()
        {
            var go = new GameObject("CustomPlaneTracker");
            go.AddComponent<ARPlane>();
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Mat("M_CustomPlane");
            mr.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<MeshCollider>();
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.widthMultiplier = 0.012f;
            lr.sharedMaterial = Mat("M_PlaneBoundary");
            lr.startColor = lr.endColor = new Color(0.35f, 0.95f, 1f, 1f);
            lr.numCornerVertices = 2;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<ARPlaneMeshVisualizer>();
            go.AddComponent<CustomPlaneVisualizer>();
            SavePrefab(go, $"{Prefabs}/AR/CustomPlaneTracker.prefab");
        }

        private static void BuildReticlePrefab()
        {
            var go = new GameObject("PlacementReticle");
            var spinner = Child(go, "Spinner");
            Quad(spinner, "Ring", Mat("M_Reticle"), Vector3.zero, new Vector3(90, 0, 0), Vector3.one * 0.45f);
            var r = go.AddComponent<PlacementReticle>();
            Set(r, "spinner", spinner.transform);
            SavePrefab(go, $"{Prefabs}/AR/PlacementReticle.prefab");
        }

        private static void BuildArenaPrefab()
        {
            var go = new GameObject("CombatArena");
            var ring = Quad(go, "ArenaRing", Mat("M_ArenaRing"), new Vector3(0, 0.004f, 0), new Vector3(90, 0, 0), Vector3.one * 0.9f);
            var pulse = Quad(go, "ArenaPulse", Mat("M_ArenaPulse"), new Vector3(0, 0.006f, 0), new Vector3(90, 0, 0), Vector3.one * 0.6f);
            var v = go.AddComponent<ArenaVisual>();
            Set(v, "ring", ring.transform);
            Set(v, "pulse", pulse.transform);
            SavePrefab(go, $"{Prefabs}/AR/CombatArena.prefab");
        }

        private static void BuildProjectile(string name, Material body, Material trailMat, Color trailColor, float speed, float radius, Vector3 visualScale)
        {
            var go = new GameObject(name);
            var vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vis.name = "Visual";
            UnityEngine.Object.DestroyImmediate(vis.GetComponent<Collider>());
            vis.transform.SetParent(go.transform, false);
            vis.transform.localScale = visualScale;
            var mr = vis.GetComponent<MeshRenderer>();
            mr.sharedMaterial = body;
            mr.shadowCastingMode = ShadowCastingMode.Off;

            var glowQuad = Quad(go, "Glow", Mat("M_FX_Spark"), Vector3.zero, Vector3.zero, Vector3.one * (radius * 3.5f));
            glowQuad.GetComponent<MeshRenderer>().sharedMaterial = trailMat;
            glowQuad.AddComponent<Billboard>();

            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = trailMat;
            trail.time = 0.12f;
            trail.minVertexDistance = 0.02f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0, radius * 1.6f), new Keyframe(1, 0));
            trail.colorGradient = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(trailColor, 0), new GradientColorKey(trailColor, 1) },
                alphaKeys = new[] { new GradientAlphaKey(1f, 0), new GradientAlphaKey(0f, 1) }
            };
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.emitting = false;

            var p = go.AddComponent<Projectile>();
            SetFloat(p, "defaultSpeed", speed);
            SetFloat(p, "radius", radius);
            SetFloat(p, "maxLifetime", name.StartsWith("Enemy") ? 4f : 2f);
            Set(p, "trail", trail);
            Set(p, "visual", vis.transform);
            Set(p, "glow", glowQuad.transform);
            SavePrefab(go, $"{Prefabs}/Projectiles/{name}.prefab");
        }

        // ------------------------------------------------------------------- FX

        private static void BuildEffects()
        {
            BuildBurst("FX_HitSpark", new Color(0.5f, 1f, 1f), 14, 2.2f, 0.035f, 0.25f, false);
            BuildBurst("FX_EnemyImpact", new Color(1f, 0.45f, 0.3f), 18, 2.6f, 0.05f, 0.35f, false);
            BuildBurst("FX_DeathBurst", new Color(1f, 0.35f, 0.4f), 26, 1.5f, 0.045f, 0.55f, false);
            BuildBurst("FX_SpawnPortal", new Color(1f, 0.3f, 0.35f), 24, 0.9f, 0.05f, 0.9f, true);
        }

        private static void BuildBurst(string name, Color color, int count, float speed, float size, float life, bool portal)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ConfigureSparks(ps, color, count, speed, size, life, portal);

            if (portal)
            {
                var ringGo = Child(go, "Ring");
                var ring = ringGo.AddComponent<ParticleSystem>();
                var main = ring.main;
                main.duration = 0.8f; main.loop = false; main.playOnAwake = false;
                main.startLifetime = 0.8f; main.startSpeed = 0f; main.startSize = 0.35f;
                main.startColor = color;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startRotation3D = true;
                main.startRotationX = Mathf.PI * 0.5f;
                var em = ring.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 1), new ParticleSystem.Burst(0.25f, 1) });
                var sh = ring.shape; sh.enabled = false;
                var sol = ring.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.2f, 1, 2.8f));
                var col = ring.colorOverLifetime; col.enabled = true; col.color = FadeGradient(color);
                var rr = ringGo.GetComponent<ParticleSystemRenderer>();
                rr.sharedMaterial = Mat("M_FX_Ring");
                rr.renderMode = ParticleSystemRenderMode.Billboard;
                rr.alignment = ParticleSystemRenderSpace.World;
                rr.shadowCastingMode = ShadowCastingMode.Off;
            }

            var fx = go.AddComponent<PooledEffect>();
            SetFloat(fx, "lifetime", life + 0.4f);
            SavePrefab(go, $"{Prefabs}/FX/{name}.prefab");
        }

        private static void ConfigureSparks(ParticleSystem ps, Color color, int count, float speed, float size, float life, bool portal)
        {
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size * 1.4f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.4f));
            main.gravityModifier = portal ? -0.15f : 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count * 2;

            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var sh = ps.shape;
            sh.enabled = true;
            if (portal)
            {
                sh.shapeType = ParticleSystemShapeType.Circle;
                sh.radius = 0.3f;
                sh.rotation = new Vector3(90, 0, 0);
                sh.radiusThickness = 0.1f;
            }
            else
            {
                sh.shapeType = ParticleSystemShapeType.Sphere;
                sh.radius = 0.03f;
            }

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = FadeGradient(color);

            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mat("M_FX_Spark");
            r.renderMode = portal ? ParticleSystemRenderMode.Billboard : ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.04f;
            r.lengthScale = 1.5f;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static Gradient FadeGradient(Color c)
        {
            return new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(c, 0.3f), new GradientColorKey(c, 1) },
                alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0, 1) }
            };
        }

        private static void BuildAudioEmitter()
        {
            var go = new GameObject("AudioEmitter3D");
            go.AddComponent<AudioSource>();
            go.AddComponent<AudioEmitter>();
            SavePrefab(go, $"{Prefabs}/Audio/AudioEmitter3D.prefab");
        }

        // ------------------------------------------------------------------- Enemies

        private static void BuildEnemy(string name, EnemyType type)
        {
            bool melee = type == EnemyType.Melee;
            GameObject source = melee
                ? Load<GameObject>($"{ZombieFolder}/copzombie_l_actisdato.fbx")
                : Load<GameObject>($"{SoldierFolder}/Soldier_demo_Prefab.prefab");
            if (source == null) throw new Exception($"Enemy source model missing for {name}");

            var root = new GameObject(name);
            var model = (GameObject)UnityEngine.Object.Instantiate(source);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);

            var animator = model.GetComponentInChildren<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = Load<RuntimeAnimatorController>($"{Anims}/{(melee ? "AC_Zombie" : "AC_Soldier")}.controller");
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            if (melee && animator.avatar == null)
                animator.avatar = AssetDatabase.LoadAllAssetsAtPath($"{ZombieFolder}/copzombie_l_actisdato.fbx").OfType<Avatar>().FirstOrDefault();

            if (melee) ApplyZombieMaterials(model);

            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
            }

            // Normalise height so both enemies read at the same AR scale
            float targetHeight = melee ? 1.35f : 1.3f;
            Bounds b = RendererBounds(model);
            float scale = b.size.y > 0.01f ? targetHeight / b.size.y : 1f;
            model.transform.localScale = Vector3.one * scale;
            b = RendererBounds(model);
            model.transform.localPosition = new Vector3(0f, -b.min.y, 0f);
            float h = targetHeight;
            Log($"{name}: raw height {b.size.y / Mathf.Max(0.0001f, scale):F2} → scale {scale:F4}");

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, h * 0.5f, 0);
            capsule.height = h;
            capsule.radius = 0.24f;

            Quad(root, "BlobShadow", Mat("M_BlobShadow"), new Vector3(0, 0.006f, 0), new Vector3(90, 0, 0), Vector3.one * 0.85f);
            var typeRing = Quad(root, "TypeRing", Mat(melee ? "M_RingMelee" : "M_RingShooter"), new Vector3(0, 0.009f, 0), new Vector3(90, 0, 0), Vector3.one * 0.7f);
            typeRing.AddComponent<SpinY>();

            // Health bar (two quads + lag)
            var hb = Child(root, "HealthBar");
            hb.transform.localPosition = new Vector3(0, h + 0.14f, 0);
            float w = 0.42f, th = 0.045f;
            Quad(hb, "Bg", Mat("M_HealthBg"), Vector3.zero, Vector3.zero, new Vector3(w + 0.02f, th + 0.02f, 1));
            var lag = Child(hb, "LagPivot"); lag.transform.localPosition = new Vector3(-w * 0.5f, 0, -0.001f);
            Quad(lag, "Lag", Mat("M_HealthLag"), new Vector3(w * 0.5f, 0, 0), Vector3.zero, new Vector3(w, th, 1));
            var fill = Child(hb, "FillPivot"); fill.transform.localPosition = new Vector3(-w * 0.5f, 0, -0.002f);
            Quad(fill, "Fill", Mat(melee ? "M_HealthMelee" : "M_HealthShooter"), new Vector3(w * 0.5f, 0, 0), Vector3.zero, new Vector3(w, th, 1));
            var bar = hb.AddComponent<EnemyHealthBar>();
            Set(bar, "fillPivot", fill.transform);
            Set(bar, "lagPivot", lag.transform);

            EnemyBase enemy;
            if (melee)
            {
                enemy = root.AddComponent<MeleeEnemy>();
                SetFloat(enemy, "maxHealth", 50);      // 2 player bullets (25 dmg)
                SetFloat(enemy, "moveSpeed", 0.62f);
                SetFloat(enemy, "attackRange", 0.85f);
                SetFloat(enemy, "attackCooldown", 1.7f);
                SetFloat(enemy, "attackDamage", 9);
                SetFloat(enemy, "scoreValue", 100);
            }
            else
            {
                var shooter = root.AddComponent<ShooterEnemy>();
                enemy = shooter;
                SetFloat(enemy, "maxHealth", 100);     // 4 player bullets (25 dmg)
                SetFloat(enemy, "moveSpeed", 0.6f);
                SetFloat(enemy, "attackRange", 3.4f);
                SetFloat(enemy, "attackCooldown", 2.6f);
                SetFloat(enemy, "attackDamage", 7);
                SetFloat(enemy, "scoreValue", 200);
                SetFloat(shooter, "stopDistance", 2.3f);

                var muzzle = Child(root, "Muzzle");
                muzzle.transform.localPosition = new Vector3(0.1f, h * 0.68f, 0.42f);
                var flash = Quad(muzzle, "MuzzleFlash", Mat("M_MuzzleGlow"), Vector3.zero, Vector3.zero, Vector3.one * 0.16f);
                flash.AddComponent<Billboard>();
                Set(shooter, "muzzle", muzzle.transform);
                Set(shooter, "muzzleFlash", flash.transform);
            }
            Set(enemy, "animator", animator);
            Set(enemy, "model", model.transform);
            Set(enemy, "healthBar", bar);

            SavePrefab(root, $"{Prefabs}/Enemies/{name}.prefab");
        }

        private static void ApplyZombieMaterials(GameObject model)
        {
            string texDir = ZombieFolder + "/Textures";
            Texture2D FindTex(string key) => AssetDatabase.FindAssets("t:Texture2D", new[] { texDir })
                .Select(g => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(t => t != null && t.name.ToLowerInvariant().Contains(key));
            var head = FindTex("fuzzombie_diffuse");
            var body = FindTex("body_diffuse");
            var normal = FindTex("normal");

            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string src = mats[i] != null ? mats[i].name : "none";
                    bool isBody = src.ToLowerInvariant().Contains("body") || (mats.Length > 1 && i == 0 && body != null && src.ToLowerInvariant().Contains("0"));
                    var tex = isBody && body != null ? body : (head != null ? head : body);
                    string name = $"M_Zombie_{r.name}_{i}".Replace(" ", "");
                    var m = new Material(LitShader) { name = name };
                    m.SetTexture("_BaseMap", tex);
                    m.SetColor("_BaseColor", Color.white);
                    m.SetFloat("_Smoothness", 0.25f);
                    if (src.ToLowerInvariant().Contains("hair") || src.ToLowerInvariant().Contains("glass") || r.name == "Eyes")
                    {
                        m.SetFloat("_AlphaClip", 1f);
                        m.SetFloat("_Cutoff", 0.45f);
                        m.EnableKeyword("_ALPHATEST_ON");
                        m.SetFloat("_Cull", 0f);
                    }
                    if (normal != null)
                    {
                        m.SetTexture("_BumpMap", normal);
                        m.EnableKeyword("_NORMALMAP");
                    }
                    mats[i] = SaveAsset(m, $"{Mats}/{name}.mat");
                    Log($"Zombie renderer '{r.name}' slot {i} (src '{src}') → {(tex != null ? tex.name : "NO TEXTURE")}");
                }
                r.sharedMaterials = mats;
            }
        }

        private static Bounds RendererBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        // =================================================================== Scene

        private static void BuildScene()
        {
            EnsureFolder(Root + "/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- AR Session + XR Origin
            var session = new GameObject("AR Session");
            session.AddComponent<ARSession>();
            session.AddComponent<ARInputManager>();

            XROrigin origin = CreateXROrigin(out Camera cam);
            var planeManager = origin.gameObject.AddComponent<ARPlaneManager>();
            planeManager.planePrefab = Load<GameObject>($"{Prefabs}/AR/CustomPlaneTracker.prefab");
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            var raycast = origin.gameObject.AddComponent<ARRaycastManager>();
            var anchors = origin.gameObject.AddComponent<ARAnchorManager>();

            // --- Player on the AR camera (first-person)
            var sphere = cam.gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.22f;
            cam.gameObject.AddComponent<PlayerHealth>();
            var shooter = cam.gameObject.AddComponent<PlayerShooter>();
            Set(shooter, "aimCamera", cam);
            cam.gameObject.AddComponent<DesktopCameraController>();

            // --- Light
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.shadows = LightShadows.None;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var est = lightGo.AddComponent<ARLightEstimator>();
            Set(est, "cameraManager", cam.GetComponent<ARCameraManager>());
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.66f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.24f, 0.23f);

            // --- Post processing (bloom only: keeps the camera feed natural)
            var volGo = new GameObject("Global Volume (Bloom)");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            string profilePath = $"{Gen}/PP_Bloom.asset";
            AssetDatabase.DeleteAsset(profilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(2.2f);
            bloom.intensity.Override(0.9f);
            bloom.scatter.Override(0.65f);
            EditorUtility.SetDirty(profile);
            vol.sharedProfile = profile;

            // --- Managers
            var managers = new GameObject("[Managers]");
            var gm = managers.AddComponent<GameManager>();
            var audio = managers.AddComponent<AudioManager>();
            ConfigureAudio(audio);
            var pools = managers.AddComponent<PoolManager>();
            Set(pools, "playerProjectilePrefab", Load<GameObject>($"{Prefabs}/Projectiles/PlayerProjectile.prefab").GetComponent<Projectile>());
            Set(pools, "enemyProjectilePrefab", Load<GameObject>($"{Prefabs}/Projectiles/EnemyProjectile.prefab").GetComponent<Projectile>());
            Set(pools, "hitSparkPrefab", FX("FX_HitSpark"));
            Set(pools, "enemyImpactPrefab", FX("FX_EnemyImpact"));
            Set(pools, "spawnPortalPrefab", FX("FX_SpawnPortal"));
            Set(pools, "deathBurstPrefab", FX("FX_DeathBurst"));

            var factory = managers.AddComponent<EnemyFactory>();
            var so = new SerializedObject(factory);
            var arr = so.FindProperty("prefabs");
            arr.arraySize = 2;
            arr.GetArrayElementAtIndex(0).FindPropertyRelative("type").enumValueIndex = (int)EnemyType.Melee;
            arr.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue = Load<GameObject>($"{Prefabs}/Enemies/MeleeZombie.prefab").GetComponent<EnemyBase>();
            arr.GetArrayElementAtIndex(1).FindPropertyRelative("type").enumValueIndex = (int)EnemyType.Shooter;
            arr.GetArrayElementAtIndex(1).FindPropertyRelative("prefab").objectReferenceValue = Load<GameObject>($"{Prefabs}/Enemies/ShooterSoldier.prefab").GetComponent<EnemyBase>();
            so.ApplyModifiedPropertiesWithoutUndo();

            managers.AddComponent<EnemySpawner>();
            var placement = managers.AddComponent<ARPlacementManager>();
            Set(placement, "raycastManager", raycast);
            Set(placement, "planeManager", planeManager);
            Set(placement, "anchorManager", anchors);
            Set(placement, "arCamera", cam);
            Set(placement, "reticlePrefab", Load<GameObject>($"{Prefabs}/AR/PlacementReticle.prefab").GetComponent<PlacementReticle>());
            Set(placement, "arenaPrefab", Load<GameObject>($"{Prefabs}/AR/CombatArena.prefab"));
            managers.AddComponent<LeaderboardManager>();

            // --- UI
            SurvivalShooterUIBuilder.BuildUI();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Log("Scene saved: " + ScenePath);
        }

        private static PooledEffect FX(string name) => Load<GameObject>($"{Prefabs}/FX/{name}.prefab").GetComponent<PooledEffect>();

        private static XROrigin CreateXROrigin(out Camera cam)
        {
            var originGo = new GameObject("XR Origin (Mobile AR)");
            var origin = originGo.AddComponent<XROrigin>();
            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(originGo.transform, false);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(offset.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 60f;
            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            var camManager = camGo.AddComponent<ARCameraManager>();
            camManager.requestedLightEstimation = UnityEngine.XR.ARFoundation.LightEstimation.AmbientIntensity | UnityEngine.XR.ARFoundation.LightEstimation.AmbientColor;
            camManager.autoFocusRequested = true;
            camGo.AddComponent<ARCameraBackground>();

            var tpd = camGo.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            var pos = new InputAction("Position", InputActionType.Value, expectedControlType: "Vector3");
            pos.AddBinding("<XRHMD>/centerEyePosition");
            pos.AddBinding("<HandheldARInputDevice>/devicePosition");
            var rot = new InputAction("Rotation", InputActionType.Value, expectedControlType: "Quaternion");
            rot.AddBinding("<XRHMD>/centerEyeRotation");
            rot.AddBinding("<HandheldARInputDevice>/deviceRotation");
            tpd.positionInput = new InputActionProperty(pos);
            tpd.rotationInput = new InputActionProperty(rot);

            origin.Camera = cam;
            origin.CameraFloorOffsetObject = offset;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.NotSpecified;
            return origin;
        }

        // ------------------------------------------------------------------- Audio

        private static void ConfigureAudio(AudioManager audio)
        {
            const string laser = "Assets/Laser Weapons Sound Pack/Free/";
            const string z = "Assets/ZombieHorrorPackageFree/WAV/";
            const string ui = "Assets/SurvivalShooter/Audio/UI/";
            const string music = "Assets/SurvivalShooter/Audio/Music/";

            var so = new SerializedObject(audio);
            var list = so.FindProperty("sounds");
            list.arraySize = 0;

            void Add(SoundId id, float vol, bool spatial, float pMin, float pMax, params string[] paths)
            {
                int i = list.arraySize;
                list.arraySize++;
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = (int)id;
                e.FindPropertyRelative("volume").floatValue = vol;
                e.FindPropertyRelative("spatial").boolValue = spatial;
                e.FindPropertyRelative("pitchRange").vector2Value = new Vector2(pMin, pMax);
                var clips = e.FindPropertyRelative("clips");
                clips.arraySize = paths.Length;
                for (int k = 0; k < paths.Length; k++) clips.GetArrayElementAtIndex(k).objectReferenceValue = Clip(paths[k]);
            }

            Add(SoundId.PlayerShoot, 0.5f, false, 0.95f, 1.08f, laser + "light_blast_1.wav", laser + "light_blast_2.wav", laser + "light_blast_3.wav");
            Add(SoundId.PlayerDeath, 1f, false, 1f, 1f, music + "Death.wav");
            Add(SoundId.EnemySpawn, 0.85f, true, 0.9f, 1.1f, z + "VO/Zombie01/Zombie001_Idle_A_001.wav", z + "VO/Zombie01/Zombie001_Idle_A_002.wav", z + "VO/Zombie03/Zombie003_Idle_A_001.wav");
            Add(SoundId.EnemyShoot, 0.7f, true, 0.92f, 1.08f, laser + "heavy_blast_001.wav", laser + "heavy_blast_002.wav", laser + "heavy_blast_003.wav");
            Add(SoundId.MeleeAttack, 1f, true, 0.92f, 1.08f, z + "Bite/Zombie_Attack_Bite_001.wav", z + "Bite/Zombie_Attack_Bite_002.wav", z + "VO/Zombie01/Zombie001_Attack_A_001.wav");
            Add(SoundId.PlayerHurt, 0.8f, false, 0.95f, 1.05f, z + "Impact/Impact_Flesh_Gory_Light_001.wav", z + "Impact/Impact_Flesh_Gory_Light_002.wav");
            Add(SoundId.EnemyHurt, 0.65f, true, 0.9f, 1.15f, z + "Impact/Impact_Flesh_001.wav", z + "Impact/Impact_Flesh_002.wav", z + "Impact/Impact_Flesh_003.wav");
            Add(SoundId.EnemyDeath, 0.9f, true, 0.9f, 1.05f, z + "BodyFall/Foley_BodyFall_001.wav", z + "BodyFall/Foley_BodyFall_002.wav", z + "VO/Zombie03/Zombie003_Hurt_A_001.wav");
            Add(SoundId.ArenaPlaced, 0.9f, false, 1f, 1f, ui + "SFX_Click_Mechanical.mp3");
            Add(SoundId.PlaneFound, 0.5f, false, 1.1f, 1.1f, ui + "Hover Button SFX.wav");
            Add(SoundId.CountdownBeep, 0.8f, false, 1f, 1f, ui + "Click Button SFX.wav");
            Add(SoundId.CountdownGo, 1f, false, 1f, 1f, ui + "SFX_Click_Whoosh.mp3");
            Add(SoundId.UIClick, 0.7f, false, 0.98f, 1.02f, ui + "Click Button SFX.wav");
            Add(SoundId.UIHover, 0.2f, false, 1f, 1f, ui + "Hover Button SFX.wav");
            Add(SoundId.UIBack, 0.6f, false, 1f, 1f, ui + "SFX_Click_Punch.ogg");
            so.ApplyModifiedPropertiesWithoutUndo();

            Set(audio, "menuMusic", Clip(music + "Ambient 1.wav"));
            Set(audio, "combatMusic", Clip(music + "Action 1 (Loop).wav"));
            Set(audio, "victoryMusic", Clip(music + "Victory.wav"));
            Set(audio, "emitterPrefab", Load<GameObject>($"{Prefabs}/Audio/AudioEmitter3D.prefab").GetComponent<AudioEmitter>());
        }
    }
}
