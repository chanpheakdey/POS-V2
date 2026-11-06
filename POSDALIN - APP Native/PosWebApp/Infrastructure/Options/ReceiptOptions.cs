namespace PosWebApp.Infrastructure.Options;

/// <summary>
/// Strongly-typed binding for the <c>Receipt</c> section in appsettings.json.
/// Carries business information printed on every receipt.
/// Requirements: 26.1, 26.2, 26.3
/// </summary>
public sealed class ReceiptOptions
{
    public const string SectionName = "Receipt";

    /// <summary>Business name displayed at the top of the receipt.</summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>Business address displayed on the receipt.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Business phone number displayed on the receipt.</summary>
    public string Phone { get; set; } = string.Empty;
}
