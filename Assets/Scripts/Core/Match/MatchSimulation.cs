using KOA.Core.Economy;
using KOA.Core.Entities;
using KOA.Core.Minions;
using KOA.Core.Structures;
using KOA.Core.World;
using KOA.Data.Models;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KOA.Core.Match
{
    public enum GoldRewardReason
    {
        MinionLastHit = 0,
        HeroElimination = 1,
        StructureDestroyed = 2
    }

    public readonly struct GoldRewardEvent
    {
        public int RecipientTeamId { get; }
        public int Amount { get; }
        public Vector3 WorldPosition { get; }
        public GoldRewardReason Reason { get; }
        public string SourceId { get; }
        public string DefeatedTargetId { get; }

        public GoldRewardEvent(
            int recipientTeamId,
            int amount,
            Vector3 worldPosition,
            GoldRewardReason reason,
            string sourceId,
            string defeatedTargetId)
        {
            RecipientTeamId = recipientTeamId;
            Amount = amount;
            WorldPosition = worldPosition;
            Reason = reason;
            SourceId = sourceId;
            DefeatedTargetId = defeatedTargetId;
        }
    }

    /// <summary>
    /// ผู้จัดการจำลองแมตช์ 1v1 ในระดับสนามแข่งขัน (Simulation Core)
    /// ควบคุมป้อมปราการ 2 Tier + Nexus (Section 3.3), ครีป (Section 3.2), ระบบต่อสู้, เศรษฐกิจ (Section 4), และการตัดสินผลแพ้ชนะ
    /// </summary>
    public class MatchSimulation
    {
        public bool IsGameOver { get; private set; } = false;
        public int WinningTeam { get; private set; } = -1; // 0 = Blue, 1 = Red
        public float MatchTime { get; private set; } = 0f;

        // ป้อมปราการและฐานฝั่ง Blue (ทีม 0)
        public TowerEntity BlueOuterTower { get; private set; }
        public TowerEntity BlueInnerTower { get; private set; }
        public TowerEntity BlueNexus { get; private set; }
        public List<TowerEntity> BlueTowers { get; private set; } = new List<TowerEntity>();

        // ป้อมปราการและฐานฝั่ง Red (ทีม 1)
        public TowerEntity RedOuterTower { get; private set; }
        public TowerEntity RedInnerTower { get; private set; }
        public TowerEntity RedNexus { get; private set; }
        public List<TowerEntity> RedTowers { get; private set; } = new List<TowerEntity>();

        // Creep Spawners
        public CreepSpawner BlueSpawner { get; private set; }
        public CreepSpawner RedSpawner { get; private set; }
        public List<MinionEntity> ActiveMinions { get; private set; } = new List<MinionEntity>();

        // ฮีโร่ในสนามแข่งขัน (Section 1.1)
        public HeroBase3D BlueHero { get; set; }
        public HeroBase3D RedHero { get; set; }

        // กระเป๋าเงินผู้เล่น
        public PlayerWallet BlueWallet { get; private set; }
        public PlayerWallet RedWallet { get; private set; }

        // Events แจ้งเตือนสถานะของแมตช์
        public event Action<int> OnGameOver; // winningTeam
        public event Action<TowerEntity> OnTowerDestroyed;
        public event Action<string> OnKillFeedMessage;
        public event Action<GoldRewardEvent> OnGoldRewardGranted;

        // ตำแหน่ง Fountain Zone (Section 3.1: รัศมี 7.5m, HP/Mana Regen ~11%/sec ตาม MOBA Standard)
        public Vector3 BlueFountainPos { get; private set; }
        public Vector3 RedFountainPos { get; private set; }
        public const float FountainZoneRadius = 7.5f;

        public MatchSimulation(Vector3 blueFountainPos, Vector3 redFountainPos)
        {
            Vector3 expectedRedFountain = DuelArenaLayout.MirrorPoint(blueFountainPos);
            if ((redFountainPos - expectedRedFountain).sqrMagnitude > 0.0001f)
                throw new ArgumentException("Fountain anchors must be point-symmetric through the arena origin.");
            if (!ArenaBounds.Contains(blueFountainPos, FountainZoneRadius)
                || !ArenaBounds.Contains(redFountainPos, FountainZoneRadius))
                throw new ArgumentOutOfRangeException(nameof(blueFountainPos), "Fountain healing zones must remain inside arena bounds.");

            // Section 3.1: all Red anchors derive from Blue anchors through point symmetry.
            // Gaps tighten toward center: Fountain-Nexus 17m, Nexus-Inner 15m,
            // Inner-Outer 13m, and Outer-Center 12m.
            BlueOuterTower = new TowerEntity("blue_tower_t1", StructureStats.CreateTier1OuterTower(), DuelArenaLayout.BlueOuterTower, 0);
            BlueInnerTower = new TowerEntity("blue_tower_t2", StructureStats.CreateTier2InnerTower(), DuelArenaLayout.BlueInnerTower, 0);
            BlueNexus = new TowerEntity("blue_nexus", StructureStats.CreateNexusCore(), DuelArenaLayout.BlueNexus, 0);
            BlueTowers.AddRange(new[] { BlueOuterTower, BlueInnerTower, BlueNexus });

            RedOuterTower = new TowerEntity("red_tower_t1", StructureStats.CreateTier1OuterTower(), DuelArenaLayout.RedOuterTower, 1);
            RedInnerTower = new TowerEntity("red_tower_t2", StructureStats.CreateTier2InnerTower(), DuelArenaLayout.RedInnerTower, 1);
            RedNexus = new TowerEntity("red_nexus", StructureStats.CreateNexusCore(), DuelArenaLayout.RedNexus, 1);
            RedTowers.AddRange(new[] { RedOuterTower, RedInnerTower, RedNexus });

            // ตั้งค่า Damage Immunity ตามลำดับ (Section 3.3)
            BlueInnerTower.IsInvulnerable = true;
            BlueNexus.IsInvulnerable = true;
            RedInnerTower.IsInvulnerable = true;
            RedNexus.IsInvulnerable = true;

            HookTowerEvents();

            // สร้าง Creep Spawner
            BlueSpawner = new CreepSpawner(0, blueFountainPos, redFountainPos);
            RedSpawner = new CreepSpawner(1, redFountainPos, blueFountainPos);

            BlueSpawner.OnWaveSpawned += HandleWaveSpawned;
            RedSpawner.OnWaveSpawned += HandleWaveSpawned;

            // สร้าง Wallet
            BlueWallet = new PlayerWallet();
            RedWallet = new PlayerWallet();

            // เก็บตำแหน่ง Fountain
            BlueFountainPos = blueFountainPos;
            RedFountainPos = redFountainPos;
        }

        private void HookTowerEvents()
        {
            // Blue Towers
            BlueOuterTower.OnDestroyed += () =>
            {
                BlueInnerTower.IsInvulnerable = false;
                GrantStructureReward(RedWallet, 1, BlueOuterTower, StructureStats.CreateTier1OuterTower().GoldBounty);
                RedSpawner.HasCannonMinion = true; // Red ได้ Cannon Minion
                OnTowerDestroyed?.Invoke(BlueOuterTower);
                OnKillFeedMessage?.Invoke($"Red Team destroyed Blue Outer Tower  |  +{StructureStats.CreateTier1OuterTower().GoldBounty} gold");
            };

            BlueInnerTower.OnDestroyed += () =>
            {
                BlueNexus.IsInvulnerable = false;
                GrantStructureReward(RedWallet, 1, BlueInnerTower, StructureStats.CreateTier2InnerTower().GoldBounty);
                RedSpawner.HasSuperMinion = true; // Red ได้ Super Creep
                OnTowerDestroyed?.Invoke(BlueInnerTower);
                OnKillFeedMessage?.Invoke($"Red Team destroyed Blue Inner Tower  |  +{StructureStats.CreateTier2InnerTower().GoldBounty} gold");
            };

            BlueNexus.OnDestroyed += () =>
            {
                TriggerGameOver(winningTeam: 1); // Red ชนะ
            };

            // Red Towers
            RedOuterTower.OnDestroyed += () =>
            {
                RedInnerTower.IsInvulnerable = false;
                GrantStructureReward(BlueWallet, 0, RedOuterTower, StructureStats.CreateTier1OuterTower().GoldBounty);
                BlueSpawner.HasCannonMinion = true; // Blue ได้ Cannon Minion
                OnTowerDestroyed?.Invoke(RedOuterTower);
                OnKillFeedMessage?.Invoke($"Blue Team destroyed Red Outer Tower  |  +{StructureStats.CreateTier1OuterTower().GoldBounty} gold");
            };

            RedInnerTower.OnDestroyed += () =>
            {
                RedNexus.IsInvulnerable = false;
                GrantStructureReward(BlueWallet, 0, RedInnerTower, StructureStats.CreateTier2InnerTower().GoldBounty);
                BlueSpawner.HasSuperMinion = true; // Blue ได้ Super Creep
                OnTowerDestroyed?.Invoke(RedInnerTower);
                OnKillFeedMessage?.Invoke($"Blue Team destroyed Red Inner Tower  |  +{StructureStats.CreateTier2InnerTower().GoldBounty} gold");
            };

            RedNexus.OnDestroyed += () =>
            {
                TriggerGameOver(winningTeam: 0); // Blue ชนะ
            };
        }

        private void HandleWaveSpawned(List<MinionEntity> wave)
        {
            ActiveMinions.AddRange(wave);
            foreach (var minion in wave)
            {
                minion.OnKilled += HandleMinionKilled;
            }
        }

        private void HandleMinionKilled(MinionEntity minion, string killerId)
        {
            if (minion == null || string.IsNullOrEmpty(killerId)) return;

            // Section 4.1: creep gold is granted only when an enemy hero delivers
            // the last hit. Allied minions and structures never credit the wallet.
            if (BlueHero != null
                && minion.TeamId != BlueHero.TeamId
                && killerId == BlueHero.DamageSourceId)
            {
                GrantLastHitReward(BlueWallet, BlueHero, minion);
            }
            else if (RedHero != null
                && minion.TeamId != RedHero.TeamId
                && killerId == RedHero.DamageSourceId)
            {
                GrantLastHitReward(RedWallet, RedHero, minion);
            }
        }

        private void GrantLastHitReward(PlayerWallet wallet, HeroBase3D hero, MinionEntity minion)
        {
            wallet.AddGold(minion.GoldBounty);
            hero.AddExp(minion.ExpBounty);
            OnGoldRewardGranted?.Invoke(new GoldRewardEvent(
                hero.TeamId,
                minion.GoldBounty,
                minion.Position,
                GoldRewardReason.MinionLastHit,
                hero.DamageSourceId,
                minion.MinionId));
        }

        private void GrantStructureReward(PlayerWallet wallet, int teamId, TowerEntity structure, int amount)
        {
            wallet.AddGold(amount);
            OnGoldRewardGranted?.Invoke(new GoldRewardEvent(
                teamId,
                amount,
                structure.Position,
                GoldRewardReason.StructureDestroyed,
                teamId == 0 ? BlueHero?.DamageSourceId : RedHero?.DamageSourceId,
                structure.TowerId));
        }

        public int RecordHeroElimination(HeroBase3D victim)
        {
            if (victim == null || victim.IsAlive) return 0;

            int victimTeamId = ReferenceEquals(victim, BlueHero) ? 0 : ReferenceEquals(victim, RedHero) ? 1 : -1;
            if (victimTeamId < 0) return 0;

            PlayerWallet victimWallet = victimTeamId == 0 ? BlueWallet : RedWallet;
            victimWallet.RecordDeath();

            HeroBase3D killer = null;
            if (BlueHero != null && victimTeamId != 0 && victim.LastDamageSourceId == BlueHero.DamageSourceId)
                killer = BlueHero;
            else if (RedHero != null && victimTeamId != 1 && victim.LastDamageSourceId == RedHero.DamageSourceId)
                killer = RedHero;

            string victimTeam = victimTeamId == 0 ? "Blue" : "Red";
            if (killer == null)
            {
                string sourceLabel = ResolveDamageSourceLabel(victim.LastDamageSourceId);
                OnKillFeedMessage?.Invoke($"{victimTeam} {victim.DisplayName} was defeated by {sourceLabel}  |  no hero bounty");
                return 0;
            }

            int killerTeamId = killer.TeamId;
            PlayerWallet killerWallet = killerTeamId == 0 ? BlueWallet : RedWallet;

            int goldBefore = killerWallet.CurrentGold;
            killerWallet.RecordHeroKill();
            int bounty = killerWallet.CurrentGold - goldBefore;
            killer.AddExp(350f);

            OnGoldRewardGranted?.Invoke(new GoldRewardEvent(
                killerTeamId,
                bounty,
                victim.Position,
                GoldRewardReason.HeroElimination,
                killer.DamageSourceId,
                victim.TargetId));

            string killerTeam = killerTeamId == 0 ? "Blue" : "Red";
            OnKillFeedMessage?.Invoke($"{killerTeam} {killer.DisplayName} defeated {victimTeam} {victim.DisplayName}  |  +{bounty} gold");
            return bounty;
        }

        private string ResolveDamageSourceLabel(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId)) return "the battlefield";

            MinionEntity sourceMinion = ActiveMinions.Find(minion => minion.MinionId == sourceId);
            if (sourceMinion != null) return sourceMinion.TeamId == 0 ? "Blue Minion" : "Red Minion";

            TowerEntity sourceTower = BlueTowers.Find(tower => tower.TowerId == sourceId);
            if (sourceTower != null) return "Blue Tower";
            sourceTower = RedTowers.Find(tower => tower.TowerId == sourceId);
            if (sourceTower != null) return "Red Tower";

            return "the battlefield";
        }

        public void SimulationTick(float deltaTime)
        {
            if (IsGameOver) return;

            MatchTime += deltaTime;

            // อัปเดตเศรษฐกิจ Passive Gold
            BlueWallet.SimulationTick(deltaTime);
            RedWallet.SimulationTick(deltaTime);

            // อัปเดตป้อมปราการและการคำนวณ Cooldown
            BlueOuterTower.SimulationTick(deltaTime);
            BlueInnerTower.SimulationTick(deltaTime);
            BlueNexus.SimulationTick(deltaTime);

            RedOuterTower.SimulationTick(deltaTime);
            RedInnerTower.SimulationTick(deltaTime);
            RedNexus.SimulationTick(deltaTime);

            // อัปเดต Spawners
            BlueSpawner.SimulationTick(deltaTime);
            RedSpawner.SimulationTick(deltaTime);

            // 1. Tower Defense Targeting & Backdoor Protection (Section 3.3)
            UpdateBackdoorProtection(BlueTowers, 1);
            UpdateBackdoorProtection(RedTowers, 0);
            UpdateTowerCombat(BlueTowers, 1);
            UpdateTowerCombat(RedTowers, 0);

            // 3. Fountain Zone HP/Mana Regen (Section 3.1: ~11% MaxHP/MaxMana per second)
            UpdateFountainZoneRegen(deltaTime);

            // 4. Minion Combat & Movement Loop (Section 3.2)
            for (int i = ActiveMinions.Count - 1; i >= 0; i--)
            {
                var minion = ActiveMinions[i];
                if (!minion.IsAlive)
                {
                    ActiveMinions.RemoveAt(i);
                    continue;
                }

                int enemyTeam = minion.TeamId == 0 ? 1 : 0;

                // ค้นหาเป้าหมายศัตรูหากยังไม่มีเป้าหมาย หรือเป้าหมายเดิมตาย/หลุดระยะ
                if (minion.CurrentTarget == null || !minion.CurrentTarget.IsAlive || Vector3.Distance(minion.Position, minion.CurrentTarget.Position) > 8.0f)
                {
                    ITargetable target = null;
                    float minDistance = float.MaxValue;

                    // 2.1 ค้นหาครีบศัตรูในระยะ Aggro (7.0m)
                    for (int j = 0; j < ActiveMinions.Count; j++)
                    {
                        var other = ActiveMinions[j];
                        if (other.IsAlive && other.TeamId == enemyTeam)
                        {
                            float dist = Vector3.Distance(minion.Position, other.Position);
                            if (dist <= 7.0f && dist < minDistance)
                            {
                                minDistance = dist;
                                target = other;
                            }
                        }
                    }

                    // 2.2 หากไม่มีครีบศัตรู ให้ค้นหาป้อมศัตรูที่สามารถโจมตีได้
                    if (target == null)
                    {
                        var enemyTowers = enemyTeam == 0 ? BlueTowers : RedTowers;
                        foreach (var tow in enemyTowers)
                        {
                            if (!tow.IsDestroyed && !tow.IsInvulnerable)
                            {
                                float dist = Vector3.Distance(minion.Position, tow.Position);
                                if (dist <= 7.0f && dist < minDistance)
                                {
                                    minDistance = dist;
                                    target = tow;
                                }
                            }
                        }
                    }

                    // 2.3 หากไม่มีป้อม ให้ค้นหาฮีโร่ศัตรู
                    if (target == null)
                    {
                        var enemyHero = enemyTeam == 0 ? BlueHero : RedHero;
                        if (enemyHero != null && enemyHero.IsAlive)
                        {
                            float dist = Vector3.Distance(minion.Position, enemyHero.Position);
                            if (dist <= 6.0f)
                            {
                                target = enemyHero;
                            }
                        }
                    }

                    minion.SetTarget(target);
                }

                Vector3 targetDest = minion.TeamId == 0 ? RedNexus.Position : BlueNexus.Position;
                minion.SimulationTick(deltaTime, targetDest);
            }
        }

        private void UpdateFountainZoneRegen(float deltaTime)
        {
            if (BlueHero != null && BlueHero.IsAlive)
            {
                bool inFountain = Vector3.Distance(BlueHero.Position, BlueFountainPos) <= FountainZoneRadius;
                BlueHero.IsInFountainZone = inFountain;
                if (inFountain) BlueHero.FountainZoneTick(deltaTime);
            }
            if (RedHero != null && RedHero.IsAlive)
            {
                bool inFountain = Vector3.Distance(RedHero.Position, RedFountainPos) <= FountainZoneRadius;
                RedHero.IsInFountainZone = inFountain;
                if (inFountain) RedHero.FountainZoneTick(deltaTime);
            }
        }

        private void UpdateTowerCombat(List<TowerEntity> towers, int enemyTeam)
        {
            foreach (var tower in towers)
            {
                if (tower.IsDestroyed || tower.Stats.BaseAttackDamage <= 0f) continue;

                ITargetable bestTarget = null;
                float closestDist = float.MaxValue;

                // Priority 1: ครีบศัตรูที่ใกล้ที่สุดในระยะป้อม
                for (int m = 0; m < ActiveMinions.Count; m++)
                {
                    var minion = ActiveMinions[m];
                    if (minion.IsAlive && minion.TeamId == enemyTeam)
                    {
                        float d = Vector3.Distance(tower.Position, minion.Position);
                        if (d <= tower.Stats.AttackRange && d < closestDist)
                        {
                            closestDist = d;
                            bestTarget = minion;
                        }
                    }
                }

                // Priority 2: ฮีโร่ศัตรู หากไม่มีครีบศัตรูในระยะ
                if (bestTarget == null)
                {
                    var enemyHero = enemyTeam == 0 ? BlueHero : RedHero;
                    if (enemyHero != null && enemyHero.IsAlive)
                    {
                        float d = Vector3.Distance(tower.Position, enemyHero.Position);
                        if (d <= tower.Stats.AttackRange)
                        {
                            bestTarget = enemyHero;
                        }
                    }
                }

                if (bestTarget != null)
                {
                    tower.TryAttackTarget(bestTarget.TargetId, bestTarget.Position, (dmg, type) =>
                    {
                        bestTarget.TakeDamage(dmg, type, tower.TowerId);
                    });
                }
            }
        }

        private void UpdateBackdoorProtection(List<TowerEntity> towers, int enemyTeam)
        {
            for (int i = 0; i < towers.Count; i++)
            {
                var tower = towers[i];
                if (tower == null || tower.IsDestroyed) continue;

                bool enemyCreepsNearby = false;
                float protectionRange = tower.Stats.AttackRange + 2.0f;
                for (int j = 0; j < ActiveMinions.Count; j++)
                {
                    var m = ActiveMinions[j];
                    if (m.IsAlive && m.TeamId == enemyTeam)
                    {
                        if (Vector3.Distance(m.Position, tower.Position) <= protectionRange)
                        {
                            enemyCreepsNearby = true;
                            break;
                        }
                    }
                }
                tower.IsBackdoorProtectionActive = !enemyCreepsNearby;
            }
        }

        private void TriggerGameOver(int winningTeam)
        {
            if (IsGameOver) return;

            IsGameOver = true;
            WinningTeam = winningTeam;
            string winnerName = winningTeam == 0 ? "Blue Team (Victory)" : "Red Team (Victory)";
            OnKillFeedMessage?.Invoke($"Game Over! {winnerName} destroyed the Nexus!");
            OnGameOver?.Invoke(winningTeam);
        }
    }
}
