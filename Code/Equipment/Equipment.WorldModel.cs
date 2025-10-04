namespace Mountain;

public sealed partial class Equipment
{
    public EquipmentWorldModel? WorldModel { get; set; }

    private void CreateWorldModel()
    {
        if (Resource.WorldModelPrefab == null)
        {
            return;
        }

        DestroyWorldModel();

        var parentBone = Owner.RightHandSocket;
        var gameObject = Resource.WorldModelPrefab.Clone(new CloneConfig
            { Parent = parentBone, StartEnabled = false, Transform = global::Transform.Zero });
        gameObject.BreakFromPrefab();

        gameObject.Flags |= GameObjectFlags.NotSaved | GameObjectFlags.NotNetworked;
        gameObject.Enabled = true;

        WorldModel = gameObject.GetComponent<EquipmentWorldModel>();
        WorldModel.ModelRenderer.Model = Resource.WorldModel;
        
        // TODO FIX ME LATER
        // ViewModel.Muzzle = ViewModel.ModelRenderer.GetBoneObject(Resource.MuzzleBoneName);
        // ViewModel.EjectionPort = ViewModel.ModelRenderer.GetBoneObject(Resource.EjectionPortBoneName);
    }

    private void DestroyWorldModel()
    {
        WorldModel?.DestroyGameObject();
        WorldModel = null;
    }
}