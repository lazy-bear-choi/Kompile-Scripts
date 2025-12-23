#if UNITY_EDITOR
namespace Script.Editor.MapSampling
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(EditMapTileSampling))]
    public class MapTileSamplingEditor : Editor
    {
        private EditMapTileSampling _sampler;
        
        private void Awake()
        {
            _sampler = target as EditMapTileSampling;
        }
        
        public override void OnInspectorGUI()
        {
            // 기본 인스펙터 표시
            base.OnInspectorGUI();
        
            if (GUILayout.Button("Save"))
            {
                if (null != _sampler)
                {
                    _sampler.Bake();
                }
            }
        }
    }  
}
#endif