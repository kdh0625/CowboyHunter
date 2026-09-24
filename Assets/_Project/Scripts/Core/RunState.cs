using System;
using System.Collections.Generic;
using CowboyHunter.Battle;

namespace CowboyHunter.Core
{
    public enum RunPhase { Board, Battle, Cleared, Dead }

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

        readonly List<WantedPoster> _posters = new();
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

        // 전투 결과 반영. 체력은 회복 없이 그대로 이어진다.
        public void CompleteBattle(bool won, int playerHpAfter)
        {
            Require(RunPhase.Battle);
            var target = CurrentTarget.Value;
            CurrentTarget = null;
            PlayerHp = playerHpAfter;

            if (!won)
            {
                Phase = RunPhase.Dead;
                return;
            }

            Gold += target.Bounty;
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

            Phase = RunPhase.Board;
            DealPosters();
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
