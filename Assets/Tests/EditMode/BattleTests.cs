using System;
using System.Collections.Generic;
using System.Linq;
using CowboyHunter.Battle;
using NUnit.Framework;
using UnityEngine;

namespace CowboyHunter.Tests
{
    public class BattleTests
    {
        static BulletData Bullet(string name, int damage = 0, int block = 0, bool pierce = false, int burn = 0)
        {
            var b = ScriptableObject.CreateInstance<BulletData>();
            b.name = name; b.damage = damage; b.block = block; b.pierce = pierce; b.burn = burn;
            return b;
        }

        static EnemyData Enemy(int hp, params EnemyAction[] pattern)
        {
            var e = ScriptableObject.CreateInstance<EnemyData>();
            e.maxHp = hp; e.pattern = pattern.ToList();
            return e;
        }

        static EnemyAction Act(EnemyActionType type, int value) => new EnemyAction { type = type, value = value };

        static List<BulletData> Many(BulletData b, int count) => Enumerable.Repeat(b, count).ToList();

        static BattleSession Session(List<BulletData> deck, params EnemyData[] enemies) =>
            new BattleSession(new Combatant(100), enemies, deck, new System.Random(1));

        static void LoadInOrder(BattleSession s, params BulletData[] order)
        {
            for (int slot = 0; slot < order.Length; slot++)
                s.Load(s.Hand.ToList().IndexOf(order[slot]), slot);
        }

        static void FillCylinder(BattleSession s)
        {
            for (int slot = 0; slot < Cylinder.SlotCount; slot++) s.Load(0, slot);
        }

        // ── 덱 ───────────────────────────────────────

        [Test]
        public void StartTurn_Draws10()
        {
            var s = Session(Many(Bullet("n", 1), 40), Enemy(999));
            s.StartTurn();
            Assert.AreEqual(10, s.Hand.Count);
            Assert.AreEqual(30, s.Deck.DrawPile.Count);
        }

        [Test]
        public void Deck40_FourTurnsEmptiesPile_FifthTurnReshuffles()
        {
            var s = Session(Many(Bullet("n", 1), 40), Enemy(9999));
            for (int turn = 0; turn < 4; turn++) { s.StartTurn(); FillCylinder(s); s.Confirm(); }

            Assert.AreEqual(0, s.Deck.DrawPile.Count);
            Assert.AreEqual(40, s.Deck.DiscardPile.Count);

            s.StartTurn();
            Assert.AreEqual(10, s.Hand.Count);
            Assert.AreEqual(30, s.Deck.DrawPile.Count);
            Assert.AreEqual(0, s.Deck.DiscardPile.Count);
        }

        [Test]
        public void Draw_WhenPileRunsOutMidDraw_ReshufflesAndContinues()
        {
            var deck = new Deck(Many(Bullet("n"), 13), new System.Random(1));
            deck.Discard(deck.Draw(10));
            var drawn = deck.Draw(10);   // 남은 3발 + 버린 더미에서 7발
            Assert.AreEqual(10, drawn.Count);
            Assert.AreEqual(3, deck.DrawPile.Count);
        }

        [Test]
        public void Confirm_DiscardsFiredAndLeftoverBullets()
        {
            var s = Session(Many(Bullet("n", 1), 40), Enemy(999));
            s.StartTurn();
            FillCylinder(s);
            s.Confirm();
            Assert.AreEqual(0, s.Hand.Count);
            Assert.AreEqual(10, s.Deck.DiscardPile.Count);
        }

        // ── 장전 / 발사 ─────────────────────────────

        [Test]
        public void Confirm_RequiresAllSixSlots()
        {
            var s = Session(Many(Bullet("n", 1), 40), Enemy(999));
            s.StartTurn();
            for (int slot = 0; slot < 5; slot++) s.Load(0, slot);
            Assert.IsFalse(s.CanConfirm);
            Assert.Throws<InvalidOperationException>(() => s.Confirm());
        }

        [Test]
        public void Load_OnOccupiedSlot_ReturnsPreviousBulletToHand()
        {
            var a = Bullet("a"); var b = Bullet("b");
            var s = Session(new List<BulletData> { a, b }, Enemy(999));
            s.StartTurn();
            LoadInOrder(s, a);
            s.Load(s.Hand.ToList().IndexOf(b), 0);
            Assert.AreSame(b, s.Cylinder[0]);
            CollectionAssert.AreEqual(new[] { a }, s.Hand);
        }

        [Test]
        public void SlotOrder_ChangesDamage_ViaCombo()
        {
            var fire = Bullet("fire", damage: 0);
            var pierce = Bullet("pierce", damage: 5);
            pierce.comboPrevious = fire;
            pierce.comboBonusDamage = 4;
            var filler = Bullet("filler");
            var deck = new List<BulletData> { fire, pierce, filler, filler, filler, filler };

            int DamageWith(params BulletData[] order)
            {
                var s = Session(new List<BulletData>(deck), Enemy(999));
                s.StartTurn();
                LoadInOrder(s, order);
                s.Confirm();
                return 999 - s.Enemies[0].Stats.Hp;
            }

            Assert.AreEqual(9, DamageWith(fire, pierce, filler, filler, filler, filler));
            Assert.AreEqual(5, DamageWith(pierce, fire, filler, filler, filler, filler));
        }

        [Test]
        public void Pierce_IgnoresEnemyBlock()
        {
            var pierce = Bullet("pierce", damage: 5, pierce: true);
            var s = Session(Many(pierce, 40), Enemy(999, Act(EnemyActionType.Block, 100)));
            s.StartTurn(); FillCylinder(s); s.Confirm();    // 30 피해 후 적이 보호막 100 획득
            s.StartTurn(); FillCylinder(s); s.Confirm();    // 보호막이 있어도 30 피해
            Assert.AreEqual(999 - 60, s.Enemies[0].Stats.Hp);
            Assert.AreEqual(100, s.Enemies[0].Stats.Block);
        }

        [Test]
        public void PlayerBlock_AbsorbsEnemyAttack_AndResetsNextTurn()
        {
            var guard = Bullet("guard", block: 5);
            var s = Session(Many(guard, 40), Enemy(999, Act(EnemyActionType.Attack, 20)));
            s.StartTurn(); FillCylinder(s); s.Confirm();    // 보호막 30, 공격 20
            Assert.AreEqual(100, s.Player.Hp);
            Assert.AreEqual(10, s.Player.Block);
            s.StartTurn();
            Assert.AreEqual(0, s.Player.Block);
        }

        [Test]
        public void Burn_TicksAtEnemyTurnStart_AndDecays()
        {
            var fire = Bullet("fire", burn: 1);
            var s = Session(Many(fire, 40), Enemy(999));
            s.StartTurn(); FillCylinder(s); s.Confirm();    // 화상 6 → 적 턴 시작 시 6 피해, 5로 감소
            Assert.AreEqual(993, s.Enemies[0].Stats.Hp);
            Assert.AreEqual(5, s.Enemies[0].Stats.Burn);
        }

        // ── 유물 보정값 ─────────────────────────────

        static BattleSession Session(List<BulletData> deck, BattleModifiers mods, params EnemyData[] enemies) =>
            new BattleSession(new Combatant(100), enemies, deck, new System.Random(1), mods);

        [Test]
        public void Modifiers_DamageBonus_OnlyForDamagingBullets_AndFocusSlot()
        {
            var shot = Bullet("shot", damage: 5);
            var guard = Bullet("guard", block: 5);
            var s = Session(new List<BulletData> { shot, shot, shot, shot, shot, guard },
                new BattleModifiers { DamageBonus = 1, FocusSlot = 6, FocusDamage = 10, BlockBonus = 2 }, Enemy(999));
            s.StartTurn();
            LoadInOrder(s, guard, shot, shot, shot, shot, shot);    // 6번 슬롯이 피해 탄
            s.Confirm();
            Assert.AreEqual(999 - (6 * 5 + 10), s.Enemies[0].Stats.Hp);
            Assert.AreEqual(7, s.Player.Block);
        }

        [Test]
        public void Modifiers_ExtraDraw_TurnStartBlock_BurnBonus()
        {
            var fire = Bullet("fire", burn: 1);
            var s = Session(Many(fire, 40), new BattleModifiers { ExtraDraw = 2, TurnStartBlock = 4, BurnBonus = 1 }, Enemy(999));
            s.StartTurn();
            Assert.AreEqual(12, s.Hand.Count);
            Assert.AreEqual(4, s.Player.Block);
            FillCylinder(s);
            var result = s.Confirm();
            Assert.AreEqual(2, result.Shots[0].BurnApplied);
        }

        // ── 특수 탄환 / 적 ──────────────────────────

        [Test]
        public void SilverBullet_BonusOnlyAgainstUndead()
        {
            var silver = Bullet("silver", damage: 5); silver.bonusVsUndead = 7;
            var undead = Enemy(999); undead.undead = true;
            var living = Session(Many(silver, 40), Enemy(999));
            var dead = Session(Many(silver, 40), undead);
            foreach (var s in new[] { living, dead }) { s.StartTurn(); FillCylinder(s); s.Confirm(); }
            Assert.AreEqual(999 - 30, living.Enemies[0].Stats.Hp);
            Assert.AreEqual(999 - 72, dead.Enemies[0].Stats.Hp);
        }

        [Test]
        public void FirebirdBullet_BonusWhenTargetAlreadyBurning_SoOrderMatters()
        {
            var fire = Bullet("fire", burn: 1);
            var firebird = Bullet("firebird", damage: 4); firebird.bonusVsBurning = 6;
            var filler = Bullet("filler");
            var deck = new List<BulletData> { fire, firebird, filler, filler, filler, filler };

            int DamageWith(params BulletData[] order)
            {
                var s = Session(new List<BulletData>(deck), Enemy(999));
                s.StartTurn();
                LoadInOrder(s, order);
                var r = s.Confirm();
                return r.Shots.Sum(x => x.DamageDealt);
            }

            Assert.AreEqual(10, DamageWith(fire, firebird, filler, filler, filler, filler));
            Assert.AreEqual(4, DamageWith(firebird, fire, filler, filler, filler, filler));
        }

        [Test]
        public void BountyBullet_GivesGoldOnlyWhenItKills()
        {
            var normal = Bullet("normal", damage: 5);
            var bounty = Bullet("bounty", damage: 5); bounty.killBonusGold = 15;
            var deck = new List<BulletData> { normal, normal, bounty, bounty, normal, normal };

            var s = Session(new List<BulletData>(deck), Enemy(15));   // 3번째 발(현상금탄)에 처치
            s.StartTurn();
            LoadInOrder(s, normal, normal, bounty, bounty, normal, normal);
            var r = s.Confirm();
            Assert.IsTrue(r.Shots[2].Killed);
            Assert.AreEqual(15, s.BonusGold);

            s = Session(new List<BulletData>(deck), Enemy(10));   // 2번째 발(일반탄)에 처치
            s.StartTurn();
            LoadInOrder(s, normal, normal, bounty, bounty, normal, normal);
            s.Confirm();
            Assert.AreEqual(0, s.BonusGold);
        }

        [Test]
        public void KillGoldRelic_AddsGoldPerKill()
        {
            var s = Session(Many(Bullet("n", 10), 40), new BattleModifiers { KillGold = 10 }, Enemy(10), Enemy(10));
            s.StartTurn(); FillCylinder(s); s.Confirm();
            Assert.AreEqual(20, s.BonusGold);
        }

        [Test]
        public void Weaken_ReducesEachHitOfNextAttack_ThenClears()
        {
            var hex = Bullet("hex"); hex.weaken = 2;
            var e = Enemy(999, new EnemyAction { type = EnemyActionType.Attack, value = 5, hits = 3 });
            var s = Session(Many(hex, 40), e);
            s.StartTurn(); FillCylinder(s);
            var r = s.Confirm();                                   // 약화 12 → 5-12 = 0 x3
            Assert.AreEqual(0, r.EnemyActions[0].DamageDealt);
            Assert.AreEqual(0, s.Enemies[0].Stats.Weak);
            Assert.AreEqual(100, s.Player.Hp);
        }

        [Test]
        public void MultiHitAttack_EachHitGoesThroughBlockSeparately()
        {
            var guard = Bullet("guard", block: 1);
            var e = Enemy(999, new EnemyAction { type = EnemyActionType.Attack, value = 4, hits = 3 });
            var s = Session(Many(guard, 40), e);
            s.StartTurn(); FillCylinder(s);
            var r = s.Confirm();                                   // 보호막 6, 4x3=12 → 6 피해
            Assert.AreEqual(6, r.EnemyActions[0].DamageDealt);
            Assert.AreEqual(94, s.Player.Hp);
        }

        // ── 타겟 / 승패 ─────────────────────────────

        [Test]
        public void AllShotsGoToSelectedTarget()
        {
            var s = Session(Many(Bullet("n", 1), 40), Enemy(999), Enemy(999));
            s.StartTurn();
            s.SelectTarget(1);
            FillCylinder(s);
            s.Confirm();
            Assert.AreEqual(999, s.Enemies[0].Stats.Hp);
            Assert.AreEqual(993, s.Enemies[1].Stats.Hp);
        }

        [Test]
        public void TargetDiesMidVolley_RemainingShotsGoToNextEnemy()
        {
            var s = Session(Many(Bullet("n", 10), 40), Enemy(20), Enemy(999));
            s.StartTurn();
            FillCylinder(s);
            var result = s.Confirm();
            Assert.IsTrue(s.Enemies[0].IsDead);
            Assert.AreEqual(999 - 40, s.Enemies[1].Stats.Hp);
            Assert.AreEqual(6, result.Shots.Count);
        }

        [Test]
        public void AllEnemiesDead_Won()
        {
            var s = Session(Many(Bullet("n", 10), 40), Enemy(30));
            s.StartTurn(); FillCylinder(s); s.Confirm();
            Assert.AreEqual(BattlePhase.Won, s.Phase);
        }

        [Test]
        public void PlayerDead_Lost()
        {
            var s = Session(Many(Bullet("n"), 40), Enemy(999, Act(EnemyActionType.Attack, 150)));
            s.StartTurn(); FillCylinder(s); s.Confirm();
            Assert.AreEqual(BattlePhase.Lost, s.Phase);
        }
    }
}
