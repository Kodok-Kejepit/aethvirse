using Aethvirse.Combat;
using Aethvirse.Interaction;
using Aethvirse.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Aethvirse.Tests
{
    public sealed class AerialDuelTests
    {
        [TestCase("Assets/Data/Combat/Greatsword/GreatswordCombo.asset")]
        [TestCase("Assets/Data/Combat/Daggers/DaggersCombo.asset")]
        [TestCase("Assets/Data/Combat/Staff/StaffCombo.asset")]
        [TestCase("Assets/Data/Combat/Bow/BowCombo.asset")]
        public void EveryWeapon_HasLauncherAirChainAndPlunge(string path)
        {
            var combo = AssetDatabase.LoadAssetAtPath<ComboData>(path);
            Assert.That(combo.launcher, Is.Not.Null);
            Assert.That(combo.launcher.launch, Is.GreaterThan(5f));
            AttackData a1 = combo.ResolveAir(null, false);
            Assert.That(a1, Is.Not.Null);
            Assert.That(a1.hover, Is.True);
            AttackData a2 = combo.ResolveAir(a1, false);
            AttackData a3 = combo.ResolveAir(a2, false);
            Assert.That(a3, Is.Not.Null);
            Assert.That(combo.ResolveAir(a3, false), Is.Null, "the air chain ends after three");
            AttackData plunge = combo.ResolveAir(a2, true);
            Assert.That(plunge.slam, Is.True);
            Assert.That(plunge.launch, Is.LessThan(0f), "spikes juggled targets down");
        }

        [Test]
        public void DuelBoard_GoesInOrder_ThenTheEcho()
        {
            var f = new WorldFlags();
            Assert.That(DuelRing.NextRival(f), Is.EqualTo(0));
            f.SetFlag(DuelRing.WonFlag("rival_kaelen"));
            Assert.That(DuelRing.NextRival(f), Is.EqualTo(1));
            f.SetFlag(DuelRing.WonFlag("rival_ilyra"));
            f.SetFlag(DuelRing.WonFlag("rival_torvald"));
            Assert.That(DuelRing.Rivals[DuelRing.NextRival(f)].id, Is.EqualTo("rival_echo"));
            f.SetFlag(DuelRing.WonFlag("rival_echo"));
            Assert.That(DuelRing.NextRival(f), Is.EqualTo(DuelRing.Rivals.Length - 1), "rematch the echo");
        }

        [Test]
        public void EveryRival_ExistsInTheCatalog_AndItsRewardIsAnItem()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<Enemies.EnemyCatalog>("Assets/Resources/EnemyCatalog.asset");
            var items = AssetDatabase.LoadAssetAtPath<Items.ItemDatabase>("Assets/Data/Items/ItemDatabase.asset");
            foreach (DuelRing.Rival r in DuelRing.Rivals)
            {
                Assert.That(catalog.Get(r.id), Is.Not.Null, r.id);
                Assert.That(items.Get(r.reward), Is.Not.Null, r.reward);
            }
        }
    }
}
