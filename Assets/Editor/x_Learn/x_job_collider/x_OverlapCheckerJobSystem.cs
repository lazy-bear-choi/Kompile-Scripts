using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;

public class x_OverlapCheckerJobSystem : MonoBehaviour
{
    [Header("Input Data (must have the same length)")]
    public Vector2[] triangleA;
    public Vector2[] triangleB;
    public Vector2[] triangleC;
    public Vector2[] circleCenters;
    public float[] circleRadii;

    [Header("Results")]
    public bool[] overlapResults;

    // Update는 매 프레임 호출되므로 성능에 유의해야 합니다.
    void Update()
    {
        int numChecks = triangleA.Length;
        if (numChecks == 0 || numChecks != triangleB.Length || numChecks != triangleC.Length || numChecks != circleCenters.Length || numChecks != circleRadii.Length)
        {
            Debug.LogError("Input arrays must have the same length.");
            return;
        }

        overlapResults = new bool[numChecks];

        // NativeArray에 데이터 복사
        var nativeTriangleA = new NativeArray<float2>(triangleA.Select(v => new float2(v.x, v.y)).ToArray(), Allocator.TempJob);
        var nativeTriangleB = new NativeArray<float2>(triangleB.Select(v => new float2(v.x, v.y)).ToArray(), Allocator.TempJob);
        var nativeTriangleC = new NativeArray<float2>(triangleC.Select(v => new float2(v.x, v.y)).ToArray(), Allocator.TempJob);
        var nativeCircleCenters = new NativeArray<float2>(circleCenters.Select(v => new float2(v.x, v.y)).ToArray(), Allocator.TempJob);
        var nativeCircleRadii = new NativeArray<float>(circleRadii, Allocator.TempJob);
        var nativeOverlapResults = new NativeArray<bool>(numChecks, Allocator.TempJob);

        var job = new x_TriangleCircleOverlapJob
        {
            TriangleA = nativeTriangleA,
            TriangleB = nativeTriangleB,
            TriangleC = nativeTriangleC,
            CircleCenters = nativeCircleCenters,
            CircleRadii = nativeCircleRadii,
            OverlapResults = nativeOverlapResults
        };

        JobHandle jobHandle = job.Schedule(numChecks, 64); // 작업 스케줄링
        jobHandle.Complete(); // 작업 완료 대기

        // NativeArray에서 결과 복사
        nativeOverlapResults.CopyTo(overlapResults);

        // 사용 후 반드시 NativeArray 메모리 해제
        nativeTriangleA.Dispose();
        nativeTriangleB.Dispose();
        nativeTriangleC.Dispose();
        nativeCircleCenters.Dispose();
        nativeCircleRadii.Dispose();
        nativeOverlapResults.Dispose();

        // 결과 확인 (예시)
        for (int i = 0; i < numChecks; i++)
        {
            Debug.Log($"Overlap check {i}: {overlapResults}");
        }
    }
}