using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Auditing;
using FiapCloudGames.Identity.Application.Pagination;
using FiapCloudGames.Identity.Application.Queries;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Application.Features.Auditing.GetSecurityAuditEvents;

public sealed class GetSecurityAuditEventsService(
    ISecurityAuditRepository auditEvents,
    ILogger<GetSecurityAuditEventsService> logger)
{
    public async Task<CursorPageResult<SecurityAuditEventResult, SecurityAuditEventCursor>> ExecuteAsync(
        GetSecurityAuditEventsInput input,
        CancellationToken cancellationToken)
    {
        CursorPagination.ValidateLimit(input.Limit);
        QueryValidation.EnsureUtc(
            input.OccurredFromUtc,
            nameof(GetSecurityAuditEventsInput.OccurredFromUtc));
        QueryValidation.EnsureUtc(
            input.OccurredToUtc,
            nameof(GetSecurityAuditEventsInput.OccurredToUtc));
        QueryValidation.EnsureValidPeriod(
            input.OccurredFromUtc,
            input.OccurredToUtc,
            nameof(GetSecurityAuditEventsInput.OccurredFromUtc),
            nameof(GetSecurityAuditEventsInput.OccurredToUtc));

        if (input.After is { } cursor)
        {
            QueryValidation.EnsureUtc(
                cursor.OccurredAtUtc,
                nameof(GetSecurityAuditEventsInput.After));
            if (cursor.Id == Guid.Empty)
            {
                throw AppException.Validation("O cursor informado é inválido.");
            }
        }

        var query = new SecurityAuditEventQuery(
            QueryValidation.NormalizeOptional(
                input.EventType,
                SecurityAuditEventQueryConstraints.EventTypeMaximumLength,
                nameof(GetSecurityAuditEventsInput.EventType)),
            QueryValidation.NormalizeOptional(
                input.ActorClientId,
                SecurityAuditEventQueryConstraints.ActorClientIdMaximumLength,
                nameof(GetSecurityAuditEventsInput.ActorClientId)),
            QueryValidation.NormalizeOptional(
                input.SubjectType,
                SecurityAuditEventQueryConstraints.SubjectTypeMaximumLength,
                nameof(GetSecurityAuditEventsInput.SubjectType)),
            QueryValidation.NormalizeOptional(
                input.SubjectId,
                SecurityAuditEventQueryConstraints.SubjectIdMaximumLength,
                nameof(GetSecurityAuditEventsInput.SubjectId)),
            input.OccurredFromUtc,
            input.OccurredToUtc,
            input.After);

        logger.LogDebug(
            "Consultando página de auditoria com limite {AuditEventLimit}, {AuditFilterCount} filtro(s) e cursor: {AuditHasCursor}.",
            input.Limit,
            CountFilters(query),
            query.After is not null);

        var events = await auditEvents.ListAsync(
            query,
            CursorPagination.GetFetchLimit(input.Limit),
            cancellationToken);
        var page = CursorPagination.CreatePage(
            events,
            input.Limit,
            SecurityAuditEventResult.From,
            static auditEvent => new SecurityAuditEventCursor(
                auditEvent.OccurredAtUtc,
                auditEvent.Id));

        logger.LogDebug(
            "Consulta de auditoria concluída com {AuditEventCount} evento(s) e próxima página: {AuditHasNextPage}.",
            page.Items.Count,
            page.NextCursor is not null);

        return page;
    }

    private static int CountFilters(SecurityAuditEventQuery query)
    {
        var count = 0;
        if (query.EventType is not null)
        {
            count++;
        }

        if (query.ActorClientId is not null)
        {
            count++;
        }

        if (query.SubjectType is not null)
        {
            count++;
        }

        if (query.SubjectId is not null)
        {
            count++;
        }

        if (query.OccurredFromUtc is not null)
        {
            count++;
        }

        if (query.OccurredToUtc is not null)
        {
            count++;
        }

        return count;
    }
}
