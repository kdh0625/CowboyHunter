using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CowboyHunter.UI
{
    // 전투 연출용 짧은 애니메이션들. BattleScreen이 StartCoroutine으로 실행한다.
    public static class BattleFx
    {
        // 대상 위에 글자를 띄우고, 위로 올라가며 사라지게 한다.
        public static IEnumerator Popup(TMP_Text template, RectTransform layer, RectTransform target, string text, Color color)
        {
            var popup = Object.Instantiate(template, layer);
            popup.gameObject.SetActive(true);
            popup.text = text;
            popup.color = color;
            var rt = popup.rectTransform;
            rt.position = target.position;
            var start = rt.anchoredPosition + new Vector2(Random.Range(-30f, 30f), 90f);
            const float duration = 0.8f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                rt.anchoredPosition = start + Vector2.up * 80f * k;
                popup.alpha = 1f - k * k;
                yield return null;
            }
            Object.Destroy(popup.gameObject);
        }

        // 제자리를 기준으로 흔들다가 원래 위치로 돌려놓는다.
        public static IEnumerator Shake(RectTransform rt, Vector2 origin, float amount, float duration = 0.2f)
        {
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                if (rt == null) yield break;
                rt.anchoredPosition = origin + Random.insideUnitCircle * amount * (1f - t / duration);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = origin;
        }

        // 한 방향으로 살짝 밀렸다가 돌아온다 (발사 반동, 공격 모션).
        public static IEnumerator Kick(RectTransform rt, Vector2 offset, float duration = 0.14f)
        {
            var origin = rt.anchoredPosition;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                if (rt == null) yield break;
                rt.anchoredPosition = origin + offset * Mathf.Sin(t / duration * Mathf.PI);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = origin;
        }

        public static IEnumerator Flash(Graphic graphic, Color flash, float duration = 0.15f)
        {
            var original = graphic.color;
            graphic.color = flash;
            yield return new WaitForSeconds(duration);
            if (graphic != null) graphic.color = original;
        }
    }
}
