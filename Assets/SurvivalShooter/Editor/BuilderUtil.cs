using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SurvivalShooter.EditorTools
{
    /// <summary>Shared helpers for the project builder (paths, materials, serialized fields).</summary>
    public static class BuilderUtil
    {
        public const string Root = "Assets/SurvivalShooter";
        public const string Art = Root + "/Art";
        public const string Gen = Root + "/Generated";
        public const string Mats = Gen + "/Materials";
        public const string Prefabs = Root + "/Prefabs";
        public const string Anims = Gen + "/Animation";
        public const string FontsOut = Gen + "/Fonts";
        public const string ScenePath = Root + "/Scenes/SurvivalShooterAR.unity";

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static T Load<T>(string path) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) Debug.LogWarning($"[SSAR Builder] Missing asset: {path}");
            return a;
        }

        public static Sprite Sprite(string name) => Load<Sprite>($"{Art}/UI/{name}.png");
        public static Sprite Icon(string name) => Load<Sprite>($"{Art}/Icons/Icon_{name}.png");
        public static Texture2D Tex(string name) => Load<Texture2D>($"{Art}/Textures/{name}.png");
        public static Texture2D UITex(string name) => Load<Texture2D>($"{Art}/UI/{name}.png");

        public static AudioClip Clip(string path) => Load<AudioClip>(path);

        public static T SaveAsset<T>(T asset, string path) where T : Object
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ------------------------------------------------------------------ Materials

        public static Shader UnlitShader => Shader.Find("Universal Render Pipeline/Unlit");
        public static Shader ParticleShader => Shader.Find("Universal Render Pipeline/Particles/Unlit");
        public static Shader LitShader => Shader.Find("Universal Render Pipeline/Lit");

        public static Material UnlitOpaque(string name, Color color)
        {
            var m = new Material(UnlitShader) { name = name };
            m.SetColor("_BaseColor", color);
            return SaveAsset(m, $"{Mats}/{name}.mat");
        }

        public static Material Transparent(string name, Shader shader, Texture tex, Color color, bool additive)
        {
            var m = new Material(shader) { name = name };
            if (tex != null)
            {
                m.SetTexture("_BaseMap", tex);
                m.mainTexture = tex;
            }
            m.SetColor("_BaseColor", color);
            ConfigureTransparent(m, additive);
            if (shader == ParticleShader)
            {
                // Fade particles/glows out as they get close to the AR camera lens (no screen wash-outs)
                const float near = 0.25f, far = 0.9f;
                m.SetFloat("_CameraFadingEnabled", 1f);
                m.SetFloat("_CameraNearFadeDistance", near);
                m.SetFloat("_CameraFarFadeDistance", far);
                m.SetVector("_CameraFadeParams", new Vector4(near, 1f / (far - near), 0f, 0f));
                m.EnableKeyword("_FADING_ON");
            }
            return SaveAsset(m, $"{Mats}/{name}.mat");
        }

        public static void ConfigureTransparent(Material m, bool additive)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        // ------------------------------------------------------------------ Serialized field helpers

        public static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[SSAR Builder] {target.GetType().Name}.{field} not found"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[SSAR Builder] {target.GetType().Name}.{field} not found"); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[SSAR Builder] {target.GetType().Name}.{field} not found"); return; }
            if (p.propertyType == SerializedPropertyType.Integer) p.intValue = Mathf.RoundToInt(value);
            else p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        public static GameObject Quad(GameObject parent, string name, Material mat, Vector3 localPos, Vector3 localEuler, Vector3 scale)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(parent.transform, false);
            q.transform.localPosition = localPos;
            q.transform.localEulerAngles = localEuler;
            q.transform.localScale = scale;
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return q;
        }

        public static AnimationClip FirstClip(string fbxPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }
    }
}
