namespace Mountain;

public sealed class EquipmentSpawnPoint : Component
{
    private static readonly Model Model = Model.Load("models/arrow.vmdl");

    protected override void DrawGizmos()
    {
        Gizmo.Hitbox.Model(Model);

        var so = Gizmo.Draw.Model(Model);

        if (so is not null)
        {
            so.Flags.CastShadows = true;
        }
    }
}