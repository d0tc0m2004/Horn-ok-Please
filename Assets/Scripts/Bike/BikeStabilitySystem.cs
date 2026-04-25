using UnityEngine;
using HornOkPlease.Data;

namespace HornOkPlease.Bike
{
    [RequireComponent(typeof(BikeController))]
    public class BikeStabilitySystem : MonoBehaviour
    {
        [Header("Handlebar Visual")]
        [SerializeField] private Transform handlebarTransform;

        private BikeController bike;
        private BikeConfig config;
        private float wobbleSeed;

        private void Awake()
        {
            bike = GetComponent<BikeController>();
            wobbleSeed = Random.Range(0f, 100f);
        }

        private void Start()
        {
            config = bike.Config;
        }

        private void LateUpdate()
        {
            if (bike.IsCrashed) return;
            if (handlebarTransform == null) return;

            float wobbleStrength = config.wobbleCurve.Evaluate(bike.SpeedRatio);

            // Visual steer angle on handlebar
            float steerAngle = bike.CurrentSteerAngle;

            // Add wobble at high speed
            float wobbleAngle = 0f;
            if (wobbleStrength > 0.01f)
            {
                float time = Time.time * config.wobbleFrequency * 1.5f;
                float wobble = (Mathf.PerlinNoise(time, wobbleSeed + 25f) - 0.5f) * 2f;
                wobbleAngle = wobble * config.maxWobbleAngle * wobbleStrength;
            }

            handlebarTransform.localRotation = Quaternion.Euler(0f, steerAngle + wobbleAngle, 0f);
        }
    }
}
