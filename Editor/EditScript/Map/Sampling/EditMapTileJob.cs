#if UNITY_EDITOR
namespace Script.Data
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    public struct EditMapTileJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<byte>   SceneIndex;  // 씬 인덱스
        [ReadOnly] public NativeArray<ushort> RenderLayer; // 레이어 인덱스
        [ReadOnly] public NativeArray<float3> Position; // 타일 좌표
        [ReadOnly] public NativeArray<float>  RotY;      // 타일 회전값 (y축 회전)
        [ReadOnly] public NativeArray<ulong>  Height;    // height mask

        public NativeArray<EditMapTileData> Data;       // result data

        public void Execute(int index)
        {
            //int sceneIndex = SceneIndex[index];  // 나중에 필요하면 추가;
            int layer = RenderLayer[index];
            ulong height = Height[index];

            // 타일 프리팹을 회전하여 배치하는 경우 있음 => 오브젝트 회전값을 반영
            float  rot          = RotY[index];
            float3 rotatedPivot = RotatePivot(Position[index], rot);
            long   naviMask     = RotateHeightMask(height, layer, rot);

            Data[index] = new EditMapTileData()
            {
                ID          = EditMapUtil.ComputeTileID(rotatedPivot),
                NaviMask    = naviMask,
                LinkMask    = default,
                RenderIndex  = (ushort)layer
            };
        }
        private readonly float3 RotatePivot(float3 position, float rot)
        {
            int rotInt = Mathf.RoundToInt(rot);
            float x = position.x;
            float y = position.y;
            float z = position.z;

            switch (rotInt)
            {
                case 90: return new float3(x, y, z - 1);
                case 180: return new float3(x - 1, y, z - 1);
                case 270: return new float3(x - 1, y, z);
                default: break;
            }
            return position;
        }
        private readonly long RotateHeightMask(ulong heightMask, int navLayer, float rotY)
        {
            ulong layerMask = (ulong)navLayer << (EditMapUtil.TOTAL_BITS * EditMapUtil.BITS_PER_CELL);

            ulong[,] matrix = BitmaskToMatrix(heightMask, rotY);
            ulong rotatedHeightMask = MatrixToBitmask(matrix);

            return (long)(layerMask | rotatedHeightMask);

            // inline methods
            ulong[,] BitmaskToMatrix(ulong mask, float rotY)
            {
                int size = EditMapUtil.MATRIX_SIZE;
                ulong cellValue;

                ulong[,] matrix = new ulong[size, size];
                int x, y;
                for (int i = 0; i < EditMapUtil.TOTAL_BITS; ++i)
                {
                    cellValue = mask & Index.MapTileIndex.HEIGHT_MASK;
                    x = EditMapUtil.INDEX_MAP[i].x;
                    y = EditMapUtil.INDEX_MAP[i].y;
                    matrix[x, y] = cellValue;

                    mask >>= EditMapUtil.BITS_PER_CELL;
                }

                int rotInt = Mathf.RoundToInt(rotY);
                rotInt = (rotInt + 360) % 360;

                return RotateMatrix(matrix, rotInt);
            }
            ulong[,] RotateMatrix(ulong[,] matrix, int rot)
            {
                if (rot == 0)
                {
                    return matrix;
                }

                int size = 5;
                ulong[,] rotatedMatrix = new ulong[size, size];

                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        // 현재 버텍스 값
                        ulong vertexValue = matrix[i, j];

                        // 값이 0이 아니면(버텍스가 있으면) 회전 계산
                        if (vertexValue != 0)
                        {
                            int newX = 0;
                            int newY = 0;

                            switch (rot)
                            {
                                case 90:
                                    // 90도 회전 (x, y) -> (-y, x) -> (y, size-1-x)
                                    newX = size - 1 - j;
                                    newY = i;
                                    break;
                                case 180:
                                    // 180도 회전 (x, y) -> (-x, -y) -> (size-1-x, size-1-y)
                                    newX = size - 1 - i;
                                    newY = size - 1 - j;
                                    break;
                                case 270:
                                    // 270도 회전 (x, y) -> (y, -x) -> (j, size-1-i)
                                    newX = j;
                                    newY = size - 1 - i;
                                    break;
                                default:
                                    Debug.LogError("잘못된 회전 각도입니다.");
                                    return matrix;
                            }
                            rotatedMatrix[newX, newY] = vertexValue;
                        }
                    }
                }
                return rotatedMatrix;
            }
            ulong MatrixToBitmask(ulong[,] matrix)
            {
                ulong newMask = 0ul;
                ulong mask;
                int x, y;

                for (int i = 0; i < EditMapUtil.TOTAL_BITS; ++i)
                {
                    x = EditMapUtil.INDEX_MAP[i].x;
                    y = EditMapUtil.INDEX_MAP[i].y;
                    mask = matrix[x, y];

                    newMask |= mask << i * EditMapUtil.BITS_PER_CELL;
                }

                return newMask;
            }
        }
    }
}
#endif