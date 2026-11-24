using System.Collections.Generic;
using System.Linq;
using Aethvirse.Jobs;
using Aethvirse.Skills;
using NUnit.Framework;
using UnityEditor;

namespace Aethvirse.Tests
{
    public sealed class JobTests
    {
        JobRegistry _reg;

        [SetUp]
        public void SetUp()
        {
            _reg = AssetDatabase.LoadAssetAtPath<JobRegistry>("Assets/Resources/JobRegistry.asset");
            Assert.That(_reg, Is.Not.Null);
        }

        static bool NoFlags(string f) => false;

        [Test]
        public void JobChangeOne_AtThirty_TwoBranchesPlusSummoner()
        {
            Assert.That(_reg.Options("vanguard", "", 29, NoFlags).Count, Is.EqualTo(0));
            List<string> ids = _reg.Options("vanguard", "", 30, NoFlags).Select(j => j.id).ToList();
            Assert.That(ids, Is.EquivalentTo(new[] { "guardian", "berserker", "summoner" }));
            Assert.That(_reg.Options("arcanist", "", 30, NoFlags).Select(j => j.id), Is.EquivalentTo(new[] { "elementalist", "sage", "summoner" }));
        }

        [Test]
        public void TierTwo_FollowsItsParentAtSixty()
        {
            Assert.That(_reg.Options("vanguard", "guardian", 59, NoFlags).Count, Is.EqualTo(0));
            Assert.That(_reg.Options("vanguard", "guardian", 60, NoFlags).Select(j => j.id), Is.EquivalentTo(new[] { "bastion" }));
        }

        [Test]
        public void HiddenJobs_NeedTheirFlags()
        {
            Assert.That(_reg.Options("phantom", "summoner", 60, NoFlags).Select(j => j.id), Does.Not.Contain("necromancer"));
            Assert.That(_reg.Options("phantom", "summoner", 60, f => f == "sealed_necromancer").Select(j => j.id), Does.Contain("necromancer"));
            Assert.That(_reg.Lineage("shadow_sovereign").Select(j => j.id), Is.EqualTo(new[] { "summoner", "necromancer", "shadow_sovereign" }));
        }

        [Test]
        public void EveryJob_HasTenSkillsAndDistinctLooks()
        {
            Assert.That(_reg.jobs.Count, Is.GreaterThanOrEqualTo(20));
            foreach (JobData j in _reg.jobs)
            {
                Assert.That(j.skills.Count, Is.GreaterThanOrEqualTo(10), j.id + ": master prompt §8, at least 10 skills per job");
                var actives = j.skills.Where(s => !s.IsPassive).ToList();
                Assert.That(actives.Select(s => s.fx).Distinct().Count(), Is.EqualTo(actives.Count), j.id);
            }
            Assert.That(_reg.Get("necromancer").skills.Any(s => s.kind == SkillKind.ShadowExtract), Is.True);
            Assert.That(_reg.Get("summoner").skills.Any(s => s.kind == SkillKind.Summon), Is.True);
        }

        [Test]
        public void Shadows_RankUpWithXp()
        {
            var army = new ShadowArmy();
            ShadowArmy.Soldier s = army.Bind("ash_hound", 5);
            Assert.That(ShadowArmy.GainXp(s, 19), Is.False);
            Assert.That(ShadowArmy.GainXp(s, 1), Is.True);
            Assert.That(ShadowArmy.RankNames[s.rank], Is.EqualTo("Knight"));
            ShadowArmy.GainXp(s, 1000);
            Assert.That(ShadowArmy.RankNames[s.rank], Is.EqualTo("Marshal"));
            Assert.That(army.Get(s.id), Is.SameAs(s));
        }
    }
}
