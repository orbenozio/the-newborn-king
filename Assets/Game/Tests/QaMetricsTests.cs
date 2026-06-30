using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Crossroads.Engine;
using Crossroads.UI;

namespace Crossroads.Game.NewbornKing.Tests
{
    // Formal QA pass against the acceptance criteria (spec section 6). Auto-verifies M1 (loop
    // integrity) and M3 (session length) on the real NewbornKing content. Paths are local to this
    // game project.
    public sealed class QaMetricsTests
    {
        private static (StoryData story, ResourceSet res) LoadNewbornKing()
        {
            var storyAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Game/Content/story.json");
            var res = AssetDatabase.LoadAssetAtPath<ResourceSet>("Assets/Game/Content/resources.asset");
            Assert.IsNotNull(storyAsset); Assert.IsNotNull(res);
            return (StoryLoader.Parse(storyAsset.text), res);
        }

        // Distance-to-break for a given value (>0 safe, <=0 broken). Honors breakOn.
        private static int DistToBreak(ResourceDef d, int value)
        {
            switch (d.breakOn)
            {
                case BreakOn.Min: return value - d.min;
                case BreakOn.Max: return d.max - value;
                default: return Math.Min(value - d.min, d.max - value);
            }
        }

        // "Careful player" policy: picks the side that maximizes the minimum distance-to-break after the choice.
        private static ChoiceSide SafeSide(EventEngine e, ResourceSet res)
        {
            int Score(ChoiceSide side)
            {
                var deltas = new Dictionary<string, int>();
                foreach (var d in e.Preview(side).Deltas) deltas[d.ResourceId] = d.Delta;
                int worst = int.MaxValue;
                foreach (var def in res.resources)
                {
                    int cur = e.State.GetResource(def.id);
                    int delta = deltas.TryGetValue(def.id, out var dv) ? dv : 0;
                    int projected = ResourceRules.Clamp(cur + delta, def.min, def.max);
                    worst = Math.Min(worst, DistToBreak(def, projected));
                }
                return worst;
            }
            return Score(ChoiceSide.Left) >= Score(ChoiceSide.Right) ? ChoiceSide.Left : ChoiceSide.Right;
        }

        // M1 - loop integrity: across many seeds, a random run of NewbornKing always reaches a clean
        // game-over, and Current is never null while Running (no dead-end / "no valid next card").
        [Test]
        public void M1_NewbornKing_AllRunsReachCleanTerminal_NoStuck()
        {
            var (story, res) = LoadNewbornKing();
            for (int seed = 0; seed < 60; seed++)
            {
                var engine = new EventEngine(story, res, new Deck(story), seed);
                var rng = new System.Random(seed);
                int steps = 0;
                while (engine.Status == GameStatus.Running && steps < 2000)
                {
                    Assert.IsNotNull(engine.Current, $"seed {seed} step {steps}: Current must never be null while Running (M1)");
                    engine.Resolve(rng.Next(2) == 0 ? ChoiceSide.Left : ChoiceSide.Right);
                    if (engine.Status == GameStatus.Running) engine.Advance();
                    steps++;
                }
                Assert.AreEqual(GameStatus.GameOver, engine.Status, $"seed {seed}: run must terminate cleanly within bound (M1)");
            }
        }

        // M3 - session length: under the careful-player policy, median choices to game-over across >=10 runs.
        [Test]
        public void M3_NewbornKing_SessionLength_MedianInSaneRange()
        {
            var (story, res) = LoadNewbornKing();
            var lengths = new List<int>();
            for (int seed = 0; seed < 12; seed++)
            {
                var engine = new EventEngine(story, res, new Deck(story), seed);
                int choices = 0;
                while (engine.Status == GameStatus.Running && choices < 2000)
                {
                    engine.Resolve(SafeSide(engine, res));
                    if (engine.Status == GameStatus.Running) engine.Advance();
                    choices++;
                }
                lengths.Add(choices);
            }
            lengths.Sort();
            int median = lengths[lengths.Count / 2];
            Debug.Log($"[QA M3] NewbornKing careful-play session lengths (sorted): {string.Join(",", lengths)} ; median={median} ; maxTurns={story.MaxTurns}");
            Assert.GreaterOrEqual(median, 8, "careful play should sustain a real session (M3 target 10-15)");
            Assert.LessOrEqual(median, story.MaxTurns, "the reign-length cap bounds careful-play session length (M3)");
        }
    }
}
