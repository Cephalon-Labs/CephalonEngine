using Cephalon.MultiTenancy.Governance.SmtpDelivery.Configuration;
using System.Net;
using System.Net.Mail;

namespace Cephalon.MultiTenancy.Governance.SmtpDelivery.Services;

internal sealed class SmtpInvitationDeliveryClient(SmtpInvitationDeliveryOptions options) : ISmtpInvitationDeliveryClient
{
    public async ValueTask<SmtpInvitationDeliveryClientResult> SendAsync(
        SmtpInvitationDeliveryMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        using var mailMessage = CreateMailMessage(message);
        using var client = CreateClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.GetTimeout());

        await client.SendMailAsync(mailMessage, timeout.Token).ConfigureAwait(false);

        return new SmtpInvitationDeliveryClientResult(
            accepted: true,
            providerMessageId: message.MessageId,
            reason: "SMTP relay accepted the invitation delivery message.");
    }

    private static MailMessage CreateMailMessage(SmtpInvitationDeliveryMessage message)
    {
        if (!SmtpInvitationDeliveryAddress.TryCreate(message.FromAddress, message.FromDisplayName, out var from))
        {
            throw new InvalidOperationException("SMTP invitation delivery from address is invalid.");
        }

        if (!SmtpInvitationDeliveryAddress.TryCreate(message.ToAddress, message.ToDisplayName, out var to))
        {
            throw new InvalidOperationException("SMTP invitation delivery recipient address is invalid.");
        }

        var mailMessage = new MailMessage(from, to)
        {
            Subject = message.Subject,
            Body = message.TextBody,
            IsBodyHtml = false
        };

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.TextBody, null, "text/plain"));
            mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.HtmlBody, null, "text/html"));
        }

        mailMessage.Headers["Message-Id"] = message.MessageId;
        foreach (var pair in message.Headers)
        {
            mailMessage.Headers[pair.Key] = pair.Value;
        }

        return mailMessage;
    }

    private SmtpClient CreateClient()
    {
        var client = new SmtpClient(options.Host!.Trim(), options.GetPort())
        {
            EnableSsl = options.UseSsl,
            Timeout = (int)Math.Min(options.GetTimeout().TotalMilliseconds, int.MaxValue)
        };

        if (!string.IsNullOrWhiteSpace(options.UserName) || !string.IsNullOrWhiteSpace(options.Password))
        {
            client.Credentials = new NetworkCredential(options.UserName, options.Password);
        }

        return client;
    }
}
