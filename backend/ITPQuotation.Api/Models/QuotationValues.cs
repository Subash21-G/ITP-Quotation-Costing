namespace ITPQuotation.Api.Models;

public static class QuotationValues
{
    public const string Draft = "Draft";
    public const string Ready = "Ready";
    public const string Sent = "Sent";
    public const string UnderReview = "UnderReview";
    public const string Negotiation = "Negotiation";
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";
    public const string Lost = "Lost";
    public const string Expired = "Expired";
    public const string PoReceived = "POReceived";

    public static readonly string[] Statuses =
    [
        Draft,
        Ready,
        Sent,
        UnderReview,
        Negotiation,
        Accepted,
        Rejected,
        Lost,
        Expired,
        PoReceived
    ];
}
