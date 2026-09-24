using System.Collections.Generic;

namespace CowboyHunter.Battle
{
    public class Cylinder
    {
        public const int SlotCount = 6;

        readonly BulletData[] _slots = new BulletData[SlotCount];

        public BulletData this[int slot] => _slots[slot];

        public bool IsFull
        {
            get
            {
                foreach (var b in _slots)
                    if (b == null) return false;
                return true;
            }
        }

        // 슬롯에 장전하고, 원래 있던 탄(없으면 null)을 돌려준다.
        public BulletData Load(int slot, BulletData bullet)
        {
            var previous = _slots[slot];
            _slots[slot] = bullet;
            return previous;
        }

        public BulletData Unload(int slot) => Load(slot, null);

        public List<BulletData> TakeAll()
        {
            var list = new List<BulletData>();
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] != null) list.Add(_slots[i]);
                _slots[i] = null;
            }
            return list;
        }
    }
}
