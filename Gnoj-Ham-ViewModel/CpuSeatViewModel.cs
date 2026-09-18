using CommunityToolkit.Mvvm.ComponentModel;
using Gnoj_Ham_Library;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// One CPU seat of an auto-play batch: which logic plays it.
/// </summary>
public sealed partial class CpuSeatViewModel : ObservableObject
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="selected">The initially chosen logic.</param>
    public CpuSeatViewModel(CpuManagerOption selected)
    {
        _selected = selected;
    }

    /// <summary>
    /// The logic playing this seat.
    /// </summary>
    [ObservableProperty]
    private CpuManagerOption _selected;
}
