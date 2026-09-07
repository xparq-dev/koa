using KOA.Core.Entities;
using KOA.Core.FSM;
using KOA.Core.Items;
using KOA.Core.Match;
using KOA.Core.Minions;
using KOA.Core.Structures;
using KOA.Data.Models;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KOA.Core.AI
{
    /// <summary>
    /// Multi-Tier AI Bot Brain รองรับ Easy, Medium, และ Hard (Section 9)
    /// รองรับฮีโร่ครบทั้ง 4 ตัว (Vorkas, Zenthis, Korvax, Gravitor) ตาม Phase 3
    /// ปรับปรุงระบบการประเมินสถานการณ์ (Situational Awareness):
    /// - การหลบหลีกและไม่วิ่งเข้าป้อมศัตรู (Tower Danger Zone & Aggro Evasion)
    /// - การเดินร่วมกับเวฟครีบและดันเลน (Minion Wave Coordination & Laning)
    /// - การเว้นระยะและการ Kiting สำหรับฮีโร่ตีไกล (Spacing & Kiting for Ranged)
    /// - การซื้อไอเทมอัตโนมัติเมื่อกลับฐาน (Economy & Item Buying)
    /// - การคำนวณ Lead Vector Prediction (Hard Tier)
    /// </summary>
    public class ModularBotBrain
    {
        public HeroBase3D BotHero { get; set; }
        public Vector3 HomeFountainPosition { get; set; }
        public Vector3 EnemyNexusPosition { get; set; }
        public MatchSimulation Match { get; set; }
        public BotDifficulty Difficulty { get; set; } = BotDifficulty.Medium;

        public HeroBase3D TargetEnemyHero { get; set; }
        public DummyTarget TargetDummy { get; set; }

        public StateMachine<ModularBotBrain, BotStateKey> FSM { get; private set; }
        public BotStateKey CurrentState => FSM != null ? FSM.CurrentStateKey : BotStateKey.Idle;

        // ความเร็วของศัตรูเพื่อการคำนวณ Prediction (Hard Tier)
        private Vector3 _lastEnemyPos;
        private Vector3 _enemyVelocity;

        // Reaction Timer สำหรับ Easy Tier (ชะลอการตัดสินใจ)
        private float _easyReactionTimer = 0f;

        // Shop System สำหรับบอทซื้อไอเทม
        private readonly ShopSystem _botShop = new ShopSystem();

        public ModularBotBrain(HeroBase3D botHero, Vector3 homeFountain, Vector3 enemyNexus, MatchSimulation match = null, BotDifficulty difficulty = BotDifficulty.Medium)
        {
            BotHero = botHero;
            HomeFountainPosition = homeFountain;
            EnemyNexusPosition = enemyNexus;
            Match = match;
            Difficulty = difficulty;

            InitializeFSM();
        }

        private void InitializeFSM()
        {
            FSM = new StateMachine<ModularBotBrain, BotStateKey>(this);
            FSM.RegisterState(BotStateKey.Idle, new IdleState());
            FSM.RegisterState(BotStateKey.LanePushing, new LanePushingState());
            FSM.RegisterState(BotStateKey.AttackingHero, new AttackingHeroState());
            FSM.RegisterState(BotStateKey.Retreating, new RetreatingState());

            FSM.ChangeState(BotStateKey.LanePushing);
        }

        public void SimulationTick(float deltaTime)
        {
            if (BotHero == null || !BotHero.IsAlive) return;

            // Dota 2 Style: Auto-allocate skill points for Bot
            if (BotHero.AvailableSkillPoints > 0)
            {
                AutoAllocateSkillPoints();
            }

            // Track Enemy Velocity สำหรับ Hard Tier Prediction Algorithm
            if (TargetEnemyHero != null && TargetEnemyHero.IsAlive && deltaTime > 0f)
            {
                Vector3 currentVel = (TargetEnemyHero.Position - _lastEnemyPos) / deltaTime;
                _enemyVelocity = Vector3.Lerp(_enemyVelocity, currentVel, deltaTime * 5f);
                _lastEnemyPos = TargetEnemyHero.Position;
            }

            // เกณฑ์การตัดสินใจถอย (Retreat Threshold) ตามระดับความยาก (Section 9)
            float hpRatio = BotHero.CurrentHp / BotHero.EffectiveMaxHp;
            float retreatThreshold = Difficulty switch
            {
                BotDifficulty.Easy => 0.15f,   // Easy: ยืนสู้จนเลือดต่ำ 15% ถึงจะถอย
                BotDifficulty.Medium => 0.35f, // Medium: ถอยที่ 35%
                BotDifficulty.Hard => 0.45f,   // Hard: ถอยที่ 45% หรือเมื่อเสียเปรียบ
                _ => 0.35f
            };

            // หากโดนป้อมเล็งและไม่มีครีบช่วยแทงค์ ให้ถอยทันที
            if (IsDangerousTowerZone(BotHero.Position, out _) && FSM.CurrentStateKey != BotStateKey.Retreating)
            {
                FSM.ChangeState(BotStateKey.Retreating);
                return;
            }

            if (hpRatio < retreatThreshold && FSM.CurrentStateKey != BotStateKey.Retreating)
            {
                FSM.ChangeState(BotStateKey.Retreating);
            }

            FSM.Update(deltaTime);
        }

        // ==========================================
        // SITUATIONAL AWARENESS HELPERS
        // ==========================================

        public TowerEntity GetClosestAliveEnemyTower()
        {
            if (Match == null) return null;
            var enemyTowers = BotHero.TeamId == 1 ? Match.BlueTowers : Match.RedTowers;
            TowerEntity closest = null;
            float minDist = float.MaxValue;
            for (int i = 0; i < enemyTowers.Count; i++)
            {
                var t = enemyTowers[i];
                if (t != null && t.IsAlive)
                {
                    float d = Vector3.Distance(BotHero.Position, t.Position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closest = t;
                    }
                }
            }
            return closest;
        }

        public TowerEntity GetClosestAliveFriendlyTower()
        {
            if (Match == null) return null;
            var friendlyTowers = BotHero.TeamId == 1 ? Match.RedTowers : Match.BlueTowers;
            TowerEntity closest = null;
            float minDist = float.MaxValue;
            for (int i = 0; i < friendlyTowers.Count; i++)
            {
                var t = friendlyTowers[i];
                if (t != null && t.IsAlive)
                {
                    float d = Vector3.Distance(BotHero.Position, t.Position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closest = t;
                    }
                }
            }
            return closest;
        }

        public int CountFriendlyMinionsNear(Vector3 position, float radius)
        {
            if (Match == null) return 0;
            int count = 0;
            for (int i = 0; i < Match.ActiveMinions.Count; i++)
            {
                var m = Match.ActiveMinions[i];
                if (m != null && m.IsAlive && m.TeamId == BotHero.TeamId)
                {
                    if (Vector3.Distance(m.Position, position) <= radius)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public bool IsDangerousTowerZone(Vector3 position, out TowerEntity dangerousTower)
        {
            dangerousTower = GetClosestAliveEnemyTower();
            if (dangerousTower == null) return false;

            float dist = Vector3.Distance(position, dangerousTower.Position);
            float towerRange = dangerousTower.Stats != null ? dangerousTower.Stats.AttackRange : 9.5f;

            // ตรวจสอบว่ามีครีบฝั่งเราช่วยแทงค์ป้อมหรือไม่
            int friendlyMinions = CountFriendlyMinionsNear(dangerousTower.Position, towerRange);

            // ถ้าไม่มีครีบฝั่งเราเลย: รัศมี 11.5m คือเขตห้ามเข้าเด็ดขาด!
            if (friendlyMinions == 0 && dist <= (towerRange + 1.5f))
            {
                return true;
            }

            return false;
        }

        public MinionEntity GetFriendlyMinionFrontline()
        {
            if (Match == null) return null;
            MinionEntity best = null;
            float closestDistToEnemyNexus = float.MaxValue;

            for (int i = 0; i < Match.ActiveMinions.Count; i++)
            {
                var m = Match.ActiveMinions[i];
                if (m != null && m.IsAlive && m.TeamId == BotHero.TeamId)
                {
                    float d = Vector3.Distance(m.Position, EnemyNexusPosition);
                    if (d < closestDistToEnemyNexus)
                    {
                        closestDistToEnemyNexus = d;
                        best = m;
                    }
                }
            }
            return best;
        }

        public MinionEntity GetClosestEnemyMinion(float maxSearchRange = 12.0f)
        {
            if (Match == null) return null;
            int enemyTeam = BotHero.TeamId == 1 ? 0 : 1;
            MinionEntity closest = null;
            float minDist = float.MaxValue;

            for (int i = 0; i < Match.ActiveMinions.Count; i++)
            {
                var m = Match.ActiveMinions[i];
                if (m != null && m.IsAlive && m.TeamId == enemyTeam)
                {
                    float d = Vector3.Distance(BotHero.Position, m.Position);
                    if (d <= maxSearchRange && d < minDist)
                    {
                        minDist = d;
                        closest = m;
                    }
                }
            }
            return closest;
        }

        public void MoveSafelyTowards(Vector3 destination)
        {
            // ถ้าจุดหมายเสี่ยงต่อการถูกป้อมศัตรูยิงโดยไม่มีครีบแทงค์ ให้ชะลอและเว้นระยะปลอดภัย
            if (IsDangerousTowerZone(destination, out TowerEntity dangerousTower))
            {
                float towerRange = dangerousTower.Stats != null ? dangerousTower.Stats.AttackRange : 9.5f;
                Vector3 dirAway = (BotHero.Position - dangerousTower.Position).normalized;
                if (dirAway.sqrMagnitude < 0.001f) dirAway = Vector3.back;

                Vector3 safePos = dangerousTower.Position + dirAway * (towerRange + 2.5f);
                safePos.y = BotHero.Position.y;
                BotHero.SetMoveDestination(safePos);
                return;
            }

            BotHero.SetMoveDestination(destination);
        }

        /// <summary>
        /// Dota 2 Style: บอทอัปสกิลอัตโนมัติตามลำดับความสำคัญ (Ultimate Lv6,10,12 > Talents Lv4,8,12 > Skill1/2 > Stat Bonus)
        /// </summary>
        public void AutoAllocateSkillPoints()
        {
            if (BotHero == null || BotHero.AvailableSkillPoints <= 0) return;

            while (BotHero.AvailableSkillPoints > 0)
            {
                int startPoints = BotHero.AvailableSkillPoints;

                // 1. Ultimate ที่เลเวล 6, 10, 12
                if (BotHero.CanLevelUltimate() && BotHero.TryLevelUltimate())
                {
                    continue;
                }

                // 2. เลือก Talent ที่เลเวล 4, 8, 12
                if (BotHero.CurrentLevel >= 4 && BotHero.TalentTier1Choice == -1)
                {
                    BotHero.TrySelectTalent(1, 0);
                    continue;
                }
                if (BotHero.CurrentLevel >= 8 && BotHero.TalentTier2Choice == -1)
                {
                    BotHero.TrySelectTalent(2, 0);
                    continue;
                }
                if (BotHero.CurrentLevel >= 12 && BotHero.TalentTier3Choice == -1)
                {
                    BotHero.TrySelectTalent(3, 0);
                    continue;
                }

                // 3. กระจาย Skill 1, 2, และ 3 สลับกัน
                if (BotHero.Skill1Rank <= BotHero.Skill2Rank && BotHero.Skill1Rank <= BotHero.Skill3Rank && BotHero.CanLevelSkill1())
                {
                    BotHero.TryLevelSkill1();
                }
                else if (BotHero.Skill2Rank <= BotHero.Skill3Rank && BotHero.CanLevelSkill2())
                {
                    BotHero.TryLevelSkill2();
                }
                else if (BotHero.CanLevelSkill3())
                {
                    BotHero.TryLevelSkill3();
                }
                else if (BotHero.CanLevelSkill1())
                {
                    BotHero.TryLevelSkill1();
                }
                else if (BotHero.CanLevelSkill2())
                {
                    BotHero.TryLevelSkill2();
                }
                else if (BotHero.CanLevelStatBonus())
                {
                    BotHero.TryLevelStatBonus();
                }
                else
                {
                    break;
                }

                if (BotHero.AvailableSkillPoints >= startPoints)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// บอทตัดสินใจซื้อไอเทมอัตโนมัติเมื่อเงินพอขณะอยู่ที่ Fountain
        /// ซื้อตาม Role ของฮีโร่: Tank → HP/Armor, Marksman → AD/AttackRate, Mage → Mana/CDR
        /// </summary>
        public void TryBuyItemsAtFountain()
        {
            if (Match == null || Match.RedWallet == null || BotHero == null) return;

            var catalog = _botShop.AvailableCatalog;
            if (catalog == null || catalog.Count == 0) return;

            // กำหนด Priority Item ID ตาม Role
            string[] priorityOrder;
            if (BotHero is GravitorHero)
            {
                // Tank: HP, Armor, MR, then CDR
                priorityOrder = new[] { "item_iron_plate_bracer", "item_void_emblem", "item_gravity_anchor", "item_focus_crystal", "item_kinetic_boots", "item_warblade_fang", "item_overclock_core", "item_nullifying_cloak" };
            }
            else if (BotHero is KorvaxHero)
            {
                // Marksman Sniper: AD, AttackRate, Boots, then survivability
                priorityOrder = new[] { "item_warblade_fang", "item_overclock_core", "item_kinetic_boots", "item_iron_plate_bracer", "item_void_emblem", "item_gravity_anchor", "item_focus_crystal", "item_nullifying_cloak" };
            }
            else if (BotHero is ZenthisHero)
            {
                // Mage/Controller: Mana/CDR, survivability, then Capstone
                priorityOrder = new[] { "item_focus_crystal", "item_void_emblem", "item_iron_plate_bracer", "item_kinetic_boots", "item_nullifying_cloak", "item_gravity_anchor", "item_warblade_fang", "item_overclock_core" };
            }
            else
            {
                // Vorkas (Warrior/Fighter): Balanced — AD, HP, then utility
                priorityOrder = new[] { "item_warblade_fang", "item_iron_plate_bracer", "item_kinetic_boots", "item_focus_crystal", "item_void_emblem", "item_overclock_core", "item_gravity_anchor", "item_nullifying_cloak" };
            }

            // ตรวจว่ามีไอเทมที่ซื้อแล้วในกระเป๋า
            var ownedIds = new System.Collections.Generic.HashSet<string>();
            for (int slot = 0; slot < KOA.Core.Items.Inventory.SlotCount; slot++)
            {
                var existing = BotHero.Inventory.GetItemInSlot(slot);
                if (existing != null) ownedIds.Add(existing.ItemId);
            }

            // ซื้อตาม priority และเงินที่มี — ซื้อได้สูงสุด 1 ชิ้นต่อครั้งที่ถึง Fountain (เพื่อไม่ให้ซื้อหมดทันที)
            foreach (string itemId in priorityOrder)
            {
                if (ownedIds.Contains(itemId)) continue; // มีแล้ว ข้ามไป

                var item = catalog.Find(i => i.ItemId == itemId);
                if (item == null) continue;

                if (Match.RedWallet.CurrentGold >= item.Cost)
                {
                    if (_botShop.TryBuyItem(item, Match.RedWallet, BotHero.Inventory))
                    {
                        // ซื้อแล้ว 1 ชิ้น — หยุดก่อน (ซื้อทีละชิ้นต่อการ retreat 1 ครั้ง)
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// คำนวณตำแหน่ง Aim Vector โดยใช้ Prediction Algorithm ตามระดับความยาก (Section 9)
        /// </summary>
        public Vector3 CalculateAimPosition(Vector3 targetPos, float projectileSpeed = 15f)
        {
            if (Difficulty == BotDifficulty.Hard && _enemyVelocity.sqrMagnitude > 0.1f)
            {
                // Hard Tier Prediction: คำนวณ Lead Position ดักหน้าเป้าหมาย
                float distance = Vector3.Distance(BotHero.Position, targetPos);
                float timeToReach = distance / Mathf.Max(5f, projectileSpeed);
                Vector3 predictedPos = targetPos + (_enemyVelocity * timeToReach);
                predictedPos.y = BotHero.Position.y;
                return predictedPos;
            }

            // Easy & Medium: ยิงตรงหาตำแหน่งปัจจุบัน
            return targetPos;
        }

        /// <summary>
        /// ใช้สกิลอัจฉริยะตามความสามารถของฮีโร่แต่ละตัว (Section 6.1 - 6.4)
        /// </summary>
        public void CastBotSkills(ITargetable currentEnemyTarget, float distance, Vector3 targetPos)
        {
            Vector3 aimPos = CalculateAimPosition(targetPos);

            if (BotHero is VorkasHero v)
            {
                // Q: Iron Cleave
                if (distance <= 6.0f && v.Skill1CooldownRemaining <= 0f)
                {
                    v.TryCastIronCleave(aimPos, currentEnemyTarget);
                }
                // W: Vanguard's Will
                if (v.CurrentHp / v.EffectiveMaxHp < 0.75f && v.Skill2CooldownRemaining <= 0f)
                {
                    v.TryCastVanguardsWill();
                }
                // E: Seismic Slam
                if (distance <= 3.8f && v.Skill3CooldownRemaining <= 0f)
                {
                    v.TryCastSeismicSlam(aimPos, currentEnemyTarget);
                }
                // R: Rebellion Impact
                if (distance <= 7.0f && v.UltimateCooldownRemaining <= 0f && (v.CurrentHp / v.EffectiveMaxHp < 0.60f || currentEnemyTarget.CurrentHp / currentEnemyTarget.MaxHp < 0.50f))
                {
                    v.TryCastRebellionImpact(aimPos, currentEnemyTarget);
                }
            }
            else if (BotHero is ZenthisHero z)
            {
                // Q: Sacred Hourglass
                if (distance <= 8.0f && z.Skill1CooldownRemaining <= 0f)
                {
                    z.TryCastSacredHourglass(aimPos, currentEnemyTarget);
                }
                // W: Aura of Eternity
                if (z.CurrentHp / z.EffectiveMaxHp < 0.70f && z.Skill2CooldownRemaining <= 0f)
                {
                    z.TryCastAuraOfEternity();
                }
                // E: Temporal Rift
                if (distance <= 8.0f && z.Skill3CooldownRemaining <= 0f)
                {
                    z.TryCastTemporalRift(aimPos, currentEnemyTarget);
                }
                // R: Grand Rewind
                if (z.CurrentHp / z.EffectiveMaxHp < 0.30f && z.UltimateCooldownRemaining <= 0f)
                {
                    z.TryCastGrandRewind();
                }
            }
            else if (BotHero is KorvaxHero k)
            {
                // W: Hunter's Focus
                if (distance <= k.AttackRange + 2.0f && k.Skill2CooldownRemaining <= 0f)
                {
                    k.TryCastHuntersFocus();
                }
                // Q: Heavy Bolt
                if (distance <= 10.0f && k.Skill1CooldownRemaining <= 0f)
                {
                    k.TryCastHeavyBolt(aimPos, currentEnemyTarget);
                }
                // E: Concussive Blast (Peel away if enemy gets too close)
                if (distance <= 4.0f && k.Skill3CooldownRemaining <= 0f)
                {
                    k.TryCastConcussiveBlast(aimPos, currentEnemyTarget);
                }
                // R: Ballista Overdrive
                if (distance <= 16.0f && k.UltimateCooldownRemaining <= 0f)
                {
                    k.TryCastBallistaOverdrive(aimPos, currentEnemyTarget);
                }
            }
            else if (BotHero is GravitorHero g)
            {
                // Q: Magnetic Pull
                if (distance <= 7.0f && g.Skill1CooldownRemaining <= 0f)
                {
                    g.TryCastMagneticPull(aimPos, currentEnemyTarget);
                }
                // W: Repulsion Zone
                if (distance <= 3.8f && g.Skill2CooldownRemaining <= 0f)
                {
                    g.TryCastRepulsionZone(currentEnemyTarget);
                }
                // E: Graviton Well
                if (distance <= 6.0f && g.Skill3CooldownRemaining <= 0f)
                {
                    g.TryCastGravitonWell(aimPos, currentEnemyTarget);
                }
                // R: Gravity Collapse
                if (distance <= 7.5f && g.UltimateCooldownRemaining <= 0f)
                {
                    g.TryCastGravityCollapse(aimPos, currentEnemyTarget);
                }
            }
        }

        // ==========================================
        // FSM States
        // ==========================================

        private class IdleState : IFSMState<ModularBotBrain>
        {
            public void OnEnter(ModularBotBrain ctx) { }
            public void OnUpdate(ModularBotBrain ctx, float deltaTime)
            {
                if (ctx.BotHero.IsAlive) ctx.FSM.ChangeState(BotStateKey.LanePushing);
            }
            public void OnExit(ModularBotBrain ctx) { }
        }

        private class LanePushingState : IFSMState<ModularBotBrain>
        {
            private float _pollTimer = 0f;

            public void OnEnter(ModularBotBrain ctx)
            {
                _pollTimer = 0f;
            }

            public void OnUpdate(ModularBotBrain ctx, float deltaTime)
            {
                _pollTimer -= deltaTime;

                // 1. ตรวจสอบฮีโร่ศัตรู
                float engageRange = ctx.Difficulty == BotDifficulty.Hard ? 8.5f : 7.0f;
                if (ctx.TargetEnemyHero != null && ctx.TargetEnemyHero.IsAlive)
                {
                    float distToHero = Vector3.Distance(ctx.BotHero.Position, ctx.TargetEnemyHero.Position);
                    if (distToHero <= engageRange)
                    {
                        // ถ้าฮีโร่ศัตรูไม่ได้ยืนปลอดภัยอยู่ใต้ป้อมที่ไม่มีครีบแทงค์ -> สู้!
                        bool heroSafeUnderTower = ctx.IsDangerousTowerZone(ctx.TargetEnemyHero.Position, out _);
                        if (!heroSafeUnderTower)
                        {
                            ctx.FSM.ChangeState(BotStateKey.AttackingHero);
                            return;
                        }
                    }
                }

                // 2. ถ้าหลงเข้าไปในระยะป้อมศัตรูโดยไม่มีครีบช่วย -> ต้องถอยออกมาทันที!
                if (ctx.IsDangerousTowerZone(ctx.BotHero.Position, out TowerEntity dangerTower))
                {
                    ctx.MoveSafelyTowards(ctx.HomeFountainPosition);
                    return;
                }

                if (_pollTimer > 0f) return;
                _pollTimer = 0.20f; // ประเมินสถานการณ์ทุก 0.2 วิ

                var enemyMinion = ctx.GetClosestEnemyMinion(10.0f);
                var friendlyFrontline = ctx.GetFriendlyMinionFrontline();
                var enemyTower = ctx.GetClosestAliveEnemyTower();

                // 3.1 มีครีบศัตรูในระยะ -> ตีครีบ / ดันเวฟ
                if (enemyMinion != null)
                {
                    float distToMinion = Vector3.Distance(ctx.BotHero.Position, enemyMinion.Position);
                    float atkThreshold = ctx.BotHero.AttackRange + enemyMinion.Radius;

                    if (distToMinion <= atkThreshold)
                    {
                        ctx.BotHero.StopMoving();
                        ctx.BotHero.TryBasicAttack(enemyMinion);
                    }
                    else
                    {
                        ctx.MoveSafelyTowards(enemyMinion.Position);
                    }
                    return;
                }

                // 3.2 ไม่มีครีบศัตรู แต่มีป้อมศัตรู
                if (enemyTower != null)
                {
                    float distToTower = Vector3.Distance(ctx.BotHero.Position, enemyTower.Position);
                    int friendlyMinionsUnderTower = ctx.CountFriendlyMinionsNear(enemyTower.Position, enemyTower.Stats.AttackRange);

                    if (friendlyMinionsUnderTower > 0)
                    {
                        // มีครีบเราช่วยแทงค์ป้อม -> ตีป้อมได้!
                        float towerAtkThreshold = ctx.BotHero.AttackRange + enemyTower.Radius;
                        if (distToTower <= towerAtkThreshold)
                        {
                            ctx.BotHero.StopMoving();
                            ctx.BotHero.TryBasicAttack(enemyTower);
                        }
                        else
                        {
                            ctx.MoveSafelyTowards(enemyTower.Position);
                        }
                        return;
                    }
                    else
                    {
                        // ไม่มีครีบเราช่วยแทงค์ป้อม -> ห้ามเดินเข้าเด็ดขาด! ถอยออกมารอครีบเวฟถัดไป
                        if (distToTower < (enemyTower.Stats.AttackRange + 2.5f))
                        {
                            Vector3 dirAway = (ctx.BotHero.Position - enemyTower.Position).normalized;
                            if (dirAway.sqrMagnitude < 0.001f) dirAway = Vector3.back;
                            Vector3 safeWaitPos = enemyTower.Position + dirAway * (enemyTower.Stats.AttackRange + 3.0f);
                            safeWaitPos.y = ctx.BotHero.Position.y;
                            ctx.BotHero.SetMoveDestination(safeWaitPos);
                            return;
                        }
                    }
                }

                // 3.3 เดินร่วมกับแนวหน้าของครีบเรา (ไม่เดินแซงหน้าครีบไปคนเดียว)
                if (friendlyFrontline != null)
                {
                    Vector3 followPos = friendlyFrontline.Position + Vector3.forward * 2.5f;
                    followPos.y = ctx.BotHero.Position.y;

                    if (Vector3.Distance(ctx.BotHero.Position, followPos) > 1.2f)
                    {
                        ctx.MoveSafelyTowards(followPos);
                    }
                    else
                    {
                        ctx.BotHero.StopMoving();
                    }
                    return;
                }

                // 3.4 ไม่มีครีบเพื่อนเลย -> ยืนคุมเชิงใกล้แม่น้ำ/กลางสนาม ไม่วิ่งไปลุยคนเดียว
                var friendlyTower = ctx.GetClosestAliveFriendlyTower();
                Vector3 holdPos = friendlyTower != null ? (friendlyTower.Position + Vector3.back * 4.0f) : ctx.HomeFountainPosition;
                holdPos.y = ctx.BotHero.Position.y;

                if (Vector3.Distance(ctx.BotHero.Position, holdPos) > 2.0f)
                {
                    ctx.BotHero.SetMoveDestination(holdPos);
                }
                else
                {
                    ctx.BotHero.StopMoving();
                }
            }

            public void OnExit(ModularBotBrain ctx) { }
        }

        private class AttackingHeroState : IFSMState<ModularBotBrain>
        {
            public void OnEnter(ModularBotBrain ctx)
            {
                ctx._easyReactionTimer = ctx.Difficulty == BotDifficulty.Easy ? 0.6f : 0f;
            }

            public void OnUpdate(ModularBotBrain ctx, float deltaTime)
            {
                if (ctx.TargetEnemyHero == null || !ctx.TargetEnemyHero.IsAlive)
                {
                    ctx.FSM.ChangeState(BotStateKey.LanePushing);
                    return;
                }

                Vector3 enemyPos = ctx.TargetEnemyHero.Position;
                float distance = Vector3.Distance(ctx.BotHero.Position, enemyPos);

                // 1. ศัตรูหนีไปไกลเกิน 11.5m -> เลิกตาม กลับไปคุมเลน
                if (distance > 11.5f)
                {
                    ctx.FSM.ChangeState(BotStateKey.LanePushing);
                    return;
                }

                // 2. ป้องกันการ Dive ป้อมศัตรูอย่างไร้เหตุผล
                // ถ้าศัตรูวิ่งหนีเข้าป้อมตัวเอง และไม่มีครีบเราแทงค์ป้อม
                if (ctx.IsDangerousTowerZone(enemyPos, out TowerEntity enemyTower))
                {
                    float botHpRatio = ctx.BotHero.CurrentHp / ctx.BotHero.EffectiveMaxHp;
                    float enemyHpRatio = ctx.TargetEnemyHero.CurrentHp / ctx.TargetEnemyHero.EffectiveMaxHp;

                    // ยอม Dive เฉพาะ Hard Tier ที่ศัตรูเลือดต่ำกว่า 12% และบอทเลือดเกิน 70%
                    bool canDive = ctx.Difficulty == BotDifficulty.Hard && enemyHpRatio < 0.12f && botHpRatio > 0.70f;
                    if (!canDive)
                    {
                        // ห้ามตามเข้าป้อม! ให้หยุดและกลับไปดันเลน
                        ctx.FSM.ChangeState(BotStateKey.LanePushing);
                        return;
                    }
                }

                // 3. ถ้าตัวบอทเองกำลังโดนป้อมอันตรายยิง -> หนีทันที!
                if (ctx.IsDangerousTowerZone(ctx.BotHero.Position, out _))
                {
                    ctx.MoveSafelyTowards(ctx.HomeFountainPosition);
                    return;
                }

                // Easy Reaction Delay
                if (ctx.Difficulty == BotDifficulty.Easy && ctx._easyReactionTimer > 0f)
                {
                    ctx._easyReactionTimer -= deltaTime;
                    return;
                }

                // 4. ใช้สกิลพร้อม Predict ทิศทาง
                ctx.CastBotSkills(ctx.TargetEnemyHero, distance, enemyPos);

                // 5. การเว้นระยะและการ Kiting
                bool isRanged = ctx.BotHero.AttackRange >= 4.0f; // Zenthis, Korvax
                float attackThreshold = ctx.BotHero.AttackRange + ctx.TargetEnemyHero.Radius;

                if (isRanged)
                {
                    // --- ฮีโร่ตีไกล (Zenthis / Korvax) ---
                    // ถ้าศัตรูพุ่งเข้ามาใกล้เกิน (< 3.2m): ถอยหลัง (Kiting) พร้อมยิงสวน!
                    if (distance < 3.2f && ctx.Difficulty != BotDifficulty.Easy)
                    {
                        Vector3 kiteDir = (ctx.BotHero.Position - enemyPos).normalized;
                        if (kiteDir.sqrMagnitude < 0.001f) kiteDir = Vector3.forward;
                        Vector3 kitePos = ctx.BotHero.Position + kiteDir * 3.0f;
                        kitePos.y = ctx.BotHero.Position.y;

                        ctx.MoveSafelyTowards(kitePos);
                        ctx.BotHero.TryBasicAttack(ctx.TargetEnemyHero);
                    }
                    else if (distance <= attackThreshold)
                    {
                        ctx.BotHero.StopMoving();
                        ctx.BotHero.TryBasicAttack(ctx.TargetEnemyHero);
                    }
                    else
                    {
                        ctx.MoveSafelyTowards(enemyPos);
                    }
                }
                else
                {
                    // --- ฮีโร่ตีใกล้ (Vorkas / Gravitor) ---
                    if (distance <= attackThreshold)
                    {
                        ctx.BotHero.StopMoving();
                        ctx.BotHero.TryBasicAttack(ctx.TargetEnemyHero);
                    }
                    else
                    {
                        ctx.MoveSafelyTowards(enemyPos);
                    }
                }
            }

            public void OnExit(ModularBotBrain ctx) { }
        }

        private class RetreatingState : IFSMState<ModularBotBrain>
        {
            public void OnEnter(ModularBotBrain ctx)
            {
                ctx.BotHero.SetMoveDestination(ctx.HomeFountainPosition);
            }

            public void OnUpdate(ModularBotBrain ctx, float deltaTime)
            {
                // เดินกลับบ้าน
                if (!ctx.BotHero.IsMoving)
                {
                    ctx.BotHero.SetMoveDestination(ctx.HomeFountainPosition);
                }

                // เมื่อถึง Fountain ให้ซื้อของ
                float distToFountain = Vector3.Distance(ctx.BotHero.Position, ctx.HomeFountainPosition);
                if (distToFountain <= 8.0f)
                {
                    ctx.TryBuyItemsAtFountain();
                }

                float recoverTarget = ctx.Difficulty == BotDifficulty.Hard ? 0.85f : 0.70f;
                if ((ctx.BotHero.CurrentHp / ctx.BotHero.EffectiveMaxHp) >= recoverTarget)
                {
                    ctx.FSM.ChangeState(BotStateKey.LanePushing);
                }
            }

            public void OnExit(ModularBotBrain ctx) { }
        }
    }
}
