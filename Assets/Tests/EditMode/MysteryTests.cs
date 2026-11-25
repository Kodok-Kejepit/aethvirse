using System.Linq;
using Aethvirse.Quests;
using Aethvirse.Secrets;
using NUnit.Framework;
using UnityEditor;

namespace Aethvirse.Tests
{
    public sealed class MysteryTests
    {
        [TestCase("ASH", CipherKind.Caesar, 3, "DVK")]
        [TestCase("BLOOM", CipherKind.Atbash, 0, "YOLLN")]
        [TestCase("SAND", CipherKind.Reverse, 0, "DNAS")]
        [TestCase("FROST", CipherKind.Numbers, 0, "6-18-15-19-20")]
        [TestCase("INK", CipherKind.Morse, 0, "..  -.  -.-")]
        public void Encode_KnownAnswers(string plain, CipherKind kind, int key, string coded) =>
            Assert.That(Cipher.Encode(plain, kind, key), Is.EqualTo(coded));

        [Test]
        public void EveryRegionLock_DecodesBackToItsAnswer_AndHasAMuralAndAReward()
        {
            var items = AssetDatabase.LoadAssetAtPath<Items.ItemDatabase>("Assets/Data/Items/ItemDatabase.asset");
            var atlas = AssetDatabase.LoadAssetAtPath<World.WorldAtlas>("Assets/Resources/WorldAtlas.asset");
            int locks = 0;
            foreach (World.RegionData r in atlas.AllRegions())
            {
                if (r.hidden || string.IsNullOrEmpty(r.scene)) continue;
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene($"Assets/Scenes/Regions/{r.scene}.unity");
                CipherLock lk = UnityEngine.Object.FindFirstObjectByType<CipherLock>();
                Assert.That(lk, Is.Not.Null, r.id + ": a cipher lock");
                var so = new SerializedObject(lk);
                string answer = so.FindProperty("answer").stringValue, coded = so.FindProperty("inscription").stringValue;
                Assert.That(coded, Is.Not.EqualTo(answer), r.id + ": the inscription must not show the answer");
                bool decodes = System.Enum.GetValues(typeof(CipherKind)).Cast<CipherKind>()
                    .Any(k => Enumerable.Range(0, 26).Any(key => Cipher.Decode(coded, k, key) == answer));
                Assert.That(decodes, Is.True, r.id + ": the inscription decodes to the answer");
                Assert.That(items.Get(so.FindProperty("rewardItem").stringValue), Is.Not.Null, r.id);
                Assert.That(UnityEngine.Object.FindObjectsByType<Interaction.NoticeBoard>(UnityEngine.FindObjectsSortMode.None)
                    .Any(n => n.gameObject.name == "Mural"), Is.True, r.id + ": a mural with the key");
                locks++;
            }
            Assert.That(locks, Is.EqualTo(14));
        }

        [Test]
        public void SealedMissions_AreHiddenChains_WithValidTargets()
        {
            var db = AssetDatabase.LoadAssetAtPath<QuestDatabase>("Assets/Resources/QuestDatabase.asset");
            var secrets = AssetDatabase.LoadAssetAtPath<SecretDatabase>("Assets/Resources/SecretDatabase.asset");
            var items = AssetDatabase.LoadAssetAtPath<Items.ItemDatabase>("Assets/Data/Items/ItemDatabase.asset");
            var sealedQuests = db.quests.Where(q => q.kind == QuestKind.Sealed).ToList();
            Assert.That(sealedQuests.Count, Is.GreaterThanOrEqualTo(14));
            foreach (QuestData q in sealedQuests)
            {
                Assert.That(q.autoStart, Is.True, q.id);
                Assert.That(q.startConditions.Count, Is.GreaterThan(0), q.id + ": never announced, so it needs a hidden trigger");
                Assert.That(q.StageCount, Is.GreaterThanOrEqualTo(2), q.id + ": a chain");
                foreach (QuestObjective o in q.objectives)
                    if (o.type == ObjectiveType.ReachArea && o.target.StartsWith("secret_"))
                        Assert.That(secrets.Get(o.target.Substring(7)), Is.Not.Null, q.id + " → " + o.target);
                foreach (Reward r in q.rewards)
                    if (r.type == RewardType.Item) Assert.That(items.Get(r.key), Is.Not.Null, q.id + " → " + r.key);
                foreach (Condition c in q.startConditions)
                    if (c.type == ConditionType.Flag && c.key.StartsWith("secret_"))
                        Assert.That(secrets.Get(c.key.Substring(7)), Is.Not.Null, q.id + " start → " + c.key);
            }
        }
    }
}
