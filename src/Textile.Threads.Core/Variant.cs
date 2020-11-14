using System;
namespace Textile.Threads.Core
{
    [Flags]
    public enum Variant : ulong
    {
        Raw = 0x55,
        AccessControlled = 0x70, // Supports access control lists
    }
}
