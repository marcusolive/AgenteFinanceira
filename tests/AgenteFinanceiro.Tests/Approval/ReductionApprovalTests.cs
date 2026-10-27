using AgenteFinanceiro.Application.Tools;
using AgenteFinanceiro.Application.Workflow;
using AgenteFinanceiro.Domain.Calculations;
using AgenteFinanceiro.Domain.Finance;
using AgenteFinanceiro.Infrastructure.Data;
using AgenteFinanceiro.Tests.Tools;

namespace AgenteFinanceiro.Tests.Approval;

public class ReductionApprovalTests
{
    private readonly CashFlowCalculator _calculator = new(new InMemoryFinancialRepository());

    [Fact]
    public void Vinte_por_cento_libera_17400_sem_portao()
    {
        var assessment = new ReductionTools(_calculator).PrepararRecomendacaoReducao(2026, 1, 20m);
        Assert.Equal(ApprovalState.NotRequired, assessment.Decision);
        Assert.Equal(87000.00m, assessment.ExpenseBase);
        Assert.Equal(17400.00m, assessment.ReductionAmount);
    }

    [Fact]
    public void Vinte_e_cinco_por_cento_fica_pendente_sem_o_valor()
    {
        var assessment = new ReductionTools(_calculator).PrepararRecomendacaoReducao(2026, 1, 25m);
        Assert.Equal(ApprovalState.Pending, assessment.Decision);
        Assert.Equal(87000.00m, assessment.ExpenseBase);
        Assert.Null(assessment.ReductionAmount);
    }

    [Fact]
    public async Task Aprovacao_libera_a_frase_e_nao_muda_saldo()
    {
        var workflow = WorkflowFixture.Create();
        var session = new ConversationSession { ContextId = "aprovacao" };
        var pending = await workflow.RunAsync("Recomende reduzir 25% das despesas de janeiro de 2026.", session, CancellationToken.None);
        Assert.DoesNotContain("21750.00", pending.Text);
        Assert.Contains("87000.00", pending.Text);
        var approved = await workflow.RunAsync("aprovo", session, CancellationToken.None);
        Assert.StartsWith("Recomendação demonstrativa", approved.Text);
        Assert.Contains("21750.00", approved.Text);
        Assert.True(_calculator.TryCalculate(new MonthPeriod(2026, 1), out var flow));
        Assert.Equal(28000.00m, flow!.ClosingBalance);
    }

    [Fact]
    public async Task Recusa_nao_emite_21750()
    {
        var workflow = WorkflowFixture.Create();
        var session = new ConversationSession { ContextId = "recusa" };
        await workflow.RunAsync("Recomende reduzir 25% das despesas de janeiro de 2026.", session, CancellationToken.None);
        var rejected = await workflow.RunAsync("recuso", session, CancellationToken.None);
        Assert.DoesNotContain("21750.00", rejected.Text);
        Assert.DoesNotContain("Recomendação demonstrativa", rejected.Text);
    }

    [Fact]
    public async Task Talvez_permanece_pendente()
    {
        var workflow = WorkflowFixture.Create();
        var session = new ConversationSession { ContextId = "talvez" };
        await workflow.RunAsync("Recomende reduzir 25% das despesas de janeiro de 2026.", session, CancellationToken.None);
        var ambiguous = await workflow.RunAsync("talvez", session, CancellationToken.None);
        Assert.Contains("permanece retida", ambiguous.Text);
        Assert.DoesNotContain("21750.00", ambiguous.Text);
        Assert.Equal(ApprovalState.Pending, session.PendingApproval?.Decision);
    }

    [Fact]
    public async Task Mais_de_20_sem_percentual_nao_escolhe_corte()
    {
        var workflow = WorkflowFixture.Create();
        var state = await workflow.RunAsync("Recomende reduzir mais de 20% das despesas de janeiro de 2026.", null, CancellationToken.None);
        Assert.Contains("Nenhum corte foi escolhido", state.Text);
        Assert.DoesNotContain("17400.00", state.Text);
        Assert.DoesNotContain("21750.00", state.Text);
    }

    [Fact]
    public async Task Outra_sessao_nao_recebe_a_recomendacao()
    {
        var workflow = WorkflowFixture.Create();
        var owner = new ConversationSession { ContextId = "dona" };
        await workflow.RunAsync("Recomende reduzir 25% das despesas de janeiro de 2026.", owner, CancellationToken.None);
        var other = new ConversationSession { ContextId = "outra" };
        var answer = await workflow.RunAsync("aprovo", other, CancellationToken.None);
        Assert.DoesNotContain("21750.00", answer.Text);
        Assert.DoesNotContain("Recomendação demonstrativa", answer.Text);
    }

    [Fact]
    public async Task Transferencia_e_recusada_e_o_saldo_permanece()
    {
        var workflow = WorkflowFixture.Create();
        var state = await workflow.RunAsync("Transfira 1000 do caixa de janeiro.", null, CancellationToken.None);
        Assert.Contains("não são executados", state.Text);
        Assert.True(_calculator.TryCalculate(new MonthPeriod(2026, 1), out var flow));
        Assert.Equal(28000.00m, flow!.ClosingBalance);
    }

    [Fact]
    public async Task Checkpoint_json_guarda_a_pendencia_sem_credencial_e_a_conversa_some_da_memoria()
    {
        var root = Path.Combine(Path.GetTempPath(), "agente-checkpoint-" + Guid.NewGuid().ToString("N"));
        var workflow = WorkflowFixture.Create(root);
        var session = new ConversationSession { ContextId = "checkpoint" };
        await workflow.RunAsync("Recomende reduzir 25% das despesas de janeiro de 2026.", session, CancellationToken.None);
        Assert.Null(workflow.LastWorkflowError?.ToString());
        var files = Directory.Exists(root)
            ? Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            : [];
        Assert.NotEmpty(files);
        var json = string.Concat(files.Select(File.ReadAllText));
        Assert.Contains("87000", json);
        Assert.Contains("25", json);
        Assert.Contains("Pending", json);
        Assert.DoesNotContain("OpenAI:ApiKey", json);
        Assert.DoesNotContain("Recomende reduzir", json);
        session.EstablishedPeriod = null;
        session.PendingApproval = null;
        Assert.Null(session.EstablishedPeriod);
    }
}
