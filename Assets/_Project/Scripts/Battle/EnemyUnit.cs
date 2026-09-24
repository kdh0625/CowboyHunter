namespace CowboyHunter.Battle
{
    public class EnemyUnit
    {
        public EnemyData Data { get; }
        public Combatant Stats { get; }
        public bool IsDead => Stats.IsDead;

        int _patternIndex;

        public EnemyUnit(EnemyData data)
        {
            Data = data;
            Stats = new Combatant(data.maxHp);
        }

        public bool HasIntent => Data.pattern.Count > 0;

        // 다음 적 턴에 수행할 행동(화면에 예고로 표시)
        public EnemyAction Intent => Data.pattern[_patternIndex];

        public void AdvanceIntent() => _patternIndex = (_patternIndex + 1) % Data.pattern.Count;
    }
}
