namespace Content.Shared._Horizon.Plumbing;

/// <summary>
/// A plumbing device that does its work on a timer instead of every tick. The server side of it is a
/// PlumbingTimedSystem, which spreads the devices over the interval and calls their tick.
/// </summary>
public interface IPlumbingTimed
{
    /// <summary>
    /// Time between two ticks of the device.
    /// </summary>
    TimeSpan UpdateInterval { get; }

    /// <summary>
    /// When the device ticks next. Components pause it with AutoPausedField.
    /// </summary>
    TimeSpan NextUpdate { get; set; }
}
