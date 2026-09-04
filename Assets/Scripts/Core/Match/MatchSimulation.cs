using KOA.Core.Economy;
using KOA.Core.Entities;
using KOA.Core.Minions;
using KOA.Core.Structures;
using KOA.Data.Models;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KOA.Core.Match
{
    /// <summary>
    /// ตัวจำลองการแข่งขัน 1v1 ตลอดทั้งแมตช์ (Simulation Core)
    /// ควบคุมป้อมปราการ 2 Tier + Nexus (Section 3.3), ครีป (Section 3.2), เศรษฐกิจ (Section 4), และการตัดสินผลชนะ
    /// </summary>
    public class MatchSimulation
    {
        public bool IsGameOver { get; private set; } = false;
        public int WinningTeam { get; private set; } = -1; // 0 = Blue, 1 = Red
        public float MatchTime { get; private set; } = 0f;

        // โครงสร้างป้อมปราการฝั่ง Blue (ทีม 0)
        public TowerEntity BlueOuterTower { get; private set; }
        public TowerEntity BlueInnerTower { get; private set; }
        public TowerEntity BlueNexus { get; private set; }

        // โครงสร้างป้อมปราการฝั่ง Red (ทีม 1)
        public TowerEntity RedOuterTower { get; private set; }
        public TowerEntity RedInnerTower { get; private set; }
        public TowerEntity RedNexus { get; private set; }

        // Creep Spawners
        public CreepSpawner BlueSpawner { get; private set; }
        public CreepSpawner RedSpawner { get; private set; }
        public List<MinionEntity> ActiveMinions { get; private set; } = new List<MinionEntity>();

        // กระเป๋าเงินผู้เล่น
        public PlayerWallet BlueWallet { get; private set; }
        public PlayerWallet RedWallet { get; private set; }

        // Events แจ้งเตือนสถานะของแมตช์
        public event Action<int> OnGameOver; // winningTeam
        public event Action<TowerEntity> OnTowerDestroyed;
        public event Action<string> OnKillFeedMessage;

        public MatchSimulation(Vector3 blueFountainPos, Vector3 redFountainPos)
        {
            // สร้างป้อมปราการฝั่ง Blue (Z ติดลบ)
            BlueOuterTower = new TowerEntity("blue_tower_t1", StructureStats.CreateTier1OuterTower(), new Vector3(0, 0, -10f));
            BlueInnerTower = new TowerEntity("blue_tower_t2", StructureStats.CreateTier2InnerTower(), new Vector3(0, 0, -20f));
            BlueNexus = new TowerEntity("blue_nexus", StructureStats.CreateNexusCore(), new Vector3(0, 0, -30f));

            // สร้างป้อมปราการฝั่ง Red (Z เป็นบวก)
            RedOuterTower = new TowerEntity("red_tower_t1", StructureStats.CreateTier1OuterTower(), new Vector3(0, 0, 10f));
            RedInnerTower = new TowerEntity("red_tower_t2", StructureStats.CreateTier2InnerTower(), new Vector3(0, 0, 20f));
            RedNexus = new TowerEntity("red_nexus", StructureStats.CreateNexusCore(), new Vector3(0, 0, 30f));

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
        }

        private void HookTowerEvents()
        {
            // Blue Towers
            BlueOuterTower.OnDestroyed += () =>
            {
                BlueInnerTower.IsInvulnerable = false;
                RedWallet.AddGold(StructureStats.CreateTier1OuterTower().GoldBounty);
                OnTowerDestroyed?.Invoke(BlueOuterTower);
                OnKillFeedMessage?.Invoke("Blue Outer Tower has been destroyed!");
            };

            BlueInnerTower.OnDestroyed += () =>
            {
                BlueNexus.IsInvulnerable = false;
                RedWallet.AddGold(StructureStats.CreateTier2InnerTower().GoldBounty);
                OnTowerDestroyed?.Invoke(BlueInnerTower);
                OnKillFeedMessage?.Invoke("Blue Inner Tower has been destroyed! The Nexus is vulnerable!");
            };

            BlueNexus.OnDestroyed += () =>
            {
                TriggerGameOver(winningTeam: 1); // Red ชนะ
            };

            // Red Towers
            RedOuterTower.OnDestroyed += () =>
            {
                RedInnerTower.IsInvulnerable = false;
                BlueWallet.AddGold(StructureStats.CreateTier1OuterTower().GoldBounty);
                OnTowerDestroyed?.Invoke(RedOuterTower);
                OnKillFeedMessage?.Invoke("Red Outer Tower has been destroyed!");
            };

            RedInnerTower.OnDestroyed += () =>
            {
                RedNexus.IsInvulnerable = false;
                BlueWallet.AddGold(StructureStats.CreateTier2InnerTower().GoldBounty);
                OnTowerDestroyed?.Invoke(RedInnerTower);
                OnKillFeedMessage?.Invoke("Red Inner Tower has been destroyed! The Nexus is vulnerable!");
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
            if (killerId == "player_blue")
            {
                BlueWallet.AddGold(minion.GoldBounty);
            }
            else if (killerId == "player_red")
            {
                RedWallet.AddGold(minion.GoldBounty);
            }
        }

        public void SimulationTick(float deltaTime)
        {
            if (IsGameOver) return;

            MatchTime += deltaTime;

            // อัปเดตเศรษฐกิจ Passive Gold
            BlueWallet.SimulationTick(deltaTime);
            RedWallet.SimulationTick(deltaTime);

            // อัปเดตป้อมปราการ
            BlueOuterTower.SimulationTick(deltaTime);
            BlueInnerTower.SimulationTick(deltaTime);
            BlueNexus.SimulationTick(deltaTime);

            RedOuterTower.SimulationTick(deltaTime);
            RedInnerTower.SimulationTick(deltaTime);
            RedNexus.SimulationTick(deltaTime);

            // อัปเดต Spawners
            BlueSpawner.SimulationTick(deltaTime);
            RedSpawner.SimulationTick(deltaTime);

            // อัปเดตมินเนี่ยนที่ยังมีชีวิต
            for (int i = ActiveMinions.Count - 1; i >= 0; i--)
            {
                var minion = ActiveMinions[i];
                if (!minion.IsAlive)
                {
                    ActiveMinions.RemoveAt(i);
                    continue;
                }

                Vector3 targetDest = minion.TeamId == 0 ? RedNexus.Position : BlueNexus.Position;
                minion.SimulationTick(deltaTime, targetDest);
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
