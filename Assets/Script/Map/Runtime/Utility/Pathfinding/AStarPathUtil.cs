namespace Script.Map.Runtime 
{
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// 외부(캐릭터, AI 등)로부터 길찾기 요청을 받아 AStarPathJob을 실행하고 결과를 반환하는 engine wrapper
    /// </summary>
    public class AStarPathUtil
    {
        /// <summary>
        /// 캐싱된 nativeMap을 주입 받아 동기(Immediate)로 길찾기를 수행
        /// </summary>
        // in => ref readonly와 동일하게 작동하지만(컴파일러가 그렇게 처리한다) 함수에서의 호환성이 더 좋다.
        public static float3[] RequestPathImmediate(Vector3 startPos, Vector3 endPos, in NativeHashMap<long, (long Navi, long Link)> nativeMap)
        {
            NativeList<float3> resultPath = new NativeList<float3>(Allocator.TempJob);
            List<float3> result = new List<float3>();

            try
            {
                AStarPathJob job = new AStarPathJob()
                {
                    StartPos   = startPos,
                    EndPos     = endPos,
                    Radius     = MapSamplingData.ENTITY_SEARCH_RADIUS,
                    Map        = nativeMap,
                    ResultPath = resultPath
                };

                job.Execute();

                for (int i = 0; i < resultPath.Length; ++i)
                {
                    result.Add(resultPath[i]);
                }
            }
            finally
            {
                if (true == resultPath.IsCreated)
                {
                    resultPath.Dispose();
                }
            }

            return result.ToArray();
        }
    }
}