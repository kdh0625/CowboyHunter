using System;
using System.Collections.Generic;
using UnityEngine;

namespace CowboyHunter.Battle
{
    [CreateAssetMenu(menuName = "CowboyHunter/Starting Deck", fileName = "StartingDeck")]
    public class StartingDeckData : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public BulletData bullet;
            public int count;
        }

        public List<Entry> entries = new();

        public List<BulletData> Build()
        {
            var list = new List<BulletData>();
            foreach (var e in entries)
                for (int i = 0; i < e.count; i++) list.Add(e.bullet);
            return list;
        }
    }
}
