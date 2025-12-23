namespace Script.Data
{
    using UnityEngine;
    using static Index.MapTileIndex;
 
    public readonly struct IngameMapTileData
    {
        public readonly int GridKey;
        public readonly int TileKey;
        public readonly long NaviMask;
        public readonly int LinkMask;

        public IngameMapTileData(int g, int t, MapTileData data)
        {
            GridKey = g;
            TileKey = t;
            NaviMask = data.NaviMask;
            LinkMask = data.LinkMask;
        }
        public IngameMapTileData(int g, int t)
        {
            GridKey = g;
            TileKey = t;
            NaviMask = NULL_TILE_NAVI;
            LinkMask = NULL_TILE_LINK;
        }

        public Vector3 Pivot
        {
            get
            {
                // grid_key to grid_parent_position
                int gx = (GridKey >> SHIFT_GRID_X) & GRID_COORD_MASK;
                int sign_x = (GridKey >> SHIFT_GRID_X_SIGN) & 1;
                if (0 != sign_x)
                {
                    gx *= -1;
                }

                int gy = (GridKey >> SHIFT_GRID_Y) & GRID_COORD_MASK;
                int sign_y = (GridKey >> SHIFT_GRID_Y_SIGN) & 1;
                if (0 != sign_y)
                {
                    gy *= -1;
                }

                int gz = (GridKey >> SHIFT_GRID_Z) & GRID_COORD_MASK;
                int sign_z = (GridKey >> SHIFT_GRID_Z_SIGN) & 1;
                if (0 != sign_z)
                {
                    gz *= -1;
                }
                Vector3 gird_pivot = GRID_SIZE * new Vector3(gx, gy, gz);


                // tile_key to tile_child_position
                int tx, ty, tz;

                tx = (TileKey >> SHIFT_TILE_X) & TILE_COORD_MASK;
                ty = (TileKey >> SHIFT_TILE_Y) & TILE_COORD_MASK;
                tz = (TileKey >> SHIFT_TILE_Z) & TILE_COORD_MASK;
                float scale = ((TileKey >> SHIFT_TILE_SMALL) & 1) > 0 ? 0.5f : 1;

                Vector3 tile_pivot = scale * new Vector3(tx, ty, tz);

                return gird_pivot + tile_pivot;
            }
        }
        public bool IsValid()
        {
            return NaviMask != NULL_TILE_NAVI;
        }
    }
}