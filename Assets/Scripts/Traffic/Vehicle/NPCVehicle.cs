using UnityEngine;
using HornOkPlease.Data;
using HornOkPlease.Traffic.Waypoints;

namespace HornOkPlease.Traffic.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class NPCVehicle : MonoBehaviour
    {
        public VehicleArchetype Archetype { get; private set; }
        public float Speed { get; private set; }
        public bool IsActive { get; private set; }
        public int Direction { get; private set; } 
        // Signal is occasionally shown when swerving
        public int SignalDirection { get; private set; } 

        private float lateralPosition;
        private float targetX;
        private float baseSpeed;
        private Rigidbody rb;
        private LayerMask avoidanceMask;

        // Boundaries
        private float minAllowedX;
        private float maxAllowedX;

        public void Initialize(VehicleArchetype archetype, int lane, float zPosition)
        {
            Archetype = archetype;
            Direction = LaneConfig.GetLaneDirection(lane);
            baseSpeed = archetype.GetRandomSpeed();
            Speed = baseSpeed;
            
            // Randomize starting lateral position anywhere in assigned directional road half
            if (Direction > 0) // Forward
            {
                minAllowedX = LaneConfig.CenterDividerX; 
                // Aggressive cars cross divider
                if (archetype.aggressiveness > 0.7f) minAllowedX = LaneConfig.CenterDividerX - LaneConfig.LaneWidth * 0.75f;
                maxAllowedX = LaneConfig.RoadRightEdge;
                
                float laneCenter = LaneConfig.GetLaneX(lane);
                lateralPosition = Random.Range(Mathf.Max(minAllowedX, laneCenter - 1f), Mathf.Min(maxAllowedX, laneCenter + 1f));
            }
            else // Oncoming
            {
                minAllowedX = LaneConfig.RoadLeftEdge;
                maxAllowedX = LaneConfig.CenterDividerX; 
                if (archetype.aggressiveness > 0.7f) maxAllowedX = LaneConfig.CenterDividerX + LaneConfig.LaneWidth * 0.75f;
                
                float laneCenter = LaneConfig.GetLaneX(lane);
                lateralPosition = Random.Range(Mathf.Max(minAllowedX, laneCenter - 1f), Mathf.Min(maxAllowedX, laneCenter + 1f));
            }
            
            targetX = lateralPosition;

            transform.position = new Vector3(lateralPosition, archetype.size.y / 2f, zPosition);
            transform.rotation = Quaternion.Euler(0f, Direction > 0 ? 0f : 180f, 0f);

            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;

            avoidanceMask = (1 << 6) | (1 << 7); // layer 6 Player, 7 NPC
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        private void Update()
        {
            if (!IsActive) return;

            EvaluateContinuousPath();

            // Move
            Vector3 pos = transform.position;
            pos.z += Speed * Direction * Time.deltaTime;
            
            // Steer towards targetX
            float swerveSpeed = Archetype.laneChangeSpeed * (1f + Archetype.aggressiveness * 1.5f);
            lateralPosition = Mathf.MoveTowards(lateralPosition, targetX, swerveSpeed * Time.deltaTime);
            pos.x = lateralPosition;

            // Slight rotational leaning for visual swerving
            float lateralDelta = targetX - lateralPosition;
            float rotationY = lateralDelta * 20f; 
            if (Direction < 0) rotationY = -rotationY;
            Quaternion targetRot = Quaternion.Euler(0f, (Direction > 0 ? 0f : 180f) + rotationY, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);

            transform.position = pos;
        }

        private void EvaluateContinuousPath()
        {
            float dt = Time.deltaTime;
            float lookAhead = Mathf.Max(15f, Speed * 2.5f);
            
            // Shoot 3 feelers: Center, Left offset, Right offset
            float offsetAmount = Archetype.size.x * 0.85f; // Test gap roughly our width

            TrafficEntity centerHit = CheckGap(0f, lookAhead);
            TrafficEntity leftHit = CheckGap(-offsetAmount, lookAhead);
            TrafficEntity rightHit = CheckGap(offsetAmount, lookAhead);

            float targetSpeed = baseSpeed;

            // 1. If Center is blocked
            if (centerHit.exists && centerHit.distance < lookAhead)
            {
                // Brake or Tailgate
                float emergencyDist = Archetype.safeFollowingDistance + 1f;
                if (centerHit.distance < emergencyDist)
                {
                    // Tailgate: Match speed exactly but stay very close
                    targetSpeed = centerHit.speed;
                    if (centerHit.distance <= Archetype.safeFollowingDistance * 0.7f)
                    {
                        targetSpeed = Mathf.Max(0f, centerHit.speed - 5f); // Brake hard to avoid rear-ending
                    }
                }
                else
                {
                    // Close the gap fast, but prep to slow
                    targetSpeed = Mathf.Min(baseSpeed, centerHit.speed * 1.2f);
                }

                // 2. Opportunistic Weaving (Squeezing)
                // We only squeeze if our speed has been compromised
                if (Speed < baseSpeed * 0.9f)
                {
                    float leftSpace = leftHit.exists ? leftHit.distance : lookAhead * 1.5f;
                    float rightSpace = rightHit.exists ? rightHit.distance : lookAhead * 1.5f;
                    
                    // Does left or right have significantly more space than center?
                    if (leftSpace > centerHit.distance + 4f || rightSpace > centerHit.distance + 4f)
                    {
                        // We prefer the side with the most space, as long as it's within bounds
                        bool choseLeft = false;
                        
                        // Favor moving back towards center divider slightly if both good, or just pick best
                        if (leftSpace > rightSpace && (lateralPosition - offsetAmount) > minAllowedX)
                        {
                            targetX = lateralPosition - offsetAmount;
                            choseLeft = true;
                        }
                        else if ((lateralPosition + offsetAmount) < maxAllowedX)
                        {
                            targetX = lateralPosition + offsetAmount;
                        }
                        else if ((lateralPosition - offsetAmount) > minAllowedX)
                        {
                            // fallback to left if right blocked by bounds
                            targetX = lateralPosition - offsetAmount;
                            choseLeft = true;
                        }

                        // Determine visual signal
                        if (targetX != lateralPosition)
                        {
                            SignalDirection = choseLeft ? -1 : 1;
                        }
                        else
                        {
                            SignalDirection = 0;
                        }
                    }
                    else
                    {
                        SignalDirection = 0; // Trapped, so just tailgate
                    }
                }
            }
            else
            {
                // Clear path
                SignalDirection = 0;
                
                // Slowly drift back to a more distinct "lane" just to not drive exactly on the white lines forever if alone
                // But do it very lazily
                if (Mathf.Abs(targetX - lateralPosition) < 0.1f)
                {
                    targetX = Mathf.Lerp(targetX, LaneConfig.GetLaneX(LaneConfig.GetNearestLane(targetX)), 0.1f * dt);
                }
            }

            // Clamp targetX so they don't drive on the sidewalk
            targetX = Mathf.Clamp(targetX, minAllowedX + Archetype.size.x / 2f, maxAllowedX - Archetype.size.x / 2f);

            // Execute Speed Smoothing
            if (Speed < targetSpeed)
            {
                Speed = Mathf.MoveTowards(Speed, targetSpeed, Archetype.acceleration * dt);
            }
            else
            {
                Speed = Mathf.MoveTowards(Speed, targetSpeed, Archetype.brakingDeceleration * dt);
            }
        }

        private struct TrafficEntity
        {
            public bool exists;
            public float distance;
            public float speed;
        }

        private TrafficEntity CheckGap(float lateralOffset, float maxDist)
        {
            TrafficEntity entity = new TrafficEntity { exists = false, distance = float.MaxValue, speed = 0f };
            Vector3 forwardDir = Direction > 0 ? Vector3.forward : Vector3.back;
            
            // X position considering absolute world coordinates
            Vector3 startPos = transform.position + new Vector3(lateralOffset, 0, 0) + forwardDir * (Archetype.size.z / 2f + 0.1f);
            
            // Thinner box is the secret to "squeezing". Aggressive vehicles think they fit anywhere.
            float checkWidth = Archetype.size.x * Mathf.Lerp(0.9f, 0.45f, Archetype.aggressiveness); 
            Vector3 boxExtents = new Vector3(checkWidth / 2f, Archetype.size.y / 2f, 0.1f);

            if (Physics.BoxCast(startPos, boxExtents, forwardDir, out RaycastHit hit, Quaternion.identity, maxDist, avoidanceMask))
            {
                entity.exists = true;
                entity.distance = hit.distance;
                
                NPCVehicle npc = hit.collider.GetComponentInParent<NPCVehicle>();
                if (npc != null)
                {
                    // Relative speed is mostly important if going same direction. Oncoming vehicles are handled naturally.
                    entity.speed = npc.Speed;
                }
                else
                {
                    Rigidbody otherRb = hit.collider.GetComponentInParent<Rigidbody>();
                    if (otherRb != null) {
                        #if UNITY_6000_0_OR_NEWER
                        entity.speed = otherRb.linearVelocity.magnitude;
                        #else
                        entity.speed = otherRb.velocity.magnitude;
                        #endif
                    }
                }
            }
            return entity;
        }
    }
}
