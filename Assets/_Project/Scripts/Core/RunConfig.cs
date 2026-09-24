using System.Collections.Generic;
using CowboyHunter.Battle;
using UnityEngine;

namespace CowboyHunter.Core
{
    [CreateAssetMenu(menuName = "CowboyHunter/Run Config", fileName = "RunConfig")]
    public class RunConfig : ScriptableObject
    {
        public StartingDeckData startingDeck;
        public int playerMaxHp = 100;
        [Tooltip("챕터마다 보스 전에 처치해야 하는 적 수")] public int enemiesPerChapter = 5;
        public int minPosters = 3;
        public int maxPosters = 5;
        [Range(0f, 1f)] public float eliteChance = 0.2f;
        public List<ChapterData> chapters = new();

        [Header("유물")]
        [Tooltip("엘리트 드랍과 상점에 나오는 유물")] public List<RelicData> relicPool = new();

        [Header("상점")]
        public List<BulletData> shopBullets = new();
        public int bulletOffers = 3;
        public int whiskeyPrice = 20;
        public int whiskeyHeal = 25;
        public int removalPrice = 30;
    }
}
