namespace CowboyHunter.Battle
{
    // 장착한 유물들의 효과를 합친 값. 전투 규칙은 이 값만 알고 유물 자체는 모른다.
    public struct BattleModifiers
    {
        public int DamageBonus;      // 피해를 주는 탄의 피해 +N
        public int BlockBonus;       // 보호막을 주는 탄의 보호막 +N
        public int BurnBonus;        // 화상을 거는 탄의 화상 +N
        public int ExtraDraw;        // 턴당 드로우 +N
        public int TurnStartBlock;   // 내 턴 시작 시 보호막 +N
        public int FocusSlot;        // 1~6번 슬롯 중 하나 (0이면 없음)
        public int FocusDamage;      // FocusSlot에서 발사된 피해 탄의 피해 +N
        public int UndeadBonus;      // 언데드에게 피해를 주는 탄의 피해 +N
        public int KillGold;         // 적을 처치할 때마다 골드 +N
    }
}
