namespace Mountain;

public abstract class EquipmentInputAction : EquipmentComponent
{
    public bool RunningWhileDeployed { get; private set; }

    /// <summary>
    ///     What input action are we going to listen for?
    /// </summary>
    [Property, Category("Base")]
    public List<string> InputActions { get; init; } = ["Attack1"];

    /// <summary>
    ///     Should we perform the action when ALL input actions match, or any?
    /// </summary>
    [Property, Category("Base")]
    public bool RequiresAllInputActions { get; init; }

    public bool IsDown { get; private set; }

    /// <summary>
    ///     Called when the input method succeeds
    /// </summary>
    protected virtual void OnInput()
    {
    }

    /// <summary>
    ///     When the button is up
    /// </summary>
    protected virtual void OnInputUp()
    {
    }

    /// <summary>
    ///     When the button is down
    /// </summary>
    protected virtual void OnInputDown()
    {
    }

    protected virtual void OnInputUpdate()
    {
    }

    protected override void OnFixedUpdate()
    {
        if (!Equipment.IsValid())
        {
            return;
        }

        if (!Equipment.IsDeployed)
        {
            return;
        }

        if (!Equipment.Owner.IsValid())
        {
            return;
        }

        if (Equipment.Owner.IsFrozen)
        {
            return;
        }

        if (!Equipment.Owner.Client.IsLocalPlayer)
        {
            return;
        }

        if (InputActions.All(x => !Input.Down(x)))
        {
            RunningWhileDeployed = false;
        }

        if (RunningWhileDeployed)
        {
            return;
        }

        OnInputUpdate();

        var matched = false;

        foreach (var down in InputActions.Select(action => Input.Down(action)))
        {
            if (RequiresAllInputActions && !down)
            {
                matched = false;

                break;
            }

            if (down)
            {
                matched = true;
            }
        }

        if (matched)
        {
            OnInput();

            if (IsDown)
            {
                return;
            }

            OnInputDown();
            IsDown = true;
        }
        else
        {
            if (!IsDown)
            {
                return;
            }

            OnInputUp();
            IsDown = false;
        }
    }
}