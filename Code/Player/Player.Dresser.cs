using System;

namespace Mountain;

public sealed partial class Player : Component.ExecuteInEditor
{
    /// <summary>
    ///     Where to get the clothing from
    /// </summary>
    [Property, Feature("Dresser")]
    public ClothingSource Source { get; set; } = ClothingSource.Manual;

    /// <summary>
    ///     Who are we dressing? This should be the renderer of the body of a Citizen or Human
    /// </summary>
    /// <summary>
    ///     Should we change the height too?
    /// </summary>
    [Property, Feature("Dresser")]
    public bool ApplyHeightScale { get; set; } = true;

    [ShowIf("Source", ClothingSource.Manual), Property, Feature("Dresser")]
    public List<ClothingContainer.ClothingEntry> Clothing { get; set; } = [];

    public enum ClothingSource
    {
        Manual,
        LocalUser,
        OwnerConnection,
    }

    [Button("Apply Clothing"), Feature("Dresser")]
    public void ApplyClothing()
    {
        if (!BodyRenderer.IsValid())
        {
            return;
        }

        var clothing = GetClothing();
        if (!ApplyHeightScale)
        {
            clothing.Height = 1;
        }

        clothing.AddRange(Clothing);
        clothing.Normalize();

        clothing.Apply(BodyRenderer);

        BodyRenderer.PostAnimationUpdate();
        
        // Render only the shadows for the third person renderer if its locally controlled.
        foreach (var renderer in BodyRenderer.GetComponentsInChildren<ModelRenderer>(true))
        {
            renderer.RenderType = IsLocallyControlled ? ModelRenderer.ShadowRenderType.ShadowsOnly : ModelRenderer.ShadowRenderType.On;
        }
    }

    private ClothingContainer GetClothing()
    {
        switch (Source)
        {
            case ClothingSource.OwnerConnection:
            {
                var clothing = new ClothingContainer();

                if (Network.Owner != null)
                {
                    clothing.Deserialize(Network.Owner.GetUserData("avatar"));
                }

                return clothing;
            }
            case ClothingSource.LocalUser:
            {
                return ClothingContainer.CreateFromLocalUser();
            }
            case ClothingSource.Manual:
            {
                var manuelClothing = new ClothingContainer();
                manuelClothing.AddRange(Clothing);
                manuelClothing.Normalize();

                return manuelClothing;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}