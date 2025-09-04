namespace Mountain;

public sealed partial class Equipment
{
    public EquipmentWorldModel? WorldModel { get; set; }

    private void CreateWorldModel()
    {
        DestroyWorldModel();

        var parentBone = Owner.RightHandSocket;
        var worldModelObj = Resource.WorldModelPrefab.Clone(new CloneConfig
            { Parent = parentBone, StartEnabled = false, Transform = global::Transform.Zero });

        worldModelObj.Flags |= GameObjectFlags.NotSaved | GameObjectFlags.NotNetworked;
        worldModelObj.Enabled = true;

        WorldModel = worldModelObj.GetComponent<EquipmentWorldModel>();
    }

    private void DestroyWorldModel()
    {
        WorldModel?.DestroyGameObject();
        WorldModel = null;
    }
}