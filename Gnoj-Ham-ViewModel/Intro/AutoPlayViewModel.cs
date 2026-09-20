using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// Plays a batch of games between four CPUs - each driven by the logic picked for its seat - and
/// gathers the players' statistics over the whole batch: meant to compare CPU logics.
/// </summary>
public sealed partial class AutoPlayViewModel : ObservableObject
{
    private readonly RulePivot _ruleset;
    private readonly IDialogService _dialogs;
    private readonly IUiDispatcher _dispatcher;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly object _statsLock = new();
    private readonly object _progressLock = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="ruleset">The ruleset the games are played with.</param>
    /// <param name="dialogs">Shows messages.</param>
    /// <param name="dispatcher">Brings progress updates back onto the UI thread.</param>
    public AutoPlayViewModel(RulePivot ruleset, IDialogService dialogs, IUiDispatcher dispatcher)
    {
        _ruleset = ruleset;
        _dialogs = dialogs;
        _dispatcher = dispatcher;

        Seats = GamePivot.PerPlayer(_ => new CpuSeatViewModel(CpuManagerCatalog.Default));
    }

    /// <summary>
    /// Raised once every game of the batch is over.
    /// </summary>
    public event EventHandler? BatchCompleted;

    /// <summary>
    /// The logics a seat can be given.
    /// </summary>
    public IReadOnlyList<CpuManagerOption> CpuOptions => CpuManagerCatalog.Implementations;

    /// <summary>
    /// The four CPU seats.
    /// </summary>
    public IReadOnlyList<CpuSeatViewModel> Seats { get; }

    /// <summary>
    /// The number of games to play, as typed.
    /// </summary>
    [ObservableProperty]
    private string _gamesCountText = "10";

    /// <summary>
    /// Where the batch is at.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreSeatsEditable))]
    [NotifyPropertyChangedFor(nameof(IsActionPanelVisible))]
    [NotifyPropertyChangedFor(nameof(IsWaitingPanelVisible))]
    [NotifyPropertyChangedFor(nameof(AreResultsVisible))]
    private AutoPlayState _state = AutoPlayState.Idle;

    /// <summary>
    /// The share of the batch's games already played, from 0 to 1. Only ever moves forward.
    /// </summary>
    [ObservableProperty]
    private double _progress;

    /// <summary>
    /// The players, with their statistics over the whole batch; <c>Null</c> until the batch is over.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<PlayerPivot>? _results;

    /// <summary>
    /// Inferred; the seats can be changed except while games are being played.
    /// </summary>
    public bool AreSeatsEditable => State != AutoPlayState.Running;

    /// <summary>
    /// Inferred; the games count and the start button show except while games are being played.
    /// </summary>
    public bool IsActionPanelVisible => State != AutoPlayState.Running;

    /// <summary>
    /// Inferred; the progress panel shows while games are being played.
    /// </summary>
    public bool IsWaitingPanelVisible => State == AutoPlayState.Running;

    /// <summary>
    /// Inferred; the results show once the batch is over.
    /// </summary>
    public bool AreResultsVisible => State == AutoPlayState.Finished;

    /// <summary>
    /// Stops the batch: no further game is started, and the ones in progress are abandoned.
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    // Plays every game of the batch to completion, several at a time (bounded by the machine's core
    // count) off the UI thread. Each game runs on its own throwaway PlayerPivot set - sharing the
    // permanent players directly across concurrently-running games would mean several games writing
    // CurrentGamePoints at once - then its final ranking is committed onto the permanent players (the
    // ones actually displayed and accumulating stats across the whole batch) under a lock, since
    // several games can finish at nearly the same moment.
    [RelayCommand]
    private async Task StartAsync()
    {
        if (!int.TryParse(GamesCountText, out var totalGamesCount))
        {
            _dialogs.ShowMessage("Invalid number of games!", "Gnoj-Ham - Error");
            return;
        }

        var factories = new Dictionary<PlayerIndices, Func<RoundPivot, CpuManagerBasePivot>>();
        var nameSuffixes = new Dictionary<PlayerIndices, string>();
        for (var i = 0; i < Seats.Count; i++)
        {
            var playerIndex = (PlayerIndices)i;
            var option = Seats[i].Selected;
            factories[playerIndex] = round => (CpuManagerBasePivot)Activator.CreateInstance(option.Type, round)!;
            nameSuffixes[playerIndex] = option.DisplayName;
        }
        var permanentPlayers = PlayerPivot.BuildPlayers(null, nameSuffixes);

        Progress = 0;
        State = AutoPlayState.Running;

        var completedGamesCount = 0;
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = _cancellationTokenSource.Token
        };

        try
        {
            await Parallel.ForEachAsync(Enumerable.Range(0, totalGamesCount), options, (_, cancellationToken) =>
            {
                var gamePlayers = PlayerPivot.BuildPlayers(null);
                var game = new GamePivot(_ruleset, gamePlayers, new Random(), factories);

                EndOfRoundInformationsPivot endOfRoundInfo;
                do
                {
                    var result = game.Round.RunAutoPlay(cancellationToken);

                    // The engine just stops playing when cancelled: what follows would be a truncated round.
                    cancellationToken.ThrowIfCancellationRequested();

                    endOfRoundInfo = game.NextRound(result.RonPlayerId);
                } while (!endOfRoundInfo.EndOfGame);

                lock (_statsLock)
                {
                    game.ComputeCurrentRanking(permanentPlayers);
                }

                var done = Interlocked.Increment(ref completedGamesCount);
                _dispatcher.Invoke(() => SetProgress(done / (double)totalGamesCount));

                return ValueTask.CompletedTask;
            });
        }
        catch (OperationCanceledException)
        {
            // The window is closing: no UI left to update.
            return;
        }

        Results = permanentPlayers;
        State = AutoPlayState.Finished;
        BatchCompleted?.Invoke(this, EventArgs.Empty);
    }

    // Games complete out of order once run in parallel, so this only ever moves forward in whole
    // "one game done" steps rather than interpolating progress within any single still-running game.
    private void SetProgress(double value)
    {
        lock (_progressLock)
        {
            Progress = Math.Max(Progress, value);
        }
    }
}
