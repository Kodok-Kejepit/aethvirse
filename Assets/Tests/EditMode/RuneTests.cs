using System.Linq;
using Aethvirse.Combat;
using Aethvirse.Items;
using Aethvirse.Jobs;
using Aethvirse.Skills;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Aethvirse.Tests
{
    public sealed class RuneTests
    {
        static SkillRuneData Make(RuneEffect e, float v, float v2 = 0f, ElementType el = ElementType.Physical)
        {
            var r = ScriptableObject.CreateInstance<SkillRuneData>();
            r.effect = e; r.value = v; r.value2 = v2; r.element = el;
            return r;
        }

        [Test]
        public void NoRune_ChangesNothing()
        {
            Assert.That(RuneMath.DamageFactor(null, 0.1f), Is.EqualTo(1f));
            Assert.That(RuneMath.MpFactor(null), Is.EqualTo(1f));
            Assert.That(RuneMath.Element(null, ElementType.Fire), Is.EqualTo(ElementType.Fire));
        }

        [Test]
        public void Overcharge_MoreDamageMoreMp_Thrift_LessMp()
        {
            var o = Make(RuneEffect.Overcharge, 0.4f, 0.5f);
            Assert.That(RuneMath.DamageFactor(o, 1f), Is.EqualTo(1.4f).Within(1e-4));
            Assert.That(RuneMath.MpFactor(o), Is.EqualTo(1.5f).Within(1e-4));
            Assert.That(RuneMath.MpFactor(Make(RuneEffect.Thrift, 0.3f)), Is.EqualTo(0.7f).Within(1e-4));
        }

        [Test]
        public void Executioner_OnlyBelowThreshold()
        {
            var r = Make(RuneEffect.Executioner, 0.7f);
            Assert.That(RuneMath.DamageFactor(r, 0.5f), Is.EqualTo(1f));
            Assert.That(RuneMath.DamageFactor(r, 0.2f), Is.EqualTo(1.7f).Within(1e-4));
        }

        [Test]
        public void Infuse_Split_Echo()
        {
            Assert.That(RuneMath.Element(Make(RuneEffect.Infuse, 0f, 0f, ElementType.Void), ElementType.Fire), Is.EqualTo(ElementType.Void));
            Assert.That(RuneMath.ExtraProjectiles(Make(RuneEffect.Split, 4f)), Is.EqualTo(4));
            Assert.That(RuneMath.EchoShare(Make(RuneEffect.Echo, 0.4f)), Is.EqualTo(0.4f));
            Assert.That(RuneMath.EchoShare(Make(RuneEffect.Split, 4f)), Is.EqualTo(0f));
        }

        [Test]
        public void SecretSkills_HaveBooks_RunesHaveItems_AndBooksAreHiddenInSecrets()
        {
            var db = AssetDatabase.LoadAssetAtPath<SecretSkillDatabase>("Assets/Resources/SecretSkillDatabase.asset");
            var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Data/Items/ItemDatabase.asset");
            var secrets = AssetDatabase.LoadAssetAtPath<Secrets.SecretDatabase>("Assets/Resources/SecretDatabase.asset");
            Assert.That(db.skills.Count, Is.GreaterThanOrEqualTo(24), "§8: at least 12 secret skills (we keep 24)");
            Assert.That(db.runes.Count, Is.GreaterThanOrEqualTo(25));
            foreach (SkillData s in db.skills)
            {
                Assert.That(items.Get("book_" + s.id), Is.Not.Null, s.id);
                Assert.That(secrets.secrets.Any(x => x.rewards.Any(r => r.key == "book_" + s.id)), Is.True, s.id + " must be hidden in a secret");
            }
            foreach (SkillRuneData r in db.runes) Assert.That(items.Get(r.id)?.runeId, Is.EqualTo(r.id));
            Assert.That(db.skills.Select(s => s.id).Distinct().Count(), Is.EqualTo(db.skills.Count));
        }

        [Test]
        public void EveryJobSkill_HasAHandWrittenName()
        {
            var reg = AssetDatabase.LoadAssetAtPath<JobRegistry>("Assets/Resources/JobRegistry.asset");
            foreach (JobData j in reg.jobs)
                foreach (SkillData s in j.skills)
                {
                    Assert.That(s.description, Does.Not.EndWith("technique."), s.id + ": generated text left");
                    Assert.That(s.description.Length, Is.GreaterThan(15), s.id);
                }
        }
    }
}
