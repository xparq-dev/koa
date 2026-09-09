using System.Collections;
using KOA.Core.Structures;
using KOA.Presentation.UI;
using UnityEngine;

namespace KOA.Presentation.Views
{
    /// <summary>
    /// Presentation View สำหรับป้อมปราการและ Nexus (Section 3.3)
    /// จัดการโมเดล, ลำแสงเลเซอร์ Heating Laser, และแถบเลือด
    /// </summary>
    public class TowerView : MonoBehaviour
    {
        [Header("UI & Visuals")]
        [SerializeField] private WorldSpaceHealthBar healthBar;
        [SerializeField] private LineRenderer laserLineRenderer;
        [SerializeField] private Renderer towerMeshRenderer;

        [Header("Colors")]
        [SerializeField] private Color normalLaserColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color heatedLaserColor = new Color(1f, 0.15f, 0.1f);

        public TowerEntity Logic { get; private set; }

        private float _laserTimer = 0f;
        private const float LaserDuration = 0.15f;
        private Texture2D _projectileTexture;
        private Texture2D _impactTexture;
        private AudioClip _fireSound;

        public void BindLogic(TowerEntity towerEntity)
        {
            Logic = towerEntity;
            _projectileTexture = Resources.Load<Texture2D>("KOA/VFX/magic_02");
            _impactTexture = Resources.Load<Texture2D>("KOA/VFX/spark_06");
            _fireSound = Resources.Load<AudioClip>("KOA/Audio/metalPot2");

            if (laserLineRenderer == null)
            {
                laserLineRenderer = GetComponentInChildren<LineRenderer>();
            }
            if (laserLineRenderer != null) laserLineRenderer.enabled = false;

            if (towerMeshRenderer == null)
            {
                towerMeshRenderer = GetComponentInChildren<Renderer>();
            }

            if (healthBar != null)
            {
                healthBar.BindTarget(transform);
                healthBar.SetHealth(Logic.CurrentHp, Logic.Stats.MaxHp);
            }

            Logic.OnHealthChanged += HandleHealthChanged;
            Logic.OnAttackFired += HandleAttackFired;
            Logic.OnDestroyed += HandleDestroyed;
        }

        public void SetHealthBar(WorldSpaceHealthBar bar)
        {
            healthBar = bar;
            if (healthBar != null && Logic != null)
            {
                healthBar.BindTarget(transform);
                healthBar.SetHealth(Logic.CurrentHp, Logic.Stats.MaxHp);
            }
        }

        public void SetLaserRenderer(LineRenderer renderer)
        {
            laserLineRenderer = renderer;
        }

        private void OnDestroy()
        {
            if (Logic != null)
            {
                Logic.OnHealthChanged -= HandleHealthChanged;
                Logic.OnAttackFired -= HandleAttackFired;
                Logic.OnDestroyed -= HandleDestroyed;
            }
            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
            }
        }

        private void Update()
        {
            if (_laserTimer > 0f)
            {
                _laserTimer -= Time.deltaTime;
                if (_laserTimer <= 0f && laserLineRenderer != null)
                {
                    laserLineRenderer.enabled = false;
                }
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.SetHealth(current, max);
            }
        }

        private void HandleAttackFired(Vector3 targetPos, float damage, bool isHeated)
        {
            if (laserLineRenderer != null) laserLineRenderer.enabled = false;
            Vector3 origin = transform.position + Vector3.up * 3.2f;
            Vector3 end = targetPos + Vector3.up * 0.9f;
            Color color = isHeated ? heatedLaserColor : normalLaserColor;
            StartCoroutine(FlyTowerProjectile(origin, end, color, isHeated));
        }

        private IEnumerator FlyTowerProjectile(Vector3 origin, Vector3 target, Color color, bool heated)
        {
            GameObject projectile = new GameObject(heated ? "KOA_HeatedTowerBolt" : "KOA_TowerBolt");
            ParticleSystem particles = projectile.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.28f;
            main.startSpeed = 0f;
            main.startSize = heated ? 0.72f : 0.48f;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = heated ? 62f : 44f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = false;
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(_projectileTexture, color);
            renderer.sortingOrder = 36;
            projectile.transform.position = origin;
            particles.Play();
            PlaySpatialSound(_fireSound, origin, heated ? 0.7f : 0.48f, heated ? 0.72f : 0.92f);

            float travelDuration = Mathf.Clamp(Vector3.Distance(origin, target) / 32f, 0.12f, 0.42f);
            float elapsed = 0f;
            while (elapsed < travelDuration && projectile != null)
            {
                elapsed += Time.deltaTime;
                projectile.transform.position = Vector3.Lerp(origin, target, Mathf.Clamp01(elapsed / travelDuration));
                yield return null;
            }
            if (projectile != null)
            {
                ParticleSystem.EmissionModule stopEmission = particles.emission;
                stopEmission.enabled = false;
                Destroy(projectile, 0.4f);
                Destroy(renderer.material, 0.5f);
            }
            CreateImpact(target, color, heated ? 34 : 20, heated ? 1.2f : 0.8f);
        }

        private void CreateImpact(Vector3 position, Color color, int count, float size)
        {
            GameObject impact = new GameObject("KOA_TowerImpact");
            impact.transform.position = position;
            ParticleSystem particles = impact.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.48f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, size);
            main.startColor = color;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.22f;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(_impactTexture, color);
            renderer.sortingOrder = 37;
            particles.Play();
            Destroy(impact, 1.2f);
            Destroy(renderer.material, 1.3f);
        }

        private static Material CreateParticleMaterial(Texture texture, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            return material;
        }

        private static void PlaySpatialSound(AudioClip clip, Vector3 position, float volume, float pitch)
        {
            if (clip == null) return;
            GameObject sound = new GameObject("KOA_TowerAudio");
            sound.transform.position = position;
            AudioSource source = sound.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = 0.75f;
            source.minDistance = 3f;
            source.maxDistance = 38f;
            source.Play();
            Destroy(sound, clip.length / Mathf.Max(0.5f, pitch) + 0.1f);
        }

        private void HandleDestroyed()
        {
            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
            }

            if (laserLineRenderer != null)
            {
                laserLineRenderer.enabled = false;
            }

            // 1. ปิด Collider ทันที เพื่อไม่ให้ตัวละครหรือครีบเดินชน และไม่สามารถคลิกเป็นเป้าหมายได้อีก
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            // 2. ปรับ Visual ให้กลายเป็นฐานซากปรักหักพัง (Destroyed Tower Ruins) สไตล์เกม MOBA
            if (towerMeshRenderer == null)
            {
                towerMeshRenderer = GetComponentInChildren<Renderer>();
            }

            if (towerMeshRenderer != null)
            {
                towerMeshRenderer.material.color = new Color(0.18f, 0.18f, 0.18f, 0.9f); // สีหินไหม้เกรียม
            }

            // ยุบความสูงของป้อมลงเหลือเพียงแท่นหินเตี้ยๆ ติดพื้น (ความสูง 0.22 เมตร)
            Vector3 ruinedScale = transform.localScale;
            ruinedScale.y = 0.22f;
            ruinedScale.x *= 1.05f;
            ruinedScale.z *= 1.05f;
            transform.localScale = ruinedScale;

            // วางแท่นหินแนบพื้นพอดี
            if (Logic != null)
            {
                transform.position = new Vector3(Logic.Position.x, 0.11f, Logic.Position.z);
            }
        }
    }
}
