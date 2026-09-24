using CowboyHunter.Core;
using CowboyHunter.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CowboyHunter.UI
{
    // Esc로 여는 일시 정지 / 설정 메뉴. 게임 시작 시 한 번 만들어져 모든 씬에서 쓰인다.
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance._root != null && Instance._root.activeSelf;

        static readonly Color Ink = new(0.20f, 0.12f, 0.08f);
        static readonly Color Light = new(0.96f, 0.91f, 0.80f);

        GameObject _root;
        TMP_Text _title;
        Slider _sfx;
        Slider _music;
        TMP_Text _fullscreenLabel;
        GameObject _toTitle;
        RectTransform _quit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("PauseMenu");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<PauseMenu>();
            Instance.Build(Resources.Load<UiSkin>("UiSkin"));
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (IsOpen) Close();
                else Open();
            }
        }

        public void Open()
        {
            bool inTitle = SceneManager.GetActiveScene().name == SceneNames.Title;
            _title.text = inTitle ? "설정" : "일시 정지";
            _toTitle.SetActive(!inTitle);
            PlaceRow(_quit, inTitle ? 0.26f : 0.14f);   // 타이틀에서는 '타이틀로' 자리를 채운다
            _sfx.SetValueWithoutNotify(GameSettings.SfxVolume);
            _music.SetValueWithoutNotify(GameSettings.MusicVolume);
            UpdateFullscreenLabel();
            _root.SetActive(true);
            Time.timeScale = 0f;
            Sound.Play(SoundId.UiClick);
        }

        public void Close()
        {
            _root.SetActive(false);
            Time.timeScale = 1f;
        }

        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void UpdateFullscreenLabel() => _fullscreenLabel.text = GameSettings.Fullscreen ? "화면: 전체 화면" : "화면: 창 모드";

        // ── 화면 만들기 ───────────────────────────────

        void Build(UiSkin skin)
        {
            var canvasGo = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _root = Rect("Root", canvasGo.transform, Vector2.zero, Vector2.one).gameObject;
            var dim = _root.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.7f);   // 뒤쪽 클릭을 막는다

            var panel = Rect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            panel.sizeDelta = new Vector2(680, 760);
            Skin(panel.gameObject.AddComponent<Image>(), skin?.panel, Color.white);

            _title = Text(panel, "일시 정지", 52, Ink, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f));

            Text(panel, "효과음", 30, Ink, new Vector2(0.08f, 0.74f), new Vector2(0.35f, 0.82f)).alignment = TextAlignmentOptions.Left;
            _sfx = SliderRow(panel, 0.78f, v => GameSettings.SfxVolume = v);
            Text(panel, "배경음", 30, Ink, new Vector2(0.08f, 0.62f), new Vector2(0.35f, 0.70f)).alignment = TextAlignmentOptions.Left;
            _music = SliderRow(panel, 0.66f, v => GameSettings.MusicVolume = v);

            _fullscreenLabel = MenuButton(panel, "화면", 0.50f, skin, () => { GameSettings.Fullscreen = !GameSettings.Fullscreen; UpdateFullscreenLabel(); });
            MenuButton(panel, "계속하기", 0.38f, skin, Close);
            _toTitle = MenuButton(panel, "타이틀로", 0.26f, skin, () =>
            {
                Close();
                GameSession.Run = null;   // 진행 중인 런은 마지막 자동 저장 시점부터 이어할 수 있다
                SceneManager.LoadScene(SceneNames.Title);
            }).transform.parent.gameObject;
            _quit = (RectTransform)MenuButton(panel, "게임 종료", 0.14f, skin, QuitGame).transform.parent;

            _root.SetActive(false);
        }

        static RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static void Skin(Image image, Sprite sprite, Color color)
        {
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Tiled : Image.Type.Simple;
            image.color = sprite != null ? color : new Color(0.91f, 0.84f, 0.66f);
        }

        static TMP_Text Text(Transform parent, string text, float size, Color color, Vector2 aMin, Vector2 aMax)
        {
            var t = Rect("Text", parent, aMin, aMax).gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        static Slider SliderRow(Transform panel, float centerY, System.Action<float> onChange)
        {
            var slider = DefaultControls.CreateSlider(new DefaultControls.Resources()).GetComponent<Slider>();
            var rt = (RectTransform)slider.transform;
            rt.SetParent(panel, false);
            rt.anchorMin = new Vector2(0.38f, centerY - 0.025f);
            rt.anchorMax = new Vector2(0.92f, centerY + 0.025f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            slider.transform.Find("Background").GetComponent<Image>().color = new Color(0.17f, 0.11f, 0.08f);
            slider.fillRect.GetComponent<Image>().color = new Color(0.85f, 0.62f, 0.25f);
            slider.handleRect.GetComponent<Image>().color = new Color(0.45f, 0.28f, 0.16f);
            ((RectTransform)slider.handleRect).sizeDelta = new Vector2(28, 0);
            slider.onValueChanged.AddListener(v => onChange(v));
            return slider;
        }

        static void PlaceRow(RectTransform rt, float centerY)
        {
            rt.anchorMin = new Vector2(0.18f, centerY - 0.05f);
            rt.anchorMax = new Vector2(0.82f, centerY + 0.05f);
        }

        static TMP_Text MenuButton(Transform panel, string label, float centerY, UiSkin skin, UnityEngine.Events.UnityAction onClick)
        {
            var rt = Rect(label, panel, Vector2.zero, Vector2.zero);
            PlaceRow(rt, centerY);
            var image = rt.gameObject.AddComponent<Image>();
            Skin(image, skin?.button, Color.white);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            return Text(rt, label, 32, skin?.button != null ? Light : Ink, Vector2.zero, Vector2.one);
        }
    }
}
