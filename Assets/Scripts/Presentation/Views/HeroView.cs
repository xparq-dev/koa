using KOA.Core.Entities;
using KOA.Core.Input;
using KOA.Data.Enums;
using KOA.Presentation.Input;
using KOA.Presentation.UI;
using UnityEngine;

namespace KOA.Presentation.Views
{
    /// <summary>
    /// Presentation View สำหรับตัวละครผู้เล่น (Vorkas) ใน Phase 1 (Vertical Slice)
    /// ทำหน้าที่เป็นสะพานเชื่อมระหว่าง Unity Visuals กับ Simulation Core (Decoupled Core Section 1.1)
    /// </summary>
    public class HeroView : MonoBehaviour
    {
        [Header("Components & UI")]
        [SerializeField] private PCInputAdapter inputAdapter;
        [SerializeField] private WorldSpaceHealthBar healthBar;
        [SerializeField] private DummyTargetView dummyTargetView;

        [Header("VFX & Visual Effects")]
        [SerializeField] private LineRenderer ironCleaveLineRenderer;
        [SerializeField] private float vfxDuration = 0.25f;

        public HeroBase3D Hero { get; private set; }

        private float _simulationAccumulator = 0f;
        private const float SimulationTickRate = 1.0f / 30.0f; // 30 Ticks/sec ตาม Section 1.1
        private float _vfxTimer = 0f;

        public void BindHero(HeroBase3D hero)
        {
            if (Hero != null) UnsubscribeHeroEvents();

            Hero = hero;
            if (Hero != null)
            {
                SubscribeHeroEvents();
                if (healthBar != null)
                {
                    healthBar.BindTarget(transform);
                    healthBar.SetHealth(Hero.CurrentHp, Hero.EffectiveMaxHp);
                    healthBar.SetMana(Hero.CurrentMana, Hero.EffectiveMaxMana);
                }
            }
        }

        private void Awake()
        {
            if (inputAdapter == null)
            {
                inputAdapter = GetComponent<PCInputAdapter>();
            }

            if (Hero == null)
            {
                BindHero(new VorkasHero(transform.position));
            }
        }

        private void SubscribeHeroEvents()
        {
            Hero.OnHealthChanged += HandleHealthChanged;
            Hero.OnManaChanged += HandleManaChanged;

            if (Hero is VorkasHero v)
            {
                v.OnIronCleaveExecuted += HandleIronCleaveExecuted;
                v.OnBasicAttackExecuted += HandleBasicAttackExecuted;
            }
        }

        private void UnsubscribeHeroEvents()
        {
            Hero.OnHealthChanged -= HandleHealthChanged;
            Hero.OnManaChanged -= HandleManaChanged;

            if (Hero is VorkasHero v)
            {
                v.OnIronCleaveExecuted -= HandleIronCleaveExecuted;
                v.OnBasicAttackExecuted -= HandleBasicAttackExecuted;
            }
        }

        private void OnDestroy()
        {
            if (Hero != null)
            {
                Hero.OnHealthChanged -= HandleHealthChanged;
                Hero.OnManaChanged -= HandleManaChanged;
                Hero.OnIronCleaveExecuted -= HandleIronCleaveExecuted;
                Hero.OnBasicAttackExecuted -= HandleBasicAttackExecuted;
            }
        }

        private void Update()
        {
            // Simulation Core Tick Runner (รักษาจังหวะ 30 Ticks/sec ตาม Section 1.1)
            _simulationAccumulator += Time.deltaTime;
            while (_simulationAccumulator >= SimulationTickRate)
            {
                ProcessInput();
                Hero.SimulationTick(SimulationTickRate);
                _simulationAccumulator -= SimulationTickRate;
            }

            // ซิงค์ตำแหน่งและการหมุนของภาพเข้ากับ Simulation Core
            transform.position = Hero.Position;
            transform.rotation = Hero.Rotation;

            // ปิด Visual Line ของสกิลเมื่อครบเวลา
            if (_vfxTimer > 0f)
            {
                _vfxTimer -= Time.deltaTime;
                if (_vfxTimer <= 0f && ironCleaveLineRenderer != null)
                {
                    ironCleaveLineRenderer.enabled = false;
                }
            }
        }

        private void ProcessInput()
        {
            if (inputAdapter == null) return;

            InputFrame input = inputAdapter.GetCurrentInput();
            DummyTarget dummyTarget = dummyTargetView != null ? dummyTargetView.Logic : null;

            // 1. ตรวจสอบการเดิน (Click-to-move Section 7.2)
            if (input.HasMoveTarget)
            {
                Hero.SetMoveDestination(input.TargetDestination);
            }

            // 2. ตรวจสอบการโจมตีหรือใช้สกิล (Section 6.1 - 6.5)
            switch (input.CastIntent)
            {
                case CastIntent.CastAttack:
                    if (dummyTarget != null)
                    {
                        if (Hero is VorkasHero v) v.TryBasicAttack(dummyTarget);
                        else if (dummyTarget.IsAlive && Vector3.Distance(Hero.Position, dummyTarget.Position) <= Hero.AttackRange)
                        {
                            dummyTarget.TakeDamage(Hero.EffectiveAttackDamage, DamageType.Physical);
                        }
                    }
                    break;

                case CastIntent.CastSkill1:
                    if (Hero is VorkasHero v1) v1.TryCastIronCleave(input.AimVector, dummyTarget);
                    else if (Hero is ZenthisHero z1) z1.TryCastSacredHourglass(input.AimVector, dummyTarget);
                    else if (Hero is KorvaxHero k1) k1.TryCastHeavyBolt(input.AimVector, dummyTarget);
                    else if (Hero is GravitorHero g1) g1.TryCastMagneticPull(input.AimVector, dummyTarget);
                    break;

                case CastIntent.CastSkill2:
                    if (Hero is ZenthisHero z2) z2.TryCastAuraOfEternity();
                    else if (Hero is KorvaxHero k2) k2.TryCastHuntersFocus();
                    else if (Hero is GravitorHero g2) g2.TryCastRepulsionZone(dummyTarget);
                    break;

                case CastIntent.CastUltimate:
                    if (Hero is ZenthisHero z3) z3.TryCastGrandRewind();
                    else if (Hero is KorvaxHero k3) k3.TryCastBallistaOverdrive(input.AimVector, dummyTarget);
                    else if (Hero is GravitorHero g3) g3.TryCastGravityCollapse(input.AimVector, dummyTarget);
                    break;
            }

            // เคลียร์ Intent หลังจากประมวลผลในรอบ Tick
            inputAdapter.ConsumeIntent();

            // 3. ตรวจสอบการกดใช้ Active Item (Hotkeys 1 - 6)
            if (inputAdapter.ActiveItemSlotToUse >= 0)
            {
                Hero.Inventory.TryUseActive(inputAdapter.ActiveItemSlotToUse);
                inputAdapter.ConsumeActiveItemSlot();
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.SetHealth(current, max);
            }
        }

        private void HandleManaChanged(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.SetMana(current, max);
            }
        }

        private void HandleIronCleaveExecuted(Vector3 start, Vector3 end, bool hit)
        {
            Debug.Log($"[Vorkas] Iron Cleave executed from {start} to {end} (Hit: {hit})");

            if (ironCleaveLineRenderer != null)
            {
                ironCleaveLineRenderer.enabled = true;
                ironCleaveLineRenderer.SetPosition(0, start + Vector3.up * 0.5f);
                ironCleaveLineRenderer.SetPosition(1, end + Vector3.up * 0.5f);
                _vfxTimer = vfxDuration;
            }
        }

        private void HandleBasicAttackExecuted(Vector3 targetPos)
        {
            Debug.Log($"[Vorkas] Basic Attack hit target at {targetPos}");
        }

        private void OnDrawGizmosSelected()
        {
            if (Hero != null)
            {
                // วาดขอบเขตระยะโจมตีพื้นฐาน
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(Hero.Position, Hero.AttackRange);

                // วาดทิศทางและระยะของ Iron Cleave (6m)
                Gizmos.color = Color.cyan;
                Vector3 forward = Hero.Rotation * Vector3.forward;
                Gizmos.DrawRay(Hero.Position + Vector3.up * 0.2f, forward * VorkasHero.Skill1Range);
            }
        }
    }
}
