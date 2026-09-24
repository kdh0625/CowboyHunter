using UnityEngine;

namespace CowboyHunter.UI
{
    // 코드로 만드는 화면(일시 정지 메뉴 등)이 쓰는 스킨. Resources/UiSkin 에 둔다.
    [CreateAssetMenu(menuName = "CowboyHunter/UI Skin", fileName = "UiSkin")]
    public class UiSkin : ScriptableObject
    {
        public Sprite panel;
        public Sprite button;
        public Sprite dark;
    }
}
