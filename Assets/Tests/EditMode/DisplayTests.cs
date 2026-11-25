using Aethvirse.Core;
using NUnit.Framework;

namespace Aethvirse.Tests
{
    public sealed class DisplayTests
    {
        [TestCase(860, 836, 1600, 900, true, true)]   // Hyprland tile size stretched over the screen (the owner's bug)
        [TestCase(1600, 900, 1600, 900, true, false)]
        [TestCase(1280, 720, 1600, 900, false, false)] // windowed: any size is fine
        public void NeedsFix_OnlyWhenFullscreenIsNotTheDisplaySize(int sw, int sh, int dw, int dh, bool full, bool fix) =>
            Assert.That(DisplayGuard.NeedsFix(sw, sh, dw, dh, full), Is.EqualTo(fix));
    }
}
