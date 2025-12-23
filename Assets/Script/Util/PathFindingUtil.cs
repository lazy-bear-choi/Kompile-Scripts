namespace Script.Util.PathFinding
{
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;
    using Script.Data;

    public class PathFindingUtil
    {
        //// Burst-safe 노드 표현 (managed MapTileData와 동일한 필드)
        public struct MapTileNative
        {
            public long Key;
            public long NavMask;
            public int LinkMask;
            public float3 Pivot;
        }

        public static class MapPathfinderBurst
        {
            // 외부(메인 스레드)에서 호출하는 편의 함수
            public static bool FindPathBurst(ConcurrentDictionary<int, MapGridData> mapGrids,
                                             Vector3 startWorld, Vector3 goalWorld,
                                             out List<Vector3> outPath, out float outDistance)
            {

                outPath = null;
                outDistance = 0f;

                Allocator allocator = Allocator.TempJob;
                int maxExpandedNodes = 200000;
                int estimatedNodeCount = 1024;

                var nodeNatives = new NativeList<MapTileNative>(estimatedNodeCount, allocator);
                var nodeKeyToIndex = new NativeHashMap<long, int>(estimatedNodeCount, allocator);

                #region 노드 데이터
                // 노드 데이터 생성
                foreach (var gKV in mapGrids)
                {
                    int gKey = gKV.Key;
                    MapGridData grid = gKV.Value;

                    foreach (var tKV in grid.NaviTileDict)
                    {
                        int tKey = tKV.Key;
                        MapTileData mt = tKV.Value;

                        long nodeKey = (long)(gKey << 32) | (uint)tKey;
                        float3 pivot = MapUtil.GetTilePivotPosition(gKey, tKey);

                        int index = nodeNatives.Length;
                        nodeNatives.Add(new MapTileNative
                        {
                            Key      = nodeKey,
                            NavMask  = mt.NaviMask,
                            LinkMask = mt.LinkMask,
                            Pivot    = pivot
                        });

                        if (false == nodeKeyToIndex.TryAdd(nodeKey, index))
                        {
                            // 혹시 중복값이 발생한다면 --length
                            --nodeNatives.Length;
                        }
                    }
                }

                // 처리할 노드가 없으므로 메모리 해제 및 종료
                if (0 == nodeNatives.Length)
                {
                    nodeNatives.Dispose();
                    nodeKeyToIndex.Dispose();
                    return false;
                }
                #endregion

                #region 이웃 노드 리스트 구성 (CSR format)
                int nodeCount = nodeNatives.Length;

                // 각 노드의 이웃 개수 (ex. [13]번 노드의 이웃은 (count)개)
                var adjacencyCounts = new NativeArray<int>(nodeCount, allocator);

                // 각 노드의 이웃 배열이 indice의 몇 번째부터 시작하는지
                var adjacencyStart = new NativeArray<int>(nodeCount, allocator);

                // 모든 이웃 인덱스를 나열한 배열. (이웃이 대략 7개씩 있다고 가정하고 메모리 초기화)
                var adjacencyIndices = new NativeList<int>(nodeCount * 7, allocator);

                int currentIndicesIndex = 0;
                for (int i = 0; i < nodeCount; ++i)
                {
                    float3 pivot = nodeNatives[i].Pivot;
                    int addCount = 0;

                    for (int o = 0; o < 8; ++o)
                    {
                        if (false == TryGetNeighborTile(nodeNatives[i].LinkMask, o, out float3 neighborOffeset))
                        {
                            continue;
                        }

                        long key = MapUtil.GetTileNodeKey(pivot + neighborOffeset);
                        if (true == nodeKeyToIndex.TryGetValue(key, out int nbIndex))
                        {
                            adjacencyIndices.Add(nbIndex);
                            ++addCount;
                        }
                    }

                    adjacencyCounts[i] = addCount;
                    currentIndicesIndex += addCount;
                }
                #endregion

                #region 시작점, 목표점 인덱스 찾기
                long startKey = MapUtil.GetTileNodeKey(startWorld);
                long goalKey = MapUtil.GetTileNodeKey(goalWorld);

                int startIndex, goalIndex;

                if (false == nodeKeyToIndex.TryGetValue(startKey, out startIndex)
                    || false == nodeKeyToIndex.TryGetValue(goalKey, out goalIndex))
                {
                    // 탐색 실패? 모든 Native Container를 해제
                    nodeNatives.Dispose();
                    adjacencyCounts.Dispose();
                    adjacencyStart.Dispose();
                    adjacencyIndices.Dispose();
                    return false;
                }

                #endregion

                #region Job Schedule
                PathfindingJob job = new PathfindingJob
                {

                };
                #endregion

                return true;
            }

            private static bool TryGetNeighborTile(int linkMask, int quarant, out float3 neighborOffeset)
            {
                neighborOffeset = default;

                if (false == MapUtil.TryGetLinkValue(linkMask, quarant, out float y))
                {
                    return false;
                }

                neighborOffeset = MapUtil.GetNeighborPivotOffset(quarant) + new float3(0f, y, 0f);
                return true;
            }
        }
    }
}