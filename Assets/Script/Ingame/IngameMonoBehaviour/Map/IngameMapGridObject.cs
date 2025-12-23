using Script.Data;
using Script.Index;
using Script.Manager;
using System.Collections.Generic;
using UnityEngine;

public class IngameMapGridObject :IngameMonoBehaviourBase
{
    private MapGridData raw_data;
    private List<IngameMapGridLayerObject> by_layer_objects;

    public MapGridData Data => raw_data;

    // 잠깐만.. 애초에 이게 맞나 싶기도 합니다?...
    // class 따로 만들어서 GameObject를 멤버로 받는게 맞지 않나?
    // GameObect에 뭐 붙이려고 하니까 생성자 규칙에서 어긋나는 것 같은데요
    // 데이터와 개체를 최대한 분리한다면...
    public async void Initialize(MapGridData data)
    {
        //asset_code = AssetCode.MapGridPrefab;

        raw_data = data;
        transform.position = Vector3.zero;
        by_layer_objects = new List<IngameMapGridLayerObject>();

        List<GridLayerData> layer_table = data.layerMeshAssets;
        for (int i = 0; i < layer_table.Count; ++i)
        {
            int layer_index = layer_table[i].layer;
            for (int j = 0; j < layer_table[i].assets.Count; ++j)
            {
                var layer_obj = await AssetManager.GetOrNewInstanceAsync<IngameMapGridLayerObject>(AssetCode.MapGridLayerPrefab, this.transform, usePooling: true);
                layer_obj.Initialize(layer_index, layer_table[i].assets[j]);

                by_layer_objects.Add(layer_obj);
            }
        }
    }

    //public override void Release()
    //{
    //    for (int i = 0; i < by_layer_objects.Count; ++i)
    //    {
    //        by_layer_objects[i].Release();
    //        AssetManager.ReleaseInstance(AssetCode.MapGridLayerPrefab, by_layer_objects[i].gameObject, true);
    //    }

    //    AssetManager.ReleaseInstance(AssetCode.MapGridPrefab, this.gameObject);
    //}
}