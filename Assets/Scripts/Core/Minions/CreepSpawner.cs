using System;
using System.Collections.Generic;
using UnityEngine;

namespace KOA.Core.Minions
{
    /// <summary>
    /// ผู้ปล่อยเวฟครีป (Creep Spawner) ตาม Section 3.2
    /// ปล่อยทุก 25 วินาที เวฟละ 2 Melee + 1 Ranged มุ่งหน้าสู่เลน
    /// </summary>
    public class CreepSpawner
    {
        public const float WaveIntervalSeconds = 25.0f; // Section 3.2

        public int TeamId { get; private set; }
        public Vector3 SpawnPoint { get; private set; }
        public Vector3 LaneDestination { get; private set; }
        public int TotalWavesSpawned { get; private set; } = 0;

        /// <summary>
        /// เมื่อทำลายป้อม 1 (Outer Tower) ของศัตรูได้ จะได้รับครีปปืนใหญ่ (Cannon Minion) เพิ่มในเวฟ
        /// </summary>
        public bool HasCannonMinion { get; set; } = false;

        /// <summary>
        /// เมื่อทำลายป้อม 2 (Inner Tower) ของศัตรูได้ จะได้รับ Super Creep (Boss Creep) เพิ่มในเวฟ
        /// </summary>
        public bool HasSuperMinion { get; set; } = false;

        private float _spawnTimer = 0f;
        private int _minionCounter = 0;

        public event Action<List<MinionEntity>> OnWaveSpawned;

        public CreepSpawner(int teamId, Vector3 spawnPoint, Vector3 laneDestination)
        {
            TeamId = teamId;
            SpawnPoint = spawnPoint;
            LaneDestination = laneDestination;
            _spawnTimer = 5.0f; // เวฟแรกเริ่มปล่อยหลังเกมเริ่ม 5 วินาที
        }

        public void SimulationTick(float deltaTime)
        {
            _spawnTimer -= deltaTime;
            if (_spawnTimer <= 0f)
            {
                SpawnWave();
                _spawnTimer = WaveIntervalSeconds;
            }
        }

        private void SpawnWave()
        {
            TotalWavesSpawned++;
            List<MinionEntity> wave = new List<MinionEntity>();
            Vector3 backDir = (SpawnPoint - LaneDestination).normalized;
            if (backDir.sqrMagnitude < 0.001f) backDir = TeamId == 0 ? new Vector3(0, 0, -1) : new Vector3(0, 0, 1);

            // 1. Melee Minion ตัวที่ 1
            _minionCounter++;
            wave.Add(new MinionEntity(
                $"minion_{TeamId}_{_minionCounter}",
                MinionType.Melee,
                TeamId,
                SpawnPoint + new Vector3(-0.9f, 0, 0),
                TotalWavesSpawned
            ));

            // 2. Melee Minion ตัวที่ 2
            _minionCounter++;
            wave.Add(new MinionEntity(
                $"minion_{TeamId}_{_minionCounter}",
                MinionType.Melee,
                TeamId,
                SpawnPoint + new Vector3(0.9f, 0, 0),
                TotalWavesSpawned
            ));

            // 3. Ranged Minion ตัวที่ 3
            _minionCounter++;
            wave.Add(new MinionEntity(
                $"minion_{TeamId}_{_minionCounter}",
                MinionType.Ranged,
                TeamId,
                SpawnPoint + backDir * 1.5f,
                TotalWavesSpawned
            ));

            // 4. Cannon Minion (เมื่อทำลายป้อม 1 ของศัตรูได้)
            if (HasCannonMinion)
            {
                _minionCounter++;
                wave.Add(new MinionEntity(
                    $"minion_cannon_{TeamId}_{_minionCounter}",
                    MinionType.Cannon,
                    TeamId,
                    SpawnPoint + backDir * 3.0f,
                    TotalWavesSpawned
                ));
            }

            // 5. Super Creep / Boss Creep (เมื่อทำลายป้อม 2 ของศัตรูได้)
            if (HasSuperMinion)
            {
                _minionCounter++;
                wave.Add(new MinionEntity(
                    $"minion_super_{TeamId}_{_minionCounter}",
                    MinionType.Super,
                    TeamId,
                    SpawnPoint + backDir * 4.5f,
                    TotalWavesSpawned
                ));
            }

            OnWaveSpawned?.Invoke(wave);
        }
    }
}
