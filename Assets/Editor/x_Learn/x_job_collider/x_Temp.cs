using UnityEngine;

public class x_Temp : MonoBehaviour
{
    private x_OverlapCheckerManager overlapManager;

    void Start()
    {
        overlapManager = new x_OverlapCheckerManager();

        // 예시 데이터 준비
        Vector2[] triA = { new Vector2(0, 0), new Vector2(1, 1) };
        Vector2[] triB = { new Vector2(1, 0), new Vector2(2, 1) };
        Vector2[] triC = { new Vector2(0, 1), new Vector2(1, 2) };
        Vector2[] centers = { new Vector2(0.5f, 0.5f), new Vector2(1.5f, 1.5f) };
        float[] radii = { 0.5f, 0.5f };

        // Job System 호출
        overlapManager.ScheduleOverlapCheck(triA, triB, triC, centers, radii);
    }

    void Update()
    {
        // Job 완료 여부 확인
        if (overlapManager.CheckIfJobIsDone(out bool[] results))
        {
            // 결과가 반환되면 이곳에서 처리
            for (int i = 0; i < results.Length; i++)
            {
                Debug.Log($"Overlap Check {i}: {results}");
            }
        }
    }
}