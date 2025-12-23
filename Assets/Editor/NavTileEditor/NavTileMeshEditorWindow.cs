using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Script.Data;

// Custom Editor + Create Asset
public partial class NavTileMeshEditorWindow : EditorWindow
{
    private readonly int[]  inputHeights  = new int[13];    // 각 vertex의 height 입력값
    private string inputFileName = "default_name"; // 에셋 파일 이름
    private bool   isSmall       = false;          // (실내 등) 타일맵을 작게 만들 때 체크

    // 커스텀 에디터 표기
    [MenuItem("Tools/Asset/Map/Generate Map Tile Mesh", priority = 0)]
    public static void ShowWindow()
    {
        GetWindow<NavTileMeshEditorWindow>("Nav Tile Mesh Editor");
    }
    private void OnGUI()
    {
        // File name input
        GUILayout.Space(5);
        var boldLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold // 글씨를 굵게 설정
        };
        EditorGUILayout.LabelField("File Name", boldLabelStyle); // 레이블을 굵게 설정
        inputFileName = EditorGUILayout.TextField(inputFileName);
        isSmall = GUILayout.Toggle(isSmall, "Is Small");

        GUILayout.Space(5);
        GUILayout.Label("Input Vertex Height", EditorStyles.boldLabel);
        
        float fieldWidth = 50; // IntField의 너비
        float spacing    = 10; // 필드 간의 간격
        float startX     = 20; // 시작 X 위치
        float startY     = 85; // 시작 Y 위치

        DrawInputRow(new[] { "h10", "h11", "h12" }, new[] { 10, 11, 12 }, startX, startY, fieldWidth, spacing);
        startY += 30;

        DrawInputRow(new[] { "h8", "h9" }, new[] { 8, 9 }, startX + fieldWidth + spacing, startY, fieldWidth, spacing);
        startY += 30;

        DrawInputRow(new[] { "h5", "h6", "h7" }, new[] { 5, 6, 7 }, startX, startY, fieldWidth, spacing);
        startY += 30;

        DrawInputRow(new[] { "h3", "h4" }, new[] { 3, 4 }, startX + fieldWidth + spacing, startY, fieldWidth, spacing);
        startY += 30;

        DrawInputRow(new[] { "h0", "h1", "h2" }, new[] { 0, 1, 2 }, startX, startY, fieldWidth, spacing);

        GUILayout.Space(startY - 30);
        if (GUILayout.Button("Save Mesh"))
        {
            NavTileMeshEditor.SaveData(inputFileName, isSmall, inputHeights);
        }

        GUILayout.Space(1);
        if (GUILayout.Button("Clear Height"))
        {
            GUI.FocusControl(null);
            
            for (var i = 0; i < inputHeights.Length; i++)
            {
                inputHeights[i] = 0;
            }
        }
        
    }
    private void DrawInputRow(string[] labels, int[] indices, float startX, float startY, float fieldWidth, float spacing)
    {
        float currentX = startX;

        for (int i = 0; i < labels.Length; i++)
        {
            // Label
            EditorGUI.LabelField(new Rect(currentX, startY, 30, 20), labels[i]);
            currentX += 30;

            // IntField
            inputHeights[indices[i]] = EditorGUI.IntField(new Rect(currentX, startY, fieldWidth, 20), inputHeights[indices[i]]);
            currentX += fieldWidth + spacing;
        }
    }
}
