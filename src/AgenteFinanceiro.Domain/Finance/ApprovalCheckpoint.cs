namespace AgenteFinanceiro.Domain.Finance;

public sealed class ApprovalCheckpoint
{
    public required string Id { get; init; }

    public required int Year { get; init; }

    public required int Month { get; init; }

    public string? Category { get; init; }

    public decimal? Percent { get; init; }

    public decimal ExpenseBase { get; init; }

    public required string State { get; init; }
}
