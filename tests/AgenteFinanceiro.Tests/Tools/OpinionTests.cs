using AgenteFinanceiro.Application.Workflow;
using AgenteFinanceiro.Domain.Calculations;
using AgenteFinanceiro.Domain.Finance;
using AgenteFinanceiro.Infrastructure.Data;
using AgenteFinanceiro.Tests.Sessions;

namespace AgenteFinanceiro.Tests.Tools;

public class OpinionTests
{
    [Fact]
    public async Task Parecer_de_fevereiro_usa_o_resultado_negativo_e_nao_o_saldo_de_marco()
    {
        var workflow = WorkflowFixture.Create();
        var session = new ConversationSession { ContextId = "parecer" };
        var state = await workflow.RunAsync("Faça um parecer de fevereiro de 2026.", session, CancellationToken.None);
        Assert.Contains("-24000.00", state.Text);
        Assert.Contains("base demonstrativa", state.Text);
        Assert.DoesNotContain("12000.00", state.Text);
        Assert.DoesNotContain("Recomendação demonstrativa", state.Text);
    }

    [Fact]
    public async Task Parecer_de_janeiro_nao_cita_fevereiro_nem_marco()
    {
        var workflow = WorkflowFixture.Create();
        var state = await workflow.RunAsync("Faça um parecer de janeiro de 2026.", null, CancellationToken.None);
        Assert.Contains("13000.00", state.Text);
        Assert.DoesNotContain("-24000.00", state.Text);
        Assert.DoesNotContain("12000.00", state.Text);
        Assert.DoesNotContain("Recomendação demonstrativa", state.Text);
    }

    [Fact]
    public async Task Parecer_disso_reusa_janeiro_na_mesma_sessao()
    {
        var workflow = WorkflowFixture.Create();
        var session = new ConversationSession { ContextId = "parecer-disso" };
        await workflow.RunAsync("Qual o fluxo de caixa de janeiro de 2026?", session, CancellationToken.None);
        var state = await workflow.RunAsync("faça o parecer disso", session, CancellationToken.None);
        Assert.Contains("2026-01", state.Text);
        Assert.Contains("13000.00", state.Text);
        Assert.DoesNotContain("-24000.00", state.Text);
        Assert.DoesNotContain("12000.00", state.Text);
    }
}

internal static class WorkflowFixture
{
    public static CashFlowWorkflowFactory Create(string? root = null)
    {
        var repository = new InMemoryFinancialRepository();
        var calculator = new CashFlowCalculator(repository);
        return new CashFlowWorkflowFactory(
            new ValidationExecutor(),
            new CalculationExecutor(calculator),
            new OpinionExecutor(new AgenteFinanceiro.Application.Analysis.AnswerComposer(repository, calculator)),
            root ?? Path.Combine(Path.GetTempPath(), "agente-workflow-" + Guid.NewGuid().ToString("N")));
    }
}
