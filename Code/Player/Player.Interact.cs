namespace Mountain;

public sealed partial class Player
{
    /// <summary>
    ///     The object we're currently using by holding down USE
    /// </summary>
    public Component? Pressed { get; set; }
    public Component? Hovered { get; set; }

    [Property, Feature("Interact")]
    private readonly float reachLength = 20f;
    [Property, Feature("Interact")]
    private readonly float interactionRadius = 2f;

    private void UpdateLookAt()
    {
        if (Pressed.IsValid())
        {
            UpdatePressed();
        }
        else
        {
            UpdateHovered();
        }
    }

    private void UpdatePressed()
    {
        var flag = Input.Down("Use");
        if (flag && Pressed is IInteraction interaction)
        {
            var e = new IInteraction.Event
            {
                Source = this,
            };

            flag = interaction.Pressing(e);
        }

        var distance = GetDistanceFromGameObject(Pressed!.GameObject, EyePosition);

        if (distance > reachLength)
        {
            flag = false;
        }

        if (flag)
        {
            return;
        }

        StopPressing();
    }

    private float GetDistanceFromGameObject(GameObject obj, Vector3 point)
    {
        var distanceFromGameObject = Vector3.DistanceBetween(obj.WorldPosition, point);
        foreach (var componentsInChild in Pressed!.GetComponentsInChildren<Collider>())
        {
            var num = Vector3.DistanceBetween(componentsInChild.FindClosestPoint(point), point);

            if (num < (double)distanceFromGameObject)
            {
                distanceFromGameObject = num;
            }
        }

        return distanceFromGameObject;
    }

    private void StopPressing()
    {
        if (!Pressed.IsValid())
        {
            return;
        }

        if (Pressed is IInteraction interaction)
        {
            var e = new IInteraction.Event
            {
                Source = this,
            };

            interaction.Release(e);
        }

        Pressed = null;
    }

    private void UpdateHovered()
    {
        SwitchHovered();

        if (Hovered is IInteraction hovered)
        {
            var e = new IInteraction.Event
            {
                Source = this,
            };

            hovered.Look(e);
        }

        if (!Input.Pressed("Use"))
        {
            return;
        }

        StartPressing(Hovered);
    }

    private void StartPressing(Component? component)
    {
        StopPressing();
        if (!component.IsValid())
        {
            return;
        }

        var interaction = component.GetComponent<IInteraction>();
        if (interaction == null)
        {
            return;
        }

        if (interaction.CanPress(new()
            {
                Source = this,
            }))
        {
            interaction.Press(new()
            {
                Source = this,
            });
        }

        Pressed = component;
    }

    private void SwitchHovered()
    {
        var e = new IInteraction.Event
        {
            Source = this,
        };

        var component = TryGetLookedAt();
        if (Hovered == component)
        {
            if (Hovered is not IInteraction hovered)
            {
                return;
            }

            hovered.Look(e);
        }
        else
        {
            if (Hovered is IInteraction oldInteraction)
            {
                oldInteraction.Blur(e);
                Hovered = null;
            }

            Hovered = component;
            if (Hovered is not IInteraction newInteraction)
            {
                return;
            }

            newInteraction.Hover(e);
            newInteraction.Look(e);
        }
    }

    private Component? TryGetLookedAt()
    {
        var eyeTrace = Scene.Trace.Ray(EyePosition, EyePosition + EyeAngles.Forward * reachLength)
            .IgnoreGameObjectHierarchy(GameObject).Radius(interactionRadius).Run();

        if (!eyeTrace.Hit || !eyeTrace.GameObject.IsValid())
        {
            return null;
        }

        foreach (var component in eyeTrace.GameObject.GetComponents<IInteraction>())
        {
            if (component.CanPress(new()
                {
                    Source = this,
                }))
            {
                return component as Component;
            }
        }

        return null;
    }
}