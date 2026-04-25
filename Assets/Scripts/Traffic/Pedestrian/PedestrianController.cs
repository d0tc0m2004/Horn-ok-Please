using UnityEngine;
using HornOkPlease.Traffic.Waypoints;

namespace HornOkPlease.Traffic.Pedestrian
{
    public class PedestrianController : MonoBehaviour
    {
        public bool IsActive { get; private set; }

        private enum State { WalkingOnSidewalk, CrossingRoad, Idle }

        private State state;
        private float walkSpeed;
        private float walkDirection; // +1 or -1 along Z
        private float crossTargetX;
        private float idleTimer;
        private float crossChanceTimer;

        // Dimensions
        private const float PedestrianWidth = 0.4f;
        private const float PedestrianHeight = 1.7f;

        public void Initialize(Vector3 position, float direction)
        {
            transform.position = position;
            walkDirection = direction;
            walkSpeed = Random.Range(0.8f, 1.5f); // normal walking speed
            state = State.WalkingOnSidewalk;
            crossChanceTimer = Random.Range(3f, 10f);
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        private void Update()
        {
            if (!IsActive) return;

            switch (state)
            {
                case State.WalkingOnSidewalk:
                    UpdateWalking();
                    break;
                case State.CrossingRoad:
                    UpdateCrossing();
                    break;
                case State.Idle:
                    UpdateIdle();
                    break;
            }
        }

        private void UpdateWalking()
        {
            float dt = Time.deltaTime;

            // Walk along sidewalk (Z direction)
            Vector3 pos = transform.position;
            pos.z += walkDirection * walkSpeed * dt;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, walkDirection));

            // Occasionally decide to cross the road
            crossChanceTimer -= dt;
            if (crossChanceTimer <= 0f)
            {
                // 30% chance to cross, otherwise just reset timer
                if (Random.value < 0.3f)
                {
                    StartCrossing();
                }
                crossChanceTimer = Random.Range(5f, 15f);
            }

            // Occasionally stop and idle
            if (Random.value < 0.002f)
            {
                state = State.Idle;
                idleTimer = Random.Range(1f, 3f);
            }
        }

        private void StartCrossing()
        {
            // Determine which side we're on and cross to the other
            float currentX = transform.position.x;
            if (currentX < 0)
                crossTargetX = LaneConfig.SidewalkRightCenter;
            else
                crossTargetX = LaneConfig.SidewalkLeftCenter;

            state = State.CrossingRoad;
        }

        private void UpdateCrossing()
        {
            float dt = Time.deltaTime;
            float crossSpeed = walkSpeed * 1.3f; // walk a bit faster when crossing

            Vector3 pos = transform.position;
            float dir = Mathf.Sign(crossTargetX - pos.x);
            pos.x += dir * crossSpeed * dt;

            // Face crossing direction
            transform.rotation = Quaternion.LookRotation(new Vector3(dir, 0f, 0f));

            // Check if we've reached the other side
            if (Mathf.Abs(pos.x - crossTargetX) < 0.3f)
            {
                pos.x = crossTargetX;
                state = State.WalkingOnSidewalk;
                // Might reverse walk direction after crossing
                if (Random.value < 0.5f) walkDirection = -walkDirection;
            }

            transform.position = pos;
        }

        private void UpdateIdle()
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                state = State.WalkingOnSidewalk;
                // Maybe change direction
                if (Random.value < 0.3f) walkDirection = -walkDirection;
            }
        }
    }
}
