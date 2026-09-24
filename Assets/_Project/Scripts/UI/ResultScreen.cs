using CowboyHunter.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CowboyHunter.UI
{
    public class ResultScreen : MonoBehaviour
    {
        [SerializeField] TMP_Text resultText;
        [SerializeField] Button titleButton;

        void Start()
        {
            var run = GameSession.Run;
            if (run == null) resultText.text = "진행 중인 런이 없습니다.";
            else if (run.Phase == RunPhase.Cleared) resultText.text = $"현상금 사냥 완료!\n\n획득 골드 {run.Gold}\n남은 체력 {run.PlayerHp}/{run.PlayerMaxHp}";
            else resultText.text = $"쓰러졌다...\n\n{run.Chapter.displayName}, 남은 적 {run.RemainingEnemies}\n획득 골드 {run.Gold}";

            titleButton.onClick.AddListener(() =>
            {
                GameSession.Run = null;
                SceneManager.LoadScene(SceneNames.Title);
            });
        }
    }
}
