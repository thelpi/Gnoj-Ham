using Gnoj_Ham_ViewModel.Services;
using Gnoj_Ham_View.Properties;

namespace Gnoj_Ham_View.Services;

/// <summary>
/// The user's settings, kept in the application settings file.
/// </summary>
internal sealed class WpfUserSettings : IUserSettings
{
    /// <inheritdoc />
    public string DefaultPlayerName
    {
        get => Settings.Default.DefaultPlayerName;
        set => Settings.Default.DefaultPlayerName = value;
    }

    /// <inheritdoc />
    public int ChronoSpeed
    {
        get => Settings.Default.ChronoSpeed;
        set => Settings.Default.ChronoSpeed = value;
    }

    /// <inheritdoc />
    public int CpuSpeed
    {
        get => Settings.Default.CpuSpeed;
        set => Settings.Default.CpuSpeed = value;
    }

    /// <inheritdoc />
    public bool PlaySounds
    {
        get => Settings.Default.PlaySounds;
        set => Settings.Default.PlaySounds = value;
    }

    /// <inheritdoc />
    public bool AutoCallMahjong
    {
        get => Settings.Default.AutoCallMahjong;
        set => Settings.Default.AutoCallMahjong = value;
    }

    /// <inheritdoc />
    public bool DiscardTip
    {
        get => Settings.Default.DiscardTip;
        set => Settings.Default.DiscardTip = value;
    }

    /// <inheritdoc />
    public int InitialPointsRule
    {
        get => Settings.Default.InitialPointsRule;
        set => Settings.Default.InitialPointsRule = value;
    }

    /// <inheritdoc />
    public int EndOfGameRule
    {
        get => Settings.Default.EndOfGameRule;
        set => Settings.Default.EndOfGameRule = value;
    }

    /// <inheritdoc />
    public bool UseRedDoras
    {
        get => Settings.Default.UseRedDoras;
        set => Settings.Default.UseRedDoras = value;
    }

    /// <inheritdoc />
    public bool UseNagashiMangan
    {
        get => Settings.Default.UseNagashiMangan;
        set => Settings.Default.UseNagashiMangan = value;
    }

    /// <inheritdoc />
    public bool UseMultipleYakumans
    {
        get => Settings.Default.UseMultipleYakumans;
        set => Settings.Default.UseMultipleYakumans = value;
    }

    /// <inheritdoc />
    public bool UseKazoeYakuman
    {
        get => Settings.Default.UseKazoeYakuman;
        set => Settings.Default.UseKazoeYakuman = value;
    }

    /// <inheritdoc />
    public bool UseSuufonRenda
    {
        get => Settings.Default.UseSuufonRenda;
        set => Settings.Default.UseSuufonRenda = value;
    }

    /// <inheritdoc />
    public bool UseDoubleYakuman
    {
        get => Settings.Default.UseDoubleYakuman;
        set => Settings.Default.UseDoubleYakuman = value;
    }

    /// <inheritdoc />
    public int UmaRule
    {
        get => Settings.Default.UmaRule;
        set => Settings.Default.UmaRule = value;
    }

    /// <inheritdoc />
    public void Save()
    {
        Settings.Default.Save();
    }
}
