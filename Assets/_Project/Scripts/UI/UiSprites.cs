using UnityEngine;
using UnityEngine.UI;

namespace CowboyHunter.UI
{
    public static class UiSprites
    {
        // 스프라이트가 없으면 이미지를 숨긴다. 아트가 없는 항목은 텍스트만 보인다.
        public static void Show(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        public static Image Child(Component parent, string name) => parent.transform.Find(name).GetComponent<Image>();
    }
}
