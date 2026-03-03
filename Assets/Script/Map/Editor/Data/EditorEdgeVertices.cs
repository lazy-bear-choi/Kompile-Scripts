namespace Script.Map.Editor.Data
{
    public readonly struct EditorEdgeVertices
    {
        public readonly int center;
        public readonly int side0;
        public readonly int side1;

        public EditorEdgeVertices(int c, int s0, int s1)
        {
            center = c;
            side0 = s0;
            side1 = s1;
        }
    }
}