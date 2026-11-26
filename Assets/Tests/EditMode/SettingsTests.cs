using Aethvirse.Core;
using NUnit.Framework;
using UnityEngine;

namespace Aethvirse.Tests
{
    public sealed class SettingsTests
    {
        string _lang;

        [SetUp] public void SetUp() => _lang = GameSettings.Language;
        [TearDown] public void TearDown() => GameSettings.Language = _lang;

        [Test]
        public void Loc_SwitchesLanguageAndFallsBack()
        {
            GameSettings.Language = "id";
            Assert.That(Loc.T("resume"), Is.EqualTo("Lanjutkan"));
            GameSettings.Language = "en";
            Assert.That(Loc.T("resume"), Is.EqualTo("Resume"));
            Assert.That(Loc.T("no_such_key"), Is.EqualTo("no_such_key"));
            GameSettings.Language = "id";
            Assert.That(Loc.T("journal"), Is.EqualTo("Jurnal Misteri"));
            Assert.That(Loc.T("fast_travel"), Is.EqualTo("Perjalanan Cepat"));
        }

        [Test]
        public void Difficulty_ScalesMonstersAndRewards()
        {
            var easy = GameSettings.DifficultyScale(0);
            var normal = GameSettings.DifficultyScale(1);
            var nightmare = GameSettings.DifficultyScale(3);
            Assert.That(normal, Is.EqualTo((1f, 1f, 1f)));
            Assert.That(easy.hp, Is.LessThan(1f));
            Assert.That(nightmare.atk, Is.GreaterThan(1.5f));
            Assert.That(nightmare.xp, Is.GreaterThan(normal.xp), "harder pays more");
        }
    }
}
