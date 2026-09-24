using System.Collections.Generic;
using CowboyHunter.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CowboyHunter.UI
{
    // 수배서 게시판. 수배서를 고르면 전투 씬으로 넘어간다.
    public class WantedBoardScreen : MonoBehaviour
    {
        [Tooltip("이 씬을 단독 실행할 때 새 런을 만드는 데 쓴다")]
        [SerializeField] RunConfig fallbackConfig;

        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text remainingText;
        [SerializeField] Transform posterRow;
        [SerializeField] Button posterTemplate;
        [SerializeField] Button bossButton;

        // 스킨 스프라이트에 곱해지는 색
        static readonly Color PosterColor = Color.white;
        static readonly Color EliteColor = new(1f, 0.78f, 0.55f);
        static readonly Color LockedColor = new(0.55f, 0.55f, 0.55f);

        void Start()
        {
            GameSession.Run ??= new RunState(fallbackConfig, new System.Random());
            var run = GameSession.Run;
            SaveSystem.Save(run);

            posterTemplate.gameObject.SetActive(false);
            for (int i = 0; i < run.Posters.Count; i++)
            {
                int index = i;
                var poster = run.Posters[i];
                var view = Instantiate(posterTemplate, posterRow);
                view.gameObject.SetActive(true);
                view.image.color = poster.IsElite ? EliteColor : PosterColor;
                view.transform.Find("Text").GetComponent<TMP_Text>().text =
                    $"{poster.Enemy.displayName}\n{(poster.IsElite ? "[엘리트]  " : "")}{poster.Bounty} GOLD";
                UiSprites.Show(UiSprites.Child(view, "Portrait"), poster.Enemy.sprite);
                view.onClick.AddListener(() => { run.Accept(index); SceneManager.LoadScene(SceneNames.Battle); });
            }

            bossButton.interactable = run.BossUnlocked;
            bossButton.image.color = run.BossUnlocked ? PosterColor : LockedColor;
            bossButton.transform.Find("Text").GetComponent<TMP_Text>().text = run.BossUnlocked
                ? $"{run.BossPoster.Enemy.displayName}\n{run.BossPoster.Bounty} GOLD"
                : "???";
            // 잠겨 있을 때는 실루엣만 보여준다
            var bossPortrait = UiSprites.Child(bossButton, "Portrait");
            UiSprites.Show(bossPortrait, run.BossPoster.Enemy.sprite);
            bossPortrait.color = run.BossUnlocked ? Color.white : new Color(0.1f, 0.08f, 0.06f, 0.9f);
            bossButton.onClick.AddListener(() => { run.AcceptBoss(); SceneManager.LoadScene(SceneNames.Battle); });

            statusText.text = $"{run.Chapter.displayName} / {run.ChapterCount}   HP {run.PlayerHp}/{run.PlayerMaxHp}   {run.Gold} GOLD";
            remainingText.text = $"남은 적 : {run.RemainingEnemies}";
        }
    }
}
