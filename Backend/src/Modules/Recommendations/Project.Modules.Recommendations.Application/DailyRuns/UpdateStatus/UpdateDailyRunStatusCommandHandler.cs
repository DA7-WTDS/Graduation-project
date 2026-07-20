using FluentResults;
using Project.Common.Application.Messaging;
using Project.Modules.Recommendations.Application.Abstractions.Data;
using Project.Modules.Recommendations.Application.Abstractions.DailyRuns;
using Project.Modules.Recommendations.Domain.DailyRuns;
using static Project.Modules.Recommendations.Domain.DailyRuns.RecommendationErrors;

namespace Project.Modules.Recommendations.Application.DailyRuns.UpdateStatus;

internal sealed class UpdateDailyRunStatusCommandHandler(
    IDailyRunRepository dailyRunRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateDailyRunStatusCommand, DailyRunStatusResponse>
{
    public async Task<Result<DailyRunStatusResponse>> Handle(
        UpdateDailyRunStatusCommand request, CancellationToken cancellationToken)
    {
        DailyRun? run = await dailyRunRepository.GetByIdAsync(request.RunId, cancellationToken);
        if (run is null)
        {
            return Result.Fail(RunNotFound(request.RunId));
        }

        Result transition = run.ChangeStatus(request.Target, request.Reason);
        if (transition.IsFailed)
        {
            return Result.Fail<DailyRunStatusResponse>(transition.Errors);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new DailyRunStatusResponse(
            run.Id, run.GeneratedAt, run.Count, run.Status.ToString(), run.StatusReason, run.StatusChangedAt));
    }
}
