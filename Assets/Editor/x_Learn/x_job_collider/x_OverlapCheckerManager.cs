using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.Linq;

public class x_OverlapCheckerManager
{
    // Job에 전달할 데이터 배열
    private NativeArray<float2> nativeTriangleA;
    private NativeArray<float2> nativeTriangleB;
    private NativeArray<float2> nativeTriangleC;
    private NativeArray<float2> nativeCircleCenters;
    private NativeArray<float> nativeCircleRadii;
    private NativeArray<bool> nativeOverlapResults;

    // Job 스케줄링 핸들
    private JobHandle jobHandle;
    private bool isJobScheduled = false;

    /// <summary>
    /// 매개변수를 받아 Job을 스케줄링하는 함수.
    /// </summary>
    public void ScheduleOverlapCheck(Vector2[] triangleA, Vector2[] triangleB, Vector2[] triangleC, Vector2[] circleCenters, float[] circleRadii)
    {
        if (isJobScheduled)
        {
            Debug.LogWarning("Job is already scheduled. Please wait for the current job to complete.");
            return;
        }

        int numChecks = triangleA.Length;
        if (numChecks == 0) return;

        // Job에 필요한 NativeArray 메모리 할당 및 데이터 복사.
        // Allocator.Persistent를 사용하여 수동으로 메모리 해제해야 함.
        nativeTriangleA = new NativeArray<float2>(triangleA.Select(v => new float2(v.x, v.y)).ToArray(), Allocator.Persistent);
        nativeTriangleB = new NativeArray<float2>(triangleB.Select(v => new float2(v.x, v.y)).ToArray(), Allocator.Persistent);
        nativeTriangleC = new NativeArray<float2>(triangleC.Select(v => new float2(v.x, v.y)).ToArray(), Allocator.Persistent);
        nativeCircleCenters = new NativeArray<float2>(circleCenters.Select(v => new float2(v.x, v.y)).ToArray(), Allocator.Persistent);
        nativeCircleRadii = new NativeArray<float>(circleRadii, Allocator.Persistent);
        nativeOverlapResults = new NativeArray<bool>(numChecks, Allocator.Persistent);

        // Job 인스턴스 생성 및 스케줄링
        var job = new x_TriangleCircleOverlapJob
        {
            TriangleA = nativeTriangleA,
            TriangleB = nativeTriangleB,
            TriangleC = nativeTriangleC,
            CircleCenters = nativeCircleCenters,
            CircleRadii = nativeCircleRadii,
            OverlapResults = nativeOverlapResults
        };

        jobHandle = job.Schedule(numChecks, 64);
        isJobScheduled = true;
    }

    /// <summary>
    /// Job이 완료되었는지 확인하고 결과를 반환하는 함수.
    /// Job이 완료되면 true를 반환하고, 결과를 out 매개변수에 할당.
    /// </summary>
    public bool CheckIfJobIsDone(out bool[] results)
    {
        if (!isJobScheduled)
        {
            results = null;
            return false;
        }

        // Job이 완료되었는지 확인
        if (jobHandle.IsCompleted)
        {
            jobHandle.Complete(); // Job 완료를 최종적으로 보장하고 결과에 접근
            results = nativeOverlapResults.ToArray(); // 결과를 Managed Array로 복사

            // 사용된 NativeArray 메모리 해제
            DisposeNativeArrays();

            isJobScheduled = false;
            return true;
        }

        results = null;
        return false;
    }

    /// <summary>
    /// NativeArray 메모리를 해제하는 함수.
    /// </summary>
    private void DisposeNativeArrays()
    {
        if (nativeTriangleA.IsCreated) nativeTriangleA.Dispose();
        if (nativeTriangleB.IsCreated) nativeTriangleB.Dispose();
        if (nativeTriangleC.IsCreated) nativeTriangleC.Dispose();
        if (nativeCircleCenters.IsCreated) nativeCircleCenters.Dispose();
        if (nativeCircleRadii.IsCreated) nativeCircleRadii.Dispose();
        if (nativeOverlapResults.IsCreated) nativeOverlapResults.Dispose();
    }
}