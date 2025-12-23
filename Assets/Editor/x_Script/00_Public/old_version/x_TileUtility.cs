//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using static Index.IDxTile;
//using Script.Util;
//using static Index.IDxInput;

//public static class TileUtility
//{
//    private struct TriangleCollision
//    {
//        // 삼각형의 꼭지점
//        public Vector3 Point0 { get; set; }
//        public Vector3 Point1 { get; set; }
//        public Vector3 Point2 { get; set; }

//        // 삼각형이 속한 타일의 key
//        public int Key { get; set; }

//        // 삼각형 인덱스
//        public int Index { get; set; }

//        public TriangleCollision(int key, Vector3 pivot, int indexTriangle, float scale)
//        {
//            Point0 = Point1 = Point2 = pivot;
//            this.Key = key;
//            Index = indexTriangle;

//            float scale_quater = scale * 0.25f;
//            switch (Index % 4)
//            {
//                case 0:
//                    Point0 += new Vector3(1f, 0f, 1f) * scale_quater;
//                    Point1 += new Vector3(2f, 0f, 0f) * scale_quater;

//                    break;
//                case 1:
//                    Point0 += new Vector3(1f, 0f, 1f) * scale_quater;
//                    Point1 += new Vector3(2f, 0f, 0) * scale_quater;
//                    Point2 += new Vector3(2f, 0f, 2f) * scale_quater;
//                    break;
//                case 2:
//                    Point0 += new Vector3(1f, 0f, 1f) * scale_quater;
//                    Point1 += new Vector3(0, 0f, 2f) * scale_quater;
//                    Point2 += new Vector3(2f, 0f, 2f) * scale_quater;
//                    break;
//                case 3:
//                    Point0 += new Vector3(1f, 0f, 1f) * scale_quater;
//                    Point1 += new Vector3(0, 0f, 2f) * scale_quater;

//                    break;
//            }

//            switch ((int)(Index * 0.25f))
//            {
//                case 1:
//                    Point0 += new Vector3(2f, 0f, 0) * scale_quater;
//                    Point1 += new Vector3(2f, 0f, 0) * scale_quater;
//                    Point2 += new Vector3(2f, 0f, 0) * scale_quater;
//                    break;
//                case 2:
//                    Point0 += new Vector3(0, 0f, 2f) * scale_quater;
//                    Point1 += new Vector3(0, 0f, 2f) * scale_quater;
//                    Point2 += new Vector3(0, 0f, 2f) * scale_quater;
//                    break;
//                case 3:
//                    Point0 += new Vector3(2f, 0f, 2f) * scale_quater;
//                    Point1 += new Vector3(2f, 0f, 2f) * scale_quater;
//                    Point2 += new Vector3(2f, 0f, 2f) * scale_quater;
//                    break;
//            }
//        }

//        //삼각형과 감지 범위가 서로 겹치는지 여부 확인
//        public bool IsIntersected(Vector3 center, float radius)
//        {
//            Vector2 center2D = new Vector2(center.x, center.z);
//            Vector2 A2d = new Vector2(Point0.x, Point0.z);
//            Vector2 B2d = new Vector2(Point1.x, Point1.z);
//            Vector2 C2d = new Vector2(Point2.x, Point2.z);

//            if (PointInTriangle(center2D, A2d, B2d, C2d))
//            {
//                return true;
//            }

//            // 삼각형의 각 꼭짓점이 원 내부에 있는지 확인
//            if (IsPointInsideCircle(A2d, center2D, radius) ||
//                IsPointInsideCircle(B2d, center2D, radius) ||
//                IsPointInsideCircle(C2d, center2D, radius))
//            {
//                return true;
//            }


//            // 삼각형의 각 변과 원의 교차 확인
//            if (IsCircleLineIntersect(center2D, radius, A2d, B2d) ||
//                IsCircleLineIntersect(center2D, radius, B2d, C2d) ||
//                IsCircleLineIntersect(center2D, radius, C2d, A2d))
//            {
//                return true;
//            }

//            return false;
//        }
//        private bool IsPointInsideCircle(Vector2 point, Vector2 circleCenter, float radius)
//        {
//            return (point - circleCenter).sqrMagnitude < radius * radius;
//        }
//        private bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
//        {
//            float s = p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * p.x + (p0.x - p2.x) * p.y;
//            float t = p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y;

//            if ((s < 0) != (t < 0))
//                return false;

//            float A = -p1.y * p2.x + p0.y * (p2.x - p1.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y;
//            if (A < 0.0)
//            {
//                s = -s;
//                t = -t;
//                A = -A;
//            }
//            return s > 0 && t > 0 && (s + t) < A;
//        }
//        private bool IsCircleLineIntersect(Vector2 circleCenter, float radius, Vector2 A, Vector2 B)
//        {
//            Vector2 d = B - A;
//            Vector2 f = A - circleCenter;

//            float a = Vector2.Dot(d, d);
//            float b = Vector2.Dot(f, d) * 2;
//            float c = Vector2.Dot(f, f) - radius * radius;

//            float discriminant = b * b - 4 * a * c;
//            if (discriminant < 0)
//            {
//                return false;
//            }
//            else
//            {
//                discriminant = Mathf.Sqrt(discriminant);

//                float t1 = (-b - discriminant) / (2 * a);
//                float t2 = (-b + discriminant) / (2 * a);
//                if (t1 >= 0 && t1 <= 1 || t2 >= 0 && t2 <= 1)
//                    return true;

//                return false;
//            }
//        }
//    }

//    private static TriangleCollision[] triangles = new TriangleCollision[16]; //0: 현재 위치의 삼각형, 1~: 비교할 대상 삼각형들
//    private static int index;

//    // get key, pivot
//    public static Vector3 GetPivotByKey(int key, float scale)
//    {
//        float x = 0f, y = 0f, z = 0f;

//        //scale = 0.5f인 경우, 추가적인 처리가 필요함
//        if (0 != ((key >> SHIFT_KEY_SCALE) & 0x1))
//        {
//            x += 0.125f;
//        }

//        key &= ~(1 << SHIFT_KEY_SCALE);
//        x += ((key >> SHIFT_KEY_X) & 0xFF) * scale;
//        y += ((key >> SHIFT_KEY_Y) & 0x0F) * scale;
//        z += ((key >> SHIFT_KEY_Z) & 0xFF) * scale;

//        return new Vector3(x, y, z);
//    }
//    public static Vector3 GetPivotByPoint(Vector3 point, float scale)
//    {
//        float scale_inverse = GetScale(ETileSizeType.Default_Inverse, scale);

//        int cx = UMath.FloorToInt(point.x * scale_inverse, 3);
//        int cy = UMath.FloorToInt(point.y * scale_inverse, 3);
//        int cz = UMath.FloorToInt(point.z * scale_inverse, 3);
//        Vector3 pivot = new Vector3(cx, cy, cz) * scale;

//        if (1f != scale)
//        {
//            float scale_quater = scale * 0.25f;
//            float x = UMath.FloorToInt((point.x - scale_quater) * scale_inverse, 3);
//            x *= scale;
//            x += scale_quater;

//            pivot = new Vector3(x, pivot.y, pivot.z);
//            //Debug.Log($"{pivot:F3}");
//        }

//        return pivot;
//    }
//    public static int GetKeyByPoint(int layer, Vector3 point, float scale)
//    {
//        //최소, 최대 범위 체크 (에러코드: -1로 설정)
//        if (0 > point.x || 128 < point.x || 0 > point.z || 128 < point.z)
//        {
//            return -1;
//        }
//        Vector3 pivot = GetPivotByPoint(point, scale);

//        //나눗셈 연산을 피하고자 역수 계산 시 미리 저장한 값을 가져온다 (GetScale(););
//        float scale_inverse = GetScale(ETileSizeType.Default_Inverse, scale);

//        //타일의 스케일이 0.5f인 경우, 추가 연산이 필요하다.
//        int key = layer << SHIFT_KEY_LAYER;
//        if (0 != pivot.x % 1f)
//        {
//            key |= 1 << SHIFT_KEY_SCALE;
//        }

//        key |= (int)(pivot.x * scale_inverse) << SHIFT_KEY_X;
//        key |= (int)(pivot.y * scale_inverse) << SHIFT_KEY_Y;
//        key |= (int)(pivot.z * scale_inverse) << SHIFT_KEY_Z;

//        return key;
//    }
//    public static int GetKeyByRelativeCoord(Dictionary<int, Script.Data.STile> map, int key, int x, int z)
//    {
//        int keyLink = key + x * (1 << SHIFT_KEY_X) + z * (1 << SHIFT_KEY_Z);

//        // y = 0
//        if (true == map.ContainsKey(keyLink))
//        {
//            return keyLink;
//        }
//        //y + 1
//        if (true == map.ContainsKey(keyLink + (1 << 8)))
//        {

//            return keyLink += (1 << 8);
//        }
//        // y - 1
//        if (true == map.ContainsKey(keyLink - (1 << 8)))
//        {

//            return keyLink -= (1 << 8);
//        }

//        return -1;
//    }
//    public static float GetScale(ETileSizeType type, float scale)
//    {
//        // for using cache data
//        float size;

//        switch (type)
//        {
//            case ETileSizeType.Default: size = SIZE; break;
//            case ETileSizeType.Half: size = SIZE_HALF; break;
//            case ETileSizeType.Default_Inverse: size = SIZE_INVERSE; break;
//            case ETileSizeType.Half_inverse: size = SIZE_HALF_INVERSE; break;
//            case ETileSizeType.Quater: size = SIZE_QUATER; break;
//            case ETileSizeType.Quater_inverse: size = SIZE_QUATER_INVERSE; break;
//            default: return 0f;
//        }
//        if (type > ETileSizeType.Inverse)
//        {
//            scale = 1 / scale;
//        }

//        return size * scale;
//    }

//    // get triangle
//    public static int GetTriangleIndex(Vector3 diff, float scale_half)
//    {
//        int quarant = 0;
//        if (diff.x >= scale_half)
//        {
//            quarant |= 0b_01;
//            diff -= new Vector3(scale_half, 0, 0);
//        }
//        if (diff.z >= scale_half)
//        {
//            quarant |= 0b_10;
//            diff -= new Vector3(0, 0, scale_half);
//        }
//        quarant *= 4;

//        int equation = 0;
//        if (diff.z >= diff.x)
//        {
//            equation |= 0b01;
//        }
//        if (diff.z >= -diff.x + scale_half)
//        {
//            equation |= 0b10;
//        }

//        switch (equation)
//        {
//            case 0b00: return quarant;
//            case 0b10: return quarant + 1;
//            case 0b11: return quarant + 2;
//            case 0b01: return quarant + 3;
//        }

//        return -1;
//    }
//    public static void GetTrianglePoints(Vector3 pivot, float scale, long flagHeight, int quarant, out Vector3 p0, out Vector3 p1, out Vector3 p2)
//    {
//        int i0, i1, i2;
//        p0 = p1 = p2 = pivot;

//        switch (quarant)
//        {
//            case 0: i0 = 0; i1 = 9; i2 = 1; break;
//            case 1: i0 = 1; i1 = 9; i2 = 4; break;
//            case 2: i0 = 3; i1 = 4; i2 = 9; break;
//            case 3: i0 = 0; i1 = 3; i2 = 9; break;

//            case 4: i0 = 1; i1 = 10; i2 = 2; break;
//            case 5: i0 = 2; i1 = 10; i2 = 5; break;
//            case 6: i0 = 4; i1 = 5; i2 = 10; break;
//            case 7: i0 = 1; i1 = 4; i2 = 10; break;

//            case 8: i0 = 3; i1 = 11; i2 = 4; break;
//            case 9: i0 = 4; i1 = 11; i2 = 7; break;
//            case 10: i0 = 6; i1 = 7; i2 = 11; break;
//            case 11: i0 = 3; i1 = 6; i2 = 11; break;

//            case 12: i0 = 4; i1 = 12; i2 = 5; break;
//            case 13: i0 = 5; i1 = 12; i2 = 8; break;
//            case 14: i0 = 7; i1 = 8; i2 = 12; break;
//            case 15: i0 = 4; i1 = 7; i2 = 12; break;

//            default: return;
//        }

//        p0 = GetTriangleOnePoint(pivot, flagHeight, i0, scale);
//        p1 = GetTriangleOnePoint(pivot, flagHeight, i1, scale);
//        p2 = GetTriangleOnePoint(pivot, flagHeight, i2, scale);
//    }
//    private static Vector3 GetTriangleOnePoint(Vector3 pivot, long flagHeight, int index, float scale)
//    {
//        float scale_half = scale * 0.5f;
//        float scale_quater = scale * 0.25f;

//        float y = (flagHeight >> (index * 3)) & 0b111;
//        y *= scale_quater;

//        switch (index)
//        {
//            case 0: return pivot;
//            case 1: return pivot + new Vector3(scale_half, y, 0);
//            case 2: return pivot + new Vector3(scale, y, 0);

//            case 3: return pivot + new Vector3(0, y, scale_half);
//            case 4: return pivot + new Vector3(scale_half, y, scale_half);
//            case 5: return pivot + new Vector3(scale, y, scale_half);

//            case 6: return pivot + new Vector3(0, y, scale);
//            case 7: return pivot + new Vector3(scale_half, y, scale);
//            case 8: return pivot + new Vector3(scale, y, scale);

//            case 9: return pivot + new Vector3(scale_quater, y, scale_quater);
//            case 10: return pivot + new Vector3(scale_half + scale_quater, y, scale_quater);
//            case 11: return pivot + new Vector3(scale_quater, y, scale_half + scale_quater);
//            case 12: return pivot + new Vector3(scale_half + scale_quater, y, scale_half + scale_quater);
//        }

//        return Vector3.zero;
//    }
//    public static void SetTriangleArray(Dictionary<int, DataStruct.STile> map, int triangle, int key, Vector3 pivot, float scale)
//    {
//        index = 0;
//        switch (triangle)
//        {
//            case 0:
//                triangles[index++] = new TriangleCollision(key, pivot, 0, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 3, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 4, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);

//                //neighbor: z-1
//                int keyLink = GetKeyByRelativeCoord(map, key, x: 0, z: -1);
//                Vector3 pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 9, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 15, scale);

//                //neighbor: x-1, z-1
//                keyLink = GetKeyByRelativeCoord(map, key, x: -1, z: -1);
//                pivotNeighbor = pivot + new Vector3(-1, 0, -1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);

//                //neighbor: x-1
//                keyLink = GetKeyByRelativeCoord(map, key, x: -1, z: 0);
//                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
//                break;

//            case 1:
//                triangles[index++] = new TriangleCollision(key, pivot, 0, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 3, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 4, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: 0, z: -1);
//                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 9, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 15, scale);
//                break;

//            case 2:
//                triangles[index++] = new TriangleCollision(key, pivot, 0, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 3, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 11, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: -1, z: 0);
//                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 6, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 12, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
//                break;

//            case 3:
//                triangles[index++] = new TriangleCollision(key, pivot, 0, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 3, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 11, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: 0, z: -1);
//                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: -1, z: -1);
//                pivotNeighbor = pivot + new Vector3(-1, 0, -1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: -1, z: 0);
//                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 6, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 12, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);

//                break;
//            case 4:
//                triangles[index++] = new TriangleCollision(key, pivot, 4, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 5, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 0, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: 0, z: -1);
//                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 9, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 15, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: +1, z: -1);
//                pivotNeighbor = pivot + new Vector3(1, 0, -1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: +1, z: 0);
//                pivotNeighbor = pivot + new Vector3(1, 0, 0) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);

//                break;
//            case 5:
//                triangles[index++] = new TriangleCollision(key, pivot, 4, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 5, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 13, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: 0, z: -1);
//                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: +1, z: -1);
//                pivotNeighbor = pivot + new Vector3(+1, 0, -1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: +1, z: 0);
//                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 2, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 8, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
//                break;

//            case 6:
//                triangles[index++] = new TriangleCollision(key, pivot, 4, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 5, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 13, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: +1, z: 0);
//                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 2, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 8, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
//                break;
//            case 7:
//                triangles[index++] = new TriangleCollision(key, pivot, 4, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 5, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 0, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: 0, z: -1);
//                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 9, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 15, scale);
//                break;
//            case 8:
//                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 10, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 11, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 3, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: -1, z: 0);
//                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 6, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 12, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
//                break;
//            case 9:
//                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 10, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 11, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 14, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: 0, z: +1);
//                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 1, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 7, scale);
//                break;
//            case 10:
//                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 10, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 11, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 14, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: 0, z: +1);
//                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 1, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 7, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: -1, z: +1);
//                pivotNeighbor = pivot + new Vector3(-1, 0, +1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: -1, z: 0);
//                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
//                break;
//            case 11:
//                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 3, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 10, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 11, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: 0, z: +1);
//                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: -1, z: +1);
//                pivotNeighbor = pivot + new Vector3(-1, 0, +1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: -1, z: 0);
//                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 6, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 12, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
//                break;
//            case 12:
//                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 13, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 14, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 5, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: +1, z: 0);
//                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 2, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 8, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
//                break;
//            case 13:
//                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 13, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 14, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 5, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: +1, z: 0);
//                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 2, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 8, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: +1, z: +1);
//                pivotNeighbor = pivot + new Vector3(+1, 0, +1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: 0, z: +1);
//                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
//                break;
//            case 14:
//                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 13, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 14, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 10, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: +1, z: 0);
//                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: +1, z: +1);
//                pivotNeighbor = pivot + new Vector3(+1, 0, +1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: 0, z: +1);
//                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 1, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 7, scale);
//                break;
//            case 15:
//                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 13, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 14, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
//                triangles[index++] = new TriangleCollision(key, pivot, 10, scale);

//                keyLink = GetKeyByRelativeCoord(map, key, x: 0, z: +1);
//                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 1, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
//                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 7, scale);
//                break;
//        }
//    }

//    // check is movable point
//    public static bool IsMovable(Dictionary<int, DataStruct.STile> map, Vector3 goal, float scale)
//    {
//        float dist = UMath.Floor(scale * SIZE_QUATER - Time.fixedDeltaTime, 3);
//        for (int i = 0; i < index; ++i)
//        {
//            //대상 삼각형에 대하여
//            TriangleCollision triangle = triangles[i];

//            //서로 맞닿는가?
//            if (false == triangle.IsIntersected(goal, dist))
//            {
//                continue;
//            }
//            //대상 삼각형은 실제하는 데이터인가? (존재하는 타일이 있는가?)
//            if (false == map.TryGetValue(triangle.Key, out DataStruct.STile tileChecked))
//            {
//                return false;
//            }
//            //대상 삼각형의 위치로 이동할 수 있는가? (충돌이 없는가?)
//            if (false == tileChecked.IsMovable(triangle.Index))
//            {
//                return false;
//            }
//        }

//        return true;
//    }


//#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
//    //부동소수점 오차를 줄이기 위해 단위 간격으로 자동 조정한다.
//    public static Vector3 SnappingPoint(Vector3 p, float dist, int exponent)
//    {
//        float x = p.x;
//        float y = p.y;
//        float z = p.z;
//        float diff;

//        //Similar to rounding, but the standard is different for each dist, not 0.5f.
//        diff = x % dist;
//        if (0 < diff & diff <= dist * 0.1f)
//        {
//            x -= diff;
//        }
//        else if (dist * 0.9f <= diff && diff < dist)
//        {
//            x += (dist - diff);
//        }

//        diff = y % dist;
//        if (0 < diff & diff <= dist * 0.1f)
//        {
//            y -= diff;
//        }
//        else if (dist * 0.9f <= diff && diff < dist)
//        {
//            y += (dist - diff);
//        }

//        diff = z % dist;
//        if (0 < diff & diff <= dist * 0.1f)
//        {
//            z -= diff;
//        }
//        else if (dist * 0.9f <= diff && diff < dist)
//        {
//            z += (dist - diff);
//        }

//        return new Vector3(x, y, z).Truncate(exponent);
//    }
//#endif
//}
