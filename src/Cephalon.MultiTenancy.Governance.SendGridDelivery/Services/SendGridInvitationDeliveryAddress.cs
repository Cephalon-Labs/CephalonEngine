using System.Net.Mail;

namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.Services;

internal static class SendGridInvitationDeliveryAddress
{
    public static bool TryCreate(string? email, string? displayName, out MailAddress? address)
    {
        address = null;
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            address = string.IsNullOrWhiteSpace(displayName)
                ? new MailAddress(email.Trim())
                : new MailAddress(email.Trim(), displayName.Trim());

            return string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
