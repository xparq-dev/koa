#if UNITY_EDITOR
using KOA.Core.AI;
using KOA.Core.Entities;
using KOA.Core.Structures;
using KOA.Data.Enums;
using KOA.Data.Models;
using UnityEditor;
using UnityEngine;

namespace KOA.Editor
{
    public static class Dota2ProgressionVerification
    {
        [MenuItem("KOA/Run Dota2 Balance & Skill Tests")]
        public static void RunAllTests()
        {
            Debug.Log("<color=cyan><b>[KOA Test Suite] Starting Dota 2 Progression & Balance Verification...</b></color>");
            int passed = 0;
            int total = 0;

            RunTest("Base Attack Damage Tuning", ref passed, ref total, () =>
            {
                var v = new VorkasHero(Vector3.zero);
                var z = new ZenthisHero(Vector3.zero);
                var k = new KorvaxHero(Vector3.zero);
                var g = new GravitorHero(Vector3.zero);

                Assert(v.BaseAttackDamage == 54f, $"Vorkas BaseAD expected 54, got {v.BaseAttackDamage}");
                Assert(z.BaseAttackDamage == 44f, $"Zenthis BaseAD expected 44, got {z.BaseAttackDamage}");
                Assert(k.BaseAttackDamage == 52f, $"Korvax BaseAD expected 52, got {k.BaseAttackDamage}");
                Assert(g.BaseAttackDamage == 48f, $"Gravitor BaseAD expected 48, got {g.BaseAttackDamage}");
            });

            RunTest("Level 1 Skill Point & Skill Lock", ref passed, ref total, () =>
            {
                var v = new VorkasHero(Vector3.zero);
                Assert(v.AvailableSkillPoints == 1, $"Expected 1 initial skill point, got {v.AvailableSkillPoints}");
                Assert(v.Skill1Rank == 0, $"Expected Skill1Rank 0, got {v.Skill1Rank}");

                // Skill cannot be cast at Rank 0
                bool castResult = v.TryCastIronCleave(Vector3.forward);
                Assert(!castResult, "Skill 1 cast should FAIL when rank is 0!");

                // Level up Skill 1
                bool levelUp = v.TryLevelSkill1();
                Assert(levelUp, "TryLevelSkill1 should SUCCEED!");
                Assert(v.Skill1Rank == 1, "Skill1Rank should now be 1");
                Assert(v.AvailableSkillPoints == 0, "AvailableSkillPoints should now be 0");

                // Now skill can be cast
                bool castResult2 = v.TryCastIronCleave(Vector3.forward);
                Assert(castResult2, "Skill 1 cast should SUCCEED after spending skill point!");
            });

            RunTest("Ultimate Level Lock (Lv 6, 10, 12)", ref passed, ref total, () =>
            {
                var z = new ZenthisHero(Vector3.zero);
                Assert(!z.CanLevelUltimate(), "Ultimate should NOT be levelable at Level 1!");
                Assert(!z.TryLevelUltimate(), "TryLevelUltimate should FAIL at Level 1!");

                // Give points and levels up to 5
                z.AddExp(3000f); // Level up
                while (z.CurrentLevel < 6) z.AddExp(1000f);
                Assert(z.CurrentLevel >= 6, $"Hero should be level 6+, currently {z.CurrentLevel}");
                Assert(z.CanLevelUltimate(), "Ultimate SHOULD be levelable at Level 6!");
                Assert(z.TryLevelUltimate(), "TryLevelUltimate should SUCCEED at Level 6!");
                Assert(z.UltimateRank == 1, $"UltimateRank should be 1, got {z.UltimateRank}");

                // Rank 2 should fail until Lv 10
                if (z.AvailableSkillPoints > 0 && z.CurrentLevel < 10)
                {
                    Assert(!z.CanLevelUltimate(), "Ultimate Rank 2 should NOT be levelable before Level 10!");
                }
            });

            RunTest("Talent Tree Selection", ref passed, ref total, () =>
            {
                var k = new KorvaxHero(Vector3.zero);
                Assert(k.TalentTier1Choice == -1, "TalentTier1 should start as -1 (unchosen)");
                Assert(!k.TrySelectTalent(1, 0), "Talent selection should FAIL before Level 4!");

                // Level up to 4
                while (k.CurrentLevel < 4) k.AddExp(500f);
                Assert(k.TrySelectTalent(1, 1), "Talent Tier 1 selection should SUCCEED at Level 4!");
                Assert(k.TalentTier1Choice == 1, "TalentTier1Choice should be 1");
            });

            RunTest("Stat Bonus (+Attributes)", ref passed, ref total, () =>
            {
                var v = new VorkasHero(Vector3.zero);
                float initialMaxHp = v.EffectiveMaxHp;
                float initialAd = v.EffectiveAttackDamage;

                v.TryLevelStatBonus();
                Assert(v.StatBonusRank == 1, "StatBonusRank should be 1");
                Assert(v.EffectiveMaxHp == initialMaxHp + 60f, "EffectiveMaxHp should increase by +60");
                Assert(v.EffectiveAttackDamage == initialAd + 3.0f, "EffectiveAttackDamage should increase by +3.0");
            });

            RunTest("Tower Anti-Backdoor Protection", ref passed, ref total, () =>
            {
                var tower = new TowerEntity("test_tower", StructureStats.CreateTier1OuterTower(), Vector3.zero);
                tower.IsBackdoorProtectionActive = false;
                float hpBefore = tower.CurrentHp;
                tower.TakeDamage(100f, DamageType.Physical);
                float normalDamageTaken = hpBefore - tower.CurrentHp;

                var tower2 = new TowerEntity("test_tower2", StructureStats.CreateTier1OuterTower(), Vector3.zero);
                tower2.IsBackdoorProtectionActive = true; // Anti-Backdoor active (no enemy creeps)
                float hpBefore2 = tower2.CurrentHp;
                tower2.TakeDamage(100f, DamageType.Physical);
                float protectedDamageTaken = hpBefore2 - tower2.CurrentHp;

                Assert(Mathf.Approximately(protectedDamageTaken, normalDamageTaken * 0.30f),
                    $"Protected damage should be exactly 30% (70% reduction)! Normal: {normalDamageTaken}, Protected: {protectedDamageTaken}");
            });

            RunTest("ModularBotBrain Auto-Allocation", ref passed, ref total, () =>
            {
                var botHero = new VorkasHero(Vector3.zero);
                var botBrain = new ModularBotBrain(botHero, Vector3.zero, Vector3.forward * 50f);
                Assert(botHero.AvailableSkillPoints == 1, "Bot should start with 1 point");

                botBrain.AutoAllocateSkillPoints();
                Assert(botHero.AvailableSkillPoints == 0, "Bot should have allocated its initial skill point");
                Assert(botHero.Skill1Rank == 1, "Bot should have allocated to Skill 1");
            });

            Debug.Log($"<color={(passed == total ? "green" : "red")}><b>[KOA Test Suite] Results: {passed}/{total} Tests Passed!</b></color>");
        }

        private static void RunTest(string testName, ref int passed, ref int total, System.Action testAction)
        {
            total++;
            try
            {
                testAction();
                passed++;
                Debug.Log($"<color=green>  [PASS] {testName}</color>");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"<color=red>  [FAIL] {testName}: {ex.Message}</color>");
            }
        }

        private static void Assert(bool condition, string failureMessage)
        {
            if (!condition) throw new System.Exception(failureMessage);
        }
    }
}
#endif
