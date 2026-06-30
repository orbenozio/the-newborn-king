using System.Linq;
using NUnit.Framework;
using UnityEditor.Compilation;

namespace Crossroads.Game.NewbornKing.Tests
{
    // Per-project architecture invariant (M7): this game depends on the engine + UI, and the engine
    // and UI never depend back on the game. The engine package enforces its own half; this guards the
    // game half now that the game lives in its own project/repo.
    public sealed class GameArchitectureTests
    {
        [Test]
        public void Game_DependsOn_EngineAndUI_OneDirectional()
        {
            var asms = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            var game = asms.FirstOrDefault(a => a.name == "Crossroads.Game.NewbornKing");
            Assert.IsNotNull(game, "Crossroads.Game.NewbornKing assembly not found");

            var refs = game.assemblyReferences.Select(r => r.name).ToList();
            Assert.Contains("Crossroads.Engine", refs, "game must depend on Engine");
            Assert.Contains("Crossroads.UI", refs, "game must depend on UI");

            foreach (var name in new[] { "Crossroads.Engine", "Crossroads.UI" })
            {
                var layer = asms.FirstOrDefault(a => a.name == name);
                Assert.IsNotNull(layer, name + " assembly not found (engine package not resolved?)");
                Assert.IsFalse(layer.assemblyReferences.Any(r => r.name.StartsWith("Crossroads.Game")),
                    name + " must not reference any Game assembly (one-directional dependency, M7)");
            }
        }
    }
}
