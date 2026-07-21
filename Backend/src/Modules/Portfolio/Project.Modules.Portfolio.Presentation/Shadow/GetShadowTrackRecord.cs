using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Project.Common.Presentation.Endpoints;
using Project.Common.Presentation.Results;
using Project.Modules.Portfolio.Application.Shadow.GetShadowTrackRecord;

namespace Project.Modules.Portfolio.Presentation.Shadow;

/// <summary>
/// Public "our model portfolios" track record (§ 6.1) — each template run as a
/// live paper portfolio, costs simulated. Anonymous: it is a marketing/trust
/// asset with no per-user data.
/// </summary>
internal sealed class GetShadowTrackRecord : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/shadow-track-record", async (ISender sender) =>
        {
            Result<ShadowTrackRecordResponse> result = await sender.Send(new GetShadowTrackRecordQuery());
            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .AllowAnonymous()
        .WithName(nameof(GetShadowTrackRecord))
        .WithSummary("Model-portfolio track record (costs simulated)")
        .WithDescription("Each strategy template run daily as a fixed-notional paper portfolio since inception, with the backtester's transaction-cost model. FRA-safe: hypothetical, not client returns.")
        .Produces<ShadowTrackRecordResponse>(StatusCodes.Status200OK)
        .WithTags(Tags.Portfolios);
    }
}
