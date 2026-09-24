using System;

namespace CowboyHunter.Battle
{
    public class Combatant
    {
        public int MaxHp { get; }
        public int Hp { get; private set; }
        public int Block { get; private set; }
        public int Burn { get; private set; }
        public int Poison { get; private set; }
        public int Weak { get; private set; }
        public bool IsDead => Hp <= 0;

        public Combatant(int maxHp) : this(maxHp, maxHp) { }

        public Combatant(int maxHp, int hp)
        {
            MaxHp = maxHp;
            Hp = hp;
        }

        // 실제로 깎인 체력을 반환한다.
        public int TakeDamage(int amount, bool pierce = false)
        {
            if (amount <= 0 || IsDead) return 0;
            if (!pierce)
            {
                int absorbed = Math.Min(Block, amount);
                Block -= absorbed;
                amount -= absorbed;
            }
            int dealt = Math.Min(Hp, amount);
            Hp -= dealt;
            return dealt;
        }

        public void GainBlock(int amount) => Block += amount;
        public void ClearBlock() => Block = 0;
        public void AddBurn(int stacks) => Burn += stacks;
        public void AddPoison(int stacks) => Poison += stacks;
        public void AddWeak(int amount) => Weak += amount;
        public void ClearWeak() => Weak = 0;

        // 턴 시작 시 상태이상 피해(보호막 무시). 화상은 1씩 줄고, 독은 전투 끝까지 유지된다.
        public int TickStatus()
        {
            int dealt = TakeDamage(Burn + Poison, pierce: true);
            if (Burn > 0) Burn--;
            return dealt;
        }
    }
}
