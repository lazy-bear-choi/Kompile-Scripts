#if UNITY_EDITOR
using Script.Data;
using Script.Map;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EditAStarTestPlay))]
public class EditAStarTestPlayInspector : Editor
{
    private EditAStarTestPlay tester;

    private void Awake()
    {
        tester = target as EditAStarTestPlay;
    }
    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 표시
        base.OnInspectorGUI();

        if (GUILayout.Button("Test Pathfinding Play"))
        {
            if (null != tester)
            {
                tester.Play();
            }
        }
    }
}
#endif