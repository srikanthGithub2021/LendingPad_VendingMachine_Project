using System.Net.Http.Json;

using static Data.VendingDtos;

namespace VendingMachineApi.Services;

public interface IThirdPartyPaymentClient
{
    Task<ThirdPartyPaymentResponse> ChargeAsync(ThirdPartyPaymentRequest request);
    Task<bool> RefundAsync(string referenceId, decimal amount);
}

public class ThirdPartyPaymentClient : IThirdPartyPaymentClient
{
    private readonly HttpClient _httpClient;

    public ThirdPartyPaymentClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ThirdPartyPaymentResponse> ChargeAsync(ThirdPartyPaymentRequest request)
    {
        // Calls external Payment Gateway REST API (e.g., Stripe/Adyen/UPI aggregator)
        var response = await _httpClient.PostAsJsonAsync("https://mock-payment-gateway.local", request);

        if (!response.IsSuccessStatusCode)
        {
            return new ThirdPartyPaymentResponse(false, string.Empty, "Payment gateway declined or unreachable.");
        }

        var result = await response.Content.ReadFromJsonAsync<ThirdPartyPaymentResponse>();
        return result ?? new ThirdPartyPaymentResponse(false, string.Empty, "Invalid gateway response.");
    }

    public async Task<bool> RefundAsync(string referenceId, decimal amount)
    {
        var response = await _httpClient.PostAsJsonAsync("https://mock-payment-gateway.local", new { referenceId, amount });
        return response.IsSuccessStatusCode;
    }
}

/// <summary>
/// Simulated HTTP handler so the third-party API works out-of-the-box without external internet calls.
/// </summary>
public class MockPaymentGatewayHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri!.AbsolutePath.EndsWith("/charges"))
        {
            var payload = await request.Content!.ReadFromJsonAsync<ThirdPartyPaymentRequest>(cancellationToken);
            // Simulate insufficient funds if token equals "DECLINE_TOKEN"
            if (payload?.Token == "DECLINE_TOKEN")
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.PaymentRequired)
                {
                    Content = JsonContent.Create(new ThirdPartyPaymentResponse(false, "", "Insufficient funds on card/UPI."))
                };
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ThirdPartyPaymentResponse(true, $"GW-{Guid.NewGuid():N}"[..12], "Approved"))
            };
        }

        return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
    }
}

public interface IHardwareController
{
    Task<bool> DispenseSlotAsync(int slotNumber);
    Task<bool> ReturnCashChangeAsync(decimal changeAmount);
}

public class MockHardwareController : IHardwareController
{
    public Task<bool> DispenseSlotAsync(int slotNumber)
    {
        // Simulates GPIO motor rotation + infrared drop sensor confirmation
        Console.WriteLine($"[HARDWARE] Rotating coil for Slot #{slotNumber}... Drop sensor confirmed!");
        return Task.FromResult(true);
    }

    public Task<bool> ReturnCashChangeAsync(decimal changeAmount)
    {
        if (changeAmount > 0)
            Console.WriteLine($"[HARDWARE] Dispensing cash change: ${changeAmount:F2}");
        return Task.FromResult(true);
    }
}
