using System.Collections;
using System.Collections.Generic;
using System.Text;
using CowboyHunter.Audio;
using CowboyHunter.Battle;
using CowboyHunter.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CowboyHunter.UI
{
    // 회색 박스 전투 화면. 규칙은 BattleSession이 처리하고, 이 클래스는 입력 전달과 표시만 한다.
    // 런이 진행 중이면 런의 적/덱/체력을 쓰고, Battle 씬을 단독 실행하면 아래 테스트용 데이터를 쓴다.
    public class BattleScreen : MonoBehaviour
    {
        [Header("단독 실행용 전투 데이터")]
        [SerializeField] StartingDeckData startingDeck;
        [SerializeField] List<EnemyData> enemies = new();
        [SerializeField] int playerMaxHp = 100;
        [SerializeField] float stepDelay = 0.35f;

        [Header("화면 요소")]
        [SerializeField] Sprite playerSprite;
        [SerializeField] UnityEngine.UI.Image playerPortrait;
        [SerializeField] TMP_Text playerText;
        [SerializeField] Transform enemyRow;
        [SerializeField] UnityEngine.UI.Button enemyTemplate;
        [SerializeField] Transform handList;
        [SerializeField] UnityEngine.UI.Button bulletTemplate;
        [SerializeField] UnityEngine.UI.Button[] slotButtons = new UnityEngine.UI.Button[Cylinder.SlotCount];
        [SerializeField] TMP_Text deckText;
        [SerializeField] TMP_Text logText;
        [SerializeField] UnityEngine.UI.Button drawButton;
        [SerializeField] UnityEngine.UI.Button confirmButton;
        [SerializeField] GameObject resultOverlay;
        [SerializeField] TMP_Text resultText;
        [SerializeField] UnityEngine.UI.Button restartButton;

        [Header("연출")]
        [SerializeField] RectTransform shakeRoot;
        [SerializeField] RectTransform fxLayer;
        [SerializeField] TMP_Text popupTemplate;
        [SerializeField] Transform playerBar;

        // 스킨 스프라이트에 곱해지는 색
        static readonly Color NormalColor = Color.white;
        static readonly Color HighlightColor = new(1f, 0.85f, 0.45f);
        static readonly Color TargetColor = new(1f, 0.62f, 0.55f);
        static readonly Color EmptyColor = new(0.6f, 0.57f, 0.55f);
        const int MaxLogLines = 14;
        static readonly Color DamageColor = new(1f, 0.35f, 0.3f);          // 플레이어가 받은 피해
        static readonly Color EnemyDamageColor = new(1f, 0.93f, 0.55f);     // 적이 받은 피해 (붉은 카드 위에서도 잘 보이게)
        static readonly Color BlockColor = new(0.55f, 0.75f, 1f);
        static readonly Color StatusColor = new(0.8f, 0.95f, 0.45f);
        static readonly Color HitFlash = new(1f, 0.45f, 0.45f);

        BattleSession _session;
        int _selectedHand = -1;
        bool _animating;
        readonly List<UnityEngine.UI.Button> _enemyViews = new();
        readonly List<UnityEngine.UI.Button> _handViews = new();
        readonly List<string> _log = new();
        Vector2 _shakeOrigin;

        public BattleSession Session => _session;

        void Awake()
        {
            enemyTemplate.gameObject.SetActive(false);
            popupTemplate.gameObject.SetActive(false);
            _shakeOrigin = shakeRoot.anchoredPosition;
            bulletTemplate.gameObject.SetActive(false);
            drawButton.onClick.AddListener(OnDraw);
            confirmButton.onClick.AddListener(OnConfirm);
            restartButton.onClick.AddListener(OnResultButton);
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int slot = i;
                slotButtons[i].onClick.AddListener(() => OnSlot(slot));
            }
        }

        void Start() => StartBattle();

        public void StartBattle()
        {
            StopAllCoroutines();
            _animating = false;
            var run = GameSession.Run;
            _session = run != null
                ? new BattleSession(new Combatant(run.PlayerMaxHp, run.PlayerHp), new[] { run.CurrentTarget.Value.Enemy }, run.Deck, new System.Random(), run.Modifiers)
                : new BattleSession(new Combatant(playerMaxHp), enemies, startingDeck.Build(), new System.Random());
            _selectedHand = -1;
            _log.Clear();
            resultOverlay.SetActive(false);
            UiSprites.Show(playerPortrait, playerSprite);
            Sound.PlayMusic(run != null && run.CurrentTarget.Value.IsBoss ? MusicId.Boss : MusicId.Battle);

            foreach (var view in _enemyViews) Destroy(view.gameObject);
            _enemyViews.Clear();
            for (int i = 0; i < _session.Enemies.Count; i++)
            {
                int index = i;
                var view = Instantiate(enemyTemplate, enemyRow);
                view.gameObject.SetActive(true);
                view.onClick.AddListener(() => OnEnemy(index));
                _enemyViews.Add(view);
            }

            Log("전투 시작! DRAW를 눌러 탄을 뽑으세요.");
            Refresh();
        }

        // ── 입력 ───────────────────────────────────

        public void OnDraw()
        {
            if (_animating || _session.Phase != BattlePhase.AwaitingDraw) return;
            int hpBefore = _session.Player.Hp;
            _session.StartTurn();
            Sound.Play(SoundId.Draw);
            if (_session.Player.Hp < hpBefore)
            {
                Log($"상태이상 피해 {hpBefore - _session.Player.Hp}");
                Pop(playerPortrait.rectTransform, $"-{hpBefore - _session.Player.Hp}", StatusColor);
            }
            Log($"── {_session.Turn}턴: {_session.Hand.Count}발 드로우");
            _selectedHand = -1;
            Refresh();
            ShowResultIfOver();
        }

        public void OnBullet(int handIndex)
        {
            if (_animating) return;
            _selectedHand = _selectedHand == handIndex ? -1 : handIndex;
            Refresh();
        }

        // 탄을 고른 상태면 장전, 아니면 그 슬롯의 탄을 손으로 되돌린다.
        public void OnSlot(int slot)
        {
            if (_animating || _session.Phase != BattlePhase.Loading) return;
            if (_selectedHand >= 0)
            {
                _session.Load(_selectedHand, slot);
                Sound.Play(SoundId.LoadBullet);
                _selectedHand = -1;
            }
            else if (_session.Cylinder[slot] != null)
            {
                _session.Unload(slot);
                Sound.Play(SoundId.UnloadBullet);
            }
            Refresh();
        }

        public void OnEnemy(int index)
        {
            if (_animating || _session.Phase != BattlePhase.Loading || _session.Enemies[index].IsDead) return;
            _session.SelectTarget(index);
            Sound.Play(SoundId.UiClick);
            Refresh();
        }

        public void OnConfirm()
        {
            if (_animating || !_session.CanConfirm) return;
            var loaded = new BulletData[Cylinder.SlotCount];
            for (int i = 0; i < loaded.Length; i++) loaded[i] = _session.Cylinder[i];
            int playerHpBefore = _session.Player.Hp;   // Confirm은 적 턴까지 한 번에 계산하므로 발사 연출에는 이전 체력을 쓴다
            var result = _session.Confirm();
            StartCoroutine(PlayTurn(loaded, result, playerHpBefore));
        }

        // 규칙 계산은 Confirm에서 끝났고, 여기서는 결과를 한 단계씩 보여준다.
        IEnumerator PlayTurn(BulletData[] loaded, TurnResult result, int playerHpBefore)
        {
            _animating = true;
            UpdateButtons();

            var player = _session.Player;
            foreach (var shot in result.Shots)
            {
                ShowCylinder(loaded, shot.Slot);
                Log($"{shot.Slot + 1}번 {shot.Bullet.displayName} → {_session.Enemies[shot.TargetIndex].Data.displayName}{DescribeShot(shot)}");

                var view = _enemyViews[shot.TargetIndex];
                var target = (RectTransform)view.transform;
                StartCoroutine(BattleFx.Kick(playerPortrait.rectTransform, new Vector2(-10f, 0f)));   // 반동
                Sound.Play(SoundId.Shot);
                SetBar(view.transform.Find("HpBar"), shot.TargetHp, _session.Enemies[shot.TargetIndex].Stats.MaxHp, shot.TargetBlock);
                SetBar(playerBar, playerHpBefore, player.MaxHp, shot.PlayerBlock);

                if (shot.DamageDealt > 0)
                {
                    Pop(target, $"-{shot.DamageDealt}", EnemyDamageColor);
                    Sound.Play(SoundId.Hit);
                    StartCoroutine(BattleFx.Flash(UiSprites.Child(view, "Portrait"), HitFlash));
                    StartCoroutine(BattleFx.Shake(target, target.anchoredPosition, shot.DamageDealt >= 10 ? 14f : 7f));
                    if (shot.DamageDealt >= 10) StartCoroutine(BattleFx.Shake(shakeRoot, _shakeOrigin, 8f));
                }
                else if (shot.Bullet.damage > 0) { Pop(target, "막힘", BlockColor); Sound.Play(SoundId.Blocked); }
                if (shot.BlockGained > 0) { Pop(playerPortrait.rectTransform, $"+{shot.BlockGained} 방어", BlockColor); Sound.Play(SoundId.BlockGain); }
                if (shot.BurnApplied > 0) Pop(target, $"화상 +{shot.BurnApplied}", StatusColor);
                if (shot.PoisonApplied > 0) Pop(target, $"독 +{shot.PoisonApplied}", StatusColor);
                if (shot.WeakApplied > 0) Pop(target, $"약화 +{shot.WeakApplied}", StatusColor);
                if (shot.BurnApplied > 0 || shot.PoisonApplied > 0 || shot.WeakApplied > 0) Sound.Play(SoundId.StatusApply);
                if (shot.Killed)
                {
                    Sound.Play(SoundId.Kill);
                    Pop(target, shot.Bullet.killBonusGold > 0 ? $"처치! +{shot.Bullet.killBonusGold}G" : "처치!", HighlightColor);
                    view.image.color = EmptyColor;
                }
                yield return new WaitForSeconds(stepDelay);
            }
            ShowCylinder(new BulletData[Cylinder.SlotCount], -1);

            foreach (var a in result.EnemyActions)
            {
                var enemy = _session.Enemies[a.EnemyIndex];
                var view = _enemyViews[a.EnemyIndex];
                var rt = (RectTransform)view.transform;
                string name = enemy.Data.displayName;
                if (a.StatusDamage > 0)
                {
                    Log($"{name} 상태이상 피해 {a.StatusDamage}");
                    Pop(rt, $"-{a.StatusDamage}", StatusColor);
                }
                if (a.Acted)
                {
                    string extra = a.Action.type == EnemyActionType.Attack ? $" → 체력 -{a.DamageDealt}" : "";
                    Log($"{name}: {Describe(a.Action)}{extra}");
                    switch (a.Action.type)
                    {
                        case EnemyActionType.Attack:
                            StartCoroutine(BattleFx.Kick(rt, new Vector2(-24f, 0f)));
                            Sound.Play(SoundId.EnemyAttack);
                            if (a.DamageDealt > 0)
                            {
                                Pop(playerPortrait.rectTransform, $"-{a.DamageDealt}", DamageColor);
                                Sound.Play(SoundId.PlayerHurt);
                                StartCoroutine(BattleFx.Flash(playerPortrait, HitFlash));
                                StartCoroutine(BattleFx.Shake(shakeRoot, _shakeOrigin, Mathf.Clamp(a.DamageDealt, 6, 20)));
                            }
                            else Pop(playerPortrait.rectTransform, "막음", BlockColor);
                            break;
                        case EnemyActionType.Block: Pop(rt, $"+{a.Action.value} 방어", BlockColor); Sound.Play(SoundId.EnemyGuard); break;
                        case EnemyActionType.Poison: Pop(playerPortrait.rectTransform, $"독 +{a.Action.value}", StatusColor); Sound.Play(SoundId.StatusApply); break;
                    }
                }
                SetBar(view.transform.Find("HpBar"), a.EnemyHp, enemy.Stats.MaxHp, a.EnemyBlock);
                SetBar(playerBar, a.PlayerHp, player.MaxHp, a.PlayerBlock);
                yield return new WaitForSeconds(stepDelay);
            }

            _animating = false;
            Refresh();
            ShowResultIfOver();
        }

        // ── 표시 ───────────────────────────────────

        void Refresh()
        {
            var p = _session.Player;
            playerText.text = $"카우보이{Statuses(p)}";
            SetBar(playerBar, p.Hp, p.MaxHp, p.Block);

            for (int i = 0; i < _enemyViews.Count; i++)
            {
                var enemy = _session.Enemies[i];
                var view = _enemyViews[i];
                var s = enemy.Stats;
                var text = new StringBuilder($"{enemy.Data.displayName}{(enemy.Data.undead ? " <size=70%>[언데드]</size>" : "")}{Statuses(s)}");
                if (!enemy.IsDead && enemy.HasIntent) text.Append($"\n다음 행동: {Describe(enemy.Intent)}");
                if (enemy.IsDead) text.Append("\n쓰러짐");
                view.transform.Find("Text").GetComponent<TMP_Text>().text = text.ToString();
                SetBar(view.transform.Find("HpBar"), s.Hp, s.MaxHp, s.Block);
                view.image.color = enemy.IsDead ? EmptyColor : i == _session.TargetIndex ? TargetColor : NormalColor;
                var portrait = UiSprites.Child(view, "Portrait");
                UiSprites.Show(portrait, enemy.Data.sprite);
                portrait.color = enemy.IsDead ? new Color(0.3f, 0.3f, 0.3f, 0.6f) : Color.white;
            }

            foreach (var view in _handViews) Destroy(view.gameObject);
            _handViews.Clear();
            for (int i = 0; i < _session.Hand.Count; i++)
            {
                int index = i;
                var bullet = _session.Hand[i];
                var view = Instantiate(bulletTemplate, handList);
                view.gameObject.SetActive(true);
                view.GetComponentInChildren<TMP_Text>().text = $"{bullet.displayName}\n<size=70%>{bullet.description}</size>";
                view.image.color = i == _selectedHand ? HighlightColor : NormalColor;
                UiSprites.Show(UiSprites.Child(view, "Icon"), bullet.icon);
                view.onClick.AddListener(() => OnBullet(index));
                _handViews.Add(view);
            }

            var current = new BulletData[Cylinder.SlotCount];
            for (int i = 0; i < current.Length; i++) current[i] = _session.Cylinder[i];
            ShowCylinder(current, -1);

            deckText.text = $"드로우 {_session.Deck.DrawPile.Count}  |  버림 {_session.Deck.DiscardPile.Count}";
            UpdateButtons();
        }

        // 체력바: Fill의 가로 길이로 남은 체력을, 라벨로 숫자와 보호막을 보여준다.
        static void SetBar(Transform bar, int hp, int maxHp, int block)
        {
            var fill = (RectTransform)bar.Find("Fill");
            fill.anchorMax = new Vector2(maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f, 1f);
            bar.Find("Label").GetComponent<TMP_Text>().text = block > 0 ? $"{hp}/{maxHp}  방어 {block}" : $"{hp}/{maxHp}";
        }

        void Pop(RectTransform target, string text, Color color) =>
            StartCoroutine(BattleFx.Popup(popupTemplate, fxLayer, target, text, color));

        void ShowCylinder(BulletData[] bullets, int firingSlot)
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                var b = bullets[i];
                var slot = slotButtons[i].transform;
                slot.Find("Text").GetComponent<TMP_Text>().text = (i + 1).ToString();
                slot.Find("Name").GetComponent<TMP_Text>().text = b != null ? b.displayName : "";
                UiSprites.Show(UiSprites.Child(slot, "Icon"), b != null ? b.icon : null);
                slotButtons[i].image.color = i == firingSlot ? HighlightColor : b != null ? NormalColor : EmptyColor;
            }
        }

        void UpdateButtons()
        {
            drawButton.interactable = !_animating && _session.Phase == BattlePhase.AwaitingDraw;
            confirmButton.interactable = !_animating && _session.CanConfirm;
        }

        // 전투가 끝나면 런에 결과를 바로 반영하고, 받은 보상을 함께 보여준다.
        void ShowResultIfOver()
        {
            bool won = _session.Phase == BattlePhase.Won;
            if (!won && _session.Phase != BattlePhase.Lost) return;
            Sound.Play(won ? SoundId.Victory : SoundId.Defeat);

            var message = won ? $"승리!\n{_session.Turn}턴, 남은 체력 {_session.Player.Hp}" : $"패배...\n{_session.Turn}턴에서 쓰러짐";
            var run = GameSession.Run;
            if (run != null)
            {
                run.CompleteBattle(won, _session.Player.Hp, _session.BonusGold);
                if (won) message += $"\n\n+{run.LastGoldReward} GOLD";
                if (run.LastRelicReward != null) message += $"\n유물 획득: {run.LastRelicReward.displayName}";
            }
            ShowResult(message);
        }

        // 런 중이면 다음 화면(상점/결과)으로, 단독 실행이면 전투를 다시 시작한다.
        void OnResultButton()
        {
            Sound.Play(SoundId.UiClick);
            var run = GameSession.Run;
            if (run == null)
            {
                StartBattle();
                return;
            }
            SceneManager.LoadScene(run.Phase == RunPhase.Shop ? SceneNames.Shop : SceneNames.Result);
        }

        void ShowResult(string message)
        {
            resultText.text = message;
            restartButton.GetComponentInChildren<TMP_Text>().text = GameSession.Run != null ? "계속" : "다시 하기";
            resultOverlay.SetActive(true);
        }

        void Log(string line)
        {
            _log.Add(line);
            if (_log.Count > MaxLogLines) _log.RemoveAt(0);
            logText.text = string.Join("\n", _log);
        }

        static string DescribeShot(ShotResult shot)
        {
            var b = shot.Bullet;
            var parts = new List<string>();
            if (b.damage > 0) parts.Add($"피해 {shot.DamageDealt}");
            if (shot.BlockGained > 0) parts.Add($"보호막 +{shot.BlockGained}");
            if (shot.BurnApplied > 0) parts.Add($"화상 +{shot.BurnApplied}");
            if (shot.PoisonApplied > 0) parts.Add($"독 +{shot.PoisonApplied}");
            if (shot.WeakApplied > 0) parts.Add($"약화 +{shot.WeakApplied}");
            if (shot.Killed) parts.Add(b.killBonusGold > 0 ? $"처치! +{b.killBonusGold} GOLD" : "처치!");
            return parts.Count > 0 ? " : " + string.Join(", ", parts) : "";
        }

        static string Describe(EnemyAction action) => action.type switch
        {
            EnemyActionType.Attack => action.HitCount > 1 ? $"공격 {action.value}×{action.HitCount}" : $"공격 {action.value}",
            EnemyActionType.Block => $"방어 {action.value}",
            EnemyActionType.Poison => $"독 {action.value}",
            _ => action.type.ToString()
        };

        static string Statuses(Combatant c)
        {
            var s = "";
            if (c.Burn > 0) s += $"\n화상 {c.Burn}";
            if (c.Poison > 0) s += $"\n독 {c.Poison}";
            if (c.Weak > 0) s += $"\n약화 {c.Weak}";
            return s;
        }
    }
}
