using System.Collections.Generic;

namespace CowboyHunter.Battle
{
    public class Deck
    {
        readonly List<BulletData> _drawPile;
        readonly List<BulletData> _discardPile = new();
        readonly System.Random _rng;

        public IReadOnlyList<BulletData> DrawPile => _drawPile;
        public IReadOnlyList<BulletData> DiscardPile => _discardPile;

        public Deck(IEnumerable<BulletData> bullets, System.Random rng)
        {
            _rng = rng;
            _drawPile = new List<BulletData>(bullets);
            Shuffle(_drawPile);
        }

        // 뽑을 탄이 없으면 버린 더미를 섞어 드로우 더미로 만든 뒤 이어서 뽑는다.
        public List<BulletData> Draw(int count)
        {
            var drawn = new List<BulletData>(count);
            while (drawn.Count < count)
            {
                if (_drawPile.Count == 0)
                {
                    if (_discardPile.Count == 0) break;
                    _drawPile.AddRange(_discardPile);
                    _discardPile.Clear();
                    Shuffle(_drawPile);
                }
                int last = _drawPile.Count - 1;
                drawn.Add(_drawPile[last]);
                _drawPile.RemoveAt(last);
            }
            return drawn;
        }

        public void Discard(IEnumerable<BulletData> bullets) => _discardPile.AddRange(bullets);

        void Shuffle(List<BulletData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
