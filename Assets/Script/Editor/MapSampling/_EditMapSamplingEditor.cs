#if UNITY_EDITOR
using System.Collections.Generic;
using Script.Data;
using Script.Map;
using UnityEditor;
using UnityEngine;

public class STUDY_EditMapSamplingEditor
{
    [MenuItem("Tools/MapSampling/Bake Path Tiles to .btyes")]
    public static void Bake()
    {
        EditMapSampling sampler = new EditMapSampling();
        sampler.Bake();
    }

    [MenuItem("Tools/MapSampling/Load Baked Map Tiles")]
    public static async Awaitable TempLoad()
    {
        //string targetAssetName = "MapNavi_0";
        string targetAssetName = "MapNavi_65280";
        Debug.Log($"Load Baked Map({targetAssetName}) Tiles ");

        MapCacheManager cacheMgr = new MapCacheManager();
        await cacheMgr.LoadFromAddressableAsync(targetAssetName);
        foreach (KeyValuePair<long, MapTileData> tileKV in cacheMgr.TileDic)
        {
            var id = tileKV.Key;
            var tile = tileKV.Value;
            var pivot = MapPathUtil.ComputeWorldPosition(id);

            Debug.Log($"[{id}] {pivot}.navi = {System.Convert.ToString(tile.NaviMask, 16)},\nlink = {System.Convert.ToString(tile.LinkMask, 2)}");
        }
    }
}
#endif