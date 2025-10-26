namespace Mountain;

public abstract class EquipmentModel : Component
{
    [Property]
    public SkinnedModelRenderer ModelRenderer { get; init; } = null!;

    [Property]
    public GameObject Muzzle { get; init; } = null!;

    [Property]
    public GameObject EjectionPort { get; init; } = null!;
}