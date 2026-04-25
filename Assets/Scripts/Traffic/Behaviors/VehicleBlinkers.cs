using UnityEngine;
using HornOkPlease.Traffic.Vehicle;

namespace HornOkPlease.Traffic.Behaviors
{
    public class VehicleBlinkers : MonoBehaviour
    {
        private NPCVehicle vehicle;
        private GameObject leftBlinker;
        private GameObject rightBlinker;
        private float blinkTimer;
        private bool blinkState;

        public void Initialize(NPCVehicle npc, GameObject left, GameObject right)
        {
            vehicle = npc;
            leftBlinker = left;
            rightBlinker = right;
            
            if (leftBlinker != null) leftBlinker.SetActive(false);
            if (rightBlinker != null) rightBlinker.SetActive(false);
        }

        private void Update()
        {
            if (vehicle == null || vehicle.SignalDirection == 0)
            {
                if (leftBlinker != null && leftBlinker.activeSelf) leftBlinker.SetActive(false);
                if (rightBlinker != null && rightBlinker.activeSelf) rightBlinker.SetActive(false);
                blinkState = false;
                blinkTimer = 0f;
                return;
            }

            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0f)
            {
                blinkTimer = 0.35f; // blink rate
                blinkState = !blinkState;
            }

            if (vehicle.SignalDirection < 0)
            {
                if (leftBlinker != null) leftBlinker.SetActive(blinkState);
                if (rightBlinker != null && rightBlinker.activeSelf) rightBlinker.SetActive(false);
            }
            else if (vehicle.SignalDirection > 0)
            {
                if (leftBlinker != null && leftBlinker.activeSelf) leftBlinker.SetActive(false);
                if (rightBlinker != null) rightBlinker.SetActive(blinkState);
            }
        }
    }
}
