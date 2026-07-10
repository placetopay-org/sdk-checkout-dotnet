using PlaceToPay.Checkout.Errors;
using PlaceToPay.Checkout.Models.Common;
using PlaceToPay.Checkout.Models.Session;
using Xunit;

namespace PlaceToPay.Checkout.Tests.Integration;

/// <summary>
/// End-to-end smoke test against the PlaceToPay sandbox. Gated by environment
/// variables — set <c>PLACETOPAY_LOGIN</c> and <c>PLACETOPAY_SECRET_KEY</c>
/// to run. Never commit credentials to source control.
/// </summary>
/// <remarks>
/// <para>
/// Exercises the happy path of session lifecycle — the endpoints the sandbox
/// can validate without a human completing a checkout UI flow:
/// <c>CreateSessionAsync</c>, <c>QuerySessionAsync</c>,
/// <c>CancelSessionAsync</c>, and a post-cancel <c>QuerySessionAsync</c>.
/// </para>
/// <para>
/// Tagged <c>Category=Integration</c> so CI can exclude it by default; run
/// locally with:
/// <code>
/// PLACETOPAY_LOGIN=... PLACETOPAY_SECRET_KEY=... \
///   dotnet test --filter "Category=Integration"
/// </code>
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
public class SandboxSmokeTests
{
    private static string? Login => System.Environment.GetEnvironmentVariable("PLACETOPAY_LOGIN");
    private static string? Secret => System.Environment.GetEnvironmentVariable("PLACETOPAY_SECRET_KEY");

    [Fact(DisplayName = "end-to-end: create → query → cancel → query")]
    public async Task SessionLifecycleAgainstSandbox()
    {
        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Secret))
        {
            // Env vars not set — treat as no-op so CI without creds stays green.
            return;
        }

        using var client = new CheckoutClient(new CheckoutClientOptions
        {
            Login = Login!,
            SecretKey = Secret!,
            Environment = PlaceToPay.Checkout.Environment.Sandbox,
        });

        // 1) create
        var created = await client.CreateSessionAsync(new CreateSessionRequest
        {
            IpAddress = "127.0.0.1",
            UserAgent = "checkout-dotnet-smoke/1.0",
            ReturnUrl = "https://example.com/return",
            Payment = new Payment
            {
                Reference = $"smoke-net-{Guid.NewGuid():N}"[..12],
                Description = ".NET SDK sandbox smoke test",
                Amount = new Amount { Currency = "COP", Total = 50_000m },
            },
        });

        Assert.NotNull(created.ProcessUrl);
        Assert.True(created.RequestId > 0, $"Expected requestId > 0, got {created.RequestId}");
        var requestId = created.RequestId!.Value;

        // 2) query — not an error whether it's PENDING or OK.
        var info = await client.QuerySessionAsync(requestId);
        Assert.NotNull(info.Status);

        // 3) cancel
        var cancel = await client.CancelSessionAsync(requestId);
        Assert.NotNull(cancel.Status);

        // 4) query again — should now be REJECTED.
        var afterCancel = await client.QuerySessionAsync(requestId);
        Assert.NotNull(afterCancel.Status);
        Assert.True(afterCancel.Status.IsRejected,
            $"Expected REJECTED after cancel; got {afterCancel.Status.Code}");
    }
}
