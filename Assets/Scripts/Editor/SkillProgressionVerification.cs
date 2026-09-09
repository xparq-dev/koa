#if UNITY_EDITOR
using KOA.Core.AI;
using KOA.Core.Economy;
using KOA.Core.Entities;
using KOA.Core.Items;
using KOA.Core.Match;
using KOA.Core.Structures;
using KOA.Core.Vision;
using KOA.Core.World;
using KOA.Data.Enums;
using KOA.Data.Models;
using UnityEditor;
using UnityEngine;

namespace KOA.Editor
{
    /// <summary>
    /// Test Suite สำหรับทดสอบระบบความก้าวหน้าสกิล พรสวรรค์ และความสมดุล (Inspire MOBA Progression)
    /// </summary>
    public static class SkillProgressionVerification
    {
        [MenuItem("KOA/Run Skill Progression & Balance Tests")]
        public static void RunAllTests()
        {
            Debug.Log("<color=cyan><b>[KOA Test Suite] Starting KOA Skill Progression & Balance Verification...</b></color>");
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
                Assert(k.BaseAttackDamage == 46f, $"Korvax BaseAD expected 46 (Balance Pass), got {k.BaseAttackDamage}");
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

            RunTest("Talent Tree Effects (Section 6.6)", ref passed, ref total, () =>
            {
                var hpTalent = new KorvaxHero(Vector3.zero);
                Assert(!hpTalent.TrySelectTalent(1, 0), "Talent selection should fail before Level 4");
                while (hpTalent.CurrentLevel < 4) hpTalent.AddExp(1000f);
                float hpBefore = hpTalent.EffectiveMaxHp;
                Assert(hpTalent.TrySelectTalent(1, 0), "Level 4 Talent A should be selectable");
                Assert(Mathf.Approximately(hpTalent.EffectiveMaxHp, hpBefore + 75f), "Level 4 Talent A must add 75 Max HP");

                var cdrTalent = new KorvaxHero(Vector3.zero);
                while (cdrTalent.CurrentLevel < 4) cdrTalent.AddExp(1000f);
                Assert(cdrTalent.TrySelectTalent(1, 1), "Level 4 Talent B should be selectable");
                Assert(Mathf.Approximately(cdrTalent.EffectiveCooldownReduction, 0.10f), "Level 4 Talent B must add 10% CDR");

                var tier2Talent = new VorkasHero(Vector3.zero);
                while (tier2Talent.CurrentLevel < 8) tier2Talent.AddExp(1000f);
                Assert(tier2Talent.TrySelectTalent(2, 0), "Level 8 Talent A should be selectable");
                Assert(Mathf.Abs(tier2Talent.EffectiveAttackCooldownFromBase(1f) - (1f / 1.15f)) < 0.001f,
                    "Level 8 Talent A must add 15% Attack Speed");

                var skillDamageTalent = new VorkasHero(Vector3.zero);
                while (skillDamageTalent.CurrentLevel < 8) skillDamageTalent.AddExp(1000f);
                Assert(skillDamageTalent.TrySelectTalent(2, 1), "Level 8 Talent B should be selectable");
                Assert(Mathf.Approximately(skillDamageTalent.EffectiveSkillDamage(100f), 115f), "Level 8 Talent B must add 15% skill damage");

                var moveTalent = new GravitorHero(Vector3.zero);
                while (moveTalent.CurrentLevel < 12) moveTalent.AddExp(2000f);
                float moveBefore = moveTalent.EffectiveMoveSpeed;
                Assert(moveTalent.TrySelectTalent(3, 0), "Level 12 Talent A should be selectable");
                Assert(Mathf.Approximately(moveTalent.EffectiveMoveSpeed, moveBefore * 1.20f), "Level 12 Talent A must add 20% Move Speed");

                var normalDefender = new KorvaxHero(Vector3.zero);
                var reducedDefender = new KorvaxHero(Vector3.zero);
                while (reducedDefender.CurrentLevel < 12) reducedDefender.AddExp(2000f);
                Assert(reducedDefender.TrySelectTalent(3, 1), "Level 12 Talent B should be selectable");
                float normalBefore = normalDefender.CurrentHp;
                float reducedBefore = reducedDefender.CurrentHp;
                normalDefender.TakeDamage(100f, DamageType.TrueDamage);
                reducedDefender.TakeDamage(100f, DamageType.TrueDamage);
                Assert(Mathf.Abs((reducedBefore - reducedDefender.CurrentHp) - (normalBefore - normalDefender.CurrentHp) * 0.90f) < 0.001f,
                    "Level 12 Talent B must reduce all incoming damage by 10%");
            });

            RunTest("Separate Attribute Points (Section 6.7)", ref passed, ref total, () =>
            {
                var v = new VorkasHero(Vector3.zero);
                Assert(v.AvailableAttributePoints == 0, "Level 1 should start without an Attribute Point");
                v.AddExp(v.RequiredExp);
                Assert(v.AvailableAttributePoints == 1, "Each level-up must award one separate Attribute Point");
                float hpBefore = v.EffectiveMaxHp;
                Assert(v.TrySpendAttributePoint(HeroAttribute.Vitality), "Vitality spend should succeed");
                Assert(Mathf.Abs(v.EffectiveMaxHp - hpBefore * 1.02f) < 0.01f, "Vitality must add 2% current Max HP");

                while (v.CurrentLevel < 3) v.AddExp(1000f);
                float manaBefore = v.EffectiveMaxMana;
                Assert(v.TrySpendAttributePoint(HeroAttribute.Focus), "Focus spend should succeed");
                Assert(Mathf.Abs(v.EffectiveMaxMana - manaBefore * 1.02f) < 0.01f, "Focus must add 2% current Max Mana");

                while (v.CurrentLevel < 4) v.AddExp(1000f);
                float armorBefore = v.EffectiveArmor;
                Assert(v.TrySpendAttributePoint(HeroAttribute.Armor), "Armor spend should succeed");
                Assert(Mathf.Approximately(v.EffectiveArmor, armorBefore + 1f), "Armor must add 1 per point");

                while (v.CurrentLevel < 5) v.AddExp(1000f);
                float mrBefore = v.EffectiveMagicResist;
                Assert(v.TrySpendAttributePoint(HeroAttribute.Resolve), "Resolve spend should succeed");
                Assert(Mathf.Approximately(v.EffectiveMagicResist, mrBefore + 1f), "Resolve must add 1 Magic Resist per point");
                Assert(v.AvailableSkillPoints > 0, "Spending Attribute Points must not consume Skill Points");
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

                Assert(Mathf.Abs(protectedDamageTaken - normalDamageTaken * 0.30f) < 0.001f,
                    $"Protected damage should be exactly 30% (70% reduction)! Normal: {normalDamageTaken}, Protected: {protectedDamageTaken}");
            });

            RunTest("Tower Escalation Spawns (Section 3.2)", ref passed, ref total, () =>
            {
                var match = new MatchSimulation(new Vector3(0f, 0f, -60f), new Vector3(0f, 0f, 60f));
                bool cannonSpawned = false;
                bool superSpawned = false;
                match.BlueSpawner.OnWaveSpawned += wave =>
                {
                    cannonSpawned |= wave.Exists(minion => minion.Type == KOA.Core.Minions.MinionType.Cannon);
                    superSpawned |= wave.Exists(minion => minion.Type == KOA.Core.Minions.MinionType.Super);
                };

                match.RedOuterTower.IsBackdoorProtectionActive = false;
                match.RedOuterTower.TakeDamage(100000f, DamageType.TrueDamage);
                Assert(match.BlueSpawner.HasCannonMinion, "Destroying the enemy Outer Tower must enable Cannon Minions");

                match.RedInnerTower.IsBackdoorProtectionActive = false;
                match.RedInnerTower.TakeDamage(100000f, DamageType.TrueDamage);
                Assert(match.BlueSpawner.HasSuperMinion, "Destroying the enemy Inner Tower must enable Super Creeps");

                match.BlueSpawner.SimulationTick(5.1f);
                Assert(cannonSpawned, "The next wave must contain a Cannon Minion");
                Assert(superSpawned, "The next wave must contain a Super Creep");
            });

            RunTest("Shop Stats, Cooldown Reduction & Active Item", ref passed, ref total, () =>
            {
                var hero = new VorkasHero(Vector3.zero);
                var wallet = new PlayerWallet(5000);
                var shop = new ShopSystem();
                var bracer = shop.AvailableCatalog.Find(i => i.ItemId == "item_iron_plate_bracer");
                var crystal = shop.AvailableCatalog.Find(i => i.ItemId == "item_focus_crystal");
                var cloak = shop.AvailableCatalog.Find(i => i.ItemId == "item_nullifying_cloak");
                float baseMaxHp = hero.EffectiveMaxHp;

                Assert(shop.TryBuyItem(bracer, wallet, hero.Inventory), "Bracer purchase should succeed");
                Assert(Mathf.Approximately(hero.EffectiveMaxHp, baseMaxHp + 150f), "Purchased HP bonus must apply immediately");
                Assert(shop.TryBuyItem(crystal, wallet, hero.Inventory), "Focus Crystal purchase should succeed");
                Assert(hero.TryLevelSkill1(), "Skill 1 level-up should succeed");
                Assert(hero.TryCastIronCleave(Vector3.forward), "Skill 1 cast should succeed");
                Assert(Mathf.Abs(hero.Skill1CooldownRemaining - 7.6f) < 0.001f, "5% CDR must reduce an 8s cooldown to 7.6s");

                Assert(hero.ApplyMovementSlow(0.5f, 5f), "Initial slow should apply");
                Assert(shop.TryBuyItem(cloak, wallet, hero.Inventory), "Nullifying Cloak purchase should succeed");
                Assert(hero.Inventory.TryUseActive(2), "Nullifying Cloak active should trigger");
                Assert(!hero.HasDebuff, "Nullifying Cloak must cleanse current debuffs");
                Assert(hero.CrowdControlImmunityRemaining > 0f, "Nullifying Cloak must grant CC immunity");
                Assert(!hero.ApplyStun(1f), "New CC must be rejected during immunity");
                hero.SimulationTick(1.1f);
                Assert(hero.ApplyStun(1f), "CC must apply again after immunity expires");
            });

            RunTest("Vision & Brush Rules", ref passed, ref total, () =>
            {
                var observer = new VorkasHero(Vector3.zero) { TeamId = 0 };
                var target = new KorvaxHero(new Vector3(0f, 0f, 8f)) { TeamId = 1 };
                Assert(VisionSystem.IsHeroVisible(observer, target), "Enemy inside vision range and outside brush should be visible");

                target.Position = new Vector3(8f, 0f, 0f);
                Assert(!VisionSystem.IsHeroVisible(observer, target), "Enemy inside brush should be hidden from an outside observer");
                observer.Position = new Vector3(8f, 0f, 1f);
                Assert(VisionSystem.IsHeroVisible(observer, target), "Enemy should be visible to an observer in the same brush");
                target.Position = new Vector3(8f, 0f, 20f);
                Assert(!VisionSystem.IsHeroVisible(observer, target), "Enemy outside hero vision range should be hidden");
            });

            RunTest("Arena Boundary Contract (Section 3.1)", ref passed, ref total, () =>
            {
                HeroBase3D[] heroes =
                {
                    new VorkasHero(new Vector3(999f, 0f, 999f)),
                    new ZenthisHero(new Vector3(-999f, 0f, -999f)),
                    new KorvaxHero(Vector3.zero),
                    new GravitorHero(Vector3.zero)
                };

                foreach (HeroBase3D hero in heroes)
                {
                    Assert(ArenaBounds.Contains(hero.Position, hero.Radius), $"{hero.DisplayName} spawn must be inside arena");
                    hero.SetMoveDestination(new Vector3(500f, 0f, -500f));
                    hero.SimulationTick(10f);
                    Assert(ArenaBounds.Contains(hero.Position, hero.Radius), $"{hero.DisplayName} movement must stay inside arena");
                    hero.TryDisplace(new Vector3(-500f, 0f, 500f));
                    Assert(ArenaBounds.Contains(hero.Position, hero.Radius), $"{hero.DisplayName} displacement must stay inside arena");
                }

                Vector2 blueCorner = ArenaBounds.ToNormalized(new Vector3(-13f, 0f, -65f));
                Vector2 redCorner = ArenaBounds.ToNormalized(new Vector3(13f, 0f, 65f));
                Assert(blueCorner == Vector2.zero, "Blue corner must map to minimap origin");
                Assert(redCorner == Vector2.one, "Red corner must map to minimap maximum");
            });

            RunTest("All 16 Abilities Cast Contract", ref passed, ref total, () =>
            {
                string[] heroes = { "Vorkas", "Zenthis", "Korvax", "Gravitor" };
                int casts = 0;
                foreach (string heroName in heroes)
                {
                    for (int skillIndex = 1; skillIndex <= 4; skillIndex++)
                    {
                        HeroBase3D attacker = CreateHero(heroName, Vector3.zero, 0);
                        HeroBase3D defender = CreateHero("Vorkas", Vector3.forward * 3f, 1);
                        PrepareSkill(attacker, skillIndex);
                        if (attacker is ZenthisHero && skillIndex == 4) attacker.SimulationTick(0.5f);
                        Assert(CastSkill(attacker, skillIndex, defender), $"{heroName} skill index {skillIndex} should cast");
                        casts++;
                    }
                }
                Assert(casts == 16, $"Expected 16 successful ability casts, got {casts}");
            });

            RunTest("Target Validation Does Not Consume Resources (Section 6.5.3)", ref passed, ref total, () =>
            {
                var gravitor = new GravitorHero(Vector3.zero) { TeamId = 0 };
                Assert(gravitor.TryLevelSkill1(), "Gravitor Q should level");
                float gravitorMana = gravitor.CurrentMana;
                Assert(!gravitor.TryCastMagneticPull(Vector3.forward * 3f), "Gravitor Q must reject an empty target");
                Assert(Mathf.Approximately(gravitor.CurrentMana, gravitorMana), "Rejected Gravitor Q must not consume Mana");
                Assert(Mathf.Approximately(gravitor.Skill1CooldownRemaining, 0f), "Rejected Gravitor Q must not start cooldown");

                var distantEnemy = new VorkasHero(Vector3.forward * 20f) { TeamId = 1 };
                Assert(!gravitor.TryCastMagneticPull(distantEnemy.Position, distantEnemy), "Gravitor Q must reject an out-of-range target");
                Assert(Mathf.Approximately(gravitor.CurrentMana, gravitorMana), "Out-of-range Gravitor Q must not consume Mana");
                Assert(Mathf.Approximately(gravitor.Skill1CooldownRemaining, 0f), "Out-of-range Gravitor Q must not start cooldown");

                var korvax = new KorvaxHero(Vector3.zero) { TeamId = 0 };
                Assert(korvax.TryLevelSkill3(), "Korvax E should level");
                float korvaxMana = korvax.CurrentMana;
                Assert(!korvax.TryCastConcussiveBlast(Vector3.forward * 3f), "Korvax E must reject an empty target");
                Assert(Mathf.Approximately(korvax.CurrentMana, korvaxMana), "Rejected Korvax E must not consume Mana");
                Assert(Mathf.Approximately(korvax.Skill3CooldownRemaining, 0f), "Rejected Korvax E must not start cooldown");

                var zenthis = new ZenthisHero(Vector3.zero) { TeamId = 0 };
                Assert(zenthis.TryLevelSkill1(), "Zenthis Q should level");
                float zenthisMana = zenthis.CurrentMana;
                Assert(!zenthis.TryCastSacredHourglass(new Vector3(500f, 0f, 500f)), "Ground-target Q must reject a point outside the arena");
                Assert(Mathf.Approximately(zenthis.CurrentMana, zenthisMana), "Invalid ground target must not consume Mana");
                Assert(Mathf.Approximately(zenthis.Skill1CooldownRemaining, 0f), "Invalid ground target must not start cooldown");
            });

            RunTest("Ability Cooldown Recovery Allows Repeat Cast", ref passed, ref total, () =>
            {
                string[] heroes = { "Vorkas", "Zenthis", "Korvax", "Gravitor" };
                foreach (string heroName in heroes)
                {
                    HeroBase3D attacker = CreateHero(heroName, Vector3.zero, 0);
                    HeroBase3D defender = CreateHero("Vorkas", Vector3.forward * 3f, 1);
                    Assert(attacker.TryLevelSkill1(), $"{heroName} Skill 1 should level");
                    Assert(CastSkill1(attacker, defender), $"{heroName} first Skill 1 cast should succeed");
                    attacker.SimulationTick(20f);
                    Assert(CastSkill1(attacker, defender), $"{heroName} Skill 1 should cast again after cooldown recovery");
                }
            });

            RunTest("All 4x4 Hero Matchup Skill Smoke Matrix", ref passed, ref total, () =>
            {
                string[] heroes = { "Vorkas", "Zenthis", "Korvax", "Gravitor" };
                int scenarios = 0;
                foreach (string attackerName in heroes)
                {
                    foreach (string defenderName in heroes)
                    {
                        HeroBase3D attacker = CreateHero(attackerName, Vector3.zero, 0);
                        HeroBase3D defender = CreateHero(defenderName, Vector3.forward * 3f, 1);
                        Assert(attacker.TryLevelSkill1(), $"{attackerName} Skill 1 should level");
                        float hpBefore = defender.CurrentHp;
                        Assert(CastSkill1(attacker, defender), $"{attackerName} Skill 1 should cast against {defenderName}");
                        bool damageApplied = defender.CurrentHp < hpBefore || (defender is GravitorHero gravitor && gravitor.CurrentShield > 0f);
                        Assert(damageApplied, $"{attackerName} must damage HP or trigger/consume the Antigravity Shield on {defenderName}");
                        Assert(IsFinite(defender.CurrentHp), $"{attackerName} vs {defenderName} produced invalid HP");
                        scenarios++;
                    }
                }
                Assert(scenarios == 16, $"Expected 16 matchup scenarios, got {scenarios}");
            });

            RunTest("20-Minute Core Stability Simulation", ref passed, ref total, () =>
            {
                var match = new MatchSimulation(new Vector3(0f, 0f, -60f), new Vector3(0f, 0f, 60f));
                match.BlueHero = new VorkasHero(new Vector3(0f, 0f, -60f)) { TeamId = 0 };
                match.RedHero = new GravitorHero(new Vector3(0f, 0f, 60f)) { TeamId = 1 };
                const float tick = 1f / 30f;
                const int tickCount = 20 * 60 * 30;

                for (int i = 0; i < tickCount; i++)
                {
                    match.BlueHero.SimulationTick(tick);
                    match.RedHero.SimulationTick(tick);
                    match.SimulationTick(tick);
                }

                Assert(Mathf.Abs(match.MatchTime - 1200f) < 1f, $"Expected 20 simulated minutes, got {match.MatchTime:F2}s");
                Assert(IsFinite(match.BlueHero.CurrentHp) && IsFinite(match.RedHero.CurrentHp), "Hero state must remain finite");
                Assert(match.ActiveMinions.Count < 300, $"Active minion count should remain bounded, got {match.ActiveMinions.Count}");
            });

            RunTest("ModularBotBrain Auto-Allocation", ref passed, ref total, () =>
            {
                var botHero = new VorkasHero(Vector3.zero);
                var botBrain = new ModularBotBrain(botHero, Vector3.zero, Vector3.forward * 50f);
                Assert(botHero.AvailableSkillPoints == 1, "Bot should start with 1 point");

                botBrain.AutoAllocateSkillPoints();
                Assert(botHero.AvailableSkillPoints == 0, "Bot should have allocated its initial skill point");
                Assert(botHero.Skill1Rank == 1, "Bot should have allocated to Skill 1");

                botHero.AddExp(botHero.RequiredExp);
                Assert(botHero.AvailableAttributePoints == 1, "Bot should receive a separate Attribute Point on level-up");
                botBrain.AutoAllocateAttributePoints();
                Assert(botHero.AvailableAttributePoints == 0, "Bot should allocate available Attribute Points");
                Assert(botHero.VitalityPoints == 1, "Vorkas bot should prioritize Vitality first");
            });

            Debug.Log($"<color={(passed == total ? "green" : "red")}><b>[KOA Test Suite] Results: {passed}/{total} Tests Passed!</b></color>");
            if (passed != total)
                throw new System.Exception($"KOA verification failed: {passed}/{total} tests passed.");
        }

        private static HeroBase3D CreateHero(string name, Vector3 position, int teamId)
        {
            HeroBase3D hero = name switch
            {
                "Zenthis" => new ZenthisHero(position),
                "Korvax" => new KorvaxHero(position),
                "Gravitor" => new GravitorHero(position),
                _ => new VorkasHero(position)
            };
            hero.TeamId = teamId;
            return hero;
        }

        private static bool CastSkill1(HeroBase3D attacker, HeroBase3D defender)
        {
            if (attacker is VorkasHero v) return v.TryCastIronCleave(defender.Position, defender);
            if (attacker is ZenthisHero z) return z.TryCastSacredHourglass(defender.Position, defender);
            if (attacker is KorvaxHero k) return k.TryCastHeavyBolt(defender.Position, defender);
            if (attacker is GravitorHero g) return g.TryCastMagneticPull(defender.Position, defender);
            return false;
        }

        private static void PrepareSkill(HeroBase3D hero, int skillIndex)
        {
            if (skillIndex == 4)
            {
                while (hero.CurrentLevel < 6) hero.AddExp(1000f);
                Assert(hero.TryLevelUltimate(), $"{hero.DisplayName} Ultimate should level at level 6");
                return;
            }

            bool leveled = skillIndex switch
            {
                1 => hero.TryLevelSkill1(),
                2 => hero.TryLevelSkill2(),
                3 => hero.TryLevelSkill3(),
                _ => false
            };
            Assert(leveled, $"{hero.DisplayName} skill index {skillIndex} should level");
        }

        private static bool CastSkill(HeroBase3D attacker, int skillIndex, HeroBase3D defender)
        {
            if (attacker is VorkasHero v)
            {
                if (skillIndex == 1) return v.TryCastIronCleave(defender.Position, defender);
                if (skillIndex == 2) return v.TryCastVanguardsWill();
                if (skillIndex == 3) return v.TryCastSeismicSlam(defender.Position, defender);
                return v.TryCastRebellionImpact(defender.Position, defender);
            }
            if (attacker is ZenthisHero z)
            {
                if (skillIndex == 1) return z.TryCastSacredHourglass(defender.Position, defender);
                if (skillIndex == 2) return z.TryCastAuraOfEternity();
                if (skillIndex == 3) return z.TryCastTemporalRift(defender.Position, defender);
                return z.TryCastGrandRewind();
            }
            if (attacker is KorvaxHero k)
            {
                if (skillIndex == 1) return k.TryCastHeavyBolt(defender.Position, defender);
                if (skillIndex == 2) return k.TryCastHuntersFocus();
                if (skillIndex == 3) return k.TryCastConcussiveBlast(defender.Position, defender);
                return k.TryCastBallistaOverdrive(defender.Position, defender);
            }
            if (attacker is GravitorHero g)
            {
                if (skillIndex == 1) return g.TryCastMagneticPull(defender.Position, defender);
                if (skillIndex == 2) return g.TryCastRepulsionZone(defender);
                if (skillIndex == 3) return g.TryCastGravitonWell(defender.Position, defender);
                return g.TryCastGravityCollapse(defender.Position, defender);
            }
            return false;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
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
