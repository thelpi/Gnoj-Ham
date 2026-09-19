using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// One of the buttons the human player can press to make a call: whether it is offered, whether it is
/// the one advised, and what pressing it does.
/// </summary>
public sealed partial class ActionButtonViewModel : ObservableObject
{
    private readonly Func<Task> _execute;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="execute">What pressing the button does.</param>
    public ActionButtonViewModel(Func<Task> execute)
    {
        _execute = execute;
    }

    /// <summary>
    /// Indicates if the action is offered to the player.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InvokeCommand))]
    private bool _isAvailable;

    /// <summary>
    /// Indicates if this is the action the advisor recommends.
    /// </summary>
    [ObservableProperty]
    private bool _isAdvised;

    // Pressing the button again while the game is still playing on is not an error: the game says
    // what it accepts at that point.
    [RelayCommand(CanExecute = nameof(IsAvailable), AllowConcurrentExecutions = true)]
    private Task Invoke() => _execute();

    internal void Reset()
    {
        IsAvailable = false;
        IsAdvised = false;
    }
}
