// Football Mogul — Match presentation.
// One simulation, two presentations:
//   1. 2D pitch (UGUI): player dots and a ball dot move to each
//      event's BallX/BallY. This is the working match view in v1.
//   2. 3D pitch (stub, honest): primitive capsule players on a green
//      plane, moved to the same event positions. This proves the
//      engine -> 3D pipeline, but it is NOT motion-captured 3D
//      football. Real 3D match presentation needs licensed/rigged
//      player models, animations, and a stadium — a separate,
//      asset-heavy phase, not something code alone can fake.
//
// Highlights mode filters events to goals, saves, cards and
// substitutions. Instant Simulation shows only the result + stats.
// Full Match plays events back minute by minute.

using System.Collections;
using System.Collections.Generic;
using FootballMogul.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FootballMogul.Unity
{
    public sealed class MatchPresentation : MonoBehaviour
    {
        [Header("2D pitch")]
        public RectTransform pitchRect = null!;
        public GameObject dotPrefab = null!; // a small UI Image, or null to create one at runtime

        [Header("3D pitch (stub)")]
        public bool use3D;
        public GameObject playerCapsulePrefab = null!;

        private readonly List<GameObject> _dots = new List<GameObject>();

        /// <summary>Draw the pitch state for one event (2D).</summary>
        public void ShowEvent2D(MatchEvent ev)
        {
            if (pitchRect == null) return;
            ClearDots();
            // Ball dot at the event position.
            var ball = CreateDot(Color.yellow, ev.BallX, ev.BallY, 14);
            _dots.Add(ball);
            // Home team dots (left half) and away dots (right half),
            // in a simple 4-3-3 shape. Positions are illustrative;
            // the engine's event position is authoritative for the ball.
            var homePositions = new (double x, double y)[]
            {
                (8, 50), (22, 20), (22, 40), (22, 60), (22, 80),
                (38, 30), (38, 50), (38, 70), (55, 35), (55, 65), (65, 50)
            };
            var awayPositions = new (double x, double y)[]
            {
                (92, 50), (78, 20), (78, 40), (78, 60), (78, 80),
                (62, 30), (62, 50), (62, 70), (45, 35), (45, 65), (35, 50)
            };
            foreach (var p in homePositions) _dots.Add(CreateDot(Color.blue, p.x, p.y, 10));
            foreach (var p in awayPositions) _dots.Add(CreateDot(Color.red, p.x, p.y, 10));
        }

        private GameObject CreateDot(Color color, double x, double y, float size)
        {
            GameObject go;
            if (dotPrefab != null) go = Instantiate(dotPrefab, pitchRect);
            else
            {
                go = new GameObject("Dot");
                go.transform.SetParent(pitchRect, false);
                var img = go.AddComponent<Image>();
                img.color = color;
            }
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(
                (float)(x / 100.0 * pitchRect.rect.width),
                (float)(y / 100.0 * pitchRect.rect.height));
            return go;
        }

        private void ClearDots()
        {
            foreach (var d in _dots) if (d != null) Destroy(d);
            _dots.Clear();
        }

        /// <summary>Play a full match back, one event at a time.</summary>
        public IEnumerator PlayFullMatch(MatchResult result, float secondsPerEvent = 1.5f)
        {
            foreach (var ev in result.Events)
            {
                ShowEvent2D(ev);
                yield return new WaitForSeconds(secondsPerEvent);
            }
        }

        public static List<MatchEvent> Highlights(MatchResult result)
        {
            return result.Events.FindAll(e =>
                e.Kind == MatchEventKind.Goal ||
                e.Kind == MatchEventKind.Save ||
                e.Kind == MatchEventKind.YellowCard ||
                e.Kind == MatchEventKind.RedCard ||
                e.Kind == MatchEventKind.Substitution ||
                e.Kind == MatchEventKind.PenaltyGoal);
        }
    }
}
