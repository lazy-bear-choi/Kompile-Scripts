#if UNITY_EDITOR
using Script.Data;
using static Script.Index.MapTileIndex;
using System.Text;
using UnityEditor;
using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic; // List를 사용하기 위해 추가

public static class NavTileMeshEditor
{
    private static readonly float3[] VerticeVerticePoint = new float3[]
    {
        new float3(0f,    0f,    0f    ),  new float3(0.5f,  0f,    0f    ),  new float3(1f,    0f,    0f),
        new float3(0.25f, 0f,    0.25f),  new float3(0.75f, 0f,    0.25f),  new float3(0f,    0f,    0.5f),
        new float3(0.5f,  0f,    0.5f ),  new float3(1f,    0f,    0.5f ),  new float3(0.25f, 0f,    0.75f),
        new float3(0.75f, 0f,    0.75f),  new float3(0f,    0f,    1f    ),  new float3(0.5f,  0f,    1f),
        new float3(1f,    0f,    1f    )
    };
    private static readonly int[] ExceptTriangleMask = new int[]
    {
        TRIANGLE_FULL_MASK & ~(1 <<  0 | 1 <<  3),
        TRIANGLE_FULL_MASK & ~(1 <<  0 | 1 <<  1 | 1 <<  4 | 1 <<  7),
        TRIANGLE_FULL_MASK & ~(1 <<  4 | 1 <<  5),
        TRIANGLE_FULL_MASK & ~(1 <<  0 | 1 <<  1 | 1 <<  2 | 1 <<  3),
        TRIANGLE_FULL_MASK & ~(1 <<  4 | 1 <<  5 | 1 <<  6 | 1 <<  7),
        TRIANGLE_FULL_MASK & ~(1 <<  2 | 1 <<  3 | 1 <<  8 | 1 << 11),
        TRIANGLE_FULL_MASK & ~(1 <<  1 | 1 <<  2 | 1 <<  6 | 1 <<  7 | 1 <<  8 | 1 <<  9 | 1 << 12 | 1 << 15),
        TRIANGLE_FULL_MASK & ~(1 <<  5 | 1 <<  6 | 1 << 12 | 1 << 13),
        TRIANGLE_FULL_MASK & ~(1 <<  8 | 1 <<  9 | 1 << 10 | 1 << 11),
        TRIANGLE_FULL_MASK & ~(1 << 12 | 1 << 13 | 1 << 14 | 1 << 15),
        TRIANGLE_FULL_MASK & ~(1 << 10 | 1 << 11),
        TRIANGLE_FULL_MASK & ~(1 <<  9 | 1 << 10 | 1 << 14 | 1 << 15),
        TRIANGLE_FULL_MASK & ~(1 << 13 | 1 << 14)
    };

    private const float HEIGHT_UNIT_VALUE = 0.125f;    // 높이값의 단위. (height * 0.125f). 0 ~ 1의 값을 가진다.
    private const int TRIANGLE_FULL_MASK = 0x_FFFF;    // 하나의 mesh 안에 4*4, 16개의 triangle로 이뤄졌다.

    private static StringBuilder stringBuilder = new StringBuilder();

    public static void SaveData(string fileName, bool isSmall, int[] heights)
    {
        int height;
        ulong heightFlag;
        ulong heightMask = 0;
        for (int i = 0; i < heights.Length; ++i)
        {
            height = heights[i];
            heightFlag = (-1 == height) ? HEIGHT_MASK : (ulong)height;
            heightMask |= heightFlag << i * HEIGHT_BITS;
        }

        stringBuilder.Append(fileName).Append("_").Append(heightMask);
        fileName = stringBuilder.ToString();
        stringBuilder.Clear();

        // set file name
        if (true == isSmall)
        {
            fileName += "_s";
        }

        Mesh mesh = InstantiateMesh(heights);

        // create | save mesh
        var path = "Assets/Rcs/Mesh/MapTileMesh/MAP_" + fileName + ".asset";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) is not null)
        {
            AssetDatabase.DeleteAsset(path);
        }
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        Debug.Log("Mesh asset created: " + fileName);

        // create|save prefab for test
        stringBuilder.Append("Assets/Editor/Prefab/MapTilePrefab/");
        stringBuilder.Append($"{fileName}");
        stringBuilder.Append(".prefab");

        path = stringBuilder.ToString();
        stringBuilder.Clear();

        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) is not null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        bool isSuccess;
        GameObject prefabObject = new GameObject(fileName);
        {
            var filter = prefabObject.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            var renderer = prefabObject.AddComponent<MeshRenderer>();
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Editor/Texture/mat_NavTile_00.mat"); //임의로 생성한 매터리얼
            renderer.sharedMaterial = material;

            // 타일 정보를 유니티의 NavMesh를 Bake하는 것처럼 데이터 저장할 때 호출하는 함수
            var maptilePrefab = prefabObject.AddComponent<EditMapTileObject>();
            maptilePrefab.InitializePrefab(heights, isSmall);

            PrefabUtility.SaveAsPrefabAsset(prefabObject, path, out isSuccess);
        }

        if (true == isSuccess)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Success to Create NavTile Prefab: " + fileName);
        }
        else
        {
            Debug.LogError("Fail to Create NavTile Prefab: " + fileName);
        }

        //testPrefab을 Scene에 생성했으나 필요 없으니 없앤다.
        Object.DestroyImmediate(prefabObject);
    }

    /// <summary> 입력받은 높이값[13]을 Mesh 정보로 변환하여 저장 </summary>
    public static Mesh InstantiateMesh(int[] inputHeights)
    {
        int length = inputHeights.Length;

        // 🚨 수정: 사용될 정점과 UV만 담을 리스트를 준비합니다.
        List<Vector3> finalVertices = new List<Vector3>(length);
        List<Vector2> finalUVs = new List<Vector2>(length);

        // 🚨 수정: 기존 Vertice 순서(0~12)와 최종 Mesh의 정점 순서(0~N)를 매핑하는 배열
        int[] virtualToActualIndex = new int[length];

        // 초기화: -1은 정점이 메쉬에 포함되지 않았음을 의미합니다.
        for (int i = 0; i < length; i++)
        {
            virtualToActualIndex[i] = -1;
        }

        Vector3 vertice;
        int height;
        int triangleMask = TRIANGLE_FULL_MASK;
        int actualIndex = 0; // 실제 Mesh에 할당될 정점의 최종 인덱스

        for (int virtualIndex = 0; virtualIndex < length; ++virtualIndex)
        {
            height = inputHeights[virtualIndex];

            vertice = VerticeVerticePoint[virtualIndex];

            if (height >= 0)
            {
                vertice += HEIGHT_UNIT_VALUE * height * Vector3.up;

                // 🚨 height >= 0 인 경우에만 정점과 UV를 추가하고 인덱스 매핑을 기록합니다.
                finalVertices.Add(vertice);
                finalUVs.Add(new Vector2(vertice.x, vertice.z));
                virtualToActualIndex[virtualIndex] = actualIndex;
                actualIndex++;
            }
            else
            {
                // 삼각형 대상에서 제외
                triangleMask &= ExceptTriangleMask[virtualIndex];
                // height < 0 인 경우 정점은 리스트에 추가되지 않습니다.
            }
        }

        // set: triangle
        int flag = 1;

        // GetTriangleCount 함수 호출
        int expectedTriangleCount = GetTriangleCount(triangleMask);
        // TriangleVertex는 외부 클래스(Script.Index.MapTileIndex 또는 EditMapUtil)에 정의된 것으로 가정
        int[] triangle = new int[expectedTriangleCount * 3];
        int triangleIndex = 0;
        int t_index = 0;

        while (flag <= triangleMask)
        {
            if (0 != (flag & triangleMask))
            {
                int index = triangleIndex * 3;

                // TriangleVertex 배열은 외부 정적 필드로 가정합니다.
                int v0_virtual = TriangleVertex[index + 0];
                int v1_virtual = TriangleVertex[index + 1];
                int v2_virtual = TriangleVertex[index + 2];

                // 🚨 수정: 가상 인덱스를 실제 메쉬의 정점 인덱스로 변환
                int v0 = virtualToActualIndex[v0_virtual];
                int v1 = virtualToActualIndex[v1_virtual];
                int v2 = virtualToActualIndex[v2_virtual];

                // 유효성 검사 (height < 0 인 정점은 v0, v1, v2 중 하나라도 -1이 됨)
                // 하지만 ExceptTriangleMask가 유효하므로 마스크된 삼각형은 이미 이 if 블록에 들어오지 않습니다.
                // 따라서 단순하게 인덱스를 할당합니다.
                triangle[t_index] = v0;
                triangle[t_index + 1] = v1;
                triangle[t_index + 2] = v2;
                t_index += 3;
            }

            flag <<= 1;
            ++triangleIndex;
        }

        // 🚨 최종 삼각형 배열 크기를 t_index와 일치시킵니다.
        if (t_index != triangle.Length)
        {
            System.Array.Resize(ref triangle, t_index);
        }

        // instantiate: mesh
        Mesh mesh = new Mesh()
        {
            vertices = finalVertices.ToArray(), // 🚨 정점 개수 == 최종 UV 개수
            triangles = triangle,
            uv = finalUVs.ToArray()             // 🚨 UV 개수 == 최종 정점 개수
        };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }
    private static int GetTriangleCount(int mask)
    {
        int count = 0;
        int flag = 1;

        while (flag <= mask)
        {
            if (0 != (flag & mask))
            {
                count += 1;
            }

            flag <<= 1;
        }

        return count;
    }
}
#endif // UNITY_EDITOR