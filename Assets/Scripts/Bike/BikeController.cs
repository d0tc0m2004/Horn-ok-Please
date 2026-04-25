using UnityEngine;
using HornOkPlease.Data;
using HornOkPlease.Utility;

namespace HornOkPlease.Bike
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BikeInputHandler))]
    public class BikeController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private BikeConfig config;

        [Header("References")]
        [SerializeField] private Transform bikeModel;
        [SerializeField] private Transform groundCheck;

        public BikeConfig Config => config;
        public Rigidbody Rb { get; private set; }
        public BikeInputHandler Input { get; private set; }

        // State - readable by other systems
        public float CurrentSpeed { get; private set; }
        public float SpeedRatio { get; private set; }
        public float CurrentSteerAngle { get; private set; }
        public bool IsGrounded { get; private set; }
        public Vector3 GroundNormal { get; private set; }
        public bool IsCrashed { get; set; }
        public bool IsSkidding { get; private set; }
        public float RollAngle { get; private set; }

        // Set by BikeLeanSystem each frame
        public float LeanVelocity { get; set; }

        // Surface modifiers (set by RoadSurfaceEffect)
        public float GripMultiplier { get; set; } = 1f;
        public float DragModifier { get; set; }

        private float currentYaw;
        private float smoothSteerInput;
        private Vector3 knockback;
        private Vector3 lastSetVelocity;
        private float visualSteerAngle;

        private void Awake()
        {
            Rb = GetComponent<Rigidbody>();
            Input = GetComponent<BikeInputHandler>();
        }

        private void Start()
        {
            Rb.mass = config.mass;
            Rb.interpolation = RigidbodyInterpolation.Interpolate;
            Rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            Rb.angularDamping = 0f;
            Rb.linearDamping = 0f;
            currentYaw = transform.eulerAngles.y;
        }

        private void FixedUpdate()
        {
            if (IsCrashed) return;

            CaptureExternalForces();
            UpdateGroundCheck();
            UpdateSpeed();
            UpdateSteering();
            ApplyVelocity();
            DecayKnockback();
        }

        private void Update()
        {
            if (IsCrashed) return;
            UpdateVisuals();
        }

        /// <summary>
        /// Detect any velocity change from collisions/external forces since last frame.
        /// Capture it as knockback so collisions feel impactful but fade out.
        /// </summary>
        private void CaptureExternalForces()
        {
            Vector3 currentVel = Rb.linearVelocity;
            Vector3 diff = currentVel - lastSetVelocity;
            diff.y = 0f; // ignore gravity
            if (diff.sqrMagnitude > 0.01f)
            {
                knockback += diff;
            }
        }

        private void UpdateGroundCheck()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                config.groundCheckDistance, config.groundLayers))
            {
                IsGrounded = true;
                GroundNormal = hit.normal;
            }
            else
            {
                IsGrounded = false;
                GroundNormal = Vector3.up;
            }
        }

        private void UpdateSpeed()
        {
            if (!IsGrounded) return;

            float throttle = Input.Throttle;
            float brake = Input.Brake;
            bool hardBrake = Input.HardBrake;
            float dt = Time.fixedDeltaTime;

            SpeedRatio = Mathf.Clamp01(Mathf.Abs(CurrentSpeed) / config.MaxSpeedMS);

            // Skid detection
            IsSkidding = hardBrake && CurrentSpeed > MathUtils.KMPHToMS(config.skidSpeedThreshold);

            // Hard brake
            if (hardBrake && CurrentSpeed > 0.1f)
            {
                float decel = config.hardBrakeDeceleration;
                if (IsSkidding) decel *= 0.6f; // locked wheels = less effective
                CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, 0f, decel * dt);
            }
            // Regular brake (also reverse from standstill)
            else if (brake > 0.01f)
            {
                if (CurrentSpeed > 0.5f)
                {
                    CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, 0f, config.brakeDeceleration * brake * dt);
                }
                else if (CurrentSpeed > -MathUtils.KMPHToMS(config.reverseMaxSpeedKMPH))
                {
                    CurrentSpeed -= config.reverseAcceleration * brake * dt;
                }
            }
            // Throttle
            else if (throttle > 0.01f)
            {
                float accelMultiplier = config.accelerationCurve.Evaluate(SpeedRatio);
                CurrentSpeed += config.acceleration * accelMultiplier * throttle * dt;
                CurrentSpeed = Mathf.Min(CurrentSpeed, config.MaxSpeedMS);
            }
            // Coasting - natural deceleration
            else
            {
                CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, 0f, config.coastDeceleration * dt);
            }

            // Update speed ratio after changes
            SpeedRatio = Mathf.Clamp01(Mathf.Abs(CurrentSpeed) / config.MaxSpeedMS);
        }

        private void UpdateSteering()
        {
            if (!IsGrounded) return;

            float steerInput = Input.Steer;
            float dt = Time.fixedDeltaTime;

            // Smooth the input
            smoothSteerInput = Mathf.Lerp(smoothSteerInput, steerInput, config.steerSmoothing * dt);

            // Turn rate depends on speed: need some speed to turn, less twitchy at high speed
            float speedGate = Mathf.Clamp01(Mathf.Abs(CurrentSpeed) / 3f);
            float speedFactor = config.steerResponseCurve.Evaluate(SpeedRatio);
            float turnRate = smoothSteerInput * config.maxTurnRate * speedFactor * speedGate;

            // Apply yaw
            currentYaw += turnRate * dt;

            // Track visual steer angle (for handlebar + debug HUD)
            CurrentSteerAngle = smoothSteerInput * config.maxSteerAngle;

            // Force the bike upright - only yaw rotates, no tipping
            Rb.MoveRotation(Quaternion.Euler(0f, currentYaw, 0f));
            Rb.angularVelocity = Vector3.zero;
        }

        private void ApplyVelocity()
        {
            // Build target velocity: forward speed + lateral lean + knockback
            Vector3 targetVel = transform.forward * CurrentSpeed
                              + transform.right * LeanVelocity
                              + knockback;

            // Preserve vertical velocity (gravity, landing)
            targetVel.y = Rb.linearVelocity.y;

            Rb.linearVelocity = targetVel;
            lastSetVelocity = targetVel;

            // Reset lean velocity (BikeLeanSystem sets it each frame)
            LeanVelocity = 0f;
        }

        private void DecayKnockback()
        {
            knockback = Vector3.MoveTowards(knockback, Vector3.zero,
                config.knockbackRecovery * Time.fixedDeltaTime);
        }

        private void UpdateVisuals()
        {
            if (bikeModel == null) return;

            // Bike model tilts into turns (visual only - physics object stays upright)
            float speedFactor = Mathf.Clamp01(Mathf.Abs(CurrentSpeed) / 5f);
            float steerTilt = -smoothSteerInput * config.steerLeanAngle * speedFactor;

            // Smooth visual steer angle for handlebar
            visualSteerAngle = Mathf.Lerp(visualSteerAngle, CurrentSteerAngle, 10f * Time.deltaTime);

            bikeModel.localRotation = Quaternion.Euler(0f, 0f, steerTilt);

            // RollAngle for other systems (camera, crash detection)
            RollAngle = steerTilt;
        }

        // Called by external systems (pothole, crash handler)
        public void ApplyDestabilize(Vector3 impulse)
        {
            knockback += impulse / Rb.mass;
        }

        public void ApplyDestabilizeTorque(Vector3 torque)
        {
            // In direct-control mode, torque translates to a lateral knockback
            knockback += Vector3.Cross(torque, Vector3.up).normalized * torque.magnitude / Rb.mass;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            float dist = config != null ? config.groundCheckDistance : 1.2f;

            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(origin, origin + Vector3.down * dist);
            Gizmos.DrawWireSphere(origin + Vector3.down * dist, 0.1f);
        }
    }
}
