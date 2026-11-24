using Aethvirse.Player;
using NUnit.Framework;
using UnityEngine;

namespace Aethvirse.Tests
{
    public sealed class MovementMathTests
    {
        const float Eps = 0.001f;
        PlayerMovementData _data;

        [SetUp] public void SetUp() => _data = ScriptableObject.CreateInstance<PlayerMovementData>();
        [TearDown] public void TearDown() => Object.DestroyImmediate(_data);

        static void AssertNear(Vector3 expected, Vector3 actual) =>
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(Eps), $"expected {expected}, got {actual}");

        [Test]
        public void CameraRelative_ForwardWithZeroYaw_IsPlusZ() =>
            AssertNear(Vector3.forward, MovementMath.CameraRelative(Vector2.up, 0f));

        [Test]
        public void CameraRelative_FollowsCameraYaw()
        {
            // Camera turned 90° to the right: "forward" now points to world +X.
            AssertNear(Vector3.right, MovementMath.CameraRelative(Vector2.up, 90f));
            AssertNear(Vector3.left, MovementMath.CameraRelative(Vector2.up, -90f));
        }

        [Test]
        public void CameraRelative_DiagonalIsClamped() =>
            Assert.That(MovementMath.CameraRelative(new Vector2(1f, 1f), 0f).magnitude, Is.EqualTo(1f).Within(Eps));

        [Test]
        public void CameraRelative_AnalogMagnitudeIsKept() =>
            Assert.That(MovementMath.CameraRelative(new Vector2(0f, 0.3f), 45f).magnitude, Is.EqualTo(0.3f).Within(Eps));

        [Test]
        public void Approach_KeepsVerticalVelocity()
        {
            Vector3 v = MovementMath.ApproachHorizontal(new Vector3(0f, -3f, 0f), new Vector3(10f, 0f, 0f), 5f, 1f);
            Assert.That(v.y, Is.EqualTo(-3f));
            Assert.That(v.x, Is.GreaterThan(0f));
            Assert.That(v.x, Is.LessThan(10f), "eases toward the target, no jump");
        }

        [Test]
        public void Approach_EasesOut()
        {
            var target = new Vector3(6f, 0f, 0f);
            Vector3 a = MovementMath.ApproachHorizontal(Vector3.zero, target, 45f, 0.05f);
            Vector3 b = MovementMath.ApproachHorizontal(a, target, 45f, 0.05f);
            Vector3 c = MovementMath.ApproachHorizontal(new Vector3(5.5f, 0f, 0f), target, 45f, 0.05f);
            Assert.That(a.x, Is.GreaterThan(c.x - 5.5f), "first step from rest is bigger than the last step near the target");
            Assert.That(b.x, Is.GreaterThan(a.x));
        }

        [Test]
        public void Approach_DoesNotOvershoot() =>
            AssertNear(new Vector3(2f, 0f, 0f), MovementMath.ApproachHorizontal(Vector3.zero, new Vector3(2f, 0f, 0f), 100f, 1f));

        [Test]
        public void GroundSpeed_WalkRunSprint()
        {
            Assert.That(MovementMath.GroundSpeed(0f, _data, false), Is.EqualTo(0f));
            Assert.That(MovementMath.GroundSpeed(0.3f, _data, false), Is.EqualTo(_data.walkSpeed));
            Assert.That(MovementMath.GroundSpeed(1f, _data, false), Is.EqualTo(_data.runSpeed));
            Assert.That(MovementMath.GroundSpeed(1f, _data, true), Is.EqualTo(_data.sprintSpeed));
        }

        [Test]
        public void JumpVelocity_ReachesHeight()
        {
            const float g = 9.81f;
            float v = MovementMath.JumpVelocity(1.4f, g);
            Assert.That(v * v / (2f * g), Is.EqualTo(1.4f).Within(Eps));
        }

        [Test]
        public void YawForDirection_MatchesUnityAxes()
        {
            Assert.That(MovementMath.YawForDirection(Vector3.forward), Is.EqualTo(0f).Within(Eps));
            Assert.That(MovementMath.YawForDirection(Vector3.right), Is.EqualTo(90f).Within(Eps));
            Assert.That(MovementMath.YawForDirection(Vector3.left), Is.EqualTo(-90f).Within(Eps));
        }

        [Test]
        public void YawForDirection_RoundTripsWithCameraRelative()
        {
            Vector3 dir = MovementMath.CameraRelative(Vector2.up, 37f);
            Assert.That(MovementMath.YawForDirection(dir), Is.EqualTo(37f).Within(Eps));
        }

        [Test]
        public void DodgeSpeed_EasesFromStartToEnd()
        {
            Assert.That(MovementMath.DodgeSpeedAt(0f, _data), Is.EqualTo(_data.dodgeSpeed).Within(Eps));
            Assert.That(MovementMath.DodgeSpeedAt(_data.dodgeDuration, _data), Is.EqualTo(_data.dodgeEndSpeed).Within(Eps));
            float mid = MovementMath.DodgeSpeedAt(_data.dodgeDuration * 0.5f, _data);
            Assert.That(mid, Is.InRange(_data.dodgeEndSpeed, _data.dodgeSpeed));
        }
    }
}
