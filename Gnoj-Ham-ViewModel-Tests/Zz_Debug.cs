using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel;
using Gnoj_Ham_ViewModel_Tests.Fakes;

namespace Gnoj_Ham_ViewModel_Tests;

public class Zz_Debug
{
    [Fact]
    public async Task Debug()
    {
        var lines = new List<string>();
        var settings = new FakeUserSettings { CpuSpeed = (int)CpuSpeedPivot.S0, ChronoSpeed = (int)ChronoPivot.Short };
        var manual = new ManualDelay();
        var setup = new HumanGameSetup("Me", RulePivot.Default, new PlayerStatisticsPivot(), null, false, new Random(1));
        var dlg = new FakeDialogService(); var vm = new GameViewModel(setup, dlg, settings, new FakePlayerStatisticsStorage(), new FakeUiDispatcher(), manual, new FakeAnimationService(), new FakeSoundService());
        await vm.StartCommand.ExecuteAsync(null);
        await vm.WhenIdleAsync();
        var h = vm.Table.Human;
        var r = vm.Game.Round;
        lines.Add($"mustDiscard={r.IsHumanPlayer && r.GetHand(PlayerIndices.Zero).IsFullHand} panel={h.IsPanelVisible} pick={h.PickTile != null} timers={string.Join(",", manual.PendingDelays)} discards={r.GetDiscard(PlayerIndices.Zero).Count}");
        lines.Add($"messages={dlg.ShownMessages.Count}");
        await manual.ElapseAllAsync();
        lines.Add($"after yield: discards={r.GetDiscard(PlayerIndices.Zero).Count}");
        try { await vm.WhenIdleAsync(); } catch (Exception e) { lines.Add("IDLE EXCEPTION " + e); }
        lines.Add($"after idle: discards={r.GetDiscard(PlayerIndices.Zero).Count} timers={string.Join(",", manual.PendingDelays)} mustDiscard={r.IsHumanPlayer && r.GetHand(PlayerIndices.Zero).IsFullHand}");
        await h.SelectTileCommand.ExecuteAsync(h.PickTile);
        lines.Add($"after manual select: discards={r.GetDiscard(PlayerIndices.Zero).Count}");
        File.WriteAllLines(Path.Combine(Path.GetTempPath(), "zz_debug.txt"), lines);
    }
}
