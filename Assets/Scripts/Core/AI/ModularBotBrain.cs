using KOA.Core.Entities;
using KOA.Core.FSM;
using System;
using UnityEngine;

namespace KOA.Core.AI
{
    /// <summary>
    /// Multi-Tier AI Bot Brain รองรับ Easy, Medium, และ Hard (Section 9)
    /// มี Prediction Algorithm สำหรับ Hard Tier เพื่อยิงดักหน้าทิศทางเดินของศัตรู
    /// </summary>
    public class ModularBotBrain
    {
        public VorkasHero BotHero { get; private set; }
        public Vector3 HomeFountainPosition { get; set; }
        public Vector3 EnemyNexusPosition { get; set; }
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

        public ModularBotBrain(VorkasHero botHero, Vector3 homeFountain, Vector3 enemyNexus, BotDifficulty difficulty = BotDifficulty.Medium)
        {
            BotHero = botHero;
            HomeFountainPosition = homeFountain;
            EnemyNexusPosition = enemyNexus;
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
                BotDifficulty.Medium => 0.30f, // Medium: ถอยที่ 30%
                BotDifficulty.Hard => 0.40f,   // Hard: ระมัดระวังตัวสูง ถอยที่ 40% หรือเมื่อศัตรูได้เปรียบ
                _ => 0.30f
            };

            if (hpRatio < retreatThreshold && FSM.CurrentStateKey != BotStateKey.Retreating)
            {
                FSM.ChangeState(BotStateKey.Retreating);
            }

            FSM.Update(deltaTime);
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
            public void OnEnter(ModularBotBrain ctx)
            {
                ctx.BotHero.SetMoveDestination(ctx.EnemyNexusPosition);
            }

            public void OnUpdate(ModularBotBrain ctx, float deltaTime)
            {
                float engageRange = ctx.Difficulty == BotDifficulty.Hard ? 8.5f : 7.0f;

                if (ctx.TargetEnemyHero != null && ctx.TargetEnemyHero.IsAlive)
                {
                    if (Vector3.Distance(ctx.BotHero.Position, ctx.TargetEnemyHero.Position) <= engageRange)
                    {
                        ctx.FSM.ChangeState(BotStateKey.AttackingHero);
                        return;
                    }
                }

                if (ctx.TargetDummy != null && ctx.TargetDummy.IsAlive)
                {
                    if (Vector3.Distance(ctx.BotHero.Position, ctx.TargetDummy.Position) <= 6.0f)
                    {
                        ctx.FSM.ChangeState(BotStateKey.AttackingHero);
                        return;
                    }
                }

                if (!ctx.BotHero.IsMoving)
                {
                    ctx.BotHero.SetMoveDestination(ctx.EnemyNexusPosition);
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
                Vector3 targetPos = Vector3.zero;
                bool hasTarget = false;

                if (ctx.TargetEnemyHero != null && ctx.TargetEnemyHero.IsAlive)
                {
                    targetPos = ctx.TargetEnemyHero.Position;
                    hasTarget = true;
                }
                else if (ctx.TargetDummy != null && ctx.TargetDummy.IsAlive)
                {
                    targetPos = ctx.TargetDummy.Position;
                    hasTarget = true;
                }

                if (!hasTarget)
                {
                    ctx.FSM.ChangeState(BotStateKey.LanePushing);
                    return;
                }

                float distance = Vector3.Distance(ctx.BotHero.Position, targetPos);
                if (distance > 11.0f)
                {
                    ctx.FSM.ChangeState(BotStateKey.LanePushing);
                    return;
                }

                // Easy Reaction Delay
                if (ctx.Difficulty == BotDifficulty.Easy && ctx._easyReactionTimer > 0f)
                {
                    ctx._easyReactionTimer -= deltaTime;
                    return;
                }

                // ใช้ Skill 1 (Iron Cleave) พร้อมคำนวณ Aim Vector
                if (distance <= VorkasHero.Skill1Range && ctx.BotHero.Skill1CooldownRemaining <= 0f)
                {
                    Vector3 aimPos = ctx.CalculateAimPosition(targetPos);
                    ctx.BotHero.TryCastIronCleave(aimPos, ctx.TargetDummy);
                }

                // การเข้าตีพื้นฐาน
                if (distance > ctx.BotHero.AttackRange)
                {
                    ctx.BotHero.SetMoveDestination(targetPos);
                }
                else
                {
                    ctx.BotHero.StopMoving();
                    if (ctx.TargetDummy != null)
                    {
                        ctx.BotHero.TryBasicAttack(ctx.TargetDummy);
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
                if (!ctx.BotHero.IsMoving)
                {
                    ctx.BotHero.SetMoveDestination(ctx.HomeFountainPosition);
                }

                float recoverTarget = ctx.Difficulty == BotDifficulty.Hard ? 0.85f : 0.75f;
                if ((ctx.BotHero.CurrentHp / ctx.BotHero.EffectiveMaxHp) >= recoverTarget)
                {
                    ctx.FSM.ChangeState(BotStateKey.LanePushing);
                }
            }

            public void OnExit(ModularBotBrain ctx) { }
        }
    }
}
