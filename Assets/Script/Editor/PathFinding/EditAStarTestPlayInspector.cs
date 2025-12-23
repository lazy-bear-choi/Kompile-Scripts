#if UNITY_EDITOR
using Script.Editor.MapSampling;
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

        if (GUILayout.Button("Save"))
        {
            if (null != tester)
            {
                tester.Play();
            }
        }
    }
}
#endif