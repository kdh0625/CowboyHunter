using System.Collections.Generic;
using CowboyHunter.Battle;
using UnityEngine;

namespace CowboyHunter.Core
{
    [CreateAssetMenu(menuName = "CowboyHunter/Chapter", fileName = "Chapter")]
    public class ChapterData : ScriptableObject
    {
        public string displayName;
        public List<EnemyData> normals = new();
        public List<EnemyData> elites = new();
        public EnemyData boss;
    }
}
