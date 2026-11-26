using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Aethvirse.Tests
{
    /// <summary>Rules Unity does not check at compile time but that break scenes and assets silently.</summary>
    public sealed class ProjectRulesTests
    {
        static readonly Regex UnityClass = new Regex(@"class\s+(\w+)\s*:\s*(MonoBehaviour|ScriptableObject)\b");

        [Test]
        public void EveryComponentAndAssetClassLivesInAFileOfTheSameName()
        {
            foreach (string path in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                string file = Path.GetFileNameWithoutExtension(path);
                string text = File.ReadAllText(path);
                foreach (Match m in UnityClass.Matches(text))
                {
                    // Partial classes (PlayerController.Attack.cs) belong to the file named after the class.
                    if (file.StartsWith(m.Groups[1].Value + ".")) continue;
                    Assert.That(m.Groups[1].Value, Is.EqualTo(file), $"{path}: {m.Groups[1].Value} must be in {m.Groups[1].Value}.cs, or Unity cannot attach / load it");
                }
            }
        }

        [Test]
        public void RuntimePrimitives_UseTheUrpMaterial()
        {
            // CreatePrimitive's default material renders magenta in a URP player build (seen in self-test screenshots).
            foreach (string file in System.IO.Directory.GetFiles("Assets/Scripts", "*.cs", System.IO.SearchOption.AllDirectories))
            {
                string text = System.IO.File.ReadAllText(file);
                if (!text.Contains("GameObject.CreatePrimitive(") || file.EndsWith("RuntimeMat.cs")) continue;
                Assert.That(text.Contains("sharedMaterial"), Is.True, file + ": use Core.RuntimeMat.Primitive or set a URP material");
            }
            Assert.That(UnityEngine.Resources.Load<UnityEngine.Material>("RuntimeLit"), Is.Not.Null, "Resources/RuntimeLit.mat");
        }
    }
}
