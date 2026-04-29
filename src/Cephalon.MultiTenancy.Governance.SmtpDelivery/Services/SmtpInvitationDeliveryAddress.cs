using System.Net.Mail;

namespace Cephalon.MultiTenancy.Governance.SmtpDelivery.Services;

internal static class SmtpInvitationDeliveryAddress
{
    public static bool TryCreate(string? address, string? displayName, out MailAddress mailAddress)
    {
        mailAddress = null!;

        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        try
        {
            mailAddress = string.IsNullOrWhiteSpace(displayName)
                ? new MailAddress(address.Trim())
                : new MailAddress(address.Trim(), displayName.Trim());
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
