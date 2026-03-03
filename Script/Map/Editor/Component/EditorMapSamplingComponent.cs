namespace Script.Map.Editor
{
    using UnityEngine;

    public class EditorMapSamplingComponent : MonoBehaviour
    {
        [Tooltip("샘플링 시 할당될 씬의 고유 인덱스")]
        [SerializeField] private byte sceneIndex;

        public byte SceneIndex => sceneIndex;
    }
}