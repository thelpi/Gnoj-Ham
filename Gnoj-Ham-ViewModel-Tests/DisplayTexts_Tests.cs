using Gnoj_Ham_ViewModel;

namespace Gnoj_Ham_ViewModel_Tests;

public class DisplayTexts_Tests
{
    [Fact]
    public void CpuSpeeds_AreShownInMillisecondsThenSeconds()
    {
        Assert.Equal(new[] { "2 sec", "1 sec", "500 ms", "200 ms", "0 ms" }, DisplayTexts.GetCpuSpeedDisplayValues());
    }

    [Fact]
    public void ChronoSpeeds_ShowTheirDelay()
    {
        Assert.Equal(new[] { "Aucun", "Long (20 sec)", "Court (5 sec)" }, DisplayTexts.GetChronoDisplayValues());
    }

    [Fact]
    public void InitialPointsRules_ShowThePointsWithAThousandsSeparator()
    {
        Assert.Equal(new[] { "25 000", "30 000" }, DisplayTexts.GetInitialPointsRuleDisplayValue());
    }

    [Fact]
    public void EndOfGameRules_AreNamed()
    {
        Assert.Equal(new[] { "Oorasu", "Tobi", "Enchousen", "Enchousen + Tobi" }, DisplayTexts.GetEndOfGameRuleDisplayValue());
    }

    [Fact]
    public void UmaRules_AreNamed()
    {
        Assert.Equal(new[] { "5 / 10", "10 / 20", "EMA (15 / 5)" }, DisplayTexts.GetUmaRuleDisplayValue());
    }

    [Fact]
    public void DrivenDrawScenarios_AreNamed()
    {
        Assert.Equal(
            new[] { "Aucun", "Kan possible au 1er tour", "2 Kans possibles au 1er tour" },
            DisplayTexts.GetDrivenDrawScenarioDisplayValue());
    }

    [Theory]
    [InlineData(CpuSpeedPivot.S2000, 2000)]
    [InlineData(CpuSpeedPivot.S500, 500)]
    [InlineData(CpuSpeedPivot.S0, 0)]
    public void CpuSpeed_ParsesToMilliseconds(CpuSpeedPivot speed, int milliseconds)
    {
        Assert.Equal(milliseconds, speed.ParseSpeed());
    }

    [Theory]
    [InlineData(ChronoPivot.None, 0)]
    [InlineData(ChronoPivot.Short, 5)]
    [InlineData(ChronoPivot.Long, 20)]
    public void Chrono_GivesItsDelayInSeconds(ChronoPivot chrono, int seconds)
    {
        Assert.Equal(seconds, chrono.GetDelay());
    }
}
