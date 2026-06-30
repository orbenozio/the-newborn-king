using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using Crossroads.Engine;
using Crossroads.UI;

namespace Crossroads.Game.NewbornKing.Tests
{
    // Validation of the real NewbornKing content (M9 on real data, not a synthetic unit test).
    // Loads story.json + resources.asset from disk and runs them through the same StoryValidator
    // the game uses at load time. Paths are local to this game project.
    public sealed class NewbornKingContentTests
    {
        private const string StoryPath = "Assets/Game/Content/story.json";
        private const string ResPath = "Assets/Game/Content/resources.asset";

        [Test]
        public void NewbornKing_Story_Validates_NoErrors()
        {
            var storyAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(StoryPath);
            var resources = AssetDatabase.LoadAssetAtPath<ResourceSet>(ResPath);
            Assert.IsNotNull(storyAsset, "NewbornKing story.json missing at " + StoryPath);
            Assert.IsNotNull(resources, "NewbornKing resources.asset missing at " + ResPath);

            var story = StoryLoader.Parse(storyAsset.text);
            var issues = StoryValidator.Validate(story, resources);

            var errors = issues.Where(i => i.Severity == IssueSeverity.Error).ToList();
            var sb = new StringBuilder();
            foreach (var e in errors) sb.AppendLine(e.ToString());
            Assert.IsEmpty(errors, "NewbornKing content has validation errors:\n" + sb);
        }

        [Test]
        public void NewbornKing_Has_FourMeters_And_StartNode()
        {
            var resources = AssetDatabase.LoadAssetAtPath<ResourceSet>(ResPath);
            var storyAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(StoryPath);
            Assert.IsNotNull(resources);
            Assert.AreEqual(4, resources.resources.Length, "the clone defines 4 meters (sleep/sanity/money/baby)");

            var story = StoryLoader.Parse(storyAsset.text);
            Assert.IsTrue(story.Nodes.Any(n => n.Id == story.StartNodeId), "startNodeId must resolve to a real node");
            Assert.GreaterOrEqual(story.Nodes.Count, 12, "a real clone has a meaningful deck, not a stub");
        }

        [Test]
        public void NewbornKing_Has_ReignLength_And_BranchingEndings()
        {
            var storyAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(StoryPath);
            var story = StoryLoader.Parse(storyAsset.text);

            Assert.Greater(story.MaxTurns, 0, "NewbornKing defines a reign length - the heir comes of age (win condition)");
            Assert.IsTrue(story.Endings.Exists(e => !string.IsNullOrEmpty(e.Flag)), "branching survival endings (by flag) exist");
            Assert.IsTrue(story.Endings.Exists(e => e.Fallback), "a fallback survival ending exists");
        }
    }
}
