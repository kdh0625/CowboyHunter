using System;
using System.Collections.Generic;
using CowboyHunter.Battle;

namespace CowboyHunter.Core
{
    public enum RunPhase { Board, Battle, Shop, Cleared, Dead }

    public readonly struct WantedPoster
    {
        public readonly EnemyData Enemy;
        public readonly bool IsElite;
        public readonly bool IsBoss;
        public int Bounty => Enemy.bounty;

        public WantedPoster(EnemyData enemy, bool isElite, bool isBoss)
        {
            Enemy = enemy;
            IsElite = isElite;
            IsBoss = isBoss;
        }
    }

    // 한 번의 런(새 게임 ~ 클리어/사망) 동안 유지되는 진행 상태
    public class RunState
    {
        public RunConfig Config { get; }
        public RunPhase Phase { get; private set; } = RunPhase.Board;
        public int ChapterIndex { get; private set; }
        public int RemainingEnemies { get; private set; }
        public int PlayerMaxHp { get; }
        public int PlayerHp { get; private set; }
        public int Gold { get; private set; }
        public List<BulletData> Deck { get; }
        public IReadOnlyList<WantedPoster> Posters => _posters;
        public WantedPoster? CurrentTarget { get; private set; }

        public ChapterData Chapter => Config.chapters[ChapterIndex];
        public int ChapterCount => Config.chapters.Count;
        public bool BossUnlocked => RemainingEnemies == 0;
        public WantedPoster BossPoster => new(Chapter.boss, false, true);

        public IReadOnlyList<RelicData> Inventory => _inventory;
        public ShopStock Shop { get; private set; }
        public int LastGoldReward { get; private set; }
        public RelicData LastRelicReward { get; private set; }

        readonly List<WantedPoster> _posters = new();
        readonly Dictionary<RelicSlot, RelicData> _equipped = new();
        readonly List<RelicData> _inventory = new();
        readonly Random _rng;

        public RunState(RunConfig config, Random rng)
        {
            Config = config;
            _rng = rng;
            PlayerMaxHp = config.playerMaxHp;
            PlayerHp = PlayerMaxHp;
            Deck = config.startingDeck.Build();
            RemainingEnemies = config.enemiesPerChapter;
            DealPosters();
        }

        public void Accept(int posterIndex)
        {
            Require(RunPhase.Board);
            CurrentTarget = _posters[posterIndex];
            Phase = RunPhase.Battle;
        }

        public void AcceptBoss()
        {
            Require(RunPhase.Board);
            if (!BossUnlocked) throw new InvalidOperationException("아직 보스 수배서가 잠겨 있습니다.");
            CurrentTarget = BossPoster;
            Phase = RunPhase.Battle;
        }

        // 전투 결과 반영. 체력은 회복 없이 그대로 이어지고, 이기면 상점으로 간다.
        public void CompleteBattle(bool won, int playerHpAfter)
        {
            Require(RunPhase.Battle);
            var target = CurrentTarget.Value;
            CurrentTarget = null;
            PlayerHp = playerHpAfter;
            LastGoldReward = 0;
            LastRelicReward = null;

            if (!won)
            {
                Phase = RunPhase.Dead;
                return;
            }

            Gold += target.Bounty;
            LastGoldReward = target.Bounty;
            if (target.IsElite)
            {
                LastRelicReward = RandomUnownedRelic();
                if (LastRelicReward != null) Acquire(LastRelicReward);
            }

            if (!target.IsBoss)
            {
                RemainingEnemies--;
            }
            else if (ChapterIndex == ChapterCount - 1)
            {
                Phase = RunPhase.Cleared;
                return;
            }
            else
            {
                ChapterIndex++;
                RemainingEnemies = Config.enemiesPerChapter;
            }

            Phase = RunPhase.Shop;
            Shop = CreateShop();
        }

        public void LeaveShop()
        {
            Require(RunPhase.Shop);
            Shop = null;
            Phase = RunPhase.Board;
            DealPosters();
        }

        // ── 유물 ───────────────────────────────────

        public RelicData GetEquipped(RelicSlot slot) => _equipped.TryGetValue(slot, out var r) ? r : null;

        public bool Owns(RelicData relic) => _inventory.Contains(relic) || _equipped.ContainsValue(relic);

        public BattleModifiers Modifiers
        {
            get
            {
                var m = new BattleModifiers();
                foreach (var relic in _equipped.Values) relic.AddTo(ref m);
                return m;
            }
        }

        // 유물 교체는 스테이지가 끝난 뒤(상점)에서만 할 수 있다. 같은 부위의 기존 유물은 인벤토리로 간다.
        public void Equip(RelicData relic)
        {
            Require(RunPhase.Shop);
            if (!_inventory.Remove(relic)) throw new InvalidOperationException("인벤토리에 없는 유물입니다.");
            var previous = GetEquipped(relic.slot);
            if (previous != null) _inventory.Add(previous);
            _equipped[relic.slot] = relic;
        }

        public void Unequip(RelicSlot slot)
        {
            Require(RunPhase.Shop);
            var relic = GetEquipped(slot);
            if (relic == null) return;
            _equipped.Remove(slot);
            _inventory.Add(relic);
        }

        // 새로 얻은 유물은 그 부위가 비어 있으면 바로 장착하고, 아니면 인벤토리에 넣는다.
        void Acquire(RelicData relic)
        {
            if (GetEquipped(relic.slot) == null) _equipped[relic.slot] = relic;
            else _inventory.Add(relic);
        }

        RelicData RandomUnownedRelic()
        {
            var candidates = Config.relicPool.FindAll(r => !Owns(r));
            return candidates.Count > 0 ? candidates[_rng.Next(candidates.Count)] : null;
        }

        // ── 상점 ───────────────────────────────────

        ShopStock CreateShop()
        {
            var stock = new ShopStock();
            var pool = new List<BulletData>(Config.shopBullets);
            for (int i = 0; i < Config.bulletOffers && pool.Count > 0; i++)
            {
                int pick = _rng.Next(pool.Count);
                stock.Bullets.Add(new BulletOffer { Bullet = pool[pick] });
                pool.RemoveAt(pick);
            }
            stock.Relic = RandomUnownedRelic();
            return stock;
        }

        bool Pay(int price)
        {
            if (Gold < price) return false;
            Gold -= price;
            return true;
        }

        public bool BuyBullet(int offerIndex)
        {
            Require(RunPhase.Shop);
            var offer = Shop.Bullets[offerIndex];
            if (offer.Sold || !Pay(offer.Bullet.price)) return false;
            offer.Sold = true;
            Deck.Add(offer.Bullet);
            return true;
        }

        public bool BuyRelic()
        {
            Require(RunPhase.Shop);
            if (Shop.Relic == null || Shop.RelicSold || !Pay(Shop.Relic.price)) return false;
            Shop.RelicSold = true;
            Acquire(Shop.Relic);
            return true;
        }

        public bool BuyWhiskey()
        {
            Require(RunPhase.Shop);
            if (Shop.WhiskeySold || !Pay(Config.whiskeyPrice)) return false;
            Shop.WhiskeySold = true;
            PlayerHp = Math.Min(PlayerMaxHp, PlayerHp + Config.whiskeyHeal);
            return true;
        }

        public bool RemoveBullet(BulletData bullet)
        {
            Require(RunPhase.Shop);
            if (Shop.RemovalUsed || !Deck.Contains(bullet) || !Pay(Config.removalPrice)) return false;
            Shop.RemovalUsed = true;
            Deck.Remove(bullet);
            return true;
        }

        // 보스가 해금되면 일반 수배서는 내리고 보스 수배서만 남긴다.
        void DealPosters()
        {
            _posters.Clear();
            if (BossUnlocked) return;

            int count = _rng.Next(Config.minPosters, Config.maxPosters + 1);
            for (int i = 0; i < count; i++)
            {
                bool elite = Chapter.elites.Count > 0 && _rng.NextDouble() < Config.eliteChance;
                var pool = elite ? Chapter.elites : Chapter.normals;
                _posters.Add(new WantedPoster(pool[_rng.Next(pool.Count)], elite, false));
            }
        }

        void Require(RunPhase phase)
        {
            if (Phase != phase) throw new InvalidOperationException($"현재 단계({Phase})에서는 할 수 없습니다.");
        }
    }
}
