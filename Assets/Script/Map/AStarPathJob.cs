namespace Script.Map
{
    using Unity.Jobs;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Mathematics;

    [BurstCompile]
    public class AStarPathJob : IJob
    {
        private const float SUBTILE_SIZE = 0.25f;
        private const int   SUBTILE_UNIT = 4;   // 0.25f 단위로 양자화 (1f/0.25f = 4;)

        // --- input data ---
        [ReadOnly] public float3 StartPos;
        [ReadOnly] public float3 EndPos;
        [ReadOnly] public float  Radius = 0.45f;
        [ReadOnly] public NativeHashMap<long, (long,long)> Map; // (Key:TileID, Value:NaviMask)

        // --- output data ---
        public NativeList<float3> ResultPath;

        // --- internal structs ----
        private struct PathNode
        {
            public int3 GridPosInt;     // 정수형 좌표 (부동 소수점 오차 방지)
            public int ParentIndex;     // 경로 역추적을 위한 부모 노드 인덱스
            public float G;             // 지금까지 온 거리
            public float H;             // 앞으로 갈 거리

            public float F => G + H;    // 거리 비용 총합
        }

        // A* 방향 벡터 (상하좌우 + 대각선): 0.25f 단위 이동을 정수 좌표(1)로 표현 (이 규칙도 꼬인 느낌인데)
        private static readonly int3[] NEIGHBOR_OFFSETS = new int3[]
        {
            new int3(-1, 0, -1), new int3(0, 0, -1), new int3( 1, 0, -1), new int3( 1, 0, 0),
            new int3( 1, 0, 1),  new int3(0, 0,  1), new int3(-1, 0,  1), new int3(-1, 0, 0)
        };

        public void Execute()
        {
            // init data
            int3 startGridInt = WorldToGrid(StartPos);
            var allNodes   = new NativeList<PathNode>(Allocator.Temp);
            var closedSet  = new NativeHashMap<int3, int>(1024, Allocator.Temp);
            var openHeap   = new NativeList<int>(Allocator.Temp);


            // add start node
            allNodes.Add(new PathNode
            {
                GridPosInt = startGridInt,
                ParentIndex = -1,
                G = 0,
                H = math.distance(StartPos, EndPos)
            });
            openHeap.Add(0);
            closedSet.Add(startGridInt, 0);


            // A* Loop
            while (0 < openHeap.Length)
            {
                int currIndex = PopMinHeap(ref openHeap, ref allNodes);
                PathNode current = allNodes[currIndex];
                float3 currWorldPos = GridToWorld(current.GridPosInt);

                if (SUBTILE_SIZE >= math.distance(currWorldPos, EndPos))
                {
                    ReconstructPath(currIndex, allNodes);
                    ResultPath.Add(EndPos);
                    break;
                }

                // 탐색 위치 (current) => Tile 조회
                long currentLinkKey = EditMapUtil.ComputeID(currWorldPos);
                if (false == Map.TryGetValue(currentLinkKey, out (long navi, long link) item))
                {
                    continue;
                }

                // 탐색 위치 (current) => Sub-Tile 조회
                int subareaIndex = MapPathUtil.GetSubTileIndex(currWorldPos);
                if (false == MapPathUtil.IsSubTileValid(item.navi, subareaIndex))
                {
                    continue;
                }

                // 이웃 노드 탐색
                for (int i = 0; i < NEIGHBOR_OFFSETS.Length; ++i)
                {
                    // link 확인 (i번째 방향으로 길이 열려 있는가?)
                    if (0 == (item.link & (0b10 << i * 2)))
                    {
                        continue;
                    }

                    // 물리적 공간(Radius) 확인
                    int yMask = (int)(item.link >> (i * 2)) & 0b11;
                    int yInt; 
                    switch(yMask)
                    {
                        case 0b00: yInt = 0; break;
                        case 0b01: yInt = 1; break;
                        case 0b11: yInt = -1; break;
                        default:
                            continue;
                    }

                    int3 nextGridInt = current.GridPosInt + new int3(NEIGHBOR_OFFSETS[i].x, yInt, NEIGHBOR_OFFSETS[i].z);
                    float3 nextWorldPos = GridToWorld(nextGridInt);
                    if (false == IsPositionWalkable(nextWorldPos))
                    {
                        continue;
                    }

                    // 이미 방문한 노드인지 확인
                    if (true == closedSet.ContainsKey(nextGridInt))
                    {
                        continue;
                    }

                    allNodes.Add(new PathNode
                    {
                        GridPosInt = nextGridInt,
                        ParentIndex = currIndex,
                        G = current.G + GetMoveCost(i),
                        H = math.distance(nextWorldPos, EndPos)
                    });
                    int nextIndex = allNodes.Length - 1;
                    closedSet.Add(nextGridInt, nextIndex);
                    PushMinHeap(ref openHeap, ref allNodes, nextIndex);
                }
            }

            // dispose native
            allNodes.Dispose();
            closedSet.Dispose();
            openHeap.Dispose();
        }

        private int3 WorldToGrid(float3 p)
        {
            int x = (int)math.round(p.x * SUBTILE_UNIT);
            int y = (int)math.round(p.y * SUBTILE_UNIT);
            int z = (int)math.round(p.z * SUBTILE_UNIT);

            return new int3(x, y, z);
        }
        private float3 GridToWorld(int3 g)
        {
            float3 offset = new float3(0.125f, 0.125f, 0.125f);
            float3 p = new float3(g.x, g.y, g.z);

            return (SUBTILE_SIZE * p) + offset;
        }
        private bool IsPositionWalkable(float3 pos)
        {
            float rSquare = Radius * Radius;
            float3 minBound = pos - new float3(Radius, 0, Radius);
            float3 maxBound = pos + new float3(Radius, 0, Radius);

            int minX = (int)math.floor(minBound.x * SUBTILE_UNIT);
            int minZ = (int)math.floor(minBound.z * SUBTILE_UNIT);

            int maxX = (int)math.floor(maxBound.x * SUBTILE_UNIT);
            int maxZ = (int)math.floor(maxBound.z * SUBTILE_UNIT);

            const float offset = 0.125f;
            for (int x = minX; x <= maxX; ++x)
            {
                for (int z = minZ; z <= maxZ; ++z)
                {
                    // 일단은 XZ로만 이동 여부를 결정한다.
                    float3 subC = new float3(x * SUBTILE_SIZE + offset, pos.y, z * SUBTILE_SIZE + offset);
                    if (rSquare < math.lengthsq(new float2(subC.x - pos.x, subC.z - pos.z)))
                    {
                        continue;
                    }
                    if (false == Map.TryGetValue(EditMapUtil.ComputeID(subC), out (long navi, long link) item))
                    {
                        continue;
                    }
                    if (false == MapPathUtil.IsSubTileValid(item.navi, MapPathUtil.GetSubTileIndex(subC)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void PushMinHeap(ref NativeList<int> heap, ref NativeList<PathNode> nodes, int index)
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

                int temp = heap[i];
                heap[i] = heap[p];
                heap[p] = temp;

                i = p;
            }
        }
        private int PopMinHeap(ref NativeList<int> heap, ref NativeList<PathNode> nodes)
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
                int left     = 2 * i + 1;
                int right    = 2 * i + 2;

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
                if (smallest == i)
                {
                    break;
                }

                // swap
                int temp = heap[i];
                heap[i] = heap[smallest];
                heap[smallest] = temp;

                i = smallest;
            }

            return result;
        }
        private void ReconstructPath(int endIndex, NativeList<PathNode> nodes)
        {
            int curr = endIndex;
            while (-1 != curr)
            {
                float3 pathPos = GridToWorld(nodes[curr].GridPosInt);
                ResultPath.Add(pathPos);

                curr = nodes[curr].ParentIndex;
            }

            // in-place reverse
            int count = ResultPath.Length;
            int half = (int)(count * 0.5f);
            for (int i = 0; i < half; ++i)
            {
                float3 temp = ResultPath[i];
                ResultPath[i] = ResultPath[count - 1 - i];
                ResultPath[count - 1 - i] = temp;
            }
        }

        private float GetMoveCost(int i)
        {
            return i switch
            {
                1 or 3 or 5 or 7 => SUBTILE_SIZE,   // 직선 (상하좌우)
                _ => 0.3535f //SUBTILE_SIZE * math.sqrt(2),   // 대각선
            };
        }
    }
}