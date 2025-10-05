using System;

namespace Mountain;

[Flags]
public enum HitboxTags
{
    None = 0,
    Head = 1,
    Chest = 2,
    Stomach = 4,
    Clavicle = 8,
    Arm = 16,
    Hand = 32,
    Leg = 64,
    Ankle = 128,
    Spine = 256,
    Neck = 512,

    UpperBody = Neck | Chest | Clavicle,
    LowerBody = Stomach,
}