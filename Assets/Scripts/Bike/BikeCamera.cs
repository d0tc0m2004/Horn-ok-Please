using UnityEngine;

namespace HornOkPlease.Bike
{
    public class BikeCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private BikeController bike;
        [SerializeField] private BikeLeanSystem leanSystem;

        [Header("First Person")]
        [Tooltip("Offset from bike pivot in local space. Y = rider head height, Z = forward.")]
        [SerializeField] private Vector3 fpOffset = new Vector3(0f, 1.5f, 0.3f);

        [Header("Third Person")]
        [SerializeField] private Vector3 tpOffset = new Vector3(0f, 2.5f, -6f);
        [SerializeField] private float tpFollowSpeed = 8f;
        [SerializeField] private float tpLookAheadDistance = 10f;

        [Header("Rotation")]
        [Tooltip("How quickly camera yaw follows bike yaw. Higher = tighter.")]
        [SerializeField] private float yawFollowSpeed = 20f;
        [Tooltip("How much camera rolls with bike lean. 0 = level horizon, 1 = full bike roll.")]
        [SerializeField] [Range(0f, 1f)] private float rollFollowFactor = 0.25f;
        [Tooltip("How much camera pitches with bike. 0 = level, 1 = full pitch.")]
        [SerializeField] [Range(0f, 1f)] private float pitchFollowFactor = 0.3f;

        [Header("Speed Effects")]
        [SerializeField] private float baseFOV = 65f;
        [SerializeField] private float maxFOV = 88f;
        [SerializeField] private float fovSmoothSpeed = 4f;

        [Header("Look Behind")]
        [SerializeField] private float lookBehindTransitionSpeed = 8f;

        [Header("Shake")]
        [SerializeField] private float baseShakeIntensity = 0.005f;
        [SerializeField] private float speedShakeMultiplier = 0.015f;
        [SerializeField] private float shakeFrequency = 18f;

        // Settable by RoadSurfaceEffect
        public float SurfaceShakeMultiplier { get; set; } = 1f;
        public float CrashShakeIntensity { get; set; }

        public bool IsFirstPerson { get; private set; } = true;

        private Camera cam;
        private float currentFOV;
        private float lookBehindBlend;
        private float shakeSeed;
        private float smoothYaw;
        private float yawVelocity;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            currentFOV = baseFOV;
            shakeSeed = Random.Range(0f, 100f);
        }

        private void Start()
        {
            if (bike != null)
            {
                smoothYaw = bike.transform.eulerAngles.y;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                IsFirstPerson = !IsFirstPerson;
            }
        }

        private void LateUpdate()
        {
            if (bike == null) return;

            if (IsFirstPerson)
                UpdateFirstPerson();
            else
                UpdateThirdPerson();

            // FOV (wider range in first person for speed sensation)
            float fovBase = IsFirstPerson ? baseFOV : baseFOV - 5f;
            float fovMax = IsFirstPerson ? maxFOV : maxFOV - 8f;
            float targetFOV = Mathf.Lerp(fovBase, fovMax, bike.SpeedRatio);
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovSmoothSpeed * Time.deltaTime);
            cam.fieldOfView = currentFOV;

            ApplyShake();
        }

        private void UpdateFirstPerson()
        {
            // Position: snap directly to rider head (no lerp = no nausea)
            transform.position = bike.transform.TransformPoint(fpOffset);

            // Rotation: follow yaw tightly, dampen roll and pitch
            float bikeYaw = bike.transform.eulerAngles.y;
            smoothYaw = Mathf.SmoothDampAngle(smoothYaw, bikeYaw, ref yawVelocity,
                1f / yawFollowSpeed);

            float leanAngle = leanSystem != null ? leanSystem.VisualLeanAngle : 0f;
            float cameraRoll = leanAngle * rollFollowFactor;

            float bikePitch = bike.transform.eulerAngles.x;
            if (bikePitch > 180f) bikePitch -= 360f;
            float cameraPitch = bikePitch * pitchFollowFactor;

            // Look behind
            float lookTarget = bike.Input.LookBehind ? 1f : 0f;
            lookBehindBlend = Mathf.MoveTowards(lookBehindBlend, lookTarget,
                lookBehindTransitionSpeed * Time.deltaTime);
            float yawOffset = lookBehindBlend * 180f;

            transform.rotation = Quaternion.Euler(cameraPitch, smoothYaw + yawOffset, -cameraRoll);
        }

        private void UpdateThirdPerson()
        {
            // Position: smooth follow behind bike
            Vector3 targetPos = bike.transform.TransformPoint(tpOffset);

            // Lean offset - camera shifts slightly in lean direction
            float leanAngle = leanSystem != null ? leanSystem.VisualLeanAngle : 0f;
            targetPos += bike.transform.right * (leanAngle / 25f) * 0.5f;

            transform.position = Vector3.Lerp(transform.position, targetPos, tpFollowSpeed * Time.deltaTime);

            // Look behind
            float lookTarget = bike.Input.LookBehind ? 1f : 0f;
            lookBehindBlend = Mathf.MoveTowards(lookBehindBlend, lookTarget,
                lookBehindTransitionSpeed * Time.deltaTime);

            // Look at point ahead of bike (or behind when looking back)
            Vector3 lookDir = Vector3.Lerp(bike.transform.forward, -bike.transform.forward, lookBehindBlend);
            Vector3 lookPoint = bike.transform.position + lookDir * tpLookAheadDistance + Vector3.up * 0.5f;

            Quaternion targetRot = Quaternion.LookRotation(lookPoint - transform.position);

            // Slight tilt with lean
            float tiltAngle = leanAngle / 25f * 3f;
            targetRot *= Quaternion.Euler(0f, 0f, tiltAngle);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);

            // Keep yaw tracker in sync so switching to FP doesn't snap
            smoothYaw = transform.eulerAngles.y;
            yawVelocity = 0f;
        }

        private void ApplyShake()
        {
            float time = Time.time * shakeFrequency;

            // Shake is subtler in third person
            float modeScale = IsFirstPerson ? 1f : 0.5f;

            float speedShake = bike.SpeedRatio * speedShakeMultiplier * modeScale;
            float surfaceShake = baseShakeIntensity * SurfaceShakeMultiplier * modeScale;
            float totalIntensity = speedShake + surfaceShake + CrashShakeIntensity;

            if (totalIntensity < 0.001f) return;

            float shakeX = (Mathf.PerlinNoise(time, shakeSeed) - 0.5f) * 2f * totalIntensity;
            float shakeY = (Mathf.PerlinNoise(time + 50f, shakeSeed + 50f) - 0.5f) * 2f * totalIntensity;

            transform.position += transform.right * shakeX + transform.up * shakeY;
        }
    }
}
