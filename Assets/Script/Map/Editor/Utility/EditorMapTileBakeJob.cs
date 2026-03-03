namespace Script.Map.Editor
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using Script.Map.Runtime;
    using Script.Map.Editor.Utility;
    using Script.Map.Editor.Data;

    [BurstCompile]
    public struct EditorMapTileBakeJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<byte>     SceneIndex;
        [ReadOnly] public NativeArray<ushort>   RenderLayer;
        [ReadOnly] public NativeArray<float3>   Position;
        [ReadOnly] public NativeArray<float>    RotY;
        [ReadOnly] public NativeArray<ulong>    Height;

        public NativeArray<EditorMapTileData> Data;

        public void Execute(int index)
        {
            int layer = RenderLayer[index];
            ulong height = Height[index];

            float rot = RotY[index];
            float3 rotatedPivot = RotatePivot(Position[index], rot);

            // [교정 완료] navieMask -> naviMask 오타 수정
            long naviMask = RotateHeightMask(height, layer, rot);

            Data[index] = new EditorMapTileData()
            {
                ID = EditorMapUtil.ComputeTileID(rotatedPivot),
                NaviMask = naviMask,
                LinkMask = default,
                RenderIndex = (ushort)layer
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
            ulong layerMask = (ulong)navLayer << (EditorMapUtil.TOTAL_BITS * EditorMapUtil.BITS_PER_CELL);

            ulong[,] matrix = BitmaskToMatrix(heightMask, rotY);
            ulong rotatedHeightMask = MatrixToBitmask(matrix);

            return (long)(layerMask | rotatedHeightMask);

            ulong MatrixToBitmask(ulong[,] matrix)
            {
                ulong newMask = 0ul;
                ulong mask;
                int x, y;

                for (int i = 0; i < EditorMapUtil.TOTAL_BITS; ++i)
                {
                    x = EditorMapUtil.INDEX_MAP[i].x;
                    y = EditorMapUtil.INDEX_MAP[i].y;
                    mask = matrix[x, y];

                    newMask |= mask << i * EditorMapUtil.BITS_PER_CELL;
                }

                return newMask;
            }

            ulong[,] BitmaskToMatrix(ulong mask, float _rotY)
            {
                int size = EditorMapUtil.MATRIX_SIZE;
                ulong cellValue;

                ulong[,] matrix = new ulong[size, size];
                int x, y;
                for (int i = 0; i < EditorMapUtil.TOTAL_BITS; ++i)
                {
                    cellValue = mask & MapSamplingData.HEIGHT_MASK;
                    x = EditorMapUtil.INDEX_MAP[i].x;
                    y = EditorMapUtil.INDEX_MAP[i].y;
                    matrix[x, y] = cellValue;
                    mask >>= EditorMapUtil.BITS_PER_CELL;
                }

                int rotInt = Mathf.RoundToInt(_rotY);
                rotInt = (rotInt + 360) % 360;

                return RotateMatrix(matrix, rotInt);
            }

            ulong[,] RotateMatrix(ulong[,] matrix, int rot)
            {
                if (0 == rot) return matrix;

                int size = 5;
                ulong[,] rotatedMatrix = new ulong[size, size];

                ulong vertexValue;
                for (int i = 0; i < size; ++i)
                {
                    for (int j = 0; j < size; ++j)
                    {
                        vertexValue = matrix[i, j];

                        if (0 != vertexValue)
                        {
                            int newX = 0;
                            int newY = 0;

                            switch (rot)
                            {
                                case 90:
                                    newX = size - 1 - j;
                                    newY = i;
                                    break;
                                case 180:
                                    newX = size - 1 - i;
                                    newY = size - 1 - j;
                                    break;
                                case 270:
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
        }
    }
}
