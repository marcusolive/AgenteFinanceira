using Microsoft.Agents.AI;

namespace AgenteFinanceiro.Application.Context;

public sealed class FinancialContextProvider : AIContextProvider
{
    public const string Catalog =
        "Empresa fictícia Aurora. Meses disponíveis: 2026-01, 2026-02 e 2026-03. " +
        "Categorias de despesa: Folha, Aluguel, Fornecedores, Impostos e Marketing. " +
        "A base é somente leitura. Não há valores monetários neste contexto.";

    protected override ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context,
        CancellationToken cancellationToken) =>
        new(new AIContext { Instructions = Catalog });
}
