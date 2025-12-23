namespace Script.Index
{
    using System;

    public static class IDxInput
    {
        [Flags]
        public enum InputFlag
        {
            NONE     = 0x00,

            DOWN        = 0x01,
            UP          = 0x02,
            LEFT        = 0x04,
            RIGHT       = 0x08,
            MOVE_ALL    = 0x0F,

            ENTER       = 0x10,
            CANCEL      = 0x20,
            ESCAPE      = 0x40,
            ACTION      = 0x80,
            ACT_ALL     = 0xF0
        }

        public static bool Contains(this InputFlag input, InputFlag compares)
        {
            return 0 != (input & compares);
        }
    }
}
