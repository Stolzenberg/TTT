using System;

namespace Mountain;

public sealed partial class Player
{
    [Sync(SyncFlags.FromHost)]
    public Guid OwnerId { get; set; }
}