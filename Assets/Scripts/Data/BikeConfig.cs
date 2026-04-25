using UnityEngine;

namespace HornOkPlease.Data
{
    [CreateAssetMenu(fileName = "BikeConfig", menuName = "HornOkPlease/Bike Config")]
    public class BikeConfig : ScriptableObject
    {
        [Header("Speed")]
        public float maxSpeedKMPH = 120f;
        public float comfortSpeedKMPH = 60f;
        public float dangerSpeedKMPH = 80f;

        public float MaxSpeedMS => maxSpeedKMPH / 3.6f;
        public float ComfortSpeedMS => comfortSpeedKMPH / 3.6f;
        public float DangerSpeedMS => dangerSpeedKMPH / 3.6f;

        [Header("Acceleration")]
        [Tooltip("How fast the bike accelerates in m/s^2")]
        public float acceleration = 8f;
        [Tooltip("X = speed ratio (0-1), Y = acceleration multiplier. Tapers at high speed.")]
        public AnimationCurve accelerationCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.3f, 0.85f),
            new Keyframe(0.6f, 0.5f),
            new Keyframe(0.85f, 0.2f),
            new Keyframe(1f, 0.02f)
        );
        [Tooltip("Deceleration when pressing S (m/s^2)")]
        public float brakeDeceleration = 25f;
        [Tooltip("Deceleration when pressing Space (m/s^2)")]
        public float hardBrakeDeceleration = 70f;
        [Tooltip("Deceleration when coasting - no throttle/brake (m/s^2)")]
        public float coastDeceleration = 4f;
        public float reverseMaxSpeedKMPH = 15f;
        [Tooltip("Reverse acceleration in m/s^2")]
        public float reverseAcceleration = 5f;

        [Header("Steering")]
        [Tooltip("Peak turn rate in degrees/sec at low speed")]
        public float maxTurnRate = 120f;
        [Tooltip("X = speed ratio, Y = turn rate multiplier. Less twitchy at high speed.")]
        public AnimationCurve steerResponseCurve = new AnimationCurve(
            new Keyframe(0f, 0.6f),
            new Keyframe(0.1f, 1f),
            new Keyframe(0.4f, 0.6f),
            new Keyframe(0.7f, 0.35f),
            new Keyframe(1f, 0.2f)
        );
        [Tooltip("How fast steering input smooths (higher = snappier)")]
        public float steerSmoothing = 12f;

        [Header("Lean (Q/E)")]
        [Tooltip("Sideways speed in m/s when fully leaning")]
        public float leanSpeed = 5f;
        public float leanVisualAngle = 25f;
        [Tooltip("How fast the visual lean interpolates")]
        public float leanVisualSpeed = 15f;

        [Header("Visual Tilt")]
        [Tooltip("How many degrees the bike model tilts into turns")]
        public float steerLeanAngle = 20f;
        [Tooltip("Max visual steer angle shown on handlebar")]
        public float maxSteerAngle = 35f;

        [Header("High Speed Wobble (Visual Only)")]
        [Tooltip("X = speed ratio, Y = wobble strength")]
        public AnimationCurve wobbleCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 0f),
            new Keyframe(0.7f, 0.3f),
            new Keyframe(1f, 1f)
        );
        public float maxWobbleAngle = 1.5f;
        public float wobbleFrequency = 3f;

        [Header("Crash")]
        public float crashImpulseThreshold = 15f;
        public float crashRecoveryTime = 2f;
        public float skidSpeedThreshold = 40f;

        [Header("Physics")]
        public float mass = 180f;
        [Tooltip("How fast collision knockback fades (m/s per second)")]
        public float knockbackRecovery = 12f;

        [Header("Ground Detection")]
        public float groundCheckDistance = 1.2f;
        public LayerMask groundLayers;
    }
}
