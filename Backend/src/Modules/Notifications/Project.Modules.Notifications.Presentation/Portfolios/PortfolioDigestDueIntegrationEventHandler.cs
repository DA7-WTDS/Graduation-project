using System.Globalization;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Project.Common.Application.EventBus;
using Project.Modules.Notifications.Application.Notifications.CreateNotification;
using Project.Modules.Notifications.Domain.Notifications;
using Project.Modules.Portfolio.IntegrationEvents;

namespace Project.Modules.Notifications.Presentation.Portfolios;

/// <summary>
/// The periodic digest (§ 3.5): a plain summary of where the goal stands. Not a
/// nudge to act — for a set-and-forget investor this is the *only* routine
/// message they get, so it reads as a calm check-in, and the honest number goes
/// in whether it's up or down.
/// </summary>
public sealed class PortfolioDigestDueIntegrationEventHandler(
    ISender sender,
    ILogger<PortfolioDigestDueIntegrationEventHandler> logger)
    : IntegrationEventHandler<PortfolioDigestDueIntegrationEvent>
{
    public override async Task Handle(PortfolioDigestDueIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        string period = integrationEvent.PeriodDays >= 90 ? "Quarterly" : "Monthly";
        string value = integrationEvent.Nav.ToString("C0", CultureInfo.GetCultureInfo("en-US"));
        string ret = $"{(integrationEvent.TotalReturnPct >= 0 ? "+" : "")}{integrationEvent.TotalReturnPct:P1}";

        string performance = integrationEvent.TotalReturnPct >= 0
            ? $"{value}, up {ret} since you started"
            : $"{value}, down {integrationEvent.TotalReturnPct:P1} since you started";

        string drawdownNote = integrationEvent.DrawdownPct > 0.01
            ? $" It sits {integrationEvent.DrawdownPct:P1} below its high — normal movement, and your plan already accounts for it."
            : string.Empty;

        Result<Guid> result = await sender.Send(new CreateNotificationCommand(
            integrationEvent.UserId,
            $"{period} check-in: {integrationEvent.TemplateName}",
            $"Your portfolio is worth {performance}.{drawdownNote} " +
            $"Next scheduled review: {integrationEvent.NextReviewDate:MMMM d, yyyy}. " +
            "Nothing needs doing — this is just your regular update.",
            NotificationType.Info), cancellationToken);

        if (result.IsFailed)
        {
            logger.LogError("PortfolioDigest: failed to notify user {UserId}: {Error}",
                integrationEvent.UserId, result.Errors[0].Message);
        }
        else
        {
            logger.LogInformation("PortfolioDigest: {Period} digest sent to user {UserId}.",
                period, integrationEvent.UserId);
        }
    }
}
