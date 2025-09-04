using System;

namespace Mountain;

public sealed class TestInteraction : Component, IInteraction
{
    [Sync(SyncFlags.FromHost)]
    public Thing testThing { get; set; }
    [Sync(SyncFlags.FromHost)]
    public int Kills { get; set; }

    public bool Press(IInteraction.Event e)
    {
        Log.Info("Press");

        Log.Info(testThing.Name);
        Log.Info(testThing.Value.Name);
        Log.Info(testThing.Value.Value);
        Log.Info(Kills);

        return true;
    }

    public void Release(IInteraction.Event e)
    {
    }

    public bool Pressing(IInteraction.Event e)
    {
        return true;
    }

    public bool CanPress(IInteraction.Event e)
    {
        return true;
    }

    protected override void OnStart()
    {
        if (!Networking.IsHost)
        {
            return;
        }

        testThing = new()
        {
            Name = new('X', Random.Shared.Next(5, 15)),
            Value = new()
            {
                Name = new('Y', Random.Shared.Next(5, 15)),
                Value = Random.Shared.Next(1000),
            },
        };

        Kills = Random.Shared.Next(1000);
    }
}

public struct Thing
{
    public string Name { get; set; }
    public Test Value { get; set; }
}

public struct Test
{
    public string Name { get; set; }
    public int Value { get; set; }
}