namespace Gnoj_Ham_ViewModel.Services;

/// <summary>
/// Marshals work onto the UI thread. The engine's auto-play loop and its timers run off the UI
/// thread, while anything a view is bound to must be changed on it.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>
    /// Runs <paramref name="action"/> on the UI thread and waits for it to complete.
    /// </summary>
    /// <param name="action">The action to run.</param>
    void Invoke(Action action);

    /// <summary>
    /// Runs <paramref name="action"/> on the UI thread without blocking the caller.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <returns>A task completing once the action has run.</returns>
    Task InvokeAsync(Action action);
}
