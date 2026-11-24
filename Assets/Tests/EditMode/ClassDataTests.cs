using System.Linq;
using Aethvirse.Skills;
using Aethvirse.Stats;
using NUnit.Framework;
using UnityEditor;

namespace Aethvirse.Tests
{
    /// <summary>The four class assets (ClassDataBuilder): many skills each, every active looks different, combos are valid.</summary>
    public sealed class ClassDataTests
    {
        static readonly string[] Files = { "Paladin", "Assassin", "Mage", "Marksman" };

        static ClassData Load(string file)
        {
            var c = AssetDatabase.LoadAssetAtPath<ClassData>($"Assets/Data/Classes/{file}.asset");
            Assert.That(c, Is.Not.Null, file);
            return c;
        }

        [TestCaseSource(nameof(Files))]
        public void HasFifteenSkillsWithOneUltimate(string file)
        {
            ClassData c = Load(file);
            Assert.That(c.skills.Count, Is.GreaterThanOrEqualTo(15), "master prompt §8: at least 15 skills per base class");
            Assert.That(c.skills.Count(s => s.IsPassive), Is.GreaterThanOrEqualTo(3));
            Assert.That(c.skills.Count(s => s.isUltimate), Is.EqualTo(1));
            Assert.That(c.skills.Select(s => s.id).Distinct().Count(), Is.EqualTo(c.skills.Count), "unique ids");
            Assert.That(c.combo, Is.Not.Null, "weapon combo");
            Assert.That(c.skills.Count(s => s.unlockLevel <= 1), Is.EqualTo(2), "two skills at level 1");
        }

        [TestCaseSource(nameof(Files))]
        public void EveryActiveLooksDifferentAndDamagingOnesHaveATimeline(string file)
        {
            ClassData c = Load(file);
            var actives = c.skills.Where(s => !s.IsPassive).ToList();
            foreach (SkillData s in actives)
            {
                Assert.That(s.fx, Is.Not.EqualTo(SkillFxKind.None), s.id);
                if (s.IsDamaging || s.kind == SkillKind.Trap) Assert.That(s.attack, Is.Not.Null, s.id);
            }
            Assert.That(actives.Select(s => s.fx).Distinct().Count(), Is.EqualTo(actives.Count), "owner: every skill moves and looks different");
        }

        [TestCaseSource(nameof(Files))]
        public void SkillCombosReferenceRealSkills(string file)
        {
            ClassData c = Load(file);
            Assert.That(c.skillCombos.Count, Is.GreaterThanOrEqualTo(3));
            foreach (SkillComboData combo in c.skillCombos)
            {
                Assert.That(c.FindSkill(combo.first), Is.Not.Null, combo.id);
                Assert.That(c.FindSkill(combo.second), Is.Not.Null, combo.id);
            }
        }

        [Test]
        public void RegistryListsAllClasses()
        {
            var reg = AssetDatabase.LoadAssetAtPath<ClassRegistry>("Assets/Resources/ClassRegistry.asset");
            Assert.That(reg.classes.Count, Is.EqualTo(4));
            Assert.That(reg.Get("arcanist").magicDamage, Is.True);
        }
    }
}
