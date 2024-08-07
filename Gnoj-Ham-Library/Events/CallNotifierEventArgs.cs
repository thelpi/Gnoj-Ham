﻿using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library.Events;

/// <summary>
/// Event triggered when a call has to be notified.
/// </summary>
public class CallNotifierEventArgs : EventArgs
{
    /// <summary>
    /// The player index.
    /// </summary>
    public PlayerIndices PlayerIndex { get; internal init; }

    /// <summary>
    /// The type of call.
    /// </summary>
    public CallTypes Action { get; internal init; }
}
