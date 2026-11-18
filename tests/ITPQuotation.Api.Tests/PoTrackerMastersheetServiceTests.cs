using System.Net;
using System.Net.Http.Json;
using ITPQuotation.Api.Services;
using Microsoft.Extensions.Configuration;

namespace ITPQuotation.Api.Tests;

public sealed class PoTrackerMastersheetServiceTests
{
    [Fact]
    public async Task FetchesProcessVendorOptionsUsingSharedIntegrationKey()
    {
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal(
                "http://localhost:5252/api/quotation-integration/mastersheet/process-options",
                request.RequestUri!.ToString());
            Assert.Equal("test-key", Assert.Single(request.Headers.GetValues("X-Quotation-Api-Key")));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new PoTrackerProcessOption(1, "Turning", "Mohan Engg", "Outsource"),
                    new PoTrackerProcessOption(2, "Tapping", "", "In-house")
                })
            });
        }));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["POTracker:BaseUrl"] = "http://localhost:5252",
            ["POTracker:ApiKey"] = "test-key"
        }).Build();

        var options = await new PoTrackerMastersheetService(configuration, http)
            .GetProcessOptionsAsync(default);

        Assert.Collection(options,
            option => Assert.Equal((1, "Turning", "Mohan Engg", "Outsource"),
                (option.Id, option.ProcessName, option.VendorName, option.Source)),
            option => Assert.Equal((2, "Tapping", "", "In-house"),
                (option.Id, option.ProcessName, option.VendorName, option.Source)));
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => send(request);
    }
}
