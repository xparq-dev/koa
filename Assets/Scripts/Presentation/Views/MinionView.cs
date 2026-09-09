using System.Collections;
using KOA.Core.Minions;
using KOA.Presentation.UI;
using UnityEngine;

namespace KOA.Presentation.Views
{
    /// <summary>
    /// Presentation View สำหรับครีปเลน (Section 3.2)
    /// ควบคุม Transform และแถบเลือดให้อัปเดตตาม MinionEntity Core
    /// </summary>
    public class MinionView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private WorldSpaceHealthBar healthBar;
        [SerializeField] private Renderer minionRenderer;

        public MinionEntity Logic { get; private set; }
        private Animator _animator;
        private bool _hasMoveSpeed;
        private bool _hasLocomotionRate;
        private Vector3 _lastLogicPosition;
        private Texture2D _attackTexture;
        private Texture2D _impactTexture;
        private AudioClip _attackSound;
        private static readonly int MoveSpeedParameter = Animator.StringToHash("MoveSpeed");
        private static readonly int LocomotionRateParameter = Animator.StringToHash("LocomotionRate");
        private static readonly int AttackParameter = Animator.StringToHash("Attack");

        public void SetHealthBar(WorldSpaceHealthBar bar)
        {
            healthBar = bar;
            if (healthBar != null && Logic != null)
            {
                healthBar.BindTarget(transform);
                healthBar.SetHealth(Logic.CurrentHp, Logic.MaxHp);
            }
        }

        public void BindLogic(MinionEntity minionEntity)
        {
            Logic = minionEntity;
            transform.position = Logic.Position + Vector3.up * 0.4f;
            _lastLogicPosition = Logic.Position;
            _animator = GetComponentInChildren<Animator>(true);
            _attackTexture = Resources.Load<Texture2D>(Logic.Type == MinionType.Melee || Logic.Type == MinionType.Super
                ? "KOA/VFX/slash_02"
                : "KOA/VFX/trace_03");
            _impactTexture = Resources.Load<Texture2D>("KOA/VFX/spark_03");
            _attackSound = Resources.Load<AudioClip>(Logic.Type == MinionType.Melee || Logic.Type == MinionType.Super
                ? "KOA/Audio/knifeSlice2"
                : "KOA/Audio/metalClick");
            _hasMoveSpeed = HasAnimatorParameter(_animator, MoveSpeedParameter);
            _hasLocomotionRate = HasAnimatorParameter(_animator, LocomotionRateParameter);

            if (minionRenderer == null)
            {
                minionRenderer = GetComponentInChildren<Renderer>();
            }

            if (minionRenderer != null)
            {
                // แยกสีตามชนิดและทีม:
                Color baseColor = Logic.TeamId == 0 
                    ? (Logic.Type == MinionType.Super ? new Color(0.35f, 0.2f, 0.85f) :
                       Logic.Type == MinionType.Cannon ? new Color(0.2f, 0.85f, 0.85f) : new Color(0.2f, 0.6f, 1f))
                    : (Logic.Type == MinionType.Super ? new Color(0.75f, 0.05f, 0.25f) :
                       Logic.Type == MinionType.Cannon ? new Color(0.95f, 0.55f, 0.1f) : new Color(1f, 0.25f, 0.2f));

                minionRenderer.material.color = baseColor;
            }

            if (healthBar != null)
            {
                healthBar.BindTarget(transform);
                healthBar.SetHealth(Logic.CurrentHp, Logic.MaxHp);
            }

            Logic.OnHealthChanged += HandleHealthChanged;
            Logic.OnAttackExecuted += HandleAttackExecuted;
            Logic.OnKilled += HandleKilled;
        }

        private void OnDestroy()
        {
            if (Logic != null)
            {
                Logic.OnHealthChanged -= HandleHealthChanged;
                Logic.OnAttackExecuted -= HandleAttackExecuted;
                Logic.OnKilled -= HandleKilled;
            }
            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
            }
        }

        private void Update()
        {
            if (Logic != null && Logic.IsAlive)
            {
                bool moved = (Logic.Position - _lastLogicPosition).sqrMagnitude > 0.000001f;
                transform.position = Logic.Position + Vector3.up * 0.4f;
                transform.rotation = Logic.Rotation;
                if (_animator != null)
                {
                    if (_hasMoveSpeed) _animator.SetFloat(MoveSpeedParameter, moved ? 1f : 0f, 0.08f, Time.deltaTime);
                    if (_hasLocomotionRate) _animator.SetFloat(LocomotionRateParameter, 0.88f);
                }
                _lastLogicPosition = Logic.Position;
            }
        }

        private static bool HasAnimatorParameter(Animator animator, int parameterHash)
        {
            if (animator == null) return false;
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == parameterHash) return true;
            }
            return false;
        }

        private void HandleAttackExecuted(Vector3 targetPosition)
        {
            if (_animator != null) _animator.SetTrigger(AttackParameter);
            Color color = Logic != null && Logic.TeamId == 0
                ? new Color(0.12f, 0.82f, 1f)
                : new Color(1f, 0.24f, 0.12f);
            if (Logic != null && (Logic.Type == MinionType.Melee || Logic.Type == MinionType.Super))
                CreateImpact(targetPosition + Vector3.up * 0.65f, color, 7, 0.62f, _attackTexture);
            else
                StartCoroutine(FlyMinionProjectile(transform.position + Vector3.up * 0.85f, targetPosition + Vector3.up * 0.6f, color));
            PlayAttackSound();
        }

        private IEnumerator FlyMinionProjectile(Vector3 start, Vector3 end, Color color)
        {
            GameObject projectile = new GameObject("KOA_MinionProjectile");
            ParticleSystem particles = projectile.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.24f;
            main.startSpeed = 0f;
            main.startSize = 0.24f;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 28f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = false;
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateMaterial(_attackTexture, color);
            renderer.sortingOrder = 31;
            particles.Play();

            float duration = Mathf.Clamp(Vector3.Distance(start, end) / 20f, 0.12f, 0.4f);
            float elapsed = 0f;
            while (elapsed < duration && projectile != null)
            {
                elapsed += Time.deltaTime;
                projectile.transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            if (projectile != null)
            {
                ParticleSystem.EmissionModule stopEmission = particles.emission;
                stopEmission.enabled = false;
                Destroy(projectile, 0.35f);
                Destroy(renderer.material, 0.45f);
            }
            CreateImpact(end, color, 10, 0.48f, _impactTexture);
        }

        private static void CreateImpact(Vector3 position, Color color, int count, float size, Texture2D texture)
        {
            GameObject impact = new GameObject("KOA_MinionImpact");
            impact.transform.position = position;
            ParticleSystem particles = impact.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.34f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, size);
            main.startColor = color;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.18f;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateMaterial(texture, color);
            renderer.sortingOrder = 32;
            particles.Play();
            Destroy(impact, 0.9f);
            Destroy(renderer.material, 1.0f);
        }

        private static Material CreateMaterial(Texture texture, Color color)
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

        private void PlayAttackSound()
        {
            if (_attackSound == null) return;
            GameObject sound = new GameObject("KOA_MinionAudio");
            sound.transform.position = transform.position;
            AudioSource source = sound.AddComponent<AudioSource>();
            source.clip = _attackSound;
            source.volume = 0.16f;
            source.pitch = Random.Range(0.9f, 1.1f);
            source.spatialBlend = 0.85f;
            source.minDistance = 2f;
            source.maxDistance = 18f;
            source.Play();
            Destroy(sound, _attackSound.length / Mathf.Max(0.5f, source.pitch) + 0.1f);
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.SetHealth(current, max);
            }
        }

        private void HandleKilled(MinionEntity minion, string killerId)
        {
            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
            }
            Destroy(gameObject);
        }
    }
}
