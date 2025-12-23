namespace Script.Index
{
    public enum CanvasType
    {
        NONE = -1,

        CAMERA = 0,
        OVERLAY,
        OVERLAY_LOADING,
    }

    /// <summary>
    /// enum.ToString() 사용하여 어드레서블 에셋 탐색 => 실제 에셋과 파일명을 동일하게 맞출 것
    /// </summary>
    public enum AssetCode
    {
        NONE = 0,
        

        DB_MAP_GRID,
        MapGridPrefab,
        MapGridLayerPrefab,

        OP_TitleObject,

        UI_LoadingCurtain,
        UI_TitleMenuObject,

        UnitBase,
        AnimCtrl_Ataho,
        AnimCtrl_Linxhang,
        AnimeCtrl_Smashu,
    }
}