using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ITPQuotation.Api.Services;

public sealed record PoTrackerProcessOption(int Id, string ProcessName, string VendorName, string Source);

public sealed class PoTrackerMastersheetException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class PoTrackerMastersheetService(IConfiguration configuration, HttpClient http)
{
    public async Task<IReadOnlyList<PoTrackerProcessOption>> GetProcessOptionsAsync(
        CancellationToken cancellationToken)
    {
        var key = configuration["POTracker:ApiKey"];
        if (!Uri.TryCreate(configuration["POTracker:BaseUrl"], UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != "https" && !(baseUri.Scheme == "http" && baseUri.IsLoopback))
            || !string.IsNullOrEmpty(baseUri.UserInfo) || !string.IsNullOrWhiteSpace(baseUri.Query)
            || !string.IsNullOrWhiteSpace(baseUri.Fragment) || string.IsNullOrWhiteSpace(key))
            throw new PoTrackerMastersheetException(
                503,
                "POTracker connection is not configured. Configure its URL and shared API key in the backend.");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/api/quotation-integration/mastersheet/process-options"));
        request.Headers.Add("X-Quotation-Api-Key", key);

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var message = response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                    ? "POTracker rejected the shared API key. Ask the administrator to check both backends."
                    : "POTracker process/vendor options could not be loaded.";
                throw new PoTrackerMastersheetException(502, message);
            }

            var options = await response.Content.ReadFromJsonAsync<PoTrackerProcessOption[]>(cancellationToken);
            if (options is null || options.Any(option =>
                option.Id <= 0 ||
                string.IsNullOrWhiteSpace(option.ProcessName) ||
                option.Source is not ("In-house" or "Outsource")))
                throw new PoTrackerMastersheetException(502, "POTracker returned invalid process/vendor options.");

            return options
                .Select(option => option with { VendorName = option.VendorName ?? string.Empty })
                .ToArray();
        }
        catch (HttpRequestException)
        {
            throw new PoTrackerMastersheetException(502, "Unable to reach POTracker. Retry loading process/vendor options.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new PoTrackerMastersheetException(504, "POTracker timed out while loading process/vendor options.");
        }
        catch (JsonException)
        {
            throw new PoTrackerMastersheetException(502, "POTracker returned unreadable process/vendor options.");
        }
    }
}
