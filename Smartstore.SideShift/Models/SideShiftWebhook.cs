namespace Smartstore.SideShift.Models
{
    public record SideShiftWebhook(
        string Id,
        string Status,
        string SettleAmount,
        string SettleCoin,
        string SettleNetwork,
        DateTime UpdatedAt
    );
}
