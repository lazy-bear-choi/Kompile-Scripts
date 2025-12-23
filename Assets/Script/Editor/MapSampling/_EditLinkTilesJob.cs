#if UNITY_EDITOR
using Script.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct EditLinkTilesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<long> KeyArray;
    [ReadOnly] public NativeHashMap<long, EditMapTileData> Map;

    [WriteOnly] public NativeArray<EditMapTileData> Results;

    [ReadOnly] public NativeArray<float2> LinkDirs;
    [ReadOnly] public NativeArray<float> DiffYs;

    public void Execute(int index)
    {
        long myID = KeyArray[index];
        if (false == Map.TryGetValue(myID, out EditMapTileData myTile))
        {
            return;
        }

        int accumulatedLinkMask = 0;
        float3 myPivot = EditMapUtil.ComputeWorldPosition(myID);

        for (int i = 0; i < LinkDirs.Length; i++)
        {
            float2 dir2D = LinkDirs[i];
            DirFlag dirFlag = EditMapUtil.GetDirFlag(dir2D.x, dir2D.y);

            for (int y = 0; y < DiffYs.Length; y++)
            {
                float dy = DiffYs[y];
                float3 targetDir = new float3(dir2D.x, dy, dir2D.y);
                long neighborID = EditMapUtil.ComputeID(myPivot + targetDir);

                if (true == Map.TryGetValue(neighborID, out EditMapTileData neighborTile))
                {
                    if (true == CheckConnectable(myTile, neighborTile, targetDir, dirFlag))
                    {
                        const int LINK_ZERO = 0b_01;
                        const int LINK_UP = 0b_10;
                        const int LINK_DOWN = 0b_11;

                        int linkValue = dy switch
                        {
                            0 => LINK_ZERO,
                            1 => LINK_UP,
                            -1 => LINK_DOWN,
                            _ => 0
                        };

                        accumulatedLinkMask |= (linkValue << EditMapUtil.GetLinkMaskShift(dirFlag));
                        break;
                    }
                }
            }
        }

        myTile.LinkMask = (ushort)accumulatedLinkMask;
        Results[index] = myTile;
    }

    private readonly bool CheckConnectable(EditMapTileData myTile, EditMapTileData neighborTile, float3 dir, DirFlag dirFlag)
    {
        // 직선 방향 체크
        if (true == IsSingleDirection(dirFlag))
        {
            return IsSingleLinked(dirFlag, myTile, neighborTile);
        }

        // 대각선 체크 (Chain Link)
        DirFlag first = dirFlag & (DirFlag.LEFT | DirFlag.RIGHT);
        DirFlag second = dirFlag & (DirFlag.UP | DirFlag.DOWN);

        return IsChainLinked(myTile, neighborTile, first, second) &&
               IsChainLinked(myTile, neighborTile, second, first);
    }
    private readonly bool IsSingleDirection(DirFlag dirFlag)
    {
        int count = 0;

        if (0 != (dirFlag & DirFlag.LEFT)) { count += 1; }
        if (0 != (dirFlag & DirFlag.RIGHT)) { count += 1; }
        if (0 != (dirFlag & DirFlag.UP)) { count += 1; }
        if (0 != (dirFlag & DirFlag.DOWN)) { count += 1; }

        return count == 1;
    }
    private readonly bool IsSingleLinked(DirFlag direction, EditMapTileData myTile, EditMapTileData neighborTile)
    {
        EditVertexIndexInfo myV = EditMapUtil.GetVertexIndexInfo(direction);

        var neighborDir = direction switch
        {
            DirFlag.LEFT => DirFlag.RIGHT,
            DirFlag.RIGHT => DirFlag.LEFT,
            DirFlag.UP => DirFlag.DOWN,
            DirFlag.DOWN => DirFlag.UP,
            _ => throw new System.ArgumentException()
        };
        EditVertexIndexInfo neighborV = EditMapUtil.GetVertexIndexInfo(neighborDir);

        // 부동소수점 이슈를 줄이고자 x1000 을 하여 정수로 만듦;
        int myHeightX1000, neighborHeightX1000;

        // 중앙 비교 (.center)
        if (false == myTile.TryGetVerticeHeight(myV.center, out myHeightX1000)
            || false == neighborTile.TryGetVerticeHeight(neighborV.center, out neighborHeightX1000))
        {
            return false;
        }
        // 중앙은 서로 높이가 다르만 연결이 불가하다 => 곧장 false 처리
        if (myHeightX1000 != neighborHeightX1000)
        {
            return false;
        }

        // 양옆 비교(.side0, .side1): 둘 중 하나라도 높이가 같다면 '절반이라도' 이어질 수 있다.
        bool compare = false;
        if (true == myTile.TryGetVerticeHeight(myV.side0, out myHeightX1000)
            && true == neighborTile.TryGetVerticeHeight(neighborV.side0, out neighborHeightX1000))
        {
            compare |= myHeightX1000 == neighborHeightX1000;
        }
        if (true == myTile.TryGetVerticeHeight(myV.side1, out myHeightX1000)
            && true == neighborTile.TryGetVerticeHeight(neighborV.side1, out neighborHeightX1000))
        {
            compare |= myHeightX1000 == neighborHeightX1000;
        }

        return compare;
    }
    private readonly bool IsChainLinked(EditMapTileData startTile, EditMapTileData targetTile, DirFlag first, DirFlag second)
    {
        float3 startPivot = EditMapUtil.ComputeWorldPosition(startTile.ID);
        float3 midPivot = startPivot + EditMapUtil.GetDirectionVector(first);

        long midID = EditMapUtil.ComputeID(midPivot);
        if (false == Map.TryGetValue(midID, out EditMapTileData midTile))
        {
            return false;
        }

        return IsSingleLinked(first, startTile, midTile)
               && IsSingleLinked(second, midTile, targetTile);
    }
}
#endif