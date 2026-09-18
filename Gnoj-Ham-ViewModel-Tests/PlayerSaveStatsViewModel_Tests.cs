using System.Text.Json;
using Gnoj_Ham_Library;
using Gnoj_Ham_ViewModel;

namespace Gnoj_Ham_ViewModel_Tests;

public class PlayerSaveStatsViewModel_Tests
{
    private static PlayerStatisticsPivot Stats(string json)
        => JsonSerializer.Deserialize<PlayerStatisticsPivot>(json)!;

    [Fact]
    public void FreshStatistics_ShowNoDatesAndZeroPercents()
    {
        var viewModel = new PlayerSaveStatsViewModel(new PlayerStatisticsPivot());

        Assert.Equal("N/A", viewModel.FirstGame);
        Assert.Equal("N/A", viewModel.LatestGame);
        Assert.Equal(0, viewModel.GamesCount);
        Assert.Equal("0 %", viewModel.RiichiPercent);
        Assert.Equal("0 %", viewModel.BankruptPercent);
        Assert.Equal("1er : 0 %\n2ème : 0 %\n3ème : 0 %\n4ème : 0 %", viewModel.GamesPercent);
    }

    [Fact]
    public void Dates_AreFormattedDayMonthYear()
    {
        var stats = Stats("{\"FirstGame\":\"2024-03-09T10:00:00\",\"LastGame\":\"2025-12-31T23:00:00\"}");

        var viewModel = new PlayerSaveStatsViewModel(stats);

        Assert.Equal(new DateTime(2024, 3, 9).ToString("dd/MM/yyyy"), viewModel.FirstGame);
        Assert.Equal(new DateTime(2025, 12, 31).ToString("dd/MM/yyyy"), viewModel.LatestGame);
    }

    [Fact]
    public void RawCountsAreExposedAsIs()
    {
        var stats = Stats("{\"GameCount\":7,\"RoundCount\":40,\"RiichiCount\":11,\"BankruptCount\":1,\"TsumoCount\":4,\"RonCount\":6,\"YakumanCount\":2,\"OpenedHandCount\":9}");

        var viewModel = new PlayerSaveStatsViewModel(stats);

        Assert.Equal(7, viewModel.GamesCount);
        Assert.Equal(40, viewModel.RoundsCount);
        Assert.Equal(11, viewModel.RiichiCount);
        Assert.Equal(1, viewModel.BankruptCount);
        Assert.Equal(4, viewModel.TsumoCount);
        Assert.Equal(6, viewModel.RonCount);
        Assert.Equal(2, viewModel.YakumanCount);
        Assert.Equal(9, viewModel.OpenedHandsCount);
    }

    [Fact]
    public void Percents_AreRelativeToRoundsExceptGamesAndBankruptcies()
    {
        // 10 rounds, 4 games: riichi 3/10 = 30 %, bankrupt 1/4 = 25 %, 1st place 2/4 = 50 %.
        var stats = Stats("{\"GameCount\":4,\"RoundCount\":10,\"RiichiCount\":3,\"BankruptCount\":1,\"ByPositionCount\":[2,1,1,0]}");

        var viewModel = new PlayerSaveStatsViewModel(stats);

        Assert.Equal("30 %", viewModel.RiichiPercent);
        Assert.Equal("25 %", viewModel.BankruptPercent);
        Assert.Equal("1er : 50 %\n2ème : 25 %\n3ème : 25 %\n4ème : 0 %", viewModel.GamesPercent);
    }

    [Fact]
    public void Percents_AreRounded()
    {
        // 1/3 = 33.33 % rounds down, 2/3 = 66.67 % rounds up.
        var stats = Stats("{\"RoundCount\":3,\"RonCount\":1,\"TsumoCount\":2}");

        var viewModel = new PlayerSaveStatsViewModel(stats);

        Assert.Equal("33 %", viewModel.RonPercent);
        Assert.Equal("67 %", viewModel.TsumoPercent);
    }
}
