using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_View.Services;

/// <summary>
/// Announces the calls in an overlay over the table, next to the seat that made them: the overlay's
/// storyboard shows it, then hides it again.
/// </summary>
internal sealed class WpfAnimationService : IAnimationService
{
    private readonly UIElement _overlay;
    private readonly Button _announcement;
    private readonly Storyboard _storyboard;
    private TaskCompletionSource? _pending;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="overlay">The overlay the announcement is shown in.</param>
    /// <param name="announcement">The element that holds the announcement, in the overlay.</param>
    /// <param name="storyboard">The storyboard that shows the overlay and hides it once the announcement is over.</param>
    public WpfAnimationService(UIElement overlay, Button announcement, Storyboard storyboard)
    {
        _overlay = overlay;
        _announcement = announcement;
        _storyboard = storyboard;

        Storyboard.SetTarget(_storyboard, overlay);
        (_storyboard.Children[^1] as ObjectAnimationUsingKeyFrames)!.KeyFrames[1].KeyTime =
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(CpuSpeedPivot.S500.ParseSpeed()));
        _storyboard.Completed += (_, _) => Complete();
    }

    /// <inheritdoc />
    public Task PlayCallAnnouncementAsync(CallTypes call, PlayerIndices playerIndex)
    {
        // An announcement starting while another is still playing takes its place: nobody should be
        // kept waiting for the one cut short.
        Complete();

        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending = pending;

        _announcement.Content = $"{call} !";
        _announcement.HorizontalAlignment = playerIndex == PlayerIndices.One ? HorizontalAlignment.Right : (playerIndex == PlayerIndices.Three ? HorizontalAlignment.Left : HorizontalAlignment.Center);
        _announcement.VerticalAlignment = playerIndex == PlayerIndices.Zero ? VerticalAlignment.Bottom : (playerIndex == PlayerIndices.Two ? VerticalAlignment.Top : VerticalAlignment.Center);
        _announcement.Margin = new Thickness(playerIndex == PlayerIndices.Three ? 20 : 0, playerIndex == PlayerIndices.Two ? 20 : 0, playerIndex == PlayerIndices.One ? 20 : 0, playerIndex == PlayerIndices.Zero ? 20 : 0);
        _overlay.Visibility = Visibility.Visible;
        _storyboard.Begin();

        return pending.Task;
    }

    private void Complete()
    {
        var pending = _pending;
        _pending = null;
        pending?.TrySetResult();
    }
}
