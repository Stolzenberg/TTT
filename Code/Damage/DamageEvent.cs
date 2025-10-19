using Sandbox.Events;

namespace Mountain;

/// <summary>
///     Event dispatched on a root object containing a <see cref="Health" /> that took damage.
/// </summary>
/// <param name="Damage">Information about the damage.</param>
public record DamageTakenEvent(DamageInfo DamageInfo) : IGameEvent;

/// <summary>
///     Event dispatched to everything when a <see cref="Health" /> takes damage.
/// </summary>
/// <param name="DamageInfo"></param>
public record DamageTakenGlobalEvent(DamageInfo DamageInfo) : IGameEvent;

/// <summary>
///     Event dispatched on a root object that inflicted damage on another object.
/// </summary>
/// <param name="Damage">Information about the damage.</param>
public record DamageGivenEvent(DamageInfo DamageInfo) : IGameEvent;

/// <summary>
///     Event dispatched in the scene when a <see cref="Health" /> died after taking damage.
/// </summary>
/// <param name="Damage">Information about the killing blow.</param>
public record GlobalKillEvent(DamageInfo DamageInfo) : IGameEvent;

/// <summary>
///     Event dispatched on a root object when a <see cref="Health" /> died after taking damage.
/// </summary>
/// <param name="Damage">Information about the killing blow.</param>
public record KillEvent(DamageInfo DamageInfo) : IGameEvent;

/// <summary>
///     Event dispatched on a root object that killed another object.
/// </summary>
/// <param name="Damage">Information about the damage.</param>
public record KilledEvent(DamageInfo DamageInfo) : IGameEvent;