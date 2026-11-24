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
        }
    }
}
