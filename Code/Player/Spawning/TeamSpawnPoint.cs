﻿namespace Mountain;

/// <summary>
///     A team spawn point.
/// </summary>
public sealed class TeamSpawnPoint : Component
{
	/// <summary>
	///     What team is this for?
	/// </summary>
	[Property]
    public Team Team { get; set; }
    private static readonly Model Model = Model.Load("models/editor/spawnpoint.vmdl");

    protected override void DrawGizmos()
    {
        Gizmo.Hitbox.Model(Model);
        Gizmo.Draw.Color = Team.GetColor().WithAlpha(Gizmo.IsHovered || Gizmo.IsSelected ? 0.7f : 0.5f);

        var so = Gizmo.Draw.Model(Model);

        if (so is not null)
        {
            so.Flags.CastShadows = true;
        }
    }
}