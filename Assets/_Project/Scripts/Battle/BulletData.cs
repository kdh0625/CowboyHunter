using UnityEngine;

namespace CowboyHunter.Battle
{
    [CreateAssetMenu(menuName = "CowboyHunter/Bullet", fileName = "Bullet")]
    public class BulletData : ScriptableObject
    {
        public string displayName;
        [Tooltip("상점 가격")] public int price = 20;
        public Sprite icon;
        [TextArea] public string description;

        [Header("효과")]
        public int damage;
        [Tooltip("적 보호막 무시")] public bool pierce;
        [Tooltip("플레이어 보호막 획득")] public int block;
        [Tooltip("대상에게 화상 부여")] public int burn;
        [Tooltip("대상에게 독 부여")] public int poison;

        [Header("연계: 직전 슬롯에서 이 탄이 발사됐으면 추가 피해")]
        public BulletData comboPrevious;
        public int comboBonusDamage;
    }
}
