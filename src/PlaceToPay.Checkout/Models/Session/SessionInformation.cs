using System.Text.Json;
using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Models.Common;
using PlaceToPay.Checkout.Models.Transaction;

namespace PlaceToPay.Checkout.Models.Session;

/// <summary>
/// Full session view returned by <c>querySession</c> and <c>collect</c>.
/// </summary>
public sealed record SessionInformation
{
    [JsonPropertyName("requestId")] public long? RequestId { get; init; }
    [JsonPropertyName("status")] public required Status Status { get; init; }
    /// <summary>Echo of the original request (without <c>auth</c>). Opaque JSON.</summary>
    [JsonPropertyName("request")] public JsonElement? Request { get; init; }
    [JsonPropertyName("payment")] public IReadOnlyList<Transaction.Transaction>? Payment { get; init; }
    [JsonPropertyName("subscription")] public SubscriptionInformation? Subscription { get; init; }
    [JsonPropertyName("autopay")] public IReadOnlyList<JsonElement>? Autopay { get; init; }

    /// <summary>
    /// The most recent transaction on the session. When <paramref name="approvedOnly"/>
    /// is <c>true</c>, walks the list and returns the first APPROVED transaction
    /// (matches PHP's <c>lastTransaction($approved)</c>). Otherwise returns the last
    /// element in chronological order.
    /// </summary>
    public Transaction.Transaction? LastTransaction(bool approvedOnly = false)
    {
        if (Payment is null || Payment.Count == 0)
        {
            return null;
        }
        if (!approvedOnly)
        {
            return Payment[^1];
        }
        foreach (var txn in Payment)
        {
            if (txn.IsApproved)
            {
                return txn;
            }
        }
        return null;
    }

    /// <summary>
    /// The first approved transaction on this session, or <c>null</c> if none are
    /// approved yet. Shortcut for <c>LastTransaction(approvedOnly: true)</c>.
    /// </summary>
    [JsonIgnore]
    public Transaction.Transaction? LastApprovedTransaction => LastTransaction(approvedOnly: true);

    /// <summary>
    /// Authorization code of the last approved transaction, or <c>""</c> if none.
    /// Safe to call even when the session has no approved payments yet.
    /// </summary>
    public string LastAuthorization() =>
        LastApprovedTransaction?.Authorization ?? string.Empty;

    /// <summary>
    /// The last payment attempt on this session (any status), or <c>null</c>.
    /// Equivalent to <c>LastTransaction()</c>; kept for backward source compatibility.
    /// </summary>
    [JsonIgnore]
    public Transaction.Transaction? LastPaymentAttempt => LastTransaction();
}
