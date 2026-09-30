namespace Content.Shared.NodeContainer.NodeGroups;

public enum NodeGroupID : byte
{
    Default,
    HVPower,
    MVPower,
    Apc,
    AMEngine,
    Pipe,
    WireNet,

    /// <summary>
    /// Group used by the TEG.
    /// </summary>
    /// <seealso cref="Content.Server.Power.Generation.Teg.TegSystem"/>
    /// <seealso cref="Content.Server.Power.Generation.Teg.TegNodeGroup"/>
    Teg,

    /// <summary>
    /// Liquid plumbing (sewage and water supply) pipes.
    /// </summary>
    /// <seealso cref="Content.Server._Horizon.Plumbing.NodeGroups.PlumbingNet"/>
    Plumbing,
}
