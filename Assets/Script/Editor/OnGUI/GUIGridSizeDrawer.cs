#if UNITY_EDITOR

using UnityEngine;
using static Script.Index.MapTileIndex;

public class GUIGridSizeDrawer : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Vector3 size = GRID_SIZE * Vector3.one;
        Vector3 center = size * 0.5f;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + center, size);
    }
}

#endif