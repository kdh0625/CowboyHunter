using System;
using System.Collections.Generic;

namespace CowboyHunter.Core
{
    [Serializable]
    public class PosterSave
    {
        public string enemy;
        public bool elite;
    }

    [Serializable]
    public class OfferSave
    {
        public string bullet;
        public bool sold;
    }

    [Serializable]
    public class ShopSave
    {
        public List<OfferSave> bullets = new();
        public string relic;
        public bool relicSold;
        public bool whiskeySold;
        public bool removalUsed;
    }

    // JSON으로 저장되는 런 스냅샷. 에셋은 이름으로 기록하고, 불러올 때 RunConfig에서 찾는다.
    [Serializable]
    public class RunSaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public RunPhase phase;
        public int chapterIndex;
        public int remainingEnemies;
        public int playerHp;
        public int gold;
        public List<string> deck = new();
        public List<PosterSave> posters = new();
        public List<string> equipped = new();   // RelicSlot 순서, 빈 칸은 ""
        public List<string> inventory = new();
        public bool hasShop;
        public ShopSave shop = new();
    }
}
