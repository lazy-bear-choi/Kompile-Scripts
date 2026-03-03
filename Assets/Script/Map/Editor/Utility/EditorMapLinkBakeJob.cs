#if UNITY_EDITOR
namespace Script.Map.Editor
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Script.Map.Editor.Data;
    using Script.Map.Editor.Utility;

    /// <summary>
    /// [Framework: Utility/Job] [Editor Only]
    /// 주변 타일과의 기하학적 연결(Link) 상태를 판정하고 마스크를 생성하는 순수 연산 구조체입니다.
    /// </summary>
    [BurstCompile]
    public struct EditorMapLinkBakeJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<long> KeyArray;
        [ReadOnly] public NativeHashMap<long, EditorMapTileData> Map;

        [WriteOnly] public NativeArray<EditorMapTileData> Results;

        [ReadOnly] public NativeArray<float2> LinkDirs;
        [ReadOnly] public NativeArray<float> DiffYs;

        public void Execute(int index)
        {
            long myID = KeyArray[index];
            if (false == Map.TryGetValue(myID, out EditorMapTileData myTile))
            {
                return;
            }

            int accumulatedLinkMask = 0;

            // [Kompile] 원칙 적용 완료: (0,0,0) 기준 절대 좌표 변환
            float3 myPivot = EditorMapUtil.ComputeWorldPosition(myID);

            for (int i = 0; i < LinkDirs.Length; i++)
            {
                float2 dir2D = LinkDirs[i];
                EditorMapTileDirFlag dirFlag = EditorMapUtil.GetDirFlag(dir2D.x, dir2D.y);

                for (int y = 0; y < DiffYs.Length; y++)
                {
                    float dy = DiffYs[y];
                    float3 targetDir = new float3(dir2D.x, dy, dir2D.y);

                    // [Kompile] 원칙 적용 완료: 음수 좌표에서도 정확한 Tile ID 추출
                    long neighborID = EditorMapUtil.ComputeTileID(myPivot + targetDir);

                    if (true == Map.TryGetValue(neighborID, out EditorMapTileData neighborTile))
                    {
                        if (true == CheckConnectable(myTile, neighborTile, dirFlag)) // targetDir 매개변수는 사용하지 않아 제거
                        {
                            const int LINK_ZERO = 0b_01;
                            const int LINK_UP = 0b_10;
                            const int LINK_DOWN = 0b_11;
                            const int LINK_NONE = 0b_00;

                            // float 비교 대신 근사치 혹은 명시적 캐스팅 사용 권장
                            int intDy = (int)math.round(dy);
                            int linkValue = intDy switch
                            {
                                0 => LINK_ZERO,
                                1 => LINK_UP,
                                -1 => LINK_DOWN,
                                _ => LINK_NONE
                            };

                            accumulatedLinkMask |= (linkValue << EditorMapUtil.GetLinkMaskShift(dirFlag));

                            // [주의] 이 break는 해당 방향(dir2D)에 대해 하나의 연결(dy)만 찾고 끝냄을 의미합니다.
                            break;
                        }
                    }
                }
            }

            myTile.LinkMask = (ushort)accumulatedLinkMask;
            Results[index] = myTile;
        }

        private bool CheckConnectable(EditorMapTileData myTile, EditorMapTileData neighborTile, EditorMapTileDirFlag dirFlag)
        {
            if (true == IsSingleDirection(dirFlag))
            {
                return IsSingleLinked(dirFlag, myTile, neighborTile);
            }

            EditorMapTileDirFlag first = dirFlag & (EditorMapTileDirFlag.LEFT | EditorMapTileDirFlag.RIGHT);
            EditorMapTileDirFlag second = dirFlag & (EditorMapTileDirFlag.UP | EditorMapTileDirFlag.DOWN);

            return IsChainLinked(myTile, neighborTile, first, second) &&
                   IsChainLinked(myTile, neighborTile, second, first);
        }

        private bool IsSingleDirection(EditorMapTileDirFlag dirFlag)
        {
            int count = 0;
            if ((dirFlag & EditorMapTileDirFlag.LEFT) != 0) { count += 1; }
            if ((dirFlag & EditorMapTileDirFlag.RIGHT) != 0) { count += 1; }
            if ((dirFlag & EditorMapTileDirFlag.UP) != 0) { count += 1; }
            if ((dirFlag & EditorMapTileDirFlag.DOWN) != 0) { count += 1; }
            return count == 1;
        }

        private bool IsSingleLinked(EditorMapTileDirFlag direction, EditorMapTileData myTile, EditorMapTileData neighborTile)
        {
            // [교정 완료] 리팩토링된 GetEdgeVertices 적용
            EditorEdgeVertices myEdge = EditorMapUtil.GetEdgeVertices(direction);

            // [Burst 호환성 수정] Managed Exception 제거 및 안전한 기본값 처리
            EditorMapTileDirFlag neighborDir = EditorMapTileDirFlag.NONE; // 초기값 NONE (사용자 정의 플래그라 가정)
            switch (direction)
            {
                case EditorMapTileDirFlag.LEFT: neighborDir = EditorMapTileDirFlag.RIGHT; break;
                case EditorMapTileDirFlag.RIGHT: neighborDir = EditorMapTileDirFlag.LEFT; break;
                case EditorMapTileDirFlag.UP: neighborDir = EditorMapTileDirFlag.DOWN; break;
                case EditorMapTileDirFlag.DOWN: neighborDir = EditorMapTileDirFlag.UP; break;
            }

            // 잘못된 방향 플래그가 들어왔을 경우 방어 코드
            // (NONE 플래그가 없다면, direction 플래그 자체를 검증하는 로직으로 대체해야 함)
            // if (neighborDir == EditorMapTileDirFlag.NONE) return false; 

            EditorEdgeVertices neighborEdge = EditorMapUtil.GetEdgeVertices(neighborDir);

            int myHeightX1000, neighborHeightX1000;

            if (false == myTile.TryGetVerticeHeight(myEdge.center, out myHeightX1000)
                || false == neighborTile.TryGetVerticeHeight(neighborEdge.center, out neighborHeightX1000))
            {
                return false;
            }

            if (myHeightX1000 != neighborHeightX1000)
            {
                return false;
            }

            bool compare = false;
            if (true == myTile.TryGetVerticeHeight(myEdge.side0, out myHeightX1000)
                && true == neighborTile.TryGetVerticeHeight(neighborEdge.side0, out neighborHeightX1000))
            {
                compare |= myHeightX1000 == neighborHeightX1000;
            }
            if (true == myTile.TryGetVerticeHeight(myEdge.side1, out myHeightX1000)
                && true == neighborTile.TryGetVerticeHeight(neighborEdge.side1, out neighborHeightX1000))
            {
                compare |= myHeightX1000 == neighborHeightX1000;
            }

            return compare;
        }

        private bool IsChainLinked(EditorMapTileData startTile, EditorMapTileData targetTile, EditorMapTileDirFlag first, EditorMapTileDirFlag second)
        {
            float3 startPivot = EditorMapUtil.ComputeWorldPosition(startTile.ID);

            // [가정] EditorMapUtil에 GetDirectionVector가 (0,0,0) 원점 기반 오프셋을 올바르게 반환한다고 가정
            float3 midPivot = startPivot + EditorMapUtil.GetDirectionVector(first);

            // [Kompile] 원칙 적용: 중간 타일 ID 추출
            long midID = EditorMapUtil.ComputeTileID(midPivot);
            if (false == Map.TryGetValue(midID, out EditorMapTileData midTile))
            {
                return false;
            }

            return IsSingleLinked(first, startTile, midTile)
                   && IsSingleLinked(second, midTile, targetTile);
        }
    }
}
#endif