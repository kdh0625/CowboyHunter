using System;
using System.Collections.Generic;
using UnityEngine;

namespace CowboyHunter.Battle
{
    public enum EnemyActionType { Attack, Block, Poison }

    [Serializable]
    public struct EnemyAction
    {
        public EnemyActionType type;
        public int value;
    }

    [CreateAssetMenu(menuName = "CowboyHunter/Enemy", fileName = "Enemy")]
    public class EnemyData : ScriptableObject
    {
        public string displayName;
        public Sprite sprite;
        public int maxHp = 30;
        [Tooltip("처치 시 받는 골드")] public int bounty = 10;
        [Tooltip("위에서부터 순서대로 반복 수행")]
        public List<EnemyAction> pattern = new();
    }
}
