using System.Collections.Generic;
using System.Linq;
using CowboyHunter.Battle;
using CowboyHunter.Core;
using NUnit.Framework;
using UnityEngine;

namespace CowboyHunter.Tests
{
    public class SaveTests
    {
        static T Named<T>(string name) where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            o.name = name;
            return o;
        }

        static RunConfig Config()
        {
            var normal = Named<BulletData>("normal");
            var fire = Named<BulletData>("fire"); fire.price = 1;
            var pierce = Named<BulletData>("pierce"); pierce.price = 1;
            var deck = Named<StartingDeckData>("deck");
            deck.entries.Add(new StartingDeckData.Entry { bullet = normal, count = 40 });

            var ch = Named<ChapterData>("ch1");
            var bandit = Named<EnemyData>("bandit"); bandit.bounty = 10;
            var scorpion = Named<EnemyData>("scorpion"); scorpion.bounty = 10;
            var ghost = Named<EnemyData>("ghost"); ghost.bounty = 40;
            ch.normals.AddRange(new[] { bandit, scorpion });
            ch.elites.Add(ghost);
            ch.boss = Named<EnemyData>("boss");

            var scope = Named<RelicData>("scope"); scope.slot = RelicSlot.Scope; scope.price = 1;
            var grip = Named<RelicData>("grip"); grip.slot = RelicSlot.Grip; grip.price = 1;

            var config = Named<RunConfig>("config");
            config.startingDeck = deck;
            config.chapters.Add(ch);
            config.chapters.Add(ch);
            config.eliteChance = 0.5f;
            config.relicPool = new List<RelicData> { scope, grip };
            config.shopBullets = new List<BulletData> { fire, pierce };
            config.whiskeyPrice = 1;
            config.removalPrice = 1;
            return config;
        }

        static RunState RoundTrip(RunConfig config, RunState run) =>
            SaveSystem.Deserialize(config, SaveSystem.Serialize(run));

        [Test]
        public void Board_RoundTrip_KeepsProgressAndPosters()
        {
            var config = Config();
            var run = new RunState(config, new System.Random(3));
            run.Accept(0); run.CompleteBattle(true, 64); run.LeaveShop();

            var loaded = RoundTrip(config, run);
            Assert.AreEqual(RunPhase.Board, loaded.Phase);
            Assert.AreEqual(run.ChapterIndex, loaded.ChapterIndex);
            Assert.AreEqual(4, loaded.RemainingEnemies);
            Assert.AreEqual(64, loaded.PlayerHp);
            Assert.AreEqual(run.Gold, loaded.Gold);
            CollectionAssert.AreEqual(run.Deck, loaded.Deck);
            CollectionAssert.AreEqual(run.Posters.Select(p => (p.Enemy, p.IsElite)), loaded.Posters.Select(p => (p.Enemy, p.IsElite)));
        }

        [Test]
        public void Shop_RoundTrip_KeepsStockSoldFlagsAndRelics()
        {
            var config = Config();
            var run = new RunState(config, new System.Random(3));
            run.Accept(0); run.CompleteBattle(true, 50);
            run.BuyBullet(0);
            run.BuyRelic();
            run.BuyWhiskey();

            var loaded = RoundTrip(config, run);
            Assert.AreEqual(RunPhase.Shop, loaded.Phase);
            CollectionAssert.AreEqual(run.Shop.Bullets.Select(o => (o.Bullet, o.Sold)), loaded.Shop.Bullets.Select(o => (o.Bullet, o.Sold)));
            Assert.AreSame(run.Shop.Relic, loaded.Shop.Relic);
            Assert.IsTrue(loaded.Shop.RelicSold);
            Assert.IsTrue(loaded.Shop.WhiskeySold);
            Assert.IsFalse(loaded.Shop.RemovalUsed);
            foreach (RelicSlot slot in System.Enum.GetValues(typeof(RelicSlot)))
                Assert.AreSame(run.GetEquipped(slot), loaded.GetEquipped(slot));
            CollectionAssert.AreEqual(run.Deck, loaded.Deck);
            Assert.AreEqual(run.PlayerHp, loaded.PlayerHp);

            loaded.LeaveShop();   // 불러온 런이 그대로 이어지는지
            Assert.AreEqual(RunPhase.Board, loaded.Phase);
        }

        [Test]
        public void Inventory_RoundTrip()
        {
            var config = Config();
            config.eliteChance = 1f;
            config.relicPool[1].slot = RelicSlot.Scope;   // 두 유물이 같은 부위 → 두 번째는 보관함으로
            var run = new RunState(config, new System.Random(3));
            run.Accept(0); run.CompleteBattle(true, 50); run.LeaveShop();
            run.Accept(0); run.CompleteBattle(true, 50);

            var loaded = RoundTrip(config, run);
            CollectionAssert.AreEqual(run.Inventory, loaded.Inventory);
            Assert.AreEqual(1, loaded.Inventory.Count);
        }

        [Test]
        public void CannotSaveDuringBattle()
        {
            var run = new RunState(Config(), new System.Random(3));
            run.Accept(0);
            Assert.Throws<System.InvalidOperationException>(() => run.ToSaveData());
        }

        [Test]
        public void UnknownAssetName_FailsToLoad()
        {
            var config = Config();
            var json = SaveSystem.Serialize(new RunState(config, new System.Random(3))).Replace("\"normal\"", "\"deleted_bullet\"");
            Assert.Throws<System.InvalidOperationException>(() => SaveSystem.Deserialize(config, json));
        }
    }
}
