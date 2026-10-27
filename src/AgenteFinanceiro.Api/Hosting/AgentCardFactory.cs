using A2A;

namespace AgenteFinanceiro.Api.Hosting;

public static class AgentCardFactory
{
    public const string AgentUrl = "http://localhost:5100/a2a/agente-financeiro";

    public static AgentCard Create() => new()
    {
        Name = "AgenteFinanceiro",
        Description = "Análise de fluxo de caixa sobre a base demonstrativa Aurora",
        Version = "1.0",
        SupportedInterfaces =
        [
            new AgentInterface
            {
                Url = AgentUrl,
                ProtocolBinding = ProtocolBindingNames.HttpJson,
                ProtocolVersion = "1.0"
            }
        ],
        Skills =
        [
            Skill("consultar-periodo", "Consultar receitas, despesas e saldos", "Um mês conhecido"),
            Skill("calcular-fluxo-caixa-mensal", "Calcular o fluxo de caixa de um mês", "Resultado, saldo inicial e saldo final"),
            Skill("identificar-resultado-negativo", "Identificar períodos com resultado negativo", "Varredura da base ou de um intervalo"),
            Skill("comparar-categorias", "Comparar despesas entre categorias", "Duas ou mais categorias, ou a mesma em meses distintos"),
            Skill("gerar-parecer", "Gerar parecer financeiro", "Texto só com números calculados na solicitação"),
            Skill("recomendar-reducao", "Gerar recomendação demonstrativa", "Redução pedida; aprovação humana se o percentual for superior a 20%")
        ]
    };

    private static AgentSkill Skill(string id, string name, string description) => new()
    {
        Id = id,
        Name = name,
        Description = description,
        Tags = ["financeiro"]
    };
}
