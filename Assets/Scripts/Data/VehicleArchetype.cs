using UnityEngine;

namespace HornOkPlease.Data
{
    [CreateAssetMenu(fileName = "VehicleArchetype", menuName = "HornOkPlease/Vehicle Archetype")]
    public class VehicleArchetype : ScriptableObject
    {
        [Header("Identity")]
        public string vehicleName = "Car";

        [Header("Dimensions (meters)")]
        public Vector3 size = new Vector3(1.7f, 1.5f, 3.8f); // width, height, length

        [Header("Speed (kmph)")]
        public float minSpeed = 40f;
        public float maxSpeed = 70f;

        [Header("Behavior")]
        [Tooltip("0 = patient (requires large gaps), 1 = aggressive (will take tight gaps)")]
        [Range(0f, 1f)]
        public float aggressiveness = 0.5f;
        [Tooltip("Safe distance to keep from the vehicle ahead in meters")]
        public float safeFollowingDistance = 2f;
        [Tooltip("How fast the vehicle accelerates (m/s^2)")]
        public float acceleration = 5f;
        [Tooltip("How fast the vehicle brakes (m/s^2)")]
        public float brakingDeceleration = 10f;
        
        [Tooltip("How often this vehicle considers changing lanes (seconds between evaluations)")]
        public float laneChangeInterval = 4f;
        [Tooltip("How quickly it changes lanes (m/s lateral)")]
        public float laneChangeSpeed = 2f;
        [Tooltip("Forward raycast distance for immediate avoidance/braking")]
        public float avoidanceDistance = 15f;
        [Tooltip("Preferred lanes (0=left, 1=center, 2=right). Empty = any lane.")]
        public int[] preferredLanes;

        [Header("Appearance")]
        public Color baseColor = Color.gray;
        [Tooltip("If true, color is randomized from a set of variants")]
        public bool randomizeColor = true;
        public Color[] colorVariants = new Color[]
        {
            Color.white,
            Color.gray,
            new Color(0.2f, 0.2f, 0.6f), // blue
            new Color(0.6f, 0.15f, 0.1f), // red
            new Color(0.1f, 0.1f, 0.1f), // black
        };

        [Header("Spawn Weight")]
        [Tooltip("Higher = more likely to spawn. Car=10, Truck=4, Auto=6, etc.")]
        public int spawnWeight = 10;

        public float GetRandomSpeed()
        {
            return Random.Range(minSpeed, maxSpeed) / 3.6f; // return m/s
        }

        public Color GetColor()
        {
            if (randomizeColor && colorVariants.Length > 0)
                return colorVariants[Random.Range(0, colorVariants.Length)];
            return baseColor;
        }
    }
}
