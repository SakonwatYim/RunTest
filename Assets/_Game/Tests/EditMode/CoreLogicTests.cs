using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RunChildRun.Tests
{
    public class ExpCurveTests
    {
        [TestCase(1, 14.35f)]
        [TestCase(10, 103f)]
        [TestCase(20, 268f)]
        public void ExpToNext_MatchesGdd(int level, float expected)
        {
            Assert.That(ExpCurve.ExpToNext(level, 8f, 6f, 0.35f), Is.EqualTo(expected).Within(0.01f));
        }

        [Test]
        public void TimeExp_IsOneUnarmed_QuarterArmed()
        {
            Assert.That(ExpCurve.TimeExpRate(false, 1f, 0.25f), Is.EqualTo(1f));
            Assert.That(ExpCurve.TimeExpRate(true, 1f, 0.25f), Is.EqualTo(0.25f));
        }

        [Test]
        public void RunState_AddExp_QueuesMultipleLevelUps()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            var state = new RunState();
            var gained = state.AddExp(14.35f + 20f, config); // Lv1 needs 14.35, Lv2 needs 21.4
            Assert.That(gained, Is.EqualTo(1));
            Assert.That(state.Level, Is.EqualTo(2));
            gained = state.AddExp(1.5f, config);
            Assert.That(gained, Is.EqualTo(1));
            Assert.That(state.PendingLevelUps, Is.EqualTo(2));
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void Coins_TwentyPerMinute_PlusOnePerFiveKills()
        {
            Assert.That(RunState.CoinsFor(180f, 52, 20f, 5), Is.EqualTo(60 + 10));
        }

        [Test]
        public void FormatTime_IsMinutesSeconds()
        {
            Assert.That(RunState.FormatTime(343.9f), Is.EqualTo("05:43"));
        }
    }

    public class RarityTests
    {
        static readonly float[] Weights = { 70f, 25f, 5f };

        [Test]
        public void Roll_UsesWeightBands()
        {
            Assert.That(RarityTable.Roll(Weights, 0, 5f, 2.5f, 0.01f), Is.EqualTo(Rarity.Epic));
            Assert.That(RarityTable.Roll(Weights, 0, 5f, 2.5f, 0.10f), Is.EqualTo(Rarity.Rare));
            Assert.That(RarityTable.Roll(Weights, 0, 5f, 2.5f, 0.50f), Is.EqualTo(Rarity.Common));
        }

        [Test]
        public void Luck_MovesWeightFromCommon()
        {
            RarityTable.GetWeights(Weights, 2, 5f, 2.5f, out var c, out var r, out var e);
            Assert.That(c, Is.EqualTo(55f).Within(0.001f));
            Assert.That(r, Is.EqualTo(35f).Within(0.001f));
            Assert.That(e, Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void Durations_Are45_60_75()
        {
            var d = new[] { 45f, 60f, 75f };
            Assert.That(RarityTable.SkillDuration(Rarity.Common, d), Is.EqualTo(45f));
            Assert.That(RarityTable.SkillDuration(Rarity.Rare, d), Is.EqualTo(60f));
            Assert.That(RarityTable.SkillDuration(Rarity.Epic, d), Is.EqualTo(75f));
        }
    }

    public class SkillInventoryTests
    {
        readonly List<SkillDef> created = new List<SkillDef>();

        SkillDef Def(string id)
        {
            var d = ScriptableObject.CreateInstance<SkillDef>();
            d.Id = id;
            created.Add(d);
            return d;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var d in created) UnityEngine.Object.DestroyImmediate(d);
            created.Clear();
        }

        [Test]
        public void NewSkill_StartsAtLv1()
        {
            var inv = new SkillInventory(5, 5, new Dictionary<string, int>());
            var r = inv.Pickup(Def("bolt"), 45f);
            Assert.That(r.Kind, Is.EqualTo(SkillPickupKind.New));
            Assert.That(r.NewLevel, Is.EqualTo(1));
            Assert.That(r.LeveledUp, Is.False);
        }

        [Test]
        public void Duplicate_WhileActive_RefillsAndLevelsUp()
        {
            var inv = new SkillInventory(5, 5, new Dictionary<string, int>());
            var bolt = Def("bolt");
            inv.Pickup(bolt, 45f);
            inv.Tick(30f, null);
            var r = inv.Pickup(bolt, 45f);
            Assert.That(r.Kind, Is.EqualTo(SkillPickupKind.Refreshed));
            Assert.That(r.NewLevel, Is.EqualTo(2));
            Assert.That(r.Skill.RemainingTime, Is.EqualTo(45f));
        }

        [Test]
        public void Expired_IsRemembered_AndRelearnStartsOneHigher()
        {
            var levels = new Dictionary<string, int>();
            var inv = new SkillInventory(5, 5, levels);
            var bolt = Def("bolt");
            inv.Pickup(bolt, 45f);
            inv.Pickup(bolt, 45f); // Lv2
            var expired = new List<ActiveSkill>();
            inv.Tick(46f, expired);
            Assert.That(expired.Count, Is.EqualTo(1));
            Assert.That(inv.AnyActive, Is.False);
            Assert.That(levels["bolt"], Is.EqualTo(2));

            var r = inv.Pickup(bolt, 45f);
            Assert.That(r.Kind, Is.EqualTo(SkillPickupKind.Relearned));
            Assert.That(r.PreviousLevel, Is.EqualTo(2));
            Assert.That(r.NewLevel, Is.EqualTo(3));
            Assert.That(r.LeveledUp, Is.True);
        }

        [Test]
        public void Level_CapsAtFive_ButStillRefills()
        {
            var levels = new Dictionary<string, int> { ["bolt"] = 5 };
            var inv = new SkillInventory(5, 5, levels);
            var bolt = Def("bolt");
            var r = inv.Pickup(bolt, 45f);
            Assert.That(r.NewLevel, Is.EqualTo(5));
            inv.Tick(40f, null);
            r = inv.Pickup(bolt, 60f);
            Assert.That(r.NewLevel, Is.EqualTo(5));
            Assert.That(r.Skill.RemainingTime, Is.EqualTo(60f));
        }

        [Test]
        public void FullBar_ReplacesSkillWithLeastTime_AndKeepsItsLevel()
        {
            var levels = new Dictionary<string, int>();
            var inv = new SkillInventory(2, 5, levels);
            var a = Def("a");
            var b = Def("b");
            var c = Def("c");
            inv.Pickup(a, 45f);
            inv.Pickup(a, 45f); // a Lv2
            inv.Tick(10f, null);
            inv.Pickup(b, 45f);
            var r = inv.Pickup(c, 45f);
            Assert.That(r.Replaced, Is.Not.Null);
            Assert.That(r.Replaced.Def, Is.SameAs(a));
            Assert.That(inv.Active.Count, Is.EqualTo(2));
            Assert.That(levels["a"], Is.EqualTo(2));
        }

        [Test]
        public void Timers_AreIndependent()
        {
            var inv = new SkillInventory(5, 5, new Dictionary<string, int>());
            inv.Pickup(Def("a"), 45f);
            inv.Tick(20f, null);
            inv.Pickup(Def("b"), 60f);
            var expired = new List<ActiveSkill>();
            inv.Tick(26f, expired);
            Assert.That(expired.Count, Is.EqualTo(1));
            Assert.That(inv.Active[0].RemainingTime, Is.EqualTo(34f).Within(0.001f));
        }
    }

    public class ItemDropSchedulerTests
    {
        static ItemDropScheduler Make(float r = 0f) => new ItemDropScheduler(12f, 40f, 25f, 40f, 20f, 1.5f, () => r);

        [Test]
        public void FirstItem_At12Seconds()
        {
            var s = Make();
            Assert.That(s.Update(11.9f, 0.1f, false, false, 0), Is.EqualTo(ItemSpawnReason.None));
            Assert.That(s.Update(12f, 0.1f, false, false, 0), Is.EqualTo(ItemSpawnReason.First));
        }

        [Test]
        public void SecondItem_GuaranteedAt40()
        {
            var s = Make();
            s.MarkSpawned(ItemSpawnReason.First, 12f);
            Assert.That(s.Update(39f, 0.1f, true, true, 0), Is.EqualTo(ItemSpawnReason.None));
            Assert.That(s.Update(40f, 0.1f, true, true, 0), Is.EqualTo(ItemSpawnReason.Second));
        }

        [Test]
        public void RandomItems_Every25To40Seconds()
        {
            var s = Make(0.5f);
            s.MarkSpawned(ItemSpawnReason.First, 12f);
            s.MarkSpawned(ItemSpawnReason.Second, 40f);
            Assert.That(s.NextRandomTime, Is.EqualTo(40f + 32.5f).Within(0.001f));
            Assert.That(s.Update(72f, 0.1f, true, true, 1), Is.EqualTo(ItemSpawnReason.None));
            Assert.That(s.Update(72.5f, 0.1f, true, true, 1), Is.EqualTo(ItemSpawnReason.Random));
        }

        [Test]
        public void BadLuck_AfterTwentySecondsUnarmed_WithNothingOnGround()
        {
            var s = Make(1f);
            s.MarkSpawned(ItemSpawnReason.First, 12f);
            s.MarkSpawned(ItemSpawnReason.Second, 40f); // next random at 80
            var t = 40f;
            ItemSpawnReason reason = ItemSpawnReason.None;
            for (var i = 0; i < 201 && reason == ItemSpawnReason.None; i++)
            {
                t += 0.1f;
                reason = s.Update(t, 0.1f, true, false, 0);
            }
            Assert.That(reason, Is.EqualTo(ItemSpawnReason.BadLuck));
            Assert.That(t, Is.EqualTo(60.1f).Within(0.15f));
        }

        [Test]
        public void BadLuck_NotWhileItemOnGround()
        {
            var s = Make(1f);
            s.MarkSpawned(ItemSpawnReason.First, 12f);
            s.MarkSpawned(ItemSpawnReason.Second, 40f);
            for (var i = 0; i < 250; i++)
            {
                Assert.That(s.Update(40f + i * 0.1f, 0.1f, true, false, 1), Is.EqualTo(ItemSpawnReason.None));
            }
        }

        [Test]
        public void Postpone_RetriesAfterDelay()
        {
            var s = Make();
            Assert.That(s.Update(12f, 0.1f, false, false, 0), Is.EqualTo(ItemSpawnReason.First));
            s.Postpone(ItemSpawnReason.First, 12f);
            Assert.That(s.Update(13f, 0.1f, false, false, 0), Is.EqualTo(ItemSpawnReason.None));
            Assert.That(s.Update(13.5f, 0.1f, false, false, 0), Is.EqualTo(ItemSpawnReason.First));
        }
    }

    public class ItemSpawnPickerTests
    {
        static readonly ArenaGeometry Arena = new ArenaGeometry(2880f, 1350f, 700f);

        static ItemSpawnRules Rules => new ItemSpawnRules
        {
            MinPlayerDistance = 380f,
            MaxPlayerDistance = 700f,
            MaxAbovePlayer = 380f,
            WallMargin = 250f,
            ObstacleMargin = 250f,
            EnemyDistances = new[] { 520f, 380f, 260f },
            AttemptsPerDistance = 40,
        };

        static Func<float> Rng(int seed)
        {
            var r = new System.Random(seed);
            return () => (float)r.NextDouble();
        }

        [Test]
        public void Result_RespectsAllRules()
        {
            var enemies = new List<Vector2> { new Vector2(600f, 0f), new Vector2(-300f, -500f) };
            for (var seed = 0; seed < 50; seed++)
            {
                Assert.That(ItemSpawnPicker.TryPick(Vector2.zero, enemies, Arena, null, Rules, Rng(seed), out var p, out var step), Is.True);
                var d = p.magnitude;
                Assert.That(d, Is.InRange(380f, 700f));
                Assert.That(p.y, Is.LessThanOrEqualTo(380f));
                Assert.That(Arena.DistanceInside(p), Is.GreaterThanOrEqualTo(250f));
                var minEnemy = Rules.EnemyDistances[step];
                foreach (var e in enemies) Assert.That((e - p).magnitude, Is.GreaterThanOrEqualTo(minEnemy));
            }
        }

        [Test]
        public void RelaxesDistance_WhenCrowded()
        {
            // Ring of enemies 450 away: 520 is impossible, 380/260 still leave gaps.
            var enemies = new List<Vector2>();
            for (var i = 0; i < 12; i++)
            {
                var a = i * Mathf.PI * 2f / 12f;
                enemies.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 1000f);
            }
            enemies.Add(Vector2.zero + new Vector2(0f, -10f));
            Assert.That(ItemSpawnPicker.TryPick(Vector2.zero, enemies, Arena, null, Rules, Rng(3), out _, out var step), Is.True);
            Assert.That(step, Is.GreaterThan(0));
        }

        [Test]
        public void Fails_WhenNoPointIsFarEnough()
        {
            var enemies = new List<Vector2>();
            for (var x = -1000; x <= 1000; x += 150)
            for (var y = -1000; y <= 1000; y += 150)
                enemies.Add(new Vector2(x, y));
            Assert.That(ItemSpawnPicker.TryPick(Vector2.zero, enemies, Arena, null, Rules, Rng(1), out _, out var step), Is.False);
            Assert.That(step, Is.EqualTo(-1));
        }

        [Test]
        public void AvoidsObstacles()
        {
            var obstacles = new List<ObstacleCircle>();
            for (var seed = 0; seed < 30; seed++)
            {
                obstacles.Clear();
                obstacles.Add(new ObstacleCircle(new Vector2(500f, 0f), 130f));
                Assert.That(ItemSpawnPicker.TryPick(Vector2.zero, null, Arena, obstacles, Rules, Rng(seed), out var p, out _), Is.True);
                Assert.That((p - new Vector2(500f, 0f)).magnitude - 130f, Is.GreaterThanOrEqualTo(250f));
            }
        }
    }

    public class ArenaAndDifficultyTests
    {
        [Test]
        public void Octagon_ContainsCenter_NotCutCorner()
        {
            var a = new ArenaGeometry(2880f, 1350f, 700f);
            Assert.That(a.Contains(Vector2.zero), Is.True);
            Assert.That(a.DistanceInside(Vector2.zero), Is.EqualTo(1350f).Within(0.01f));
            Assert.That(a.Contains(new Vector2(2850f, 1320f)), Is.False, "cut corner is outside");
            Assert.That(a.Contains(new Vector2(2850f, 0f)), Is.True);
            Assert.That(a.Contains(new Vector2(2900f, 0f)), Is.False);
        }

        [Test]
        public void EnemySpeed_IsCappedAt105Percent()
        {
            Assert.That(DifficultyCurve.EnemySpeed(0.9f, 1.4f, 420f, 1.05f), Is.EqualTo(441f).Within(0.01f));
            Assert.That(DifficultyCurve.EnemySpeed(0.7f, 1.0f, 420f, 1.05f), Is.EqualTo(294f).Within(0.01f));
        }

        [Test]
        public void Difficulty_FollowsGddTable_AndGrowsAfter12Minutes()
        {
            var table = ScriptableObject.CreateInstance<WaveTable>();
            var d = table.Evaluate(5f);
            Assert.That(d.SpawnRate, Is.EqualTo(0.4f));
            d = table.Evaluate(200f);
            Assert.That(d.SpawnRate, Is.EqualTo(2.5f));
            Assert.That(d.HpMultiplier, Is.EqualTo(1.5f));
            d = table.Evaluate(720f + 120f);
            Assert.That(d.SpawnRate, Is.EqualTo(14f).Within(0.001f));
            Assert.That(d.HpMultiplier, Is.EqualTo(7f).Within(0.001f));
            Assert.That(d.SpeedMultiplier, Is.EqualTo(1.4f));
            UnityEngine.Object.DestroyImmediate(table);
        }
    }

    public class StatAndMetaTests
    {
        readonly List<ScriptableObject> created = new List<ScriptableObject>();

        T Make<T>() where T : ScriptableObject
        {
            var t = ScriptableObject.CreateInstance<T>();
            created.Add(t);
            return t;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) UnityEngine.Object.DestroyImmediate(o);
            created.Clear();
        }

        StatBlock MakeStats()
        {
            var defs = new List<StatDef>();
            for (var i = 0; i < StatBlock.StatCount; i++)
            {
                var d = Make<StatDef>();
                d.Kind = (StatKind)i;
                d.MaxLevel = d.Kind == StatKind.Luck ? 3 : 5;
                d.ValuePerLevel = 0.1f;
                defs.Add(d);
            }
            return new StatBlock(defs);
        }

        [Test]
        public void Stats_CapAtMaxLevel()
        {
            var s = MakeStats();
            for (var i = 0; i < 10; i++) s.LevelUp(StatKind.Luck);
            Assert.That(s.GetLevel(StatKind.Luck), Is.EqualTo(3));
            Assert.That(s.CanLevel(StatKind.Luck), Is.False);
        }

        [Test]
        public void PickChoices_AreDistinct_AndSkipMaxed()
        {
            var s = MakeStats();
            for (var i = 0; i < 5; i++) s.LevelUp(StatKind.Damage);
            var rng = new System.Random(7);
            var list = new List<StatKind>();
            for (var n = 0; n < 50; n++)
            {
                s.PickChoices(3, () => (float)rng.NextDouble(), list);
                Assert.That(list.Count, Is.EqualTo(3));
                Assert.That(list, Is.Unique);
                Assert.That(list, Has.No.Member(StatKind.Damage));
            }
        }

        [Test]
        public void Meta_BuyAndUnlock_SpendCoins()
        {
            var save = new SaveData { Coins = 1000 };
            var up = Make<MetaUpgradeDef>();
            up.Id = "hp";
            up.Kind = MetaUpgradeKind.StartingHp;
            up.BonusPerLevel = 0.02f;
            up.Costs = new[] { 100, 200 };
            Assert.That(MetaProgression.TryBuyUpgrade(save, up), Is.True);
            Assert.That(MetaProgression.TryBuyUpgrade(save, up), Is.True);
            Assert.That(MetaProgression.TryBuyUpgrade(save, up), Is.False, "maxed");
            Assert.That(save.Coins, Is.EqualTo(700));
            Assert.That(MetaProgression.GetModifiers(save, new[] { up }).HpMultiplier, Is.EqualTo(1.04f).Within(0.0001f));

            var skill = Make<SkillDef>();
            skill.Id = "mine";
            skill.RequiresUnlock = true;
            skill.UnlockCost = 600;
            Assert.That(MetaProgression.IsSkillAvailable(save, skill), Is.False);
            Assert.That(MetaProgression.TryUnlockSkill(save, skill), Is.True);
            Assert.That(MetaProgression.IsSkillAvailable(save, skill), Is.True);
            Assert.That(save.Coins, Is.EqualTo(100));
        }
    }
}

namespace RunChildRun.Tests
{
    public class EdgePlacementTests
    {
        static readonly Rect Area = new Rect(-960f, -540f, 1920f, 1080f);

        [Test]
        public void ArrowsStayClearOfHud()
        {
            var allowed = EdgePlacement.AllowedRect(Area, 54f, 215f, 175f, 45f);
            var down = EdgePlacement.PointOnEdge(Vector2.down, allowed, Vector2.zero);
            Assert.That(down.y, Is.EqualTo(-540f + 175f + 45f).Within(0.01f), "above the skill slots");
            var up = EdgePlacement.PointOnEdge(Vector2.up, allowed, Vector2.zero);
            Assert.That(up.y, Is.EqualTo(540f - 215f - 45f).Within(0.01f), "below the EXP bar");
            var right = EdgePlacement.PointOnEdge(new Vector2(1f, 0.01f), allowed, Vector2.zero);
            Assert.That(right.x, Is.EqualTo(960f - 54f - 45f).Within(0.01f));
            var diag = EdgePlacement.PointOnEdge(new Vector2(-1f, -1f), allowed, Vector2.zero);
            Assert.That(allowed.Contains(diag + new Vector2(0.01f, 0.01f)), Is.True);
        }
    }
}
