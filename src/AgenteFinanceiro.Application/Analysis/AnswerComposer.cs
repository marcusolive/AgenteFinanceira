using AgenteFinanceiro.Domain.Abstractions;
using AgenteFinanceiro.Domain.Finance;

namespace AgenteFinanceiro.Application.Analysis;

public sealed class AnswerComposer
{
    private readonly IFinancialDataRepository _repository;
    private readonly ICashFlowCalculator _calculator;

    public AnswerComposer(IFinancialDataRepository repository, ICashFlowCalculator calculator)
    {
        _repository = repository;
        _calculator = calculator;
    }

    public string Compose(AnalysisState state, ConversationSession? session)
    {
        ApplySessionPeriod(state, session);
        return state.Kind switch
        {
            AnalysisKind.Empty => "A pergunta chegou vazia. Reformule a solicitação. Nenhum valor foi calculado.",
            AnalysisKind.GapTransfer => "Transferência, pagamento e alteração de saldo não são executados. Os saldos permanecem os da base.",
            AnalysisKind.GapDocument => "A base não contém documento fiscal nem identificador de documento.",
            AnalysisKind.GapAliquot => Aliquot(),
            AnalysisKind.GapMutation => "A base é somente leitura. A inclusão, alteração ou exclusão foi recusada. Janeiro de 2026 permanece inalterado.",
            AnalysisKind.GapCategory => $"A categoria {state.UnknownLabel} não existe na base. Categorias de despesa: {CategoryList()}. Nenhum valor zero foi inventado.",
            AnalysisKind.GapPeriod => GapPeriod(state),
            AnalysisKind.MissingPeriod => "Não há período estabelecido nesta sessão. Informe o mês. Meses disponíveis: 2026-01, 2026-02, 2026-03.",
            AnalysisKind.VagueReduction => "O pedido não informa o percentual da redução. Nenhum corte foi escolhido.",
            AnalysisKind.Ambiguous => Ambiguous(session),
            AnalysisKind.Approval => Approve(session),
            AnalysisKind.Rejection => Reject(session),
            AnalysisKind.NegativeScan => Negative(state),
            AnalysisKind.CompareCategories => Compare(state),
            AnalysisKind.CompareAcrossMonths => CompareMonths(state),
            AnalysisKind.MixedNature => "Vendas é receita e Folha é despesa. A comparação não produz ranking de despesa misto.",
            AnalysisKind.Opinion => Opinion(state, session),
            AnalysisKind.Reduction => Reduction(state, session),
            _ => CashFlow(state, session)
        };
    }

    private void ApplySessionPeriod(AnalysisState state, ConversationSession? session)
    {
        if (state.Month is not null || session?.EstablishedPeriod is null)
        {
            return;
        }

        if (state.Kind is AnalysisKind.CashFlow or AnalysisKind.CompareCategories or AnalysisKind.Opinion or AnalysisKind.Reduction or AnalysisKind.MissingPeriod)
        {
            state.Year = session.EstablishedPeriod.Year;
            state.Month = session.EstablishedPeriod.Month;
            if (state.Kind == AnalysisKind.MissingPeriod)
            {
                state.Kind = AnalysisKind.CashFlow;
            }
        }
    }

    private string CashFlow(AnalysisState state, ConversationSession? session)
    {
        if (!TryPeriod(state, out var period))
        {
            return GapPeriod(state);
        }

        if (!_calculator.TryCalculate(period, out var flow) || flow is null)
        {
            return GapPeriod(state);
        }

        Remember(session, period, state);
        var revenues = string.Join(", ", flow.Revenues.Select(line => $"{SourceName(line.Source)} {MoneyText.Format(line.Amount)}"));
        var expenses = string.Join(", ", flow.Expenses.Select(line => $"{line.Category} {MoneyText.Format(line.Amount)}"));
        return $"Base demonstrativa Aurora. Fluxo de caixa de {period.Key}. Receitas: {revenues}. Total de receitas {MoneyText.Format(flow.TotalRevenue)}. Despesas: {expenses}. Total de despesas {MoneyText.Format(flow.TotalExpense)}. Saldo inicial {MoneyText.Format(flow.OpeningBalance)}. Resultado {MoneyText.Format(flow.Result)}. Saldo final {MoneyText.Format(flow.ClosingBalance)}. Consulta factual, sem pedido de aprovação.";
    }

    private string Negative(AnalysisState state)
    {
        var start = state.Month is null
            ? new MonthPeriod(2026, 1)
            : new MonthPeriod(state.Year ?? 2026, state.Month.Value);
        var end = state.EndMonth is null
            ? (state.Month is null ? new MonthPeriod(2026, 3) : start)
            : new MonthPeriod(state.EndYear ?? state.Year ?? 2026, state.EndMonth.Value);

        if (start.Key == end.Key && _calculator.TryCalculate(start, out var single) && single is not null && !single.IsNegative)
        {
            return $"{start.Key} não é negativo. Resultado {MoneyText.Format(single.Result)}.";
        }

        var negative = _calculator.ListNegative(start, end, out var missing);
        var missingText = missing.Count == 0
            ? string.Empty
            : $"Meses ausentes da base, sem valor inventado: {string.Join(", ", missing.Select(period => period.Key))}. ";
        if (negative.Count == 0)
        {
            return $"{missingText}Nenhum período negativo no intervalo informado.";
        }

        var lines = string.Join(" ", negative.Select(flow =>
            $"Período com resultado negativo: {flow.Period.Key}, resultado {MoneyText.Format(flow.Result)}, saldo inicial {MoneyText.Format(flow.OpeningBalance)}, saldo final {MoneyText.Format(flow.ClosingBalance)}."));
        return missingText + lines;
    }

    private string Compare(AnalysisState state)
    {
        if (!TryPeriod(state, out var period))
        {
            return "Informe o mês da comparação. Meses disponíveis: 2026-01, 2026-02, 2026-03.";
        }

        if (state.Categories.Count < 2)
        {
            return "Informe ao menos duas categorias de despesa.";
        }

        var comparison = _calculator.CompareCategories(period, state.Categories);
        var items = string.Join(", ", comparison.Items.Select(item => $"{item.Category} {MoneyText.Format(item.Amount)}"));
        var difference = comparison.Difference is null
            ? string.Empty
            : $" Diferença {MoneyText.Format(comparison.Difference.Value)}.";
        return $"{period.Key}. {items}.{difference} Maior: {comparison.Largest}.";
    }

    private string CompareMonths(AnalysisState state)
    {
        var category = state.Categories[0];
        var start = new MonthPeriod(state.Year ?? 2026, state.Month ?? 1);
        var end = new MonthPeriod(state.EndYear ?? start.Year, state.EndMonth ?? start.Month);
        var periods = new List<MonthPeriod> { start, end };
        _calculator.TryCompareCategoryAcrossMonths(category, periods, out var comparison, out _);
        var points = string.Join(" ", comparison!.Points.Select(point => $"{category} em {point.Period.Key}: {MoneyText.Format(point.Amount)}."));
        return $"{points} Diferença {MoneyText.Format(comparison.Difference ?? 0m)}.";
    }

    private string Opinion(AnalysisState state, ConversationSession? session)
    {
        if (!TryPeriod(state, out var period))
        {
            return "Não há período para o parecer. Informe o mês.";
        }

        if (!_calculator.TryCalculate(period, out var flow) || flow is null)
        {
            return GapPeriod(state);
        }

        Remember(session, period, state);
        return $"Parecer sobre a base demonstrativa Aurora em {period.Key}. O resultado calculado nesta solicitação é {MoneyText.Format(flow.Result)}. Saldo inicial {MoneyText.Format(flow.OpeningBalance)}. Saldo final {MoneyText.Format(flow.ClosingBalance)}. Este parecer usa somente os números calculados desta solicitação.";
    }

    private string Reduction(AnalysisState state, ConversationSession? session)
    {
        if (!TryPeriod(state, out var period))
        {
            return "Informe o mês da redução. Nenhum corte foi escolhido.";
        }

        var category = state.Categories.Count == 1 ? state.Categories[0] : (ExpenseCategory?)null;
        var assessment = _calculator.PrepareReduction(period, category, state.Percent);
        state.ExpenseBase = assessment.ExpenseBase;
        state.Decision = assessment.Decision;
        state.CategoryName = category?.ToString();
        Remember(session, period, state);
        if (assessment.Decision == ApprovalState.NotRequired && assessment.ReductionAmount is not null && assessment.Percent is not null)
        {
            state.AsksForApproval = false;
            return $"Recomendação demonstrativa: reduzir {MoneyText.Format(assessment.ReductionAmount.Value)}, equivalente a {MoneyText.Format(assessment.Percent.Value)}% da base de despesas {MoneyText.Format(assessment.ExpenseBase)} em {period.Key}. Consulta sem pedido de aprovação. Os saldos não mudam.";
        }

        state.AsksForApproval = true;
        state.CheckpointId ??= Guid.NewGuid().ToString("N");
        if (session is not null)
        {
            session.PendingApproval = assessment;
        }

        return $"A redução de {MoneyText.Format(assessment.Percent ?? 0m)}% sobre a base de despesas {MoneyText.Format(assessment.ExpenseBase)} em {period.Key} aguarda aprovação humana. A recomendação permanece retida.";
    }

    private string Approve(ConversationSession? session)
    {
        var pending = session?.PendingApproval;
        if (pending?.Percent is null || pending.Decision != ApprovalState.Pending)
        {
            return "Não há recomendação pendente nesta sessão.";
        }

        var amount = _calculator.QuoteReduction(pending.ExpenseBase, pending.Percent.Value);
        session!.PendingApproval = new ReductionAssessment
        {
            Period = pending.Period,
            Category = pending.Category,
            Percent = pending.Percent,
            ExpenseBase = pending.ExpenseBase,
            ReductionAmount = amount,
            Decision = ApprovalState.Approved
        };
        return $"Recomendação demonstrativa: reduzir {MoneyText.Format(amount)}, equivalente a {MoneyText.Format(pending.Percent.Value)}% da base de despesas {MoneyText.Format(pending.ExpenseBase)} em {pending.Period.Key}. Os saldos permanecem os da base.";
    }

    private static string Reject(ConversationSession? session)
    {
        if (session?.PendingApproval is null)
        {
            return "Não há recomendação pendente nesta sessão.";
        }

        var pending = session.PendingApproval;
        session.PendingApproval = new ReductionAssessment
        {
            Period = pending.Period,
            Category = pending.Category,
            Percent = pending.Percent,
            ExpenseBase = pending.ExpenseBase,
            Decision = ApprovalState.Rejected
        };
        return "A recomendação foi recusada. O valor da redução não será emitido.";
    }

    private static string Ambiguous(ConversationSession? session) =>
        session?.PendingApproval?.Decision == ApprovalState.Pending
            ? "A decisão não foi explícita. A recomendação permanece retida."
            : "A decisão não foi explícita. Informe se aprova ou recusa.";

    private string Aliquot()
    {
        _calculator.TryCalculate(new MonthPeriod(2026, 1), out var flow);
        var tax = flow!.Expenses.Single(line => line.Category == ExpenseCategory.Impostos).Amount;
        return $"A base não contém alíquota. Impostos é uma despesa. Em 2026-01, Impostos é {MoneyText.Format(tax)}. Não há razão entre Impostos e a receita.";
    }

    private string GapPeriod(AnalysisState state)
    {
        var key = state.Year is null || state.Month is null
            ? "informado"
            : new MonthPeriod(state.Year.Value, state.Month.Value).Key;
        var known = string.Join(", ", _repository.ListPeriods().Select(period => period.Key));
        return $"Não há dados de {key} na base demonstrativa Aurora. Meses disponíveis: {known}.";
    }

    private string CategoryList() => string.Join(", ", _repository.ExpenseCategories);

    private static bool TryPeriod(AnalysisState state, out MonthPeriod period)
    {
        if (state.Year is null || state.Month is null)
        {
            period = new MonthPeriod(2026, 1);
            return false;
        }

        period = new MonthPeriod(state.Year.Value, state.Month.Value);
        return true;
    }

    private static void Remember(ConversationSession? session, MonthPeriod period, AnalysisState state)
    {
        if (session is not null && state.RememberPeriod)
        {
            session.EstablishedPeriod = period;
        }
    }

    private static string SourceName(RevenueSource source) =>
        source == RevenueSource.Servicos ? "Serviços" : source.ToString();
}
