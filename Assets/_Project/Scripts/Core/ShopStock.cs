using System.Collections.Generic;
using CowboyHunter.Battle;

namespace CowboyHunter.Core
{
    public class BulletOffer
    {
        public BulletData Bullet;
        public bool Sold;
    }

    // 상점 한 번 방문 동안의 진열 상태. 각 품목은 방문당 한 번만 살 수 있다.
    public class ShopStock
    {
        public readonly List<BulletOffer> Bullets = new();
        public RelicData Relic;
        public bool RelicSold;
        public bool WhiskeySold;
        public bool RemovalUsed;
    }
}
