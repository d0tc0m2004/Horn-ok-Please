using UnityEngine;
using HornOkPlease.Data;

namespace HornOkPlease.Bike
{
    [RequireComponent(typeof(BikeController))]
    public class BikeLeanSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform bikeModel;
        [SerializeField] private Transform riderModel;

        public float CurrentLeanAngle { get; private set; }
        public float VisualLeanAngle { get; private set; }

        private BikeController bike;
        private BikeConfig config;
        private float visualLeanVelocity;

        private void Awake()
        {
            bike = GetComponent<BikeController>();
        }

        private void Start()
        {
            config = bike.Config;
        }

        private void FixedUpdate()
        {
            if (bike.IsCrashed) return;
            if (!bike.IsGrounded) return;

            float leanInput = bike.Input.Lean;

            // Need some forward speed for lean to work (can't sidestep a stationary bike)
            float speedGate = Mathf.Clamp01(Mathf.Abs(bike.CurrentSpeed) / 3f);

            // Set lateral velocity directly on the controller
            bike.LeanVelocity = leanInput * config.leanSpeed * speedGate;

            CurrentLeanAngle = leanInput * config.leanVisualAngle;
        }

        private void LateUpdate()
        {
            if (bike.IsCrashed) return;

            // Smooth visual lean
            VisualLeanAngle = Mathf.SmoothDamp(VisualLeanAngle, CurrentLeanAngle,
                ref visualLeanVelocity, 1f / config.leanVisualSpeed);

            // Rider leans more than the bike body
            if (riderModel != null)
            {
                riderModel.localRotation = Quaternion.Euler(0f, 0f, -VisualLeanAngle * 1.5f);
            }
        }
    }
}
