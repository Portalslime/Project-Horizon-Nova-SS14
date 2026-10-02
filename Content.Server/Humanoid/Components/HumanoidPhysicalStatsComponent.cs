using System;
using System.Collections.Generic;

namespace Content.Server.Humanoid;

/// <summary>
/// Horizon: хранит исходные плотности фикстур тела, чтобы рост/вес можно было
/// применять многократно без накопления.
/// </summary>
[RegisterComponent]
public sealed partial class HumanoidPhysicalStatsComponent : Component
{
    [NonSerialized]
    public readonly Dictionary<string, float> BaseDensities = new();
}
