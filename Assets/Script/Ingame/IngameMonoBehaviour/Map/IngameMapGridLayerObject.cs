using Script.Manager;
using Script.Index;
using UnityEngine;

public class IngameMapGridLayerObject : IngameMonoBehaviourBase
{
    private int index;
    private int mesh_instance_id;

    public async void Initialize(int layer_index, string address)
    {
        index = layer_index;
        //asset_code = AssetCode.MapGridLayerPrefab;

        MeshFilter mesh_filter = transform.GetComponent<MeshFilter>();
        Mesh layer_mesh = await AssetManager.GetAssetAsync<Mesh>(address);
        mesh_filter.mesh = layer_mesh;
        mesh_instance_id = layer_mesh.GetInstanceID();

        gameObject.SetActive(index == FieldManager.CurrentLayerIndex);
    }
    //public override void Release()
    //{
    //    MeshFilter mesh_filter = transform.GetComponent<MeshFilter>();
    //    mesh_filter.mesh = null;

    //    AssetManager.ReleaseInstance(mesh_instance_id);
    //    AssetManager.ReleaseInstance(asset_code, this.gameObject);
    //}
}
