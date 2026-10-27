namespace AgenteFinanceiro.Domain.Finance;

public sealed class ConversationSession
{
    public required string ContextId { get; init; }

    public MonthPeriod? EstablishedPeriod { get; set; }

    public ReductionAssessment? PendingApproval { get; set; }

    public string? ApprovalCheckpointId { get; set; }
}
