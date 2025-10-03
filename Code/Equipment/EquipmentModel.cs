namespace Mountain;

public abstract class EquipmentModel : Component
{
    [Property]
    public SkinnedModelRenderer ModelRenderer { get; init; } = null!;

    public GameObject Muzzle { get; set; } = null!;
    public GameObject EjectionPort { get; set; } = null!;
}