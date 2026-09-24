using System;
using System.Collections.Generic;

namespace CowboyHunter.Battle
{
    public enum BattlePhase { AwaitingDraw, Loading, Won, Lost }

    public struct ShotResult
    {
        public int Slot;
        public BulletData Bullet;
        public int TargetIndex;
        public int DamageDealt;
        public int BlockGained;
        public int BurnApplied;
        public int PoisonApplied;
    }

    public struct EnemyActionResult
    {
        public int EnemyIndex;
        public int StatusDamage;
        public bool Acted;
        public EnemyAction Action;
        public int DamageDealt;
    }

    public class TurnResult
    {
        public readonly List<ShotResult> Shots = new();
        public readonly List<EnemyActionResult> EnemyActions = new();
    }

    // 전투 규칙만 담당한다. UI는 이 객체를 호출하고, 반환된 결과로 연출한다.
    public class BattleSession
    {
        public const int DefaultDrawCount = 10;

        public int DrawCount { get; set; } = DefaultDrawCount;
        public Combatant Player { get; }
        public Deck Deck { get; }
        public Cylinder Cylinder { get; } = new();
        public IReadOnlyList<EnemyUnit> Enemies => _enemies;
        public IReadOnlyList<BulletData> Hand => _hand;
        public int TargetIndex { get; private set; }
        public BattlePhase Phase { get; private set; } = BattlePhase.AwaitingDraw;
        public int Turn { get; private set; }

        readonly List<EnemyUnit> _enemies = new();
        readonly List<BulletData> _hand = new();
        readonly BattleModifiers _mods;

        public BattleSession(Combatant player, IEnumerable<EnemyData> enemies, IEnumerable<BulletData> deck, System.Random rng, BattleModifiers modifiers = default)
        {
            Player = player;
            _mods = modifiers;
            DrawCount = DefaultDrawCount + modifiers.ExtraDraw;
            foreach (var e in enemies) _enemies.Add(new EnemyUnit(e));
            Deck = new Deck(deck, rng);
        }

        // DRAW: 턴 시작. 보호막 초기화 → 상태이상 피해 → 드로우
        public void StartTurn()
        {
            Require(BattlePhase.AwaitingDraw);
            Turn++;
            Player.ClearBlock();
            if (_mods.TurnStartBlock > 0) Player.GainBlock(_mods.TurnStartBlock);
            Player.TickStatus();
            if (CheckEnd()) return;

            _hand.AddRange(Deck.Draw(DrawCount));
            if (_enemies[TargetIndex].IsDead) TargetIndex = FirstAliveEnemy();
            Phase = BattlePhase.Loading;
        }

        public void SelectTarget(int enemyIndex)
        {
            Require(BattlePhase.Loading);
            if (_enemies[enemyIndex].IsDead) throw new InvalidOperationException("쓰러진 적은 타겟으로 지정할 수 없습니다.");
            TargetIndex = enemyIndex;
        }

        // 손의 탄을 슬롯에 장전. 슬롯에 탄이 있었다면 손으로 돌아온다.
        public void Load(int handIndex, int slot)
        {
            Require(BattlePhase.Loading);
            var bullet = _hand[handIndex];
            _hand.RemoveAt(handIndex);
            var previous = Cylinder.Load(slot, bullet);
            if (previous != null) _hand.Add(previous);
        }

        public void Unload(int slot)
        {
            Require(BattlePhase.Loading);
            var bullet = Cylinder.Unload(slot);
            if (bullet != null) _hand.Add(bullet);
        }

        // 손에 탄이 남아 있으면 6칸을 모두 채워야 발사할 수 있다.
        public bool CanConfirm => Phase == BattlePhase.Loading && (Cylinder.IsFull || _hand.Count == 0);

        // CONFIRM: 1→6번 순차 발사 → 남은 탄 버림 → 적 턴 → 턴 종료
        public TurnResult Confirm()
        {
            if (!CanConfirm) throw new InvalidOperationException("실린더 6칸을 모두 장전해야 발사할 수 있습니다.");
            var result = new TurnResult();

            BulletData previous = null;
            for (int slot = 0; slot < Cylinder.SlotCount; slot++)
            {
                var bullet = Cylinder[slot];
                if (bullet == null) { previous = null; continue; }

                if (_enemies[TargetIndex].IsDead)
                {
                    int next = FirstAliveEnemy();
                    if (next < 0) break;
                    TargetIndex = next;
                }
                result.Shots.Add(Fire(slot, bullet, previous));
                previous = bullet;
            }

            Deck.Discard(Cylinder.TakeAll());
            Deck.Discard(_hand);
            _hand.Clear();

            if (!CheckEnd())
            {
                RunEnemyTurn(result);
                if (!CheckEnd()) Phase = BattlePhase.AwaitingDraw;
            }
            return result;
        }

        ShotResult Fire(int slot, BulletData bullet, BulletData previous)
        {
            var target = _enemies[TargetIndex].Stats;
            int damage = bullet.damage;
            if (damage > 0)
            {
                damage += _mods.DamageBonus;
                if (slot == _mods.FocusSlot - 1) damage += _mods.FocusDamage;
            }
            if (bullet.comboPrevious != null && previous == bullet.comboPrevious)
                damage += bullet.comboBonusDamage;

            var shot = new ShotResult { Slot = slot, Bullet = bullet, TargetIndex = TargetIndex };
            shot.DamageDealt = target.TakeDamage(damage, bullet.pierce);
            if (bullet.burn > 0) { shot.BurnApplied = bullet.burn + _mods.BurnBonus; target.AddBurn(shot.BurnApplied); }
            if (bullet.poison > 0) { shot.PoisonApplied = bullet.poison; target.AddPoison(shot.PoisonApplied); }
            if (bullet.block > 0) { shot.BlockGained = bullet.block + _mods.BlockBonus; Player.GainBlock(shot.BlockGained); }
            return shot;
        }

        void RunEnemyTurn(TurnResult result)
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                var enemy = _enemies[i];
                if (enemy.IsDead) continue;

                enemy.Stats.ClearBlock();
                var r = new EnemyActionResult { EnemyIndex = i, StatusDamage = enemy.Stats.TickStatus() };

                if (!enemy.IsDead && enemy.HasIntent)
                {
                    r.Acted = true;
                    r.Action = enemy.Intent;
                    switch (r.Action.type)
                    {
                        case EnemyActionType.Attack: r.DamageDealt = Player.TakeDamage(r.Action.value); break;
                        case EnemyActionType.Block: enemy.Stats.GainBlock(r.Action.value); break;
                        case EnemyActionType.Poison: Player.AddPoison(r.Action.value); break;
                    }
                    enemy.AdvanceIntent();
                }
                result.EnemyActions.Add(r);
                if (Player.IsDead) break;
            }
        }

        bool CheckEnd()
        {
            if (Player.IsDead) Phase = BattlePhase.Lost;
            else if (FirstAliveEnemy() < 0) Phase = BattlePhase.Won;
            return Phase == BattlePhase.Won || Phase == BattlePhase.Lost;
        }

        int FirstAliveEnemy()
        {
            for (int i = 0; i < _enemies.Count; i++)
                if (!_enemies[i].IsDead) return i;
            return -1;
        }

        void Require(BattlePhase phase)
        {
            if (Phase != phase) throw new InvalidOperationException($"현재 단계({Phase})에서는 할 수 없습니다.");
        }
    }
}
