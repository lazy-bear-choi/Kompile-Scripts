namespace Script.Map.Runtime
{
    using System.Collections.Specialized;
    using System.Reflection;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;

    /// <summary>
    /// 타일 기반의 A* 알고리즘을 초고속으로 계산하는 Job system struct;
    /// </summary>
    [BurstCompile]
    public class AStarPathJob : IJob
    {
        private const float PATH_SERACH_UNIT = MapSamplingData.HEIGHT_MULTIPLIER; // == 0.125f;
        private const int PATH_SEARCH_RECIPROCAL = MapPathUtil.SIZE_TILE; // == 8

        private const int LINK_ZERO = 0b_01;
        private const int LINK_UP = 0b_10;
        private const int LINK_DOWN = 0b_11;

        // -- internal structs --
        private struct PathVerticeNode
        {
            public int3 VerticeInt;
            public int ParentIndex;
            public float G;
            public float H;

            public readonly float F => G + H;
            public readonly float3 Vertice
            {
                get
                {
                    return PATH_SERACH_UNIT * new float3(VerticeInt.x, VerticeInt.y, VerticeInt.z);
                }
            }
        }

        private static readonly int3[] NEIGHBOR_OFFSETS_INT = new int3[]
        {
            new int3(-2, 0, -2), new int3(0, 0, -4), new int3( 2, 0, -2), new int3( 4, 0, 0),
            new int3( 2, 0, 2),  new int3(0, 0,  4), new int3(-2, 0,  2), new int3(-4, 0, 0)
        };

        // -- input data --
        [ReadOnly] public float3 StartPos;
        [ReadOnly] public float3 EndPos;
        [ReadOnly] public float Radius;
        [ReadOnly] public NativeHashMap<long, (long NaviMask, long LinkMask)> Map;

        // -- output data --
        public NativeList<float3> ResultPath;


        public void Execute()
        {
            var allNodes = new NativeList<PathVerticeNode>(Allocator.Temp);
            var gCosts   = new NativeHashMap<int3, float>(1024, Allocator.Temp);
            var openHeap = new NativeList<int>(Allocator.Temp);

            int3 startVerticeInt = GetVerticeInt(StartPos);

            long startID = MapPathUtil.ComputeTileIDInt(startVerticeInt);
            if (false == Map.ContainsKey(startID))
            {
                goto DISPOSE;
            }

            allNodes.Add(new PathVerticeNode 
            {
                VerticeInt = startVerticeInt,
                ParentIndex = -1,
                G = 0,
                H = math.distance(StartPos, EndPos)
            });
            openHeap.Add(0);
            gCosts.Add(startVerticeInt, 0);

            int nextIndex;

            while (0 < openHeap.Length)
            {
                int currIndex = PopMinHeap(ref openHeap, ref allNodes);
                PathVerticeNode currentNode = allNodes[currIndex];
                int3 currentVerticeInt = currentNode.VerticeInt;

                // 현재 탐색한 경로보다 더 멀다면 break;
                if (true == gCosts.TryGetValue(currentVerticeInt, out float bestG)
                    && currentNode.G > bestG)
                {
                    continue;
                }

                // 탐색 범위 안에 목적지가 있다면 탐색 종료;
                if (PATH_SERACH_UNIT >= math.distance(currentNode.Vertice, EndPos))
                {
                    ReconstructPath(currIndex, allNodes);
                    break;
                }

                // 현재 위치한 타일 확인
                long currentID = MapPathUtil.ComputeTileIDInt(currentVerticeInt);
                if (false == Map.TryGetValue(currentID, out (long NaviMask, long LinkMask) currentItem))
                {
                    continue;
                }

                // 주변 타일 확인
                for (int i = 0; i < NEIGHBOR_OFFSETS_INT.Length; ++i)
                {
                    int3 targetVerticeInt = currentVerticeInt + NEIGHBOR_OFFSETS_INT[i];
                    long targetID = MapPathUtil.ComputeTileIDInt(targetVerticeInt);

                    (long NaviMask, long LinkMask) targetItem = currentItem;

                    if (currentID != targetID)
                    {
                        int3 diffInt = GetTileDiff(currentID, targetID);
                        int yMask;

                        switch ((diffInt.x, diffInt.z))
                        {
                            case (-1, -1): yMask = 0; break;
                            case ( 0, -1): yMask = 1; break;
                            case ( 1, -1): yMask = 2; break;
                            case ( 1,  0): yMask = 3; break;
                            case ( 1,  1): yMask = 4; break;
                            case ( 0,  1): yMask = 5; break;
                            case (-1,  1): yMask = 6; break;
                            case (-1,  0): yMask = 7; break;
                            default: continue;
                        }

                        int y = (int)(currentItem.LinkMask >> (yMask * 2)) & 0b11;
                        switch (y)
                        {
                            case LINK_ZERO: y = 0; break;
                            case LINK_UP: y = 1; break;
                            case LINK_DOWN: y = -1; break;
                            default: continue;
                        }

                        targetVerticeInt += PATH_SEARCH_RECIPROCAL * new int3(0, y, 0);
                        targetID = MapPathUtil.ComputeTileIDInt(targetVerticeInt);

                        if(false == Map.TryGetValue(targetID, out targetItem))
                        {
                            continue;
                        }
                    }

                    int baseLayerY = (int)math.floor((float)targetVerticeInt.y / PATH_SEARCH_RECIPROCAL) * PATH_SEARCH_RECIPROCAL;
                    int3 targetPivotInt = PATH_SEARCH_RECIPROCAL * MapPathUtil.ComputeWorldPositionInt(targetID);
                    int3 localPos = targetVerticeInt - targetPivotInt;

                    int vIndex = MapPathUtil.GetVertexIndexFromLocalPos(localPos.x, localPos.z);
                    if (-1 != vIndex)
                    {
                        int heightY = MapPathUtil.GetHeightFromNaviMask(targetItem.NaviMask, vIndex);
                        if ((int)MapSamplingData.VERTEX_MISSING_FLAG == heightY)
                        {
                            continue;
                        }
                        targetVerticeInt.y = baseLayerY + heightY;
                    }

                    float3 targetPivot = MapPathUtil.ComputeWorldPosition(targetID);
                    targetPivotInt = PATH_SEARCH_RECIPROCAL * MapPathUtil.ComputeWorldPositionInt(targetID);
                    float3 circleCenter = PATH_SERACH_UNIT * new float3(targetVerticeInt.x, targetVerticeInt.y, targetVerticeInt.z);

                    if (false == IsVerticeMovable(targetItem.NaviMask, targetPivot, circleCenter, Radius))
                    {
                        continue;
                    }

                    int verticeIndex = GetVerticeIndex(targetVerticeInt - targetPivotInt);
                    if (false == TryGetNeighborLinkIndex(verticeIndex, out int linkIndex, out int length))
                    {
                        continue;
                    }
                    else if (0 == length)
                    {
                        goto ADD_PATH;
                    }

                    for (int l = 0; l < length; ++l)
                    {
                        int index = (linkIndex + l + 8) % 8;
                        int x, y, z;

                        switch (index)
                        {
                            case 0: x = -1; z = -1; break;
                            case 1: x = 0; z = -1; break;
                            case 2: x = 1; z = -1; break;
                            case 3: x = 1; z = 0; break;
                            case 4: x = 1; z = 1; break;
                            case 5: x = 0; z = 1; break;
                            case 6: x = -1; z = 1; break;
                            case 7: x = -1; z = 0; break;
                            default: goto CONTINUE;
                        }

                        long tempID = MapPathUtil.ComputeTileIDInt(targetPivotInt + PATH_SEARCH_RECIPROCAL * new int3(x, 0, z));
                        var tempInt = MapPathUtil.ComputeWorldPositionInt(tempID);
                        if (false == MapPathUtil.IsCircleOverlappingSquare(tempInt, new float2(circleCenter.x, circleCenter.z), Radius))
                        {
                            continue;
                        }

                        if (false == MapPathUtil.TryGetYInt(targetItem.LinkMask, index, out y))
                        {
                            goto CONTINUE;
                        }

                        int3 neighborPivotInt = targetPivotInt + PATH_SEARCH_RECIPROCAL * new int3(x, y, z);
                        long neighborID = MapPathUtil.ComputeTileIDInt(neighborPivotInt);

                        if (false == Map.TryGetValue(neighborID, out var neighborItem))
                        {
                            goto CONTINUE;
                        }
                        if (false == IsVerticeMovable(neighborItem.NaviMask, neighborPivotInt, circleCenter, Radius))
                        {
                            goto CONTINUE;
                        }
                    }

                ADD_PATH:
                    float newG = currentNode.G + GetMoveCost(i);
                    bool foundBetterPath = false;

                    if (gCosts.TryGetValue(targetVerticeInt, out float oldG))
                    {
                        if (newG < oldG)
                        {
                            gCosts[targetVerticeInt] = newG;
                            foundBetterPath = true;
                        }
                    }
                    else
                    {
                        gCosts.Add(targetVerticeInt, newG);
                        foundBetterPath = true;
                    }

                    if (true == foundBetterPath)
                    {
                        allNodes.Add(new PathVerticeNode()
                        {
                            VerticeInt = targetVerticeInt,
                            ParentIndex = currIndex,
                            G = newG,
                            H = math.distance(targetVerticeInt, EndPos)
                        });

                        nextIndex = allNodes.Length - 1;
                        PushMinHeap(ref openHeap, ref allNodes, nextIndex);
                    }
                    CONTINUE:
                    continue;
                }
            }

        DISPOSE:
            allNodes.Dispose();
            gCosts.Dispose();
            openHeap.Dispose();
        }

        private int3 GetVerticeInt(float3 p)
        {
            int x = (int)math.round(p.x * PATH_SEARCH_RECIPROCAL);
            int y = (int)math.round(p.y * PATH_SEARCH_RECIPROCAL);
            int z = (int)math.round(p.z * PATH_SEARCH_RECIPROCAL);

            return new int3(x, y, z);
        }
        private int3 GetTileDiff(long idFrom, long idTo)
        {
            const int SHIFT_GRID_X = 48;
            const int SHIFT_GRID_Z = 32;
            const int SHIFT_TILE_X = 12;
            const int SHIFT_TILE_Z =  0;

            int gX1 = (sbyte)((idFrom >> SHIFT_GRID_X) & 0xFF);
            int gZ1 = (sbyte)((idFrom >> SHIFT_GRID_Z) & 0xFF);

            int gX2 = (sbyte)((idTo >> SHIFT_GRID_X) & 0xFF);
            int gZ2 = (sbyte)((idTo >> SHIFT_GRID_Z) & 0xFF);

            int tX1 = (int)((idFrom >> SHIFT_TILE_X) & MapPathUtil.TILE_MASK);
            int tZ1 = (int)((idFrom >> SHIFT_TILE_Z) & MapPathUtil.TILE_MASK);

            int tX2 = (int)((idTo >> SHIFT_TILE_X) & MapPathUtil.TILE_MASK);
            int tZ2 = (int)((idTo >> SHIFT_TILE_Z) & MapPathUtil.TILE_MASK);

            int diffX = ((gX2 - gX1) << MapPathUtil.TILE_BITS) + (tX2 - tX1);
            int diffZ = ((gZ2 - gZ1) << MapPathUtil.TILE_BITS) + (tZ2 - tZ1);

            return new int3(diffX, 0, diffZ);
        }
        private int GetVerticeIndex(int3 diffInt)
        {
            switch ((diffInt.x, diffInt.z))
            {
                case (0, 0): return 0;
                case (4, 0): return 1;
                case (8, 0): return 2;
                case (2, 2): return 3;
                case (6, 2): return 4;
                case (0, 4): return 5;
                case (4, 4): return 6;
                case (8, 4): return 7;
                case (2, 6): return 8;
                case (6, 6): return 9;
                case (0, 8): return 10;
                case (4, 8): return 11;
                case (8, 8): return 12;
                default: break;
            }
            return -1;
        }

        private bool TryGetNeighborLinkIndex(int verticeIndex, out int linkIndex, out int length)
        {
            linkIndex = 0;
            length = 0;

            switch (verticeIndex)
            {
                case 0: case 3: 
                    linkIndex = 7; length = 3; 
                    break;
                case 1: 
                    linkIndex = 1; length = 1; 
                    break;
                case 2: case 4: 
                    linkIndex = 1; length = 3; 
                    break;
                case 6:
                    break;
                case 7: 
                    linkIndex = 3; length = 1; 
                    break;
                case 9: case 12: 
                    linkIndex = 3; length = 3; 
                    break;
                case 11: 
                    length = 1; 
                    break;
                case 8: case 10: 
                    linkIndex = 5; length = 3;
                    break;
                case 5: 
                    linkIndex = 7; length = 1; 
                    break;
                default:
                    return false;
            }

            return true;
        }
        private bool IsVerticeMovable(long naviMask, float3 tilePivot, float3 circleCenter, float radius)
        {
            float2 localCircleCenter = new float2(circleCenter.x - tilePivot.x, circleCenter.z - tilePivot.z);
            float radiusSq = radius * radius;

            for (int sIndex = 0; sIndex < 16; ++sIndex)
            {
                if (false == MapPathUtil.IsCircleOverlappingSubTile(sIndex, localCircleCenter, radiusSq))
                {
                    continue;
                }
                if (false == MapPathUtil.IsSubTileValid(naviMask, sIndex))
                {
                    return false;
                }
            }

            return true;
        }

        private int PopMinHeap(ref NativeList<int> heap, ref NativeList<PathVerticeNode> nodes)
        {
            int result = heap[0];
            int last = heap.Length - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);

            int i = 0;
            int length = heap.Length;
            while (true)
            {
                int smallest = i;
                int left = 2 * i + 1;
                int right = 2 * i + 2;

                if (left < length
                    && nodes[heap[left]].F < nodes[heap[smallest]].F)
                {
                    smallest = left;
                }
                if (right < length
                    && nodes[heap[right]].F < nodes[heap[smallest]].F)
                {
                    smallest = right;
                }
                if (i == smallest)
                {
                    break;
                }

                int temp = heap[i];
                heap[i] = heap[smallest];
                heap[smallest] = temp;
                i = smallest;
            }

            return result;
        }
        private void PushMinHeap(ref NativeList<int> heap, ref NativeList<PathVerticeNode> nodes, int index)
        {
            heap.Add(index);
            int i = heap.Length - 1;
            while (0 < i)
            {
                int p = (int)((i - 1) * 0.5f);
                if (nodes[heap[i]].F >= nodes[heap[p]].F)
                {
                    break;
                }

                // swap
                (heap[i], heap[p]) = (heap[p], heap[i]);
                i = p;
            }
        }
        private float GetMoveCost(int i)
        {
            return i switch
            {
                1 or 3 or 5 or 7 => PATH_SERACH_UNIT * 4, // 상하좌우
                _ => PATH_SERACH_UNIT * 2 * math.sqrt(2)  // 대각선
            };
        }
        private void ReconstructPath(int endIndex, NativeList<PathVerticeNode> nodes)
        {
            int curr = endIndex;
            while (-1 != curr)
            {
                float3 pathPos = nodes[curr].Vertice;
                ResultPath.Add(pathPos);
                curr = nodes[curr].ParentIndex;
            }

            int count = ResultPath.Length;
            int half = count >> 1; //언제나 0 이상의 양수이므로 /2 대신에 >>1 연산도 괜찮다.
            for (int i = 0; i < half; ++i)
            {
                int swapIndex = count - 1 - i;
                float3 temp = ResultPath[i];
                ResultPath[i] = ResultPath[swapIndex];
                ResultPath[swapIndex] = temp;
            }
        }
    }
}