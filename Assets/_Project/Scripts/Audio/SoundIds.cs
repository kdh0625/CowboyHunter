namespace CowboyHunter.Audio
{
    // 번호는 에셋에 저장되므로, 새 항목은 끝에 추가하고 기존 번호는 바꾸지 않는다.
    public enum SoundId
    {
        None = 0,

        // 전투
        Shot = 1,          // 한 발 발사
        Hit = 2,           // 적이 피해를 입음
        Blocked = 3,       // 적 보호막에 막힘
        Kill = 4,          // 적 처치
        BlockGain = 5,     // 내가 보호막 획득
        StatusApply = 6,   // 화상·독·약화 부여
        EnemyAttack = 7,   // 적 공격 모션
        PlayerHurt = 8,    // 내가 피해를 입음
        EnemyGuard = 9,    // 적이 방어
        Draw = 10,         // 탄 드로우
        LoadBullet = 11,   // 슬롯에 장전
        UnloadBullet = 12, // 슬롯에서 빼기
        Victory = 13,
        Defeat = 14,

        // 화면 / 런
        UiClick = 20,
        PosterSelect = 21,
        Buy = 22,
        BuyFail = 23,
        Equip = 24,
        Heal = 25,
    }

    public enum MusicId
    {
        None = 0,
        Title = 1,
        Board = 2,
        Battle = 3,
        Boss = 4,
        Shop = 5,
        Result = 6,
    }
}
