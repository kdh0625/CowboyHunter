using System.Collections.Generic;
using System.Linq;
using CowboyHunter.Battle;
using CowboyHunter.Core;
using NUnit.Framework;
using UnityEngine;

namespace CowboyHunter.Tests
{
    public class RelicShopTests
    {
        static BulletData Bullet(string name, int damage = 0, int block = 0, int burn = 0, int price = 20)
        {
            var b = ScriptableObject.CreateInstance<BulletData>();
            b.name = name; b.damage = damage; b.block = block; b.burn = burn; b.price = price;
            return b;
        }

        static EnemyData Enemy(string name, int bounty)
        {
            var e = ScriptableObject.CreateInstance<EnemyData>();
            e.name = name; e.maxHp = 10; e.bounty = bounty;
            return e;
        }

        static RelicData Relic(string name, RelicSlot slot, int price = 50)
        {
            var r = ScriptableObject.CreateInstance<RelicData>();
            r.name = name; r.slot = slot; r.price = price;
            return r;
        }

        static RunConfig Config(List<RelicData> relics, float eliteChance = 0f, List<BulletData> shopBullets = null)
        {
            var deck = ScriptableObject.CreateInstance<StartingDeckData>();
            deck.entries.Add(new StartingDeckData.Entry { bullet = Bullet("normal", 6), count = 40 });
            var ch = ScriptableObject.CreateInstance<ChapterData>();
            ch.normals.Add(Enemy("normal", 10));
            ch.elites.Add(Enemy("elite", 30));
            ch.boss = Enemy("boss", 100);

            var config = ScriptableObject.CreateInstance<RunConfig>();
            config.startingDeck = deck;
            config.eliteChance = eliteChance;
            config.chapters.Add(ch);
            config.relicPool = relics;
            config.shopBullets = shopBullets ?? new List<BulletData> { Bullet("a", price: 20), Bullet("b", price: 20), Bullet("c", price: 20), Bullet("d", price: 20) };
            return config;
        }

        static RunState WinAndEnterShop(RunConfig config, int seed = 1)
        {
            var run = new RunState(config, new System.Random(seed));
            run.Accept(0);
            run.CompleteBattle(true, 80);
            return run;
        }

        // ── 유물 획득 / 장착 ────────────────────────

        [Test]
        public void EliteWin_DropsUnownedRelic_AutoEquipsIntoEmptySlot()
        {
            var scope = Relic("scope", RelicSlot.Scope);
            var run = WinAndEnterShop(Config(new List<RelicData> { scope }, eliteChance: 1f));
            Assert.AreSame(scope, run.LastRelicReward);
            Assert.AreSame(scope, run.GetEquipped(RelicSlot.Scope));
            Assert.AreEqual(0, run.Inventory.Count);
        }

        [Test]
        public void NormalWin_DropsNoRelic()
        {
            var run = WinAndEnterShop(Config(new List<RelicData> { Relic("r", RelicSlot.Grip) }, eliteChance: 0f));
            Assert.IsNull(run.LastRelicReward);
            Assert.IsNull(run.GetEquipped(RelicSlot.Grip));
        }

        [Test]
        public void Equip_OnlyIntoItsOwnSlot_AndSwapsPreviousToInventory()
        {
            var first = Relic("first", RelicSlot.Muzzle);
            var second = Relic("second", RelicSlot.Muzzle);
            var run = new RunState(Config(new List<RelicData> { first, second }, eliteChance: 1f), new System.Random(1));
            for (int i = 0; i < 2; i++) { run.Accept(0); run.CompleteBattle(true, 80); if (i == 0) run.LeaveShop(); }

            var inInventory = run.Inventory.Single();
            var equipped = run.GetEquipped(RelicSlot.Muzzle);
            run.Equip(inInventory);
            Assert.AreSame(inInventory, run.GetEquipped(RelicSlot.Muzzle));
            CollectionAssert.AreEqual(new[] { equipped }, run.Inventory);
            Assert.IsNull(run.GetEquipped(RelicSlot.Scope));
        }

        [Test]
        public void Equip_OutsideShop_Throws()
        {
            var run = WinAndEnterShop(Config(new List<RelicData> { Relic("r", RelicSlot.Grip) }, eliteChance: 1f));
            run.Unequip(RelicSlot.Grip);
            run.LeaveShop();
            Assert.Throws<System.InvalidOperationException>(() => run.Equip(run.Inventory[0]));
        }

        [Test]
        public void Modifiers_SumOnlyEquippedRelics()
        {
            var muzzle = Relic("muzzle", RelicSlot.Muzzle); muzzle.damageBonus = 2;
            var run = WinAndEnterShop(Config(new List<RelicData> { muzzle }, eliteChance: 1f));
            Assert.AreEqual(2, run.Modifiers.DamageBonus);
            run.Unequip(RelicSlot.Muzzle);
            Assert.AreEqual(0, run.Modifiers.DamageBonus);
        }

        // ── 상점 ───────────────────────────────────

        [Test]
        public void Shop_Offers3DistinctBullets_AndAnUnownedRelic()
        {
            var relic = Relic("r", RelicSlot.Grip);
            var run = WinAndEnterShop(Config(new List<RelicData> { relic }));
            Assert.AreEqual(3, run.Shop.Bullets.Count);
            Assert.AreEqual(3, run.Shop.Bullets.Select(o => o.Bullet).Distinct().Count());
            Assert.AreSame(relic, run.Shop.Relic);
        }

        [Test]
        public void BuyBullet_AddsToDeck_OncePerVisit_AndNeedsGold()
        {
            var run = WinAndEnterShop(Config(new List<RelicData>()));   // 골드 10
            Assert.IsFalse(run.BuyBullet(0), "골드 부족");
            Assert.AreEqual(40, run.Deck.Count);

            var cheap = Bullet("cheap", price: 5);
            run = WinAndEnterShop(Config(new List<RelicData>(), shopBullets: new List<BulletData> { cheap }));
            Assert.IsTrue(run.BuyBullet(0));
            Assert.AreEqual(41, run.Deck.Count);
            Assert.AreEqual(5, run.Gold);
            Assert.IsFalse(run.BuyBullet(0), "이미 팔림");
        }

        [Test]
        public void BuyRelic_AutoEquips_AndIsNotOfferedAgain()
        {
            var relic = Relic("r", RelicSlot.Cylinder, price: 5);
            var run = WinAndEnterShop(Config(new List<RelicData> { relic }));
            Assert.IsTrue(run.BuyRelic());
            Assert.AreSame(relic, run.GetEquipped(RelicSlot.Cylinder));
            run.LeaveShop();
            run.Accept(0); run.CompleteBattle(true, 80);
            Assert.IsNull(run.Shop.Relic);
        }

        [Test]
        public void Whiskey_HealsUpToMaxHp()
        {
            var config = Config(new List<RelicData>());
            config.whiskeyPrice = 5; config.whiskeyHeal = 25;
            var run = WinAndEnterShop(config);   // 체력 80
            Assert.IsTrue(run.BuyWhiskey());
            Assert.AreEqual(100, run.PlayerHp);
            Assert.IsFalse(run.BuyWhiskey(), "방문당 1회");
        }

        [Test]
        public void RemoveBullet_RemovesOne_OncePerVisit()
        {
            var config = Config(new List<RelicData>());
            config.removalPrice = 5;
            var run = WinAndEnterShop(config);
            var bullet = run.Deck[0];
            Assert.IsTrue(run.RemoveBullet(bullet));
            Assert.AreEqual(39, run.Deck.Count);
            Assert.IsFalse(run.RemoveBullet(bullet));
        }

        [Test]
        public void LastBossWin_ClearsWithoutShop()
        {
            var run = new RunState(Config(new List<RelicData>()), new System.Random(1));
            for (int i = 0; i < 5; i++) { run.Accept(0); run.CompleteBattle(true, 80); run.LeaveShop(); }
            run.AcceptBoss();
            run.CompleteBattle(true, 80);
            Assert.AreEqual(RunPhase.Cleared, run.Phase);
            Assert.IsNull(run.Shop);
        }
    }
}
