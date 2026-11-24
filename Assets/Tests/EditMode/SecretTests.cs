using System.Collections.Generic;
using System.Linq;
using Aethvirse.Save;
using Aethvirse.Secrets;
using Aethvirse.World;
using NUnit.Framework;
using UnityEditor;

namespace Aethvirse.Tests
{
    public sealed class SecretTests
    {
        [Test]
        public void EverySecret_HasTwoHintsAWayToBeFoundAndAReward()
        {
            var db = AssetDatabase.LoadAssetAtPath<SecretDatabase>("Assets/Resources/SecretDatabase.asset");
            Assert.That(db, Is.Not.Null);
            foreach (SecretData s in db.secrets)
            {
                Assert.That(s.hints.Count(h => !string.IsNullOrWhiteSpace(h)), Is.GreaterThanOrEqualTo(2), s.id + ": §9.5 fair — at least two hints");
                Assert.That(s.rewards.Count, Is.GreaterThan(0), s.id + ": §9.5 always a reward");
                Assert.That(s.conditions.Count > 0 || !string.IsNullOrEmpty(s.foundByFlag), Is.True, s.id + ": needs a way to be found");
                Assert.That(s.title.ToLowerInvariant(), Does.Not.Contain("seal"), s.id + ": 'seal' is an internal word, never shown");
            }
            Assert.That(db.secrets.Select(s => s.id).Distinct().Count(), Is.EqualTo(db.secrets.Count));
        }

        [Test]
        public void EveryVisibleRegion_HasAPageWithAtLeastFiveSecrets_AndTheRoadWaitsForIt()
        {
            var db = AssetDatabase.LoadAssetAtPath<SecretDatabase>("Assets/Resources/SecretDatabase.asset");
            var atlas = AssetDatabase.LoadAssetAtPath<WorldAtlas>("Assets/Resources/WorldAtlas.asset");
            List<RegionData> regions = atlas.AllRegions().Where(r => !r.hidden).ToList();
            for (int i = 0; i < regions.Count; i++)
            {
                Assert.That(db.InRegion(regions[i].id).Count(), Is.GreaterThanOrEqualTo(5), regions[i].id + ": §7 at least 5 secrets per region");
                Assert.That(db.PageFor(regions[i].id), Is.Not.Null);
                if (i > 0) Assert.That(regions[i].requiredFlag, Is.EqualTo(SecretDatabase.PageFullFlag(regions[i - 1].id)));
            }
        }

        [Test]
        public void Puzzle_StepsResetOnAWrongPiece()
        {
            var order = new List<int> { 0, 2, 1 };
            Assert.That(PuzzleSequence.Step(order, 0, 0), Is.EqualTo(1));
            Assert.That(PuzzleSequence.Step(order, 1, 1), Is.EqualTo(0), "wrong piece resets");
            Assert.That(PuzzleSequence.Step(order, 1, 0), Is.EqualTo(1), "a wrong piece that is the first one starts over at step 1");
            Assert.That(PuzzleSequence.Step(order, 1, 2), Is.EqualTo(2));
            Assert.That(PuzzleSequence.Step(order, 2, 1), Is.EqualTo(-1), "solved");
        }

        [Test]
        public void State_FindsOnceAndSurvivesASave()
        {
            var st = new SecretState();
            Assert.That(st.MarkFound("r1_ash_room"), Is.True);
            Assert.That(st.MarkFound("r1_ash_room"), Is.False);
            st.SeeHint("r1_small_gate", 1);
            var data = new SaveData();
            data.world.secrets = st;
            SaveData back = SaveData.FromJson(data.ToJson());
            Assert.That(back.world.secrets.IsFound("r1_ash_room"), Is.True);
            Assert.That(back.world.secrets.HintSeen("r1_small_gate", 1), Is.True);
            Assert.That(back.world.secrets.HintSeen("r1_small_gate", 0), Is.False);
        }
    }
}
