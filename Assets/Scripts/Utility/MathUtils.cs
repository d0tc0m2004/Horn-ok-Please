using UnityEngine;

namespace HornOkPlease.Utility
{
    public static class MathUtils
    {
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float t = Mathf.InverseLerp(fromMin, fromMax, value);
            return Mathf.Lerp(toMin, toMax, t);
        }

        public static float MSToKMPH(float ms)
        {
            return ms * 3.6f;
        }

        public static float KMPHToMS(float kmph)
        {
            return kmph / 3.6f;
        }

        public static float GetRollAngle(Transform transform)
        {
            Vector3 right = transform.right;
            right.y = 0f;
            if (right.sqrMagnitude < 0.001f) return 0f;
            right.Normalize();

            float dot = Vector3.Dot(transform.right, right);
            float sign = Mathf.Sign(Vector3.Dot(transform.up, Vector3.down));
            float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;

            if (Vector3.Dot(transform.up, Vector3.up) < 0f)
                return 180f * sign;

            return angle * (Vector3.Dot(transform.up, Vector3.Cross(Vector3.up, transform.forward)) > 0f ? 1f : -1f);
        }

        public static float SmoothDampAngle(float current, float target, ref float velocity, float smoothTime, float deltaTime)
        {
            return Mathf.SmoothDampAngle(current, target, ref velocity, smoothTime, Mathf.Infinity, deltaTime);
        }
    }
}
