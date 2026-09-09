using System.Collections.Generic;
using KOA.Core.Entities;
using KOA.Core.Vision;
using KOA.Core.Input;
using KOA.Core.World;
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
        [SerializeField] private MoveCommandIndicatorView moveCommandIndicator;

        public HeroBase3D Hero { get; private set; }
        public ITargetable SelectedTarget { get; private set; }

        private float _simulationAccumulator = 0f;
        private const float SimulationTickRate = 1.0f / 30.0f; // 30 Ticks/sec ตาม Section 1.1
        private Animator _animator;
        private bool _hasLocomotionRate;
        private AudioSource _footstepSource;
        private AudioClip[] _footstepClips;
        private float _footstepTimer;
        private static readonly int MoveSpeedParameter = Animator.StringToHash("MoveSpeed");
        private static readonly int LocomotionRateParameter = Animator.StringToHash("LocomotionRate");
        private static readonly int AttackParameter = Animator.StringToHash("Attack");
        private static readonly int CastParameter = Animator.StringToHash("Cast");
        private static readonly int DeadParameter = Animator.StringToHash("Dead");

        public void BindHero(HeroBase3D hero)
        {
            if (Hero != null) UnsubscribeHeroEvents();

            Hero = hero;
            _animator = GetComponentInChildren<Animator>(true);
            _hasLocomotionRate = HasAnimatorParameter(_animator, LocomotionRateParameter);
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

            if (moveCommandIndicator == null)
            {
                moveCommandIndicator = GetComponent<MoveCommandIndicatorView>() ?? gameObject.AddComponent<MoveCommandIndicatorView>();
            }

            _footstepSource = gameObject.AddComponent<AudioSource>();
            _footstepSource.playOnAwake = false;
            _footstepSource.spatialBlend = 0.72f;
            _footstepSource.minDistance = 2.5f;
            _footstepSource.maxDistance = 24f;
            _footstepSource.volume = 0.22f;
            _footstepClips = new[]
            {
                Resources.Load<AudioClip>("KOA/Audio/footstep02"),
                Resources.Load<AudioClip>("KOA/Audio/footstep07")
            };

            if (Hero == null)
            {
                BindHero(new VorkasHero(transform.position));
            }
        }

        private void SubscribeHeroEvents()
        {
            Hero.OnHealthChanged += HandleHealthChanged;
            Hero.OnManaChanged += HandleManaChanged;
            Hero.OnBasicAttackExecuted += HandleBasicAttackExecuted;

            if (Hero is VorkasHero v)
            {
                v.OnIronCleaveExecuted += HandleIronCleaveExecuted;
                v.OnVanguardsWillExecuted += HandleSkillCast;
                v.OnSeismicSlamExecuted += HandleAreaSkillCast;
                v.OnRebellionImpactExecuted += HandleAreaSkillCast;
            }
            else if (Hero is ZenthisHero z)
            {
                z.OnSacredHourglassCast += HandlePositionRadiusSkillCast;
                z.OnAuraOfEternityCast += HandleSkillCast;
                z.OnTemporalRiftCast += HandleLineSkillCast;
                z.OnGrandRewindCast += HandlePositionRadiusSkillCast;
            }
            else if (Hero is KorvaxHero k)
            {
                k.OnHeavyBoltFired += HandleLineSkillCast;
                k.OnHuntersFocusActivated += HandleSkillCast;
                k.OnConcussiveBlastFired += HandlePositionHitSkillCast;
                k.OnBallistaOverdriveFired += HandleLineSkillCast;
            }
            else if (Hero is GravitorHero g)
            {
                g.OnMagneticPullCast += HandlePositionSkillCast;
                g.OnRepulsionZoneCast += HandleRadiusSkillCast;
                g.OnGravitonWellCast += HandleAreaSkillCast;
                g.OnGravityCollapseCast += HandlePositionRadiusSkillCast;
            }
        }

        private void UnsubscribeHeroEvents()
        {
            Hero.OnHealthChanged -= HandleHealthChanged;
            Hero.OnManaChanged -= HandleManaChanged;
            Hero.OnBasicAttackExecuted -= HandleBasicAttackExecuted;

            if (Hero is VorkasHero v)
            {
                v.OnIronCleaveExecuted -= HandleIronCleaveExecuted;
                v.OnVanguardsWillExecuted -= HandleSkillCast;
                v.OnSeismicSlamExecuted -= HandleAreaSkillCast;
                v.OnRebellionImpactExecuted -= HandleAreaSkillCast;
            }
            else if (Hero is ZenthisHero z)
            {
                z.OnSacredHourglassCast -= HandlePositionRadiusSkillCast;
                z.OnAuraOfEternityCast -= HandleSkillCast;
                z.OnTemporalRiftCast -= HandleLineSkillCast;
                z.OnGrandRewindCast -= HandlePositionRadiusSkillCast;
            }
            else if (Hero is KorvaxHero k)
            {
                k.OnHeavyBoltFired -= HandleLineSkillCast;
                k.OnHuntersFocusActivated -= HandleSkillCast;
                k.OnConcussiveBlastFired -= HandlePositionHitSkillCast;
                k.OnBallistaOverdriveFired -= HandleLineSkillCast;
            }
            else if (Hero is GravitorHero g)
            {
                g.OnMagneticPullCast -= HandlePositionSkillCast;
                g.OnRepulsionZoneCast -= HandleRadiusSkillCast;
                g.OnGravitonWellCast -= HandleAreaSkillCast;
                g.OnGravityCollapseCast -= HandlePositionRadiusSkillCast;
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
            UpdateAnimationState();
            UpdateFootstepAudio();
            UpdateHealthBarVisibility();

        }

        private void UpdateFootstepAudio()
        {
            if (_footstepSource == null || _footstepClips == null || Hero == null || !Hero.IsAlive || !Hero.IsMoving)
            {
                _footstepTimer = 0f;
                return;
            }

            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer > 0f) return;
            AudioClip clip = _footstepClips[Random.Range(0, _footstepClips.Length)];
            if (clip != null)
            {
                _footstepSource.pitch = Random.Range(0.94f, 1.06f);
                _footstepSource.PlayOneShot(clip);
            }
            _footstepTimer = Mathf.Lerp(0.58f, 0.42f, Mathf.InverseLerp(3.8f, 5.8f, Hero.EffectiveMoveSpeed));
        }

        private void UpdateHealthBarVisibility()
        {
            if (healthBar == null || Hero == null) return;

            var bootstrap = KOA.Presentation.Testing.VerticalSliceBootstrap.Instance;
            HeroBase3D observer = bootstrap != null ? bootstrap.CurrentPlayerHero : null;
            bool visible = observer == null || VisionSystem.IsHeroVisible(observer, Hero);
            healthBar.SetVisible(visible);
        }

        private void UpdateAnimationState()
        {
            if (_animator == null || Hero == null) return;
            _animator.SetFloat(MoveSpeedParameter, Hero.IsMoving ? 1f : 0f, 0.08f, Time.deltaTime);
            float locomotionRate = Mathf.Clamp(Hero.EffectiveMoveSpeed / 4.3f, 0.78f, 1.25f);
            if (_hasLocomotionRate) _animator.SetFloat(LocomotionRateParameter, locomotionRate);
            _animator.SetBool(DeadParameter, !Hero.IsAlive);
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

        private void TriggerAttackAnimation()
        {
            if (_animator != null) _animator.SetTrigger(AttackParameter);
        }

        private void TriggerCastAnimation()
        {
            if (_animator != null) _animator.SetTrigger(CastParameter);
        }

        public void ShowMoveCommand(Vector3 position, bool attackCommand)
        {
            moveCommandIndicator?.Show(position, attackCommand);
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

        private ITargetable FindEnemyTargetUnderCursor(Vector3 aimPosition)
        {
            ITargetable hoveredTarget = GetTargetFromCollider(inputAdapter.HoveredCollider);
            if (IsValidEnemyTarget(hoveredTarget)) return hoveredTarget;

            const float cursorAssistRadius = 0.45f;
            ITargetable best = null;
            float bestSqr = float.MaxValue;
            foreach (ITargetable target in GetAllTargets(enemyOnly: true))
            {
                if (!IsValidEnemyTarget(target)) continue;

                Vector3 delta = target.Position - aimPosition;
                delta.y = 0f;
                float allowedRadius = target.Radius + cursorAssistRadius;
                float sqr = delta.sqrMagnitude;
                if (sqr <= allowedRadius * allowedRadius && sqr < bestSqr)
                {
                    best = target;
                    bestSqr = sqr;
                }
            }

            return best;
        }

        private bool IsValidEnemyTarget(ITargetable target)
        {
            return target != null
                && target.IsAlive
                && (target.TeamId != Hero.TeamId || target.TeamId == -1);
        }

        private static bool IsWithinCastRange(HeroBase3D hero, ITargetable target, float range)
        {
            return hero != null
                && target != null
                && Vector3.Distance(hero.Position, target.Position) <= range + target.Radius;
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
                    moveCommandIndicator?.Show(clickedEnemy.Position, true);
                }
                else
                {
                    Hero.CurrentAttackTarget = null;
                    Hero.SetMoveDestination(input.TargetDestination);
                    moveCommandIndicator?.Show(input.TargetDestination, false);
                }
            }
            else if (input.HasMoveTarget && Hero.CurrentAttackTarget == null)
            {
                Hero.SetMoveDestination(input.TargetDestination);
            }

            // 3. ตรวจสอบการโจมตีหรือใช้สกิล (Section 6.1 - 6.5)
            bool castAttempted = false;
            bool castSucceeded = false;
            int attemptedSkillIndex = -1;
            string rejectionReason = null;
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
                    castAttempted = true;
                    attemptedSkillIndex = 0;
                    var enemiesS1 = GetAllTargets(enemyOnly: true);
                    if (Hero is VorkasHero v1) castSucceeded = v1.TryCastIronCleave(input.AimVector, enemiesS1);
                    else if (Hero is ZenthisHero z1)
                    {
                        if (!ArenaBounds.Contains(input.AimVector)) rejectionReason = "Q requires a ground point inside the arena";
                        else castSucceeded = z1.TryCastSacredHourglass(input.AimVector, FindTargetNear(input.AimVector, ZenthisHero.Skill1Radius, enemyOnly: true));
                    }
                    else if (Hero is KorvaxHero k1) castSucceeded = k1.TryCastHeavyBolt(input.AimVector, FindTargetNear(input.AimVector, 10.0f, enemyOnly: true));
                    else if (Hero is GravitorHero g1)
                    {
                        ITargetable target = FindEnemyTargetUnderCursor(input.AimVector);
                        if (target == null) rejectionReason = "Q requires an enemy target under the cursor";
                        else
                        {
                            SetSelectedTarget(target);
                            if (!IsWithinCastRange(Hero, target, GravitorHero.Skill1Range)) rejectionReason = "Q target is out of range";
                            else castSucceeded = g1.TryCastMagneticPull(input.AimVector, target);
                        }
                    }
                    break;

                case CastIntent.CastSkill2:
                    castAttempted = true;
                    attemptedSkillIndex = 1;
                    if (Hero is VorkasHero v2) castSucceeded = v2.TryCastVanguardsWill();
                    else if (Hero is ZenthisHero z2) castSucceeded = z2.TryCastAuraOfEternity();
                    else if (Hero is KorvaxHero k2) castSucceeded = k2.TryCastHuntersFocus();
                    else if (Hero is GravitorHero g2)
                    {
                        ITargetable skill2Target = FindTargetNear(Hero.Position, 4.0f, enemyOnly: true);
                        castSucceeded = g2.TryCastRepulsionZone(skill2Target);
                    }
                    break;

                case CastIntent.CastSkill3:
                    castAttempted = true;
                    attemptedSkillIndex = 2;
                    var enemiesS3 = GetAllTargets(enemyOnly: true);
                    if (Hero is VorkasHero v3) castSucceeded = v3.TryCastSeismicSlam(input.AimVector, enemiesS3);
                    else if (Hero is ZenthisHero z3) castSucceeded = z3.TryCastTemporalRift(input.AimVector, enemiesS3);
                    else if (Hero is KorvaxHero k3)
                    {
                        ITargetable target = FindEnemyTargetUnderCursor(input.AimVector);
                        if (target == null) rejectionReason = "E requires an enemy target under the cursor";
                        else
                        {
                            SetSelectedTarget(target);
                            if (!IsWithinCastRange(Hero, target, KorvaxHero.Skill3Range)) rejectionReason = "E target is out of range";
                            else castSucceeded = k3.TryCastConcussiveBlast(input.AimVector, target);
                        }
                    }
                    else if (Hero is GravitorHero g3)
                    {
                        if (!ArenaBounds.Contains(input.AimVector)) rejectionReason = "E requires a ground point inside the arena";
                        else castSucceeded = g3.TryCastGravitonWell(input.AimVector, enemiesS3);
                    }
                    break;

                case CastIntent.CastUltimate:
                    castAttempted = true;
                    attemptedSkillIndex = 3;
                    var enemiesUlt = GetAllTargets(enemyOnly: true);
                    if (Hero is VorkasHero v4)
                    {
                        if (!ArenaBounds.Contains(input.AimVector)) rejectionReason = "R requires a ground point inside the arena";
                        else castSucceeded = v4.TryCastRebellionImpact(input.AimVector, enemiesUlt);
                    }
                    else if (Hero is ZenthisHero z4) castSucceeded = z4.TryCastGrandRewind();
                    else if (Hero is KorvaxHero k4) castSucceeded = k4.TryCastBallistaOverdrive(input.AimVector, FindTargetNear(input.AimVector, 18.0f, enemyOnly: true));
                    else if (Hero is GravitorHero g4)
                    {
                        if (!ArenaBounds.Contains(input.AimVector)) rejectionReason = "R requires a ground point inside the arena";
                        else castSucceeded = g4.TryCastGravityCollapse(input.AimVector, FindTargetNear(input.AimVector, GravitorHero.UltimateRadius, enemyOnly: true));
                    }
                    break;
            }

            if (castAttempted && !castSucceeded)
                MatchHUD.NotifyAbilityRejected(Hero, attemptedSkillIndex, rejectionReason);

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

                        Hero.TryBasicAttack(Hero.CurrentAttackTarget);
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
            TriggerCastAnimation();
        }

        private void HandleBasicAttackExecuted(Vector3 targetPos)
        {
            TriggerAttackAnimation();
        }

        private void HandleSkillCast() => TriggerCastAnimation();
        private void HandlePositionSkillCast(Vector3 position) => TriggerCastAnimation();
        private void HandleRadiusSkillCast(float radius) => TriggerCastAnimation();
        private void HandlePositionHitSkillCast(Vector3 position, bool hit) => TriggerCastAnimation();
        private void HandlePositionRadiusSkillCast(Vector3 position, float radius) => TriggerCastAnimation();
        private void HandleLineSkillCast(Vector3 start, Vector3 end, bool hit) => TriggerCastAnimation();
        private void HandleAreaSkillCast(Vector3 center, float radius, bool hit) => TriggerCastAnimation();

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
