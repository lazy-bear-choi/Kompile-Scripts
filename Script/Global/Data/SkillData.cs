using MessagePack;

namespace Script.GamePlay.Global.Data
{
    public struct SkillData
    {
        [Key(0)] public int SkillID { get; set; }
        [Key(1)] public string AnimationStateName { get; set; }
        [Key(2)] public int StartupTicks { get; set; }
        [Key(3)] public int ActiveTicks { get; set; }
        [Key(4)] public int RecoveryTicks { get; set; }
        [Key(5)] public int HitFrameOffset { get; set; }
        [Key(6)] public int ComboWindow { get; set; }
    }
}