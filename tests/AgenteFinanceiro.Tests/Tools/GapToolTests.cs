using AgenteFinanceiro.Application.Analysis;
using AgenteFinanceiro.Domain.Calculations;
using AgenteFinanceiro.Domain.Finance;
using AgenteFinanceiro.Infrastructure.Data;

namespace AgenteFinanceiro.Tests.Tools;

public class GapToolTests
{
    private readonly InMemoryFinancialRepository _repository = new();
    private readonly AnswerComposer _composer;

    public GapToolTests()
    {
        var calculator = new CashFlowCalculator(_repository);
        _composer = new AnswerComposer(_repository, calculator);
    }

    [Fact]
    public void Abril_nao_tem_valores_e_lista_os_meses_da_base()
    {
        var text = Ask("Qual o fluxo de caixa de abril de 2026?");
        Assert.Contains("2026-04", text);
        Assert.Contains("2026-01", text);
        Assert.Contains("2026-02", text);
        Assert.Contains("2026-03", text);
        Assert.DoesNotContain("0.00", text);
    }

    [Fact]
    public void Viagens_nao_vira_zero_e_lista_as_cinco_categorias()
    {
        var text = Ask("Quanto gastei com Viagens em janeiro de 2026?");
        Assert.Contains("Viagens", text);
        Assert.Contains("Folha", text);
        Assert.Contains("Aluguel", text);
        Assert.Contains("Fornecedores", text);
        Assert.Contains("Impostos", text);
        Assert.Contains("Marketing", text);
        Assert.DoesNotContain("0.00", text);
    }

    [Fact]
    public void Aliquota_nao_calcula_razao_e_pode_citar_impostos_de_janeiro()
    {
        var text = Ask("Qual a alíquota de impostos de janeiro de 2026?");
        Assert.Contains("12000.00", text);
        Assert.Contains("Não há razão", text);
        Assert.DoesNotContain("alíquota de", text);
    }

    [Fact]
    public void Documento_nao_ganha_identificador()
    {
        var text = Ask("Mostre o documento fiscal de janeiro.");
        Assert.Contains("não contém documento", text);
        Assert.DoesNotContain("NF", text);
    }

    [Fact]
    public void Inclusao_e_recusada_e_janeiro_permanece_igual()
    {
        var before = Closing();
        var text = Ask("Inclua uma despesa de viagem em janeiro de 2026.");
        Assert.Contains("somente leitura", text);
        Assert.Equal(before, Closing());
    }

    private string Ask(string question) =>
        _composer.Compose(QuestionInterpreter.Interpret(question, "lacuna"), null);

    private decimal Closing()
    {
        Assert.True(_repository.TryGetSnapshot(new MonthPeriod(2026, 1), out var snapshot));
        return snapshot!.OpeningBalance;
    }
}
