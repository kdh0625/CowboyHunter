using CowboyHunter.Battle;
using UnityEngine;

namespace CowboyHunter.Core
{
    public enum RelicSlot { Muzzle, Scope, Cylinder, Grip }

    [CreateAssetMenu(menuName = "CowboyHunter/Relic", fileName = "Relic")]
    public class RelicData : ScriptableObject
    {
        public string displayName;
        public Sprite icon;
        [TextArea] public string description;
        [Tooltip("장착할 수 있는 총 부위")] public RelicSlot slot;
        [Tooltip("상점 가격")] public int price = 60;

        [Header("효과")]
        public int damageBonus;
        public int blockBonus;
        public int burnBonus;
        public int extraDraw;
        public int turnStartBlock;
        [Tooltip("1~6번 슬롯 (0이면 없음)")] public int focusSlot;
        public int focusDamage;
        public int undeadBonus;
        public int killGold;

        public static string SlotName(RelicSlot slot) => slot switch
        {
            RelicSlot.Muzzle => "총구",
            RelicSlot.Scope => "조준경",
            RelicSlot.Cylinder => "실린더",
            RelicSlot.Grip => "손잡이",
            _ => slot.ToString()
        };

        public void AddTo(ref BattleModifiers m)
        {
            m.DamageBonus += damageBonus;
            m.BlockBonus += blockBonus;
            m.BurnBonus += burnBonus;
            m.ExtraDraw += extraDraw;
            m.TurnStartBlock += turnStartBlock;
            m.UndeadBonus += undeadBonus;
            m.KillGold += killGold;
            if (focusSlot > 0)
            {
                m.FocusSlot = focusSlot;
                m.FocusDamage += focusDamage;
            }
        }
    }
}
