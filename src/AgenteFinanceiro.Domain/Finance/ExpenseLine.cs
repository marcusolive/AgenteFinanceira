namespace AgenteFinanceiro.Domain.Finance;

public sealed record ExpenseLine(MonthPeriod Period, ExpenseCategory Category, decimal Amount);
