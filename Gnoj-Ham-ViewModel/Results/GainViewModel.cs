using System.Globalization;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// A points variation, ready to display: the text (with an explicit <c>+</c> for a gain) and whether
/// the view should style it as a gain or a loss.
/// </summary>
public sealed class GainViewModel
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="value">The variation.</param>
    public GainViewModel(int value)
    {
        Value = value;
    }

    /// <summary>
    /// The variation.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Inferred; the text to display.
    /// </summary>
    public string Text => Value > 0
        ? $"+{Value.ToString(CultureInfo.InvariantCulture)}"
        : Value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Inferred; the kind of variation.
    /// </summary>
    public GainKind Kind => Value > 0 ? GainKind.Gain : (Value < 0 ? GainKind.Loss : GainKind.None);
}
