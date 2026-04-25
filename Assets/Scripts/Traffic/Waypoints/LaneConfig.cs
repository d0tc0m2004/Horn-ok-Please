using UnityEngine;

namespace HornOkPlease.Traffic.Waypoints
{
    /// <summary>
    /// 4-lane divided road configuration.
    /// Lanes 0-1: oncoming traffic (moving in -Z)
    /// Lanes 2-3: same direction as player (moving in +Z)
    /// </summary>
    public static class LaneConfig
    {
        public const int LaneCount = 6;
        public const float LaneWidth = 3.5f;
        public const float RoadWidth = LaneWidth * LaneCount; // 21m
        public const float SidewalkWidth = 2f;
        public const float ShoulderWidth = 0.75f;

        // Lane center X positions (road centered at X=0)
        // Oncoming: left half, Player: right half
        private static readonly float[] LaneCenters = new float[]
        {
            -LaneWidth * 2.5f,
            -LaneWidth * 1.5f, 
            -LaneWidth * 0.5f, 
             LaneWidth * 0.5f, 
             LaneWidth * 1.5f,
             LaneWidth * 2.5f,
        };

        /// <summary>
        /// Direction of travel for each lane. +1 = forward (+Z), -1 = oncoming (-Z).
        /// </summary>
        private static readonly int[] LaneDirections = new int[] { -1, -1, -1, 1, 1, 1 };

        /// <summary>
        /// Lanes that go in the same direction as the player (+Z).
        /// </summary>
        public static readonly int[] ForwardLanes = new int[] { 3, 4, 5 };

        /// <summary>
        /// Lanes for oncoming traffic (-Z).
        /// </summary>
        public static readonly int[] OncomingLanes = new int[] { 0, 1, 2 };

        public static float GetLaneX(int lane)
        {
            lane = Mathf.Clamp(lane, 0, LaneCount - 1);
            return LaneCenters[lane];
        }

        public static int GetLaneDirection(int lane)
        {
            lane = Mathf.Clamp(lane, 0, LaneCount - 1);
            return LaneDirections[lane];
        }

        public static bool IsOncomingLane(int lane)
        {
            return lane >= 0 && lane < LaneCount && LaneDirections[lane] < 0;
        }

        public static int GetNearestLane(float xPosition)
        {
            int best = 0;
            float bestDist = Mathf.Abs(xPosition - LaneCenters[0]);
            for (int i = 1; i < LaneCount; i++)
            {
                float dist = Mathf.Abs(xPosition - LaneCenters[i]);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>
        /// Get a random adjacent lane in the same direction.
        /// Vehicles don't cross the center divider via lane changes.
        /// </summary>
        public static int GetAdjacentLane(int currentLane)
        {
            if (IsOncomingLane(currentLane))
            {
                if (currentLane == 0) return 1;
                if (currentLane == 1) return Random.value > 0.5f ? 0 : 2;
                return 1;
            }
            else
            {
                if (currentLane == 3) return 4;
                if (currentLane == 4) return Random.value > 0.5f ? 3 : 5;
                return 4;
            }
        }

        public static float RoadLeftEdge => -(RoadWidth / 2f) - ShoulderWidth;
        public static float RoadRightEdge => (RoadWidth / 2f) + ShoulderWidth;
        public static float SidewalkLeftCenter => RoadLeftEdge - SidewalkWidth / 2f;
        public static float SidewalkRightCenter => RoadRightEdge + SidewalkWidth / 2f;
        public static float CenterDividerX => 0f; // between lanes 1 and 2
    }
}
