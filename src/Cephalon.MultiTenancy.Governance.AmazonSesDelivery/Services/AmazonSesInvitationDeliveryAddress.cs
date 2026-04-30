using System.Net.Mail;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Services;

internal sealed class AmazonSesInvitationDeliveryAddress
{
    private AmazonSesInvitationDeliveryAddress(string address, string? displayName)
    {
        Address = address;
        DisplayName = displayName;
    }

    public string Address { get; }

    public string? DisplayName { get; }

    public static bool TryCreate(string? address, string? displayName, out AmazonSesInvitationDeliveryAddress? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        try
        {
            var parsed = new MailAddress(address.Trim(), string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim());
            value = new AmazonSesInvitationDeliveryAddress(parsed.Address, string.IsNullOrWhiteSpace(parsed.DisplayName) ? null : parsed.DisplayName);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
