using System.Collections;
using KOA.Core.Entities;
using UnityEngine;

namespace KOA.Presentation.Views
{
    /// <summary>
    /// Converts deterministic Core combat events into textured combat feedback.
    /// All particles, materials and audio remain Presentation-only (Section 1.1).
    /// </summary>
    public sealed class HeroAbilityVfxView : MonoBehaviour
    {
        private HeroBase3D _hero;
        private Color _teamColor = Color.cyan;

        private Texture2D _slash;
        private Texture2D _heavySlash;
        private Texture2D _magic;
        private Texture2D _rune;
        private Texture2D _spark;
        private Texture2D _energySpark;
        private Texture2D _circle;
        private Texture2D _light;
        private Texture2D _smoke;
        private Texture2D _muzzle;
        private Texture2D _trace;
        private Texture2D _twirl;
        private Texture2D _dirt;
        private Texture2D _flame;

        private AudioClip _slashSound;
        private AudioClip _heavySound;
        private AudioClip _metalSound;

        public void Bind(HeroBase3D hero, Color teamColor)
        {
            Unsubscribe();
            LoadAssets();
            _hero = hero;
            _teamColor = teamColor;
            if (_hero == null) return;

            _hero.OnBasicAttackExecuted += HandleBasicAttack;
            if (_hero is VorkasHero vorkas)
            {
                vorkas.OnIronCleaveExecuted += HandleVorkasLine;
                vorkas.OnVanguardsWillExecuted += HandleVorkasAura;
                vorkas.OnSeismicSlamExecuted += HandleVorkasSlam;
                vorkas.OnRebellionImpactExecuted += HandleVorkasUltimate;
            }
            else if (_hero is ZenthisHero zenthis)
            {
                zenthis.OnSacredHourglassCast += HandleZenthisHourglass;
                zenthis.OnAuraOfEternityCast += HandleZenthisAura;
                zenthis.OnTemporalRiftCast += HandleZenthisRift;
                zenthis.OnGrandRewindCast += HandleZenthisRewind;
            }
            else if (_hero is KorvaxHero korvax)
            {
                korvax.OnHeavyBoltFired += HandleKorvaxBolt;
                korvax.OnHuntersFocusActivated += HandleKorvaxFocus;
                korvax.OnConcussiveBlastFired += HandleKorvaxBlast;
                korvax.OnBallistaOverdriveFired += HandleKorvaxUltimate;
            }
            else if (_hero is GravitorHero gravitor)
            {
                gravitor.OnMagneticPullCast += HandleGravitorPull;
                gravitor.OnRepulsionZoneCast += HandleGravitorRepulsion;
                gravitor.OnGravitonWellCast += HandleGravitorWell;
                gravitor.OnGravityCollapseCast += HandleGravitorUltimate;
            }
        }

        private void LoadAssets()
        {
            _slash = Resources.Load<Texture2D>("KOA/VFX/slash_02");
            _heavySlash = Resources.Load<Texture2D>("KOA/VFX/slash_04");
            _magic = Resources.Load<Texture2D>("KOA/VFX/magic_02");
            _rune = Resources.Load<Texture2D>("KOA/VFX/magic_05");
            _spark = Resources.Load<Texture2D>("KOA/VFX/spark_03");
            _energySpark = Resources.Load<Texture2D>("KOA/VFX/spark_06");
            _circle = Resources.Load<Texture2D>("KOA/VFX/circle_03");
            _light = Resources.Load<Texture2D>("KOA/VFX/light_02");
            _smoke = Resources.Load<Texture2D>("KOA/VFX/smoke_04");
            _muzzle = Resources.Load<Texture2D>("KOA/VFX/muzzle_03");
            _trace = Resources.Load<Texture2D>("KOA/VFX/trace_03");
            _twirl = Resources.Load<Texture2D>("KOA/VFX/twirl_02");
            _dirt = Resources.Load<Texture2D>("KOA/VFX/dirt_03");
            _flame = Resources.Load<Texture2D>("KOA/VFX/flame_04");
            _slashSound = Resources.Load<AudioClip>("KOA/Audio/knifeSlice");
            _heavySound = Resources.Load<AudioClip>("KOA/Audio/chop");
            _metalSound = Resources.Load<AudioClip>("KOA/Audio/metalClick");
        }

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (_hero == null) return;
            _hero.OnBasicAttackExecuted -= HandleBasicAttack;
            if (_hero is VorkasHero vorkas)
            {
                vorkas.OnIronCleaveExecuted -= HandleVorkasLine;
                vorkas.OnVanguardsWillExecuted -= HandleVorkasAura;
                vorkas.OnSeismicSlamExecuted -= HandleVorkasSlam;
                vorkas.OnRebellionImpactExecuted -= HandleVorkasUltimate;
            }
            else if (_hero is ZenthisHero zenthis)
            {
                zenthis.OnSacredHourglassCast -= HandleZenthisHourglass;
                zenthis.OnAuraOfEternityCast -= HandleZenthisAura;
                zenthis.OnTemporalRiftCast -= HandleZenthisRift;
                zenthis.OnGrandRewindCast -= HandleZenthisRewind;
            }
            else if (_hero is KorvaxHero korvax)
            {
                korvax.OnHeavyBoltFired -= HandleKorvaxBolt;
                korvax.OnHuntersFocusActivated -= HandleKorvaxFocus;
                korvax.OnConcussiveBlastFired -= HandleKorvaxBlast;
                korvax.OnBallistaOverdriveFired -= HandleKorvaxUltimate;
            }
            else if (_hero is GravitorHero gravitor)
            {
                gravitor.OnMagneticPullCast -= HandleGravitorPull;
                gravitor.OnRepulsionZoneCast -= HandleGravitorRepulsion;
                gravitor.OnGravitonWellCast -= HandleGravitorWell;
                gravitor.OnGravityCollapseCast -= HandleGravitorUltimate;
            }
            _hero = null;
        }

        private void HandleBasicAttack(Vector3 targetPosition)
        {
            Vector3 origin = Raised(transform.position, 1.0f);
            Vector3 target = Raised(targetPosition, 0.8f);
            if (_hero is VorkasHero || _hero is GravitorHero)
            {
                CreateBurst(target, _slash, Color.Lerp(_teamColor, Color.white, 0.5f), 2, 1.35f, 0.08f, 0.24f, false);
                CreateBurst(target, _spark, _teamColor, 10, 0.42f, 2.2f, 0.34f);
                PlaySound(_slashSound, target, 0.44f, Random.Range(0.94f, 1.05f));
            }
            else
            {
                StartCoroutine(FlyProjectile(origin, target, _hero is KorvaxHero ? _muzzle : _magic,
                    Color.Lerp(_teamColor, Color.white, 0.25f), 0.24f, 0.16f, 0.75f));
                PlaySound(_metalSound, origin, 0.25f, Random.Range(0.96f, 1.08f));
            }
        }

        private void HandleVorkasLine(Vector3 start, Vector3 end, bool hit)
        {
            Color color = hit ? new Color(1f, 0.55f, 0.08f) : new Color(0.35f, 0.82f, 1f);
            StartCoroutine(FlyProjectile(Raised(start, 0.85f), Raised(end, 0.45f), _heavySlash, color, 0.75f, 0.20f, 1.35f));
            CreateBurst(Raised(end, 0.35f), _spark, color, hit ? 22 : 10, 0.6f, 4.2f, 0.55f);
            PlaySound(_slashSound, start, 0.75f, 0.86f);
        }

        private void HandleVorkasAura()
        {
            CreateGroundGlyph(transform.position, 2.15f, _circle, new Color(1f, 0.58f, 0.08f), 2.6f, false);
            CreateBurst(Raised(transform.position, 0.8f), _flame, new Color(1f, 0.65f, 0.1f), 28, 0.7f, 1.3f, 1.2f);
            PlaySound(_heavySound, transform.position, 0.45f, 0.82f);
        }

        private void HandleVorkasSlam(Vector3 center, float radius, bool hit)
        {
            CreateGroundGlyph(center, radius, _circle, new Color(1f, 0.26f, 0.04f), 0.8f);
            CreateBurst(Raised(center, 0.18f), _dirt, new Color(0.72f, 0.37f, 0.12f), 34, 0.9f, 5.2f, 0.8f);
            CreateBurst(Raised(center, 0.35f), _spark, Color.yellow, hit ? 24 : 12, 0.55f, 4.0f, 0.55f);
            PlaySound(_heavySound, center, 0.9f, 0.72f);
        }

        private void HandleVorkasUltimate(Vector3 center, float radius, bool hit)
        {
            CreateGroundGlyph(center, radius, _rune, new Color(1f, 0.12f, 0.02f), 1.1f);
            CreateGroundGlyph(center, radius * 0.55f, _circle, new Color(1f, 0.85f, 0.1f), 0.85f);
            CreateBurst(Raised(center, 0.5f), _flame, new Color(1f, 0.22f, 0.02f), hit ? 64 : 42, 1.2f, 6.0f, 1.0f);
            PlaySound(_heavySound, center, 1f, 0.58f);
        }

        private void HandleZenthisHourglass(Vector3 center, float radius)
        {
            CreateGroundGlyph(center, radius, _rune, new Color(0.1f, 0.68f, 1f), 1.4f, false);
            CreateBurst(Raised(center, 0.7f), _magic, new Color(0.48f, 0.92f, 1f), 34, 0.85f, 1.6f, 1.2f);
            PlaySound(_metalSound, center, 0.35f, 1.32f);
        }

        private void HandleZenthisAura()
        {
            CreateGroundGlyph(transform.position, 2.35f, _circle, new Color(0.1f, 0.88f, 1f), 3.8f, false);
            CreateBurst(Raised(transform.position, 1f), _light, new Color(0.55f, 0.95f, 1f), 40, 0.65f, 0.8f, 1.5f);
        }

        private void HandleZenthisRift(Vector3 start, Vector3 end, bool hit)
        {
            StartCoroutine(FlyProjectile(Raised(start, 1f), Raised(end, 0.65f), _trace,
                hit ? new Color(0.05f, 1f, 1f) : new Color(0.2f, 0.48f, 1f), 0.48f, 0.25f, 1.1f));
            CreateBurst(Raised(end, 0.5f), _energySpark, new Color(0.15f, 0.85f, 1f), 30, 0.55f, 3.5f, 0.7f);
        }

        private void HandleZenthisRewind(Vector3 position, float restoredHp)
        {
            CreateGroundGlyph(position, 3.2f, _twirl, new Color(0.48f, 0.16f, 1f), 1.5f, false);
            CreateBurst(Raised(position, 1.0f), _light, Color.white, 55, 0.9f, 2.0f, 1.4f);
            PlaySound(_metalSound, position, 0.5f, 1.5f);
        }

        private void HandleKorvaxBolt(Vector3 start, Vector3 end, bool hit)
        {
            CreateBurst(Raised(start, 1.1f), _muzzle, new Color(1f, 0.82f, 0.2f), 5, 0.8f, 0.3f, 0.2f, false);
            StartCoroutine(FlyProjectile(Raised(start, 1.0f), Raised(end, 0.7f), _trace,
                hit ? new Color(1f, 0.82f, 0.15f) : new Color(1f, 0.4f, 0.04f), 0.34f, 0.20f, 1.0f));
            CreateBurst(Raised(end, 0.6f), _spark, new Color(1f, 0.58f, 0.08f), hit ? 28 : 12, 0.48f, 4.5f, 0.55f);
            PlaySound(_metalSound, start, 0.62f, 1.18f);
        }

        private void HandleKorvaxFocus()
        {
            CreateGroundGlyph(transform.position, 2.0f, _circle, new Color(1f, 0.78f, 0.08f), 3.8f, false);
            CreateBurst(Raised(transform.position, 1.2f), _light, new Color(1f, 0.88f, 0.28f), 26, 0.55f, 0.8f, 1.3f);
        }

        private void HandleKorvaxBlast(Vector3 origin, bool hit)
        {
            Vector3 end = origin + transform.forward * 4f;
            CreateBurst(Raised(origin, 1f), _muzzle, Color.white, 8, 1.0f, 1.0f, 0.24f, false);
            StartCoroutine(FlyProjectile(Raised(origin, 1f), Raised(end, 0.7f), _smoke,
                new Color(1f, 0.38f, 0.08f), 0.75f, 0.16f, 1.35f));
            CreateBurst(Raised(end, 0.6f), _spark, Color.white, hit ? 38 : 20, 0.55f, 5.5f, 0.65f);
            PlaySound(_heavySound, origin, 0.7f, 1.15f);
        }

        private void HandleKorvaxUltimate(Vector3 start, Vector3 end, bool hit)
        {
            CreateBurst(Raised(start, 1.15f), _muzzle, new Color(1f, 0.25f, 0.02f), 12, 1.4f, 1.2f, 0.3f, false);
            StartCoroutine(FlyProjectile(Raised(start, 1.1f), Raised(end, 0.7f), _flame,
                new Color(1f, 0.52f, 0.05f), 0.95f, 0.32f, 1.7f));
            CreateGroundGlyph(end, 1.7f, _circle, new Color(1f, 0.22f, 0.02f), 0.75f);
            CreateBurst(Raised(end, 0.7f), _flame, new Color(1f, 0.32f, 0.02f), hit ? 70 : 45, 1.0f, 6.5f, 0.9f);
            PlaySound(_heavySound, start, 1f, 0.67f);
        }

        private void HandleGravitorPull(Vector3 targetPosition)
        {
            StartCoroutine(FlyProjectile(Raised(transform.position, 1.1f), Raised(targetPosition, 0.8f), _twirl,
                new Color(0.72f, 0.15f, 1f), 0.55f, 0.3f, 1.1f));
            CreateGroundGlyph(targetPosition, 1.25f, _twirl, new Color(0.35f, 0.04f, 0.8f), 0.7f);
            CreateBurst(Raised(targetPosition, 0.8f), _energySpark, new Color(0.75f, 0.3f, 1f), 26, 0.52f, 2.8f, 0.7f);
        }

        private void HandleGravitorRepulsion(float radius)
        {
            CreateGroundGlyph(transform.position, radius, _circle, new Color(0.7f, 0.12f, 1f), 0.85f);
            CreateBurst(Raised(transform.position, 0.7f), _twirl, new Color(0.84f, 0.22f, 1f), 48, 0.85f, 5.4f, 0.8f);
            PlaySound(_heavySound, transform.position, 0.45f, 0.62f);
        }

        private void HandleGravitorWell(Vector3 center, float radius, bool hit)
        {
            CreateGroundGlyph(center, radius, _twirl, hit ? new Color(0.72f, 0.08f, 1f) : new Color(0.34f, 0.08f, 0.62f), 2.7f, false);
            CreateBurst(Raised(center, 0.45f), _smoke, new Color(0.28f, 0.02f, 0.45f), 55, 0.9f, 1.5f, 1.8f);
            CreateBurst(Raised(center, 0.7f), _energySpark, new Color(0.86f, 0.3f, 1f), 30, 0.5f, 2.2f, 1.2f);
        }

        private void HandleGravitorUltimate(Vector3 center, float radius)
        {
            CreateGroundGlyph(center, radius, _rune, new Color(0.78f, 0.05f, 1f), 1.35f, false);
            CreateGroundGlyph(center, radius * 0.58f, _twirl, new Color(0.18f, 0.01f, 0.32f), 1.2f, false);
            CreateBurst(Raised(center, 0.8f), _energySpark, new Color(0.92f, 0.35f, 1f), 85, 0.8f, 7.0f, 1.2f);
            PlaySound(_heavySound, center, 1f, 0.48f);
        }

        private IEnumerator FlyProjectile(Vector3 start, Vector3 end, Texture2D texture, Color color, float size, float travelTime, float impactSize)
        {
            GameObject projectile = new GameObject("KOA_TexturedProjectile");
            ParticleSystem trail = ConfigureParticles(projectile, texture, color, 0.34f, size, 0.06f, 0f, false);
            ParticleSystem.EmissionModule emission = trail.emission;
            emission.rateOverTime = 42f;
            trail.Play();

            float elapsed = 0f;
            while (elapsed < travelTime && projectile != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, travelTime));
                projectile.transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            if (projectile != null)
            {
                ParticleSystem.EmissionModule stoppedEmission = trail.emission;
                stoppedEmission.enabled = false;
                Destroy(projectile, 0.45f);
            }
            CreateBurst(end, _spark, color, 22, impactSize * 0.38f, impactSize * 2.2f, 0.48f);
        }

        private void CreateGroundGlyph(Vector3 center, float radius, Texture2D texture, Color color, float duration, bool expand = true)
        {
            GameObject glyph = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glyph.name = "KOA_GroundGlyph_VFX";
            Collider collider = glyph.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            glyph.transform.position = new Vector3(center.x, Mathf.Max(0.14f, center.y + 0.14f), center.z);
            glyph.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            glyph.transform.localScale = Vector3.one * radius * 2f;
            Material material = CreateEffectMaterial(texture, color);
            glyph.GetComponent<MeshRenderer>().material = material;
            StartCoroutine(FadeGlyph(glyph.transform, material, color, duration, expand));
            Destroy(glyph, duration + 0.05f);
            Destroy(material, duration + 0.1f);
        }

        private void CreateBurst(Vector3 position, Texture2D texture, Color color, int count, float size, float speed, float lifetime, bool sphereShape = true)
        {
            GameObject effect = new GameObject("KOA_ParticleBurst_VFX");
            effect.transform.position = position;
            ParticleSystem particles = ConfigureParticles(effect, texture, color, lifetime, size, 0.1f, speed, sphereShape);
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, short.MaxValue)) });
            particles.Play();
            Material material = particles.GetComponent<ParticleSystemRenderer>().material;
            Destroy(effect, lifetime + 0.8f);
            Destroy(material, lifetime + 0.9f);
        }

        private static ParticleSystem ConfigureParticles(GameObject owner, Texture2D texture, Color color, float lifetime, float size, float minSize, float speed, bool sphereShape)
        {
            ParticleSystem particles = owner.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = new ParticleSystem.MinMaxCurve(Mathf.Max(0.02f, minSize), Mathf.Max(minSize, size));
            main.startColor = new ParticleSystem.MinMaxGradient(Color.Lerp(color, Color.white, 0.22f), color);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 160;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = sphereShape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = sphereShape ? 0.35f : 0.05f;

            ParticleSystem.ColorOverLifetimeModule colorOverLife = particles.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.22f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = gradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLife = particles.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1f));

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 35;
            renderer.material = CreateEffectMaterial(texture, color);
            return particles;
        }

        private static Material CreateEffectMaterial(Texture texture, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                            ?? Shader.Find("Sprites/Default")
                            ?? Shader.Find("Universal Render Pipeline/Unlit");
            Material material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 1f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            return material;
        }

        private static IEnumerator FadeGlyph(Transform glyph, Material material, Color color, float duration, bool expand)
        {
            float elapsed = 0f;
            Vector3 baseScale = glyph.localScale;
            while (elapsed < duration && glyph != null && material != null)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                float pulse = 0.92f + Mathf.Sin(progress * Mathf.PI * 5f) * 0.08f;
                float growth = expand ? Mathf.Lerp(0.72f, 1.05f, progress) : 1f;
                glyph.localScale = baseScale * pulse * growth;
                material.color = new Color(color.r, color.g, color.b, (1f - progress) * 0.78f);
                yield return null;
            }
        }

        private static Vector3 Raised(Vector3 position, float height)
        {
            position.y = Mathf.Max(height, position.y + height);
            return position;
        }

        private static void PlaySound(AudioClip clip, Vector3 position, float volume, float pitch)
        {
            if (clip == null) return;
            GameObject sourceObject = new GameObject("KOA_CombatAudio");
            sourceObject.transform.position = position;
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, 0.5f, 1.8f);
            source.spatialBlend = 0.7f;
            source.minDistance = 3f;
            source.maxDistance = 32f;
            source.Play();
            Destroy(sourceObject, clip.length / Mathf.Max(0.5f, source.pitch) + 0.1f);
        }
    }
}
