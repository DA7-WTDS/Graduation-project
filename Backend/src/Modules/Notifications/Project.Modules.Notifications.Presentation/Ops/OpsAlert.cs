using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Project.Modules.Notifications.Application.Abstractions.Emails;
using Project.Modules.Notifications.Application.Notifications.CreateNotification;
using Project.Modules.Notifications.Domain.Notifications;
using Project.Modules.Users.PublicApi;

namespace Project.Modules.Notifications.Presentation.Ops;

/// <summary>Delivers an operational alert to everyone who can act on it.</summary>
public interface IOpsAlert
{
    Task RaiseAsync(string title, string message, CancellationToken cancellationToken = default);
}

/// <summary>
/// One path for every ops alert: an in-app notification for each Admin AND an email.
///
/// The in-app bell alone is a pull channel — it only works if someone happens to open the
/// app. These alerts exist for the nights when nobody does: a quarantined run, a shadow
/// track record that stopped advancing, a model drifting below its floor. Email is what
/// makes them reach a person who is not looking (MVP_PLAN week 1, "ops alert (email)").
///
/// Delivery is best-effort per channel and per recipient. A dead SMTP host must not stop
/// the in-app notification from being written, and one bad address must not stop the
/// other admins being told. If there is no one to tell at all, that is itself logged at
/// warning — an alert with no audience is the failure mode most likely to go unnoticed.
/// </summary>
public sealed class OpsAlert(
    ISender sender,
    IUsersApi usersApi,
    IEmailService emailService,
    ILogger<OpsAlert> logger) : IOpsAlert
{
    public async Task RaiseAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> adminIds = await usersApi.GetAdminUserIdsAsync(cancellationToken);

        if (adminIds.Count == 0)
        {
            // Log the full text, not just the fact of failure: with no admins this line is
            // the only surviving record that the alert ever happened.
            logger.LogWarning(
                "OPS ALERT with no Admin users to receive it — {Title}: {Message}", title, message);
            return;
        }

        int notified = 0, emailed = 0;

        foreach (Guid adminId in adminIds)
        {
            Result<Guid> result = await sender.Send(
                new CreateNotificationCommand(adminId, title, message, NotificationType.Warning),
                cancellationToken);

            if (result.IsFailed)
            {
                logger.LogError("Ops alert notification failed for admin {AdminId}: {Error}",
                    adminId, result.Errors[0].Message);
            }
            else
            {
                notified++;
            }

            if (await EmailAsync(adminId, title, message, cancellationToken))
            {
                emailed++;
            }
        }

        logger.LogInformation(
            "Ops alert {Title}: {Notified}/{Total} notified, {Emailed}/{Total} emailed.",
            title, notified, adminIds.Count, emailed, adminIds.Count);
    }

    private async Task<bool> EmailAsync(Guid adminId, string title, string message, CancellationToken cancellationToken)
    {
        try
        {
            UserResponse? admin = await usersApi.GetAsync(adminId, cancellationToken);
            if (admin is null || string.IsNullOrWhiteSpace(admin.Email))
            {
                logger.LogWarning("Ops alert: no email address for admin {AdminId}.", adminId);
                return false;
            }

            return await emailService.SendAsync(
                admin.Email,
                $"[QuantWise ops] {title}",
                Body(title, message),
                isHtml: true,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Never let mail delivery take down the handler: the integration-event consumer
            // would retry the whole thing and duplicate the in-app notifications.
            logger.LogError(ex, "Ops alert email failed for admin {AdminId}.", adminId);
            return false;
        }
    }

    private static string Body(string title, string message) =>
        $"""
        <div style="font-family:system-ui,-apple-system,Segoe UI,sans-serif;line-height:1.5">
          <h2 style="margin:0 0 12px">{title}</h2>
          <p style="margin:0 0 16px">{message}</p>
          <p style="color:#666;font-size:13px;margin:0">
            Automated operational alert from the QuantWise backend. It was also delivered
            in-app to every Admin account.
          </p>
        </div>
        """;
}
