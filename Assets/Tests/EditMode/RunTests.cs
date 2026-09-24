using System;
using System.Linq;
using CowboyHunter.Battle;
using CowboyHunter.Core;
using NUnit.Framework;
using UnityEngine;

namespace CowboyHunter.Tests
{
    public class RunTests
    {
        static EnemyData Enemy(string name, int bounty)
        {
            var e = ScriptableObject.CreateInstance<EnemyData>();
            e.name = name; e.maxHp = 10; e.bounty = bounty;
            return e;
        }

        static RunConfig Config(int chapters = 2, float eliteChance = 0f)
        {
            var bullet = ScriptableObject.CreateInstance<BulletData>();
            var deck = ScriptableObject.CreateInstance<StartingDeckData>();
            deck.entries.Add(new StartingDeckData.Entry { bullet = bullet, count = 40 });

            var config = ScriptableObject.CreateInstance<RunConfig>();
            config.startingDeck = deck;
            config.eliteChance = eliteChance;
            for (int i = 0; i < chapters; i++)
            {
                var ch = ScriptableObject.CreateInstance<ChapterData>();
                ch.normals.Add(Enemy("normal", 10));
                ch.elites.Add(Enemy("elite", 30));
                ch.boss = Enemy("boss", 100);
                config.chapters.Add(ch);
            }
            return config;
        }

        static void WinNormal(RunState run, int hp = 100) { run.Accept(0); run.CompleteBattle(true, hp); }

        [Test]
        public void NewRun_Deals3To5Posters_AndBuilds40BulletDeck()
        {
            var counts = Enumerable.Range(0, 200).Select(seed => new RunState(Config(), new System.Random(seed)).Posters.Count).ToList();
            Assert.That(counts.Min(), Is.EqualTo(3));
            Assert.That(counts.Max(), Is.EqualTo(5));
            Assert.AreEqual(40, new RunState(Config(), new System.Random(1)).Deck.Count);
        }

        [Test]
        public void Win_AddsBounty_DecrementsRemaining_KeepsHp()
        {
            var run = new RunState(Config(), new System.Random(1));
            WinNormal(run, hp: 73);
            Assert.AreEqual(10, run.Gold);
            Assert.AreEqual(4, run.RemainingEnemies);
            Assert.AreEqual(73, run.PlayerHp);
            Assert.AreEqual(RunPhase.Board, run.Phase);
        }

        [Test]
        public void AfterFiveWins_BossUnlocked_AndOnlyBossRemains()
        {
            var run = new RunState(Config(), new System.Random(1));
            Assert.Throws<InvalidOperationException>(() => run.AcceptBoss());
            for (int i = 0; i < 5; i++) WinNormal(run);
            Assert.IsTrue(run.BossUnlocked);
            Assert.AreEqual(0, run.Posters.Count);
            run.AcceptBoss();
            Assert.IsTrue(run.CurrentTarget.Value.IsBoss);
        }

        [Test]
        public void BossWin_AdvancesChapter_AndResetsRemaining()
        {
            var run = new RunState(Config(), new System.Random(1));
            for (int i = 0; i < 5; i++) WinNormal(run);
            run.AcceptBoss();
            run.CompleteBattle(true, 50);
            Assert.AreEqual(1, run.ChapterIndex);
            Assert.AreEqual(5, run.RemainingEnemies);
            Assert.AreEqual(50, run.PlayerHp);
            Assert.AreEqual(150, run.Gold);
        }

        [Test]
        public void LastBossWin_Cleared()
        {
            var run = new RunState(Config(chapters: 1), new System.Random(1));
            for (int i = 0; i < 5; i++) WinNormal(run);
            run.AcceptBoss();
            run.CompleteBattle(true, 10);
            Assert.AreEqual(RunPhase.Cleared, run.Phase);
        }

        [Test]
        public void Loss_EndsRun()
        {
            var run = new RunState(Config(), new System.Random(1));
            run.Accept(0);
            run.CompleteBattle(false, 0);
            Assert.AreEqual(RunPhase.Dead, run.Phase);
        }

        [Test]
        public void EliteChanceOne_AllPostersAreElite()
        {
            var run = new RunState(Config(eliteChance: 1f), new System.Random(1));
            Assert.IsTrue(run.Posters.All(p => p.IsElite && p.Enemy.name == "elite"));
        }
    }
}
