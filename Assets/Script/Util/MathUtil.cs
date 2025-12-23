namespace Script.Util
{
    using System;
    using UnityEngine;
    using Unity.Mathematics;

    /// <summary> System.Math, UnityEngine.Math와 구분하기 위하여 MathUtil로 명명</summary>
    public static class MathUtil
    {
        public static int ToInt(this float value, int digits = 3)
        {
            return (int)Math.Round(value, digits);
        }
        public static Vector3Int ToInt(this Vector3 value)
        {
            int intX = value.x.ToInt();
            int intY = value.y.ToInt();
            int intZ = value.z.ToInt();

            return new Vector3Int(intX, intY, intZ);
        }
        public static float3 Normalize(this float3 vector)
        {
            // 1. 벡터의 크기(Magnitude)를 계산합니다.
            // Magnitude = Mathf.Sqrt(x*x + y*y + z*z)
            float sqrMagnitude = vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
            float magnitude = Mathf.Sqrt(sqrMagnitude);

            // 2. 크기가 0인지 확인하여 0으로 나누는 것을 방지합니다.
            if (magnitude > 0.00001f) // 부동 소수점 오차를 고려해 아주 작은 값과 비교
            {
                // 3. 각 성분을 크기로 나누어 정규화된 벡터를 반환합니다.
                // 정규화된 벡터의 각 성분 = 원래 성분 / 크기
                return new Vector3(
                    vector.x / magnitude,
                    vector.y / magnitude,
                    vector.z / magnitude
                );
            }
            else
            {
                // 크기가 0이면 정규화할 수 없으므로, (0, 0, 0) 벡터를 반환합니다.
                // 이는 Unity의 Vector3.Normalize()가 하는 동작과 유사합니다.
                return Vector3.zero;
            }
        }
    }
}