using KOA.Core.Entities;
using KOA.Core.FSM;
using KOA.Data.Enums;
using System;
using UnityEngine;

namespace KOA.Core.AI
{
    /// <summary>
    /// AI Bot สมองระดับ Medium Tier ตาม Section 9
    /// ควบคุมฮีโร่ฝั่งบอทด้วย Interface-based State Machine บน Simulation Core (Decoupled Core)
    /// </summary>
    public class MediumBotBrain
    {
        public VorkasHero BotHero { get; private set; }
        public Vector3 HomeFountainPosition { get; set; }
        public Vector3 EnemyNexusPosition { get; set; }

        public HeroBase3D TargetEnemyHero { get; set; }
        public DummyTarget TargetDummy { get; set; } // สำหรับทดสอบกับ Dummy

        public StateMachine<MediumBotBrain, BotStateKey> FSM { get; private set; }

        public BotStateKey CurrentState => FSM.CurrentStateKey;

        public MediumBotBrain(VorkasHero botHero, Vector3 homeFountain, Vector3 enemyNexus)
        {
            BotHero = botHero;
            HomeFountainPosition = homeFountain;
            EnemyNexusPosition = enemyNexus;

            InitializeFSM();
        }

        private void InitializeFSM()
        {
            FSM = new StateMachine<MediumBotBrain, BotStateKey>(this);
            FSM.RegisterState(BotStateKey.Idle, new IdleState());
            FSM.RegisterState(BotStateKey.LanePushing, new LanePushingState());
            FSM.RegisterState(BotStateKey.AttackingHero, new AttackingHeroState());
            FSM.RegisterState(BotStateKey.Retreating, new RetreatingState());

            FSM.ChangeState(BotStateKey.LanePushing);
        }

        public void SimulationTick(float deltaTime)
        {
            if (BotHero == null || !BotHero.IsAlive) return;

            // ตรวจสอบเงื่อนไขถอยฉุกเฉิน (Retreat) เมื่อ HP < 30%
            float hpRatio = BotHero.CurrentHp / BotHero.EffectiveMaxHp;
            if (hpRatio < 0.30f && FSM.CurrentStateKey != BotStateKey.Retreating)
            {
                FSM.ChangeState(BotStateKey.Retreating);
            }

            FSM.Update(deltaTime);
        }

        // ==========================================
        // FSM State Implementations (Section 9)
        // ==========================================

        private class IdleState : IFSMState<MediumBotBrain>
        {
            public void OnEnter(MediumBotBrain ctx) { }
            public void OnUpdate(MediumBotBrain ctx, float deltaTime)
            {
                if (ctx.BotHero.IsAlive)
                {
                    ctx.FSM.ChangeState(BotStateKey.LanePushing);
                }
            }
            public void OnExit(MediumBotBrain ctx) { }
        }

        private class LanePushingState : IFSMState<MediumBotBrain>
        {
            public void OnEnter(MediumBotBrain ctx)
            {
                // มุ่งหน้าดันเลนไปทางศัตรู
                ctx.BotHero.SetMoveDestination(ctx.EnemyNexusPosition);
            }

            public void OnUpdate(MediumBotBrain ctx, float deltaTime)
            {
                // ถ้ามีศัตรูอยู่ในระยะประชิด (7m) และบอท HP แข็งแรง -> เข้าปะทะ
                if (ctx.TargetEnemyHero != null && ctx.TargetEnemyHero.IsAlive)
                {
                    float distToEnemy = Vector3.Distance(ctx.BotHero.Position, ctx.TargetEnemyHero.Position);
                    if (distToEnemy <= 7.0f)
                    {
                        ctx.FSM.ChangeState(BotStateKey.AttackingHero);
                        return;
                    }
                }

                // ถ้ามี Dummy Target ในระยะสำหรับทดสอบ
                if (ctx.TargetDummy != null && ctx.TargetDummy.IsAlive)
                {
                    float distToDummy = Vector3.Distance(ctx.BotHero.Position, ctx.TargetDummy.Position);
                    if (distToDummy <= 6.0f)
                    {
                        ctx.FSM.ChangeState(BotStateKey.AttackingHero);
                        return;
                    }
                }

                // เดินหน้าดันเลนต่อ
                if (!ctx.BotHero.IsMoving)
                {
                    ctx.BotHero.SetMoveDestination(ctx.EnemyNexusPosition);
                }
            }

            public void OnExit(MediumBotBrain ctx) { }
        }

        private class AttackingHeroState : IFSMState<MediumBotBrain>
        {
            public void OnEnter(MediumBotBrain ctx) { }

            public void OnUpdate(MediumBotBrain ctx, float deltaTime)
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

                // หากศัตรูหนีไปไกลเกิน 10m ให้กลับไปดันเลน
                if (distance > 10.0f)
                {
                    ctx.FSM.ChangeState(BotStateKey.LanePushing);
                    return;
                }

                // 1. ใช้ Skill 1 (Iron Cleave) หากพร้อมและอยู่ในระยะ 6m (Section 6.1)
                if (distance <= VorkasHero.Skill1Range && ctx.BotHero.Skill1CooldownRemaining <= 0f)
                {
                    ctx.BotHero.TryCastIronCleave(targetPos, ctx.TargetDummy);
                }

                // 2. เคลื่อนที่เข้าหาระยะโจมตีปกติ (2.2m)
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

            public void OnExit(MediumBotBrain ctx) { }
        }

        private class RetreatingState : IFSMState<MediumBotBrain>
        {
            public void OnEnter(MediumBotBrain ctx)
            {
                // มุ่งหน้าถอยกลับ Fountain ของตนเอง
                ctx.BotHero.SetMoveDestination(ctx.HomeFountainPosition);
            }

            public void OnUpdate(MediumBotBrain ctx, float deltaTime)
            {
                // เดินต่อไปจนถึง Fountain
                if (!ctx.BotHero.IsMoving)
                {
                    ctx.BotHero.SetMoveDestination(ctx.HomeFountainPosition);
                }

                // เลือดฟื้นฟูกลับมาเกิน 75% แล้วจึงกลับไปดันเลน
                float hpRatio = ctx.BotHero.CurrentHp / ctx.BotHero.EffectiveMaxHp;
                if (hpRatio >= 0.75f)
                {
                    ctx.FSM.ChangeState(BotStateKey.LanePushing);
                }
            }

            public void OnExit(MediumBotBrain ctx) { }
        }
    }
}
