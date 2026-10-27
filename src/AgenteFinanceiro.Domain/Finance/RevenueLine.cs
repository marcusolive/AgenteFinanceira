namespace AgenteFinanceiro.Domain.Finance;

public sealed record RevenueLine(MonthPeriod Period, RevenueSource Source, decimal Amount);
