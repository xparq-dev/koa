using System.Collections.Generic;
using KOA.Core.Entities;
using KOA.Core.Input;
using KOA.Data.Enums;
using KOA.Presentation.Input;
using KOA.Presentation.UI;
using UnityEngine;

namespace KOA.Presentation.Views
{
    /// <summary>
    /// Presentation View สำหรับตัวละครผู้เล่น (Vorkas, Zenthis, Korvax, Gravitor)
    /// ทำหน้าที่เป็นสะพานเชื่อมระหว่าง Unity Visuals กับ Simulation Core (Decoupled Core Section 1.1)
    /// จัดการการเลือกเป้าหมาย (Left-Click Select), การเดินโจมตีระยะถูกต้อง (Right-Click Smart Attack),
    /// และการแสดง Attack Range Indicator ใต้เท้าตัวละคร
    /// </summary>
    public class HeroView : MonoBehaviour
    {
        [Header("Components & UI")]
        [SerializeField] private PCInputAdapter inputAdapter;
        [SerializeField] private WorldSpaceHealthBar healthBar;
        [SerializeField] private RangeIndicatorView rangeIndicator;
        [SerializeField] private DummyTargetView dummyTargetView;

        [Header("VFX & Visual Effects")]
        [SerializeField] private LineRenderer ironCleaveLineRenderer;
        [SerializeField] private float vfxDuration = 0.25f;

        public HeroBase3D Hero { get; private set; }
        public ITargetable SelectedTarget { get; private set; }

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

                if (rangeIndicator == null)
                {
                    rangeIndicator = GetComponent<RangeIndicatorView>() ?? gameObject.AddComponent<RangeIndicatorView>();
                }
                rangeIndicator.BindHero(Hero);
            }
        }

        public void SetHealthBar(WorldSpaceHealthBar bar)
        {
            healthBar = bar;
            if (healthBar != null && Hero != null)
            {
                healthBar.BindTarget(transform);
                healthBar.SetHealth(Hero.CurrentHp, Hero.EffectiveMaxHp);
                healthBar.SetMana(Hero.CurrentMana, Hero.EffectiveMaxMana);
            }
        }

        public void SetSelectedTarget(ITargetable target)
        {
            SelectedTarget = target;
            if (rangeIndicator != null)
            {
                rangeIndicator.SetSelectedTarget(target);
            }
        }

        private void Awake()
        {
            if (inputAdapter == null)
            {
                inputAdapter = GetComponent<PCInputAdapter>();
            }

            if (rangeIndicator == null)
            {
                rangeIndicator = GetComponent<RangeIndicatorView>() ?? gameObject.AddComponent<RangeIndicatorView>();
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
                UnsubscribeHeroEvents();
            }
            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
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

        private ITargetable GetTargetFromCollider(Collider col)
        {
            if (col == null) return null;

            var minionView = col.GetComponentInParent<MinionView>();
            if (minionView != null && minionView.Logic != null && minionView.Logic.IsAlive) 
                return minionView.Logic;

            var towerView = col.GetComponentInParent<TowerView>();
            if (towerView != null && towerView.Logic != null && !towerView.Logic.IsDestroyed) 
                return towerView.Logic;

            var dummy = col.GetComponentInParent<DummyTargetView>();
            if (dummy != null && dummy.Logic != null && dummy.Logic.IsAlive) 
                return dummy.Logic;

            var heroView = col.GetComponentInParent<HeroView>();
            if (heroView != null && heroView.Hero != null && heroView.Hero != Hero && heroView.Hero.IsAlive) 
                return heroView.Hero;

            var bootstrap = KOA.Presentation.Testing.VerticalSliceBootstrap.Instance ?? FindAnyObjectByType<KOA.Presentation.Testing.VerticalSliceBootstrap>();
            if (bootstrap != null && bootstrap.BotHero != null && bootstrap.BotHero.IsAlive)
            {
                if (col.gameObject.name.Contains("Bot"))
                {
                    return bootstrap.BotHero;
                }
            }

            return null;
        }

        private List<ITargetable> GetAllTargets(bool enemyOnly = false)
        {
            var list = new List<ITargetable>();
            if (dummyTargetView != null && dummyTargetView.Logic != null && dummyTargetView.Logic.IsAlive)
            {
                list.Add(dummyTargetView.Logic);
            }

            var bootstrap = KOA.Presentation.Testing.VerticalSliceBootstrap.Instance ?? FindAnyObjectByType<KOA.Presentation.Testing.VerticalSliceBootstrap>();
            if (bootstrap == null || bootstrap.MatchSimulation == null) return list;

            int enemyTeam = Hero.TeamId == 0 ? 1 : 0;

            if (bootstrap.BotHero != null && bootstrap.BotHero.IsAlive)
            {
                if (!enemyOnly || bootstrap.BotHero.TeamId == enemyTeam)
                {
                    list.Add(bootstrap.BotHero);
                }
            }

            foreach (var minion in bootstrap.MatchSimulation.ActiveMinions)
            {
                if (minion.IsAlive && (!enemyOnly || minion.TeamId == enemyTeam))
                {
                    list.Add(minion);
                }
            }

            var enemyTowers = enemyTeam == 0 ? bootstrap.MatchSimulation.BlueTowers : bootstrap.MatchSimulation.RedTowers;
            if (enemyOnly)
            {
                foreach (var tower in enemyTowers)
                {
                    if (!tower.IsDestroyed && !tower.IsInvulnerable)
                    {
                        list.Add(tower);
                    }
                }
            }
            else
            {
                foreach (var tower in bootstrap.MatchSimulation.BlueTowers)
                {
                    if (!tower.IsDestroyed) list.Add(tower);
                }
                foreach (var tower in bootstrap.MatchSimulation.RedTowers)
                {
                    if (!tower.IsDestroyed) list.Add(tower);
                }
            }

            return list;
        }

        private ITargetable FindTargetNear(Vector3 position, float searchRadius = 8.0f, bool enemyOnly = true)
        {
            var targets = GetAllTargets(enemyOnly);
            ITargetable best = null;
            float closestSqr = float.MaxValue;
            float maxSqr = searchRadius * searchRadius;

            foreach (var e in targets)
            {
                float sqr = (e.Position - position).sqrMagnitude;
                if (sqr <= maxSqr && sqr < closestSqr)
                {
                    closestSqr = sqr;
                    best = e;
                }
            }
            return best;
        }

        private void ProcessInput()
        {
            if (inputAdapter == null) return;

            InputFrame input = inputAdapter.GetCurrentInput();

            // 1. การคลิกซ้าย (Left-Click): เลือกเป้าหมาย (Unit Selection)
            if (inputAdapter.ConsumeLeftClick(out Collider leftCol, out Vector3 leftPos))
            {
                ITargetable clicked = GetTargetFromCollider(leftCol) ?? FindTargetNear(leftPos, 2.0f, enemyOnly: false);
                SetSelectedTarget(clicked);
            }

            // 2. การคลิกขวา (Right-Click): สั่งเดิน (Ground) หรือสั่งตี (Enemy Unit)
            if (inputAdapter.ConsumeRightClick(out Collider rightCol, out Vector3 rightPos))
            {
                ITargetable clickedEnemy = GetTargetFromCollider(rightCol) ?? FindTargetNear(rightPos, 2.5f, enemyOnly: true);
                if (clickedEnemy != null && clickedEnemy.TeamId != Hero.TeamId)
                {
                    SetSelectedTarget(clickedEnemy);
                    Hero.CurrentAttackTarget = clickedEnemy;
                }
                else
                {
                    Hero.CurrentAttackTarget = null;
                    Hero.SetMoveDestination(input.TargetDestination);
                }
            }
            else if (input.HasMoveTarget && Hero.CurrentAttackTarget == null)
            {
                Hero.SetMoveDestination(input.TargetDestination);
            }

            // 3. ตรวจสอบการโจมตีหรือใช้สกิล (Section 6.1 - 6.5)
            switch (input.CastIntent)
            {
                case CastIntent.CastAttack:
                    ITargetable attackTarget = FindTargetNear(input.AimVector, 5.0f, enemyOnly: true) ?? FindTargetNear(Hero.Position, 20.0f, enemyOnly: true);
                    if (attackTarget != null)
                    {
                        SetSelectedTarget(attackTarget);
                        Hero.CurrentAttackTarget = attackTarget;
                    }
                    break;

                case CastIntent.CastSkill1:
                    var enemiesS1 = GetAllTargets(enemyOnly: true);
                    if (Hero is VorkasHero v1) v1.TryCastIronCleave(input.AimVector, enemiesS1);
                    else if (Hero is ZenthisHero z1) z1.TryCastSacredHourglass(input.AimVector, FindTargetNear(input.AimVector, 6.0f, enemyOnly: true));
                    else if (Hero is KorvaxHero k1) k1.TryCastHeavyBolt(input.AimVector, FindTargetNear(input.AimVector, 10.0f, enemyOnly: true));
                    else if (Hero is GravitorHero g1) g1.TryCastMagneticPull(input.AimVector, FindTargetNear(input.AimVector, 8.0f, enemyOnly: true));
                    break;

                case CastIntent.CastSkill2:
                    if (Hero is VorkasHero v2) v2.TryCastVanguardsWill();
                    else if (Hero is ZenthisHero z2) z2.TryCastAuraOfEternity();
                    else if (Hero is KorvaxHero k2) k2.TryCastHuntersFocus();
                    else if (Hero is GravitorHero g2)
                    {
                        ITargetable skill2Target = FindTargetNear(Hero.Position, 4.0f, enemyOnly: true);
                        g2.TryCastRepulsionZone(skill2Target);
                    }
                    break;

                case CastIntent.CastSkill3:
                    var enemiesS3 = GetAllTargets(enemyOnly: true);
                    if (Hero is VorkasHero v3) v3.TryCastSeismicSlam(input.AimVector, enemiesS3);
                    else if (Hero is ZenthisHero z3) z3.TryCastTemporalRift(input.AimVector, enemiesS3);
                    else if (Hero is KorvaxHero k3) k3.TryCastConcussiveBlast(input.AimVector, FindTargetNear(input.AimVector, 5.0f, enemyOnly: true));
                    else if (Hero is GravitorHero g3) g3.TryCastGravitonWell(input.AimVector, enemiesS3);
                    break;

                case CastIntent.CastUltimate:
                    var enemiesUlt = GetAllTargets(enemyOnly: true);
                    if (Hero is VorkasHero v4) v4.TryCastRebellionImpact(input.AimVector, enemiesUlt);
                    else if (Hero is ZenthisHero z4) z4.TryCastGrandRewind();
                    else if (Hero is KorvaxHero k4) k4.TryCastBallistaOverdrive(input.AimVector, FindTargetNear(input.AimVector, 18.0f, enemyOnly: true));
                    else if (Hero is GravitorHero g4) g4.TryCastGravityCollapse(input.AimVector, FindTargetNear(input.AimVector, 10.0f, enemyOnly: true));
                    break;
            }

            // 4. ระบบติดตามและเข้าตีเป้าหมายอัตโนมัติ (MOBA Auto-Attack / Walk-in-range)
            // คุมระยะหยุดให้หยุดที่ขอบระยะโจมตี ไม่เดินเข้าไปชนหรือทะลุเข้ากลางตัว Collider
            if (Hero.CurrentAttackTarget != null)
            {
                if (Hero.CurrentAttackTarget.IsAlive)
                {
                    float dist = Vector3.Distance(Hero.Position, Hero.CurrentAttackTarget.Position);
                    float attackThreshold = Hero.AttackRange + Hero.CurrentAttackTarget.Radius;
                    if (dist <= attackThreshold)
                    {
                        Hero.StopMoving();

                        // หันหน้าเข้าหาเป้าหมาย
                        Vector3 toTarget = Hero.CurrentAttackTarget.Position - Hero.Position;
                        toTarget.y = 0;
                        if (toTarget.sqrMagnitude > 0.001f)
                        {
                            Hero.Rotation = Quaternion.LookRotation(toTarget);
                        }

                        if (Hero is VorkasHero v) v.TryBasicAttack(Hero.CurrentAttackTarget);
                        else if (Hero is ZenthisHero z) z.TryBasicAttack(Hero.CurrentAttackTarget);
                        else if (Hero is KorvaxHero k) k.TryBasicAttack(Hero.CurrentAttackTarget);
                        else if (Hero is GravitorHero g) g.TryBasicAttack(Hero.CurrentAttackTarget);
                    }
                    else
                    {
                        // เดินมุ่งหน้าหาเป้าหมาย แต่หยุดที่ระยะปลอดภัย (85% ของระยะโจมตี + รัศมีตัวเป้าหมาย) ไม่เดินชนตัว
                        Vector3 dirToHero = (Hero.Position - Hero.CurrentAttackTarget.Position);
                        dirToHero.y = 0;
                        if (dirToHero.sqrMagnitude < 0.001f) dirToHero = Vector3.forward;
                        else dirToHero.Normalize();

                        float desiredDistance = Mathf.Max(0.8f, attackThreshold * 0.85f);
                        Vector3 stopPos = Hero.CurrentAttackTarget.Position + dirToHero * desiredDistance;
                        Hero.SetMoveDestination(stopPos);
                    }
                }
                else
                {
                    Hero.CurrentAttackTarget = null;
                    Hero.StopMoving();
                }
            }

            // เคลียร์ Intent หลังจากประมวลผลในรอบ Tick
            inputAdapter.ConsumeIntent();

            // 5. ตรวจสอบการกดใช้ Active Item (Hotkeys 1 - 6)
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

            if (ironCleaveLineRenderer == null)
            {
                ironCleaveLineRenderer = gameObject.AddComponent<LineRenderer>();
                ironCleaveLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                ironCleaveLineRenderer.startColor = new Color(0.2f, 0.9f, 1f, 0.9f);
                ironCleaveLineRenderer.endColor = new Color(0.7f, 1f, 1f, 0.3f);
                ironCleaveLineRenderer.startWidth = 1.5f;
                ironCleaveLineRenderer.endWidth = 1.8f;
                ironCleaveLineRenderer.positionCount = 2;
            }

            ironCleaveLineRenderer.enabled = true;
            ironCleaveLineRenderer.SetPosition(0, start + Vector3.up * 0.5f);
            ironCleaveLineRenderer.SetPosition(1, end + Vector3.up * 0.5f);
            _vfxTimer = 0.35f;
        }

        private void HandleBasicAttackExecuted(Vector3 targetPos)
        {
            Debug.Log($"[Vorkas] Basic Attack hit target at {targetPos}");
            DamagePopupManager.Instance?.ShowDamage(targetPos, Hero != null ? Hero.EffectiveAttackDamage : 50f, DamageType.Physical);
        }

        private void OnDrawGizmosSelected()
        {
            if (Hero != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(Hero.Position, Hero.AttackRange);

                Gizmos.color = Color.cyan;
                Vector3 forward = Hero.Rotation * Vector3.forward;
                Gizmos.DrawRay(Hero.Position + Vector3.up * 0.2f, forward * VorkasHero.Skill1Range);
            }
        }
    }
}
