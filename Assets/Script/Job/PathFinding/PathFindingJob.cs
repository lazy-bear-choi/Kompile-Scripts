namespace Script.Util.PathFinding
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using static Script.Util.PathFinding.PathFindingUtil;

    [BurstCompile]
    public struct PathfindingJob : IJob
    {
        [ReadOnly] public NativeArray<MapTileNative> Nodes;
        [ReadOnly] public NativeArray<int> AdjacencyCounts;
        [ReadOnly] public NativeArray<int> AdjacencyStart;
        [ReadOnly] public NativeArray<int> AdjacencyIndices;

        public int StartIndex;
        public int GoalIndex;
        public int MaxExpanded; // 노드를 최대 몇 개까지 사용할거니? 정도의 안전 장치

        public NativeList<int> ResultPath; // 결과: 경로 노드 인덱스 모음

        public void Execute()
        {
            int nodeCount = Nodes.Length;
            Allocator allocator = Allocator.Temp;
            NativeArrayOptions arrayOption = NativeArrayOptions.UninitializedMemory;

            // job 내부에서의 임시 메모리 -> job이 끝나면 자동으로 해제된다.
            NativeArray<float> gScore  = new NativeArray<float>(nodeCount, allocator, arrayOption);
            NativeArray<int> cameFrom  = new NativeArray<int>(nodeCount, allocator, arrayOption);
            NativeArray<bool>  closed  = new NativeArray<bool> (nodeCount, allocator, arrayOption);
            for (int i = 0; i < nodeCount; ++i)
            {
                gScore[i] = float.MaxValue;
                cameFrom[i] = -1;
                closed[i] = false;
            }

            // Open Set (열린 노드 구조)
            NativeList<PathNode> heapNodes = new NativeList<PathNode>();

            // 시작점 설정
            gScore[StartIndex] = 0f;
            float startH = Heuristic(Nodes[StartIndex].Pivot, Nodes[GoalIndex].Pivot); // H = heuristic, 목적지까지의 거리 어림짐작 값
            HeapPush(ref heapNodes, StartIndex, startH);

            // 순회 시작
            int expandedCount = 0;
            bool found = false;

            while (heapNodes.Length > 0)
            {
                // 힙에서 최저 비용 노드를 추출
                int current = HeapPop(ref heapNodes);

                if (true == closed[current])
                {
                    continue;
                }
                if (GoalIndex == current)
                {
                    found = true;
                    break;
                }

                closed[current] = true;
                if (++expandedCount > MaxExpanded)
                {
                    break;
                }

                // 이웃 노드를 순회
                int start = AdjacencyStart[current];
                int count = AdjacencyCounts[current];

                for (int i = 0; i < count; ++i)
                {
                    int neighbor = AdjacencyIndices[start + i];
                    if (true == closed[neighbor])
                    {
                        continue;
                    }

                    // 대상 이웃 -> 비용 계산
                    float dist = Vector3.Distance(Nodes[current].Pivot, Nodes[neighbor].Pivot);

                    float currentG = gScore[current];
                    float tentativeG = currentG + dist;
                    if (tentativeG < currentG)
                    {
                        gScore[neighbor] = tentativeG;
                        cameFrom[neighbor] = current;
                        float f = tentativeG + Heuristic(Nodes[neighbor].Pivot, Nodes[GoalIndex].Pivot);

                        HeapPush(ref heapNodes, neighbor, f);
                    }
                }
            }

            if (true == found)
            {
                NativeList<int> tempStack = new NativeList<int>(allocator);
                int curr = GoalIndex;
                
                while (-1 != curr)
                {
                    tempStack.Add(curr);
                    curr = cameFrom[curr];
                }

                // Start -> Goal 순서대로 ResultPath에 저장
                for (int i = tempStack.Length - 1; i >= 0; --i)
                {
                    ResultPath.Add(tempStack[i]);
                }
                tempStack.Dispose();
            }

            // NativeArray: 임시 메모리는 Allocator.Temp를 사용했으므로 Job 종료 시 자동으로 해제됨
            // NativeList: (Job 내부에서 Allocator.Temp을 했을지라도) 명시적으로 해제하는 것이 안전함
            heapNodes.Dispose();
            gScore.Dispose();
            cameFrom.Dispose();
            closed.Dispose();
        }

        private readonly float Heuristic(float3 a, float3 b)
        {
            return Vector3.Distance(a, b);
        }

        private void HeapPush(ref NativeList<PathNode> heap, int index, float heuristic)
        {
            heap.Add(new PathNode 
            { 
                Index = index, 
                F = heuristic 
            });

            int parent;
            int child = heap.Length - 1;

            while (child > 0)
            {
                parent = (child - 1) >> 1;
                if (heap[child].F >= heap[parent].F)
                {
                    break;
                }

                // swap
                PathNode temp = heap[child];
                heap[child] = heap[parent];
                heap[parent] = temp;

                child = parent;
            }
        }

        /// <summary>
        /// 현재 가장 작은 노드를 꺼내놓고 (result), 그 다음에 다시 가장 작은 값을 위로 올리기 위해 정렬한다
        /// </summary>
        private readonly int HeapPop(ref NativeList<PathNode> heap)
        {
            // 반환할 값(최소 노드의 인덱스)을 미리 저장
            int result = heap[0].Index;

            // 마지막 요소를 루트로 이동 및 삭제
            int last = heap.Length - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);

            // 재정렬 (Shift-Down)
            int parent = 0;
            int length = heap.Length;

            // 가장 우선순위가 높은(==도착점까지 남은 거리가 짧은) 노드가 위로 오도록 정렬(Min Heap)
            while (true)
            {
                int left = parent * 2 + 1;
                int right = parent * 2 + 2;
                int smallest = parent;

                if (left < length
                    && heap[left].F < heap[smallest].F)
                {
                    smallest = left;
                }
                if (right < length
                    && heap[right].F < heap[smallest].F)
                {
                    smallest = right;
                }

                if (parent == smallest)
                {
                    break;
                }

                // Swap
                PathNode temp = heap[parent];
                heap[parent] = heap[smallest];
                heap[smallest] = temp;

                parent = smallest;
            }

            return result;
        }
    }
}