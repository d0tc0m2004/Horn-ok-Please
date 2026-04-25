using UnityEngine;
using HornOkPlease.Utility;

namespace HornOkPlease.Bike
{
    public class BikeDebugHUD : MonoBehaviour
    {
        private BikeController bike;
        private BikeInputHandler input;
        
        private float deltaTime = 0.0f;

        private void Awake()
        {
            bike = GetComponent<BikeController>();
            input = GetComponent<BikeInputHandler>();
        }

        private void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 18;
            style.normal.textColor = Color.white;

            float y = 10;
            float lineHeight = 24;

            void Line(string text)
            {
                GUI.Label(new Rect(11, y + 1, 500, lineHeight), text, new GUIStyle(style) { normal = { textColor = Color.black } });
                GUI.Label(new Rect(10, y, 500, lineHeight), text, style);
                y += lineHeight;
            }

            Line("=== BIKE DEBUG ===");
            
            float fps = 1.0f / deltaTime;
            float msec = deltaTime * 1000.0f;
            Line($"FPS: {fps:0.} ({msec:0.0} ms)");

            if (bike == null)
            {
                Line("BikeController: NULL!");
                return;
            }

            if (bike.Config == null)
            {
                Line("BikeConfig: NULL! (not assigned)");
                return;
            }

            // Input state
            Line($"--- INPUT ---");
            if (input == null)
            {
                Line("BikeInputHandler: NULL!");
            }
            else
            {
                Line($"Throttle (W): {input.Throttle:F2}");
                Line($"Brake (S): {input.Brake:F2}");
                Line($"Steer (A/D): {input.Steer:F2}");
                Line($"Lean (Q/E): {input.Lean:F2}");
                Line($"HardBrake (Space): {input.HardBrake}");
            }

            // State
            Line($"--- STATE ---");
            Line($"Grounded: {bike.IsGrounded}");
            Line($"Speed: {MathUtils.MSToKMPH(bike.CurrentSpeed):F1} kmph ({bike.CurrentSpeed:F2} m/s)");
            Line($"SpeedRatio: {bike.SpeedRatio:F3}");
            Line($"SteerAngle: {bike.CurrentSteerAngle:F1}");
            Line($"Skidding: {bike.IsSkidding}");
            Line($"Crashed: {bike.IsCrashed}");

            // Rigidbody
            Line($"--- RIGIDBODY ---");
            Rigidbody rb = bike.Rb;
            if (rb != null)
            {
                Line($"Velocity: {rb.linearVelocity.magnitude:F2} m/s");
                Line($"Position: {rb.position}");
            }

            // Config
            Line($"--- CONFIG ---");
            Line($"GroundLayers mask: {bike.Config.groundLayers.value}");
            Line($"Acceleration: {bike.Config.acceleration} m/s^2");
            Line($"MaxSpeed: {bike.Config.maxSpeedKMPH} kmph");
        }
    }
}
