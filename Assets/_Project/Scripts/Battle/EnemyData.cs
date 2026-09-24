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
        [Tooltip("공격 횟수 (0이면 1회)")] public int hits;

        public int HitCount => hits > 0 ? hits : 1;
    }

    [CreateAssetMenu(menuName = "CowboyHunter/Enemy", fileName = "Enemy")]
    public class EnemyData : ScriptableObject
    {
        public string displayName;
        public Sprite sprite;
        public int maxHp = 30;
        [Tooltip("처치 시 받는 골드")] public int bounty = 10;
        [Tooltip("은탄 등 언데드 특효가 적용된다")] public bool undead;
        [Tooltip("위에서부터 순서대로 반복 수행")]
        public List<EnemyAction> pattern = new();
    }
}
