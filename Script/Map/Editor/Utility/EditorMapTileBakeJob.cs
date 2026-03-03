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
            int totalBits = EditorMapUtil.TOTAL_BITS; // 13
            int bitsPerCell = EditorMapUtil.BITS_PER_CELL; // 4
            int size = EditorMapUtil.MATRIX_SIZE; // 5

            ulong layerMask = (ulong)navLayer << (totalBits * bitsPerCell);

            int rotInt = Mathf.RoundToInt(rotY);
            rotInt = (rotInt + 360) % 360;

            // 회전이 없으면 연산 없이 조기 반환
            if (rotInt == 0)
            {
                return (long)(layerMask | heightMask);
            }

            // Burst 호환을 위해 2차원 배열 대신 1차원 NativeArray(Temp) 사용
            NativeArray<ulong> matrix = new NativeArray<ulong>(size * size, Allocator.Temp);
            NativeArray<ulong> rotatedMatrix = new NativeArray<ulong>(size * size, Allocator.Temp);

            // 1. BitmaskToMatrix
            ulong tempMask = heightMask;
            for (int i = 0; i < totalBits; ++i)
            {
                ulong cellValue = tempMask & MapSamplingData.HEIGHT_MASK;
                int2 pos = GetIndexMap(i);
                matrix[pos.x * size + pos.y] = cellValue;
                tempMask >>= bitsPerCell;
            }

            // 2. RotateMatrix
            for (int x = 0; x < size; ++x)
            {
                for (int y = 0; y < size; ++y)
                {
                    ulong vertexValue = matrix[x * size + y];
                    if (vertexValue != 0)
                    {
                        int newX = 0;
                        int newY = 0;
                        switch (rotInt)
                        {
                            case 90:
                                newX = size - 1 - y;
                                newY = x;
                                break;
                            case 180:
                                newX = size - 1 - x;
                                newY = size - 1 - y;
                                break;
                            case 270:
                                newX = y;
                                newY = size - 1 - x;
                                break;
                        }
                        rotatedMatrix[newX * size + newY] = vertexValue;
                    }
                }
            }

            // 3. MatrixToBitmask
            ulong rotatedHeightMask = 0ul;
            for (int i = 0; i < totalBits; ++i)
            {
                int2 pos = GetIndexMap(i);
                ulong maskVal = rotatedMatrix[pos.x * size + pos.y];
                rotatedHeightMask |= maskVal << (i * bitsPerCell);
            }

            // [중요] 임시 할당된 NativeArray는 Job 내에서 반드시 메모리 해제
            matrix.Dispose();
            rotatedMatrix.Dispose();

            return (long)(layerMask | rotatedHeightMask);
        }

        // Burst에서 외부 static readonly 배열 접근 시 발생하는 오류를 방지하기 위한 로컬 함수
        private readonly int2 GetIndexMap(int index)
        {
            switch (index)
            {
                case 0: return new int2(0, 4);
                case 1: return new int2(2, 4);
                case 2: return new int2(4, 4);
                case 3: return new int2(1, 3);
                case 4: return new int2(3, 3);
                case 5: return new int2(0, 2);
                case 6: return new int2(2, 2);
                case 7: return new int2(4, 2);
                case 8: return new int2(1, 1);
                case 9: return new int2(3, 1);
                case 10: return new int2(0, 0);
                case 11: return new int2(2, 0);
                case 12: return new int2(4, 0);
                default: return new int2(0, 0);
            }
        }
    }
}
