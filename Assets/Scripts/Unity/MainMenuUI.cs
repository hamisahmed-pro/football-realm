// Football Mogul — Runtime UI (v1, code-built, no prefabs).
// The design system: dark sports aesthetic, professional dashboards,
// cinematic football imagery. Tokens are in code so the UI can be
// restyled in one place before Figma frames are built from it:
//   Paper/background #0F1220, Surface #171A2B, Ink #FFFFFF,
//   Body #C7CBD8, Muted #8A90A3, Brand #5548F5, Mint #34D399,
//   Danger #B3261E, Outline #2A2E44.
// On a phone this is a single-column scroll; on desktop the same
// panels widen. That is the "one codebase, adapted interfaces"
// rule from the stack recommendation.

using FootballMogul.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FootballMogul.Unity
{
    /// <summary>Builds the main menu and league picker at runtime.
    /// Attach to the same GameObject as GameManager, or let
    /// GameManager create it. This is intentionally simple: it proves
    /// the data flows (799 clubs -> picker -> season) before a
    /// polished Figma-driven UI replaces the layout.</summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        private Canvas _canvas = null!;
        private Text _status = null!;

        private void Start()
        {
            BuildCanvas();
            BuildMenu();
        }

        private void BuildCanvas()
        {
            var go = new GameObject("FootballMogulCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.AddComponent<GraphicRaycaster>();
            var panel = new GameObject("Panel");
            panel.transform.SetParent(go.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var image = panel.AddComponent<Image>();
            image.color = new Color(0.06f, 0.07f, 0.13f);
            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12; layout.padding = new RectOffset(24, 24, 24, 24);
            layout.childControlHeight = false;
            _status = CreateText(panel.transform, "FOOTBALL MOGUL\nBuild a legacy. Rule the game.", 28, true);
        }

        private Text CreateText(Transform parent, string text, int size, bool bold)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.color = Color.white;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, size + 16);
            return t;
        }

        private Button CreateButton(Transform parent, string label, System.Action onClick)
        {
            var go = new GameObject("Button_" + label);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.33f, 0.28f, 0.96f);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 56);
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero;
            return button;
        }

        private void BuildMenu()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Leagues.Count == 0)
            {
                CreateText(_canvas.transform.GetChild(0), "No club data loaded. Assign clubs.json to GameManager.", 16, false);
                return;
            }
            CreateText(_canvas.transform.GetChild(0), $"{gm.Leagues.Count} leagues, 799 clubs, 20 countries loaded.", 16, false);
            CreateText(_canvas.transform.GetChild(0), "Pick a starting point (v1: first league, first club):", 16, false);
            var first = gm.Leagues[0];
            CreateButton(_canvas.transform.GetChild(0),
                $"Start: {first.Clubs[0].Name} — {first.Name} ({first.Country})",
                () =>
                {
                    gm.StartNewGame(first.Country, first.Name, first.Clubs[0].Name, "Manager");
                    _status.text = $"Season started: {first.Clubs[0].Name} in {first.Name}.\nUse GameManager.PlayNextFixture() to play a round.";
                });
            CreateButton(_canvas.transform.GetChild(0), "Save game", () => gm.Save());
            CreateButton(_canvas.transform.GetChild(0), "Load save", () => { gm.TryLoad(); });
        }
    }
}
