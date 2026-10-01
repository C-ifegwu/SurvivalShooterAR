using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SurvivalShooter.Core;
using SurvivalShooter.Enemies;

namespace SurvivalShooter.UI
{
    /// <summary>
    /// Edge-of-screen arrows pointing at enemies that are behind or beside the player.
    /// Essential in AR where enemies can approach from any direction.
    /// Arrow widgets are pre-created and re-used (pooled UI).
    /// </summary>
    public class OffscreenIndicators : MonoBehaviour
    {
        [SerializeField] private RectTransform arrowTemplate;
        [SerializeField] private int poolSize = 12;
        [SerializeField] private float edgePadding = 90f;
        [SerializeField] private Color meleeColor = new Color(1f, 0.28f, 0.36f, 1f);
        [SerializeField] private Color shooterColor = new Color(1f, 0.7f, 0.22f, 1f);

        private readonly List<RectTransform> arrows = new List<RectTransform>();
        private readonly List<Image> arrowImages = new List<Image>();
        private RectTransform area;
        private Camera cam;

        private void Awake()
        {
            area = (RectTransform)transform;
            if (arrowTemplate == null) return;
            arrowTemplate.gameObject.SetActive(false);
            for (int i = 0; i < poolSize; i++)
            {
                RectTransform a = Instantiate(arrowTemplate, area);
                a.gameObject.SetActive(false);
                arrows.Add(a);
                arrowImages.Add(a.GetComponent<Image>());
            }
        }

        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            int used = 0;

            if (cam != null && EnemySpawner.HasInstance)
            {
                Vector2 half = area.rect.size * 0.5f;
                Vector2 limit = half - new Vector2(edgePadding, edgePadding);

                foreach (EnemyBase e in EnemySpawner.Instance.ActiveEnemies)
                {
                    if (used >= arrows.Count) break;
                    if (e == null || !e.IsAlive) continue;

                    Vector3 vp = cam.WorldToViewportPoint(e.AimPoint);
                    bool onScreen = vp.z > 0f && vp.x > 0.02f && vp.x < 0.98f && vp.y > 0.02f && vp.y < 0.98f;
                    if (onScreen) continue;

                    Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
                    if (vp.z < 0f) dir = -dir;
                    if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
                    dir.Normalize();

                    float scale = Mathf.Min(limit.x / Mathf.Max(0.0001f, Mathf.Abs(dir.x)), limit.y / Mathf.Max(0.0001f, Mathf.Abs(dir.y)));
                    Vector2 pos = dir * scale;

                    RectTransform arrow = arrows[used];
                    arrow.gameObject.SetActive(true);
                    arrow.anchoredPosition = pos;
                    arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
                    float dist = Vector3.Distance(cam.transform.position, e.transform.position);
                    float s = Mathf.Lerp(1.15f, 0.7f, Mathf.InverseLerp(1f, 4f, dist));
                    arrow.localScale = Vector3.one * s;
                    if (arrowImages[used] != null)
                    {
                        Color c = e.Type == EnemyType.Melee ? meleeColor : shooterColor;
                        c.a = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 8f);
                        arrowImages[used].color = c;
                    }
                    used++;
                }
            }

            for (int i = used; i < arrows.Count; i++)
            {
                if (arrows[i].gameObject.activeSelf) arrows[i].gameObject.SetActive(false);
            }
        }
    }
}
