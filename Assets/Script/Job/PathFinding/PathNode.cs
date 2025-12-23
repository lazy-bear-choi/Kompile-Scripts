using System;

namespace Script.Util.PathFinding
{
    public struct PathNode //: IComparable<PathNode>
    {
        public int Index;
        public float F;

        //public int CompareTo(PathNode other)
        //{
        //    return F.CompareTo(other.F);
        //}
    }
}