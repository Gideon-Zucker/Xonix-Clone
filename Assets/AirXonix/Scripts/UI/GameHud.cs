using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AirXonix
{
    /// <summary>Screen-space HUD, menus and on-screen D-pad, all built in code.</summary>
    public class GameHud : MonoBehaviour
    {
        GameManager _gm;
        Canvas _canvas;
        Text _stats, _msg, _overText;
        GameObject _menu, _pause, _over, _dpad, _pauseBtn;
        Coroutine _flash;

        public void Init(GameManager gm)
        {
            _gm = gm;
            EnsureEventSystem();

            var cgo = new GameObject("HUD Canvas");
            cgo.transform.SetParent(transform, false);
            _canvas = cgo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var sc = cgo.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1280, 720);
            sc.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();

            _stats = Mk.Label(_canvas.transform, "Stats", 24, TextAnchor.UpperLeft);
            Place(_stats.rectTransform, new Vector2(0, 1), new Vector2(24, -16), new Vector2(1180, 60));

            _msg = Mk.Label(_canvas.transform, "Message", 58, TextAnchor.MiddleCenter);
            Place(_msg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1200, 140));
            _msg.gameObject.SetActive(false);

            _pauseBtn = Mk.Button(_canvas.transform, "| |", 24, _gm.TogglePause);
            Place((RectTransform)_pauseBtn.transform, new Vector2(1, 1), new Vector2(-20, -16), new Vector2(72, 62));

            BuildDpad();
            BuildMenu();
            BuildPause();
            BuildOver();
            HideAll();
        }

        // --------------------------------------------------------- public API

        public void ShowMenu()
        {
            HideAll();
            _menu.SetActive(true);
        }

        public void ShowPlaying()
        {
            HideAll();
            _dpad.SetActive(true);
            _pauseBtn.SetActive(true);
            _stats.gameObject.SetActive(true);
        }

        public void ShowPause()
        {
            _pause.SetActive(true);
            _dpad.SetActive(false);
            _pauseBtn.SetActive(false);
        }

        public void ShowGameOver(int score)
        {
            HideAll();
            _stats.gameObject.SetActive(true);
            _over.SetActive(true);
            _overText.text = "GAME OVER\n\nFinal score   " + score;
        }

        public void SetStats(int lvl, int score, int lives, float frac, float target)
        {
            _stats.text =
                $"LEVEL {lvl}     SCORE {score}     LIVES {Mathf.Max(0, lives)}     " +
                $"CLAIMED {Mathf.RoundToInt(frac * 100f)}%  /  {Mathf.RoundToInt(target * 100f)}%";
        }

        public void FlashMessage(string text, float seconds)
        {
            if (_flash != null) StopCoroutine(_flash);
            _flash = StartCoroutine(FlashRoutine(text, seconds));
        }

        // ------------------------------------------------------------- building

        void HideAll()
        {
            _menu.SetActive(false);
            _pause.SetActive(false);
            _over.SetActive(false);
            _dpad.SetActive(false);
            _pauseBtn.SetActive(false);
            _stats.gameObject.SetActive(false);
            _msg.gameObject.SetActive(false);
        }

        IEnumerator FlashRoutine(string text, float seconds)
        {
            _msg.text = text;
            _msg.gameObject.SetActive(true);
            yield return new WaitForSeconds(seconds);
            _msg.gameObject.SetActive(false);
            _flash = null;
        }

        void BuildDpad()
        {
            _dpad = new GameObject("Dpad", typeof(RectTransform));
            _dpad.transform.SetParent(_canvas.transform, false);
            var rt = (RectTransform)_dpad.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-28, 28);
            rt.sizeDelta = new Vector2(250, 250);

            AddPadButton("^", new Vector2(125, 188), Vector2Int.up);
            AddPadButton("v", new Vector2(125, 60), Vector2Int.down);
            AddPadButton("<", new Vector2(58, 124), Vector2Int.left);
            AddPadButton(">", new Vector2(192, 124), Vector2Int.right);
        }

        void AddPadButton(string label, Vector2 center, Vector2Int dir)
        {
            var b = Mk.Button(_dpad.transform, label, 36, () => _gm.OnDirectionInput(dir));
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center;
            rt.sizeDelta = new Vector2(66, 66);
        }

        void BuildMenu()
        {
            _menu = Mk.Panel(_canvas.transform, 0.93f);

            var title = Mk.Label(_menu.transform, "Title", 84, TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 210), new Vector2(1200, 120));
            title.text = "A I R   X O N I X";

            var info = Mk.Label(_menu.transform, "Info", 24, TextAnchor.UpperCenter);
            Place(info.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(940, 300));
            info.text =
                "Swipe anywhere, or use the on-screen D-pad, to steer.\n\n" +
                "Leave the safe land to cut through the water. Get back to land to\n" +
                "claim every area that has no ball trapped in it.\n\n" +
                "Claim 75% of the water to clear the level.\n" +
                "A ball touching you or your unfinished trail costs a life.";

            var play = Mk.Button(_menu.transform, "PLAY", 34, _gm.StartGame);
            Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -210), new Vector2(300, 86));
        }

        void BuildPause()
        {
            _pause = Mk.Panel(_canvas.transform, 0.82f);

            var t = Mk.Label(_pause.transform, "T", 72, TextAnchor.MiddleCenter);
            Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1000, 110));
            t.text = "PAUSED";

            var resume = Mk.Button(_pause.transform, "RESUME", 32, _gm.TogglePause);
            Place((RectTransform)resume.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(300, 82));

            var menu = Mk.Button(_pause.transform, "MAIN MENU", 28, _gm.RestartToMenu);
            Place((RectTransform)menu.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(300, 74));
        }

        void BuildOver()
        {
            _over = Mk.Panel(_canvas.transform, 0.9f);

            _overText = Mk.Label(_over.transform, "T", 60, TextAnchor.MiddleCenter);
            Place(_overText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1100, 200));
            _overText.text = "GAME OVER";

            var retry = Mk.Button(_over.transform, "RETRY", 32, _gm.StartGame);
            Place((RectTransform)retry.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(300, 82));

            var menu = Mk.Button(_over.transform, "MAIN MENU", 28, _gm.RestartToMenu);
            Place((RectTransform)menu.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -130), new Vector2(300, 74));
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }

    /// <summary>Minimal uGUI factory helpers.</summary>
    static class Mk
    {
        public static Text Label(Transform parent, string name, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Util.UIFont();
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static GameObject Button(Transform parent, string label, int size, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn " + label, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.16f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var t = Label(go.transform, "Text", size, TextAnchor.MiddleCenter);
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return go;
        }

        public static GameObject Panel(Transform parent, float alpha)
        {
            var go = new GameObject("Panel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.02f, 0.04f, 0.10f, alpha);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }
    }
}
