using System.Text.Json;
using Flurl.Http;

namespace Gnoj_Ham_Library;

/// <summary>
/// These class manages http callbacks
/// </summary>
internal class CallBackPivot
{
    private readonly string _baseUrl;

    /// <summary>
    /// Ctor.
    /// </summary>
    /// <param name="baseUrl"></param>
    internal CallBackPivot(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    private async Task CallWithDataAsync(string route, object? request)
    {
        try
        {
            await $"{_baseUrl}{route}"
                .PostAsync(request == null ? null : new StringContent(
                    JsonSerializer.Serialize(request),
                    System.Text.Encoding.UTF8,
                    "application/json"))
                .ConfigureAwait(false);
        }
        catch
        {
            // TODO: basic error management
        }
    }

    internal void Notify(string route, object? request)
    {
        // fire and forger
        Task.Run(() => CallWithDataAsync(route, request));
    }
}
