# AgenteFinanceiro

Agente de fluxo de caixa da base demonstrativa Aurora. A Api expõe o agente em A2A para o console `Sofia.Simulator`. Não há banco de dados, servidor MCP nem dependência da SOFIA.

O passo a passo de execução e os valores esperados estão em [specs/001-cash-flow-agent/quickstart.md](specs/001-cash-flow-agent/quickstart.md).

## Executar

Na raiz do repositório, com o SDK .NET 10:

```powershell
dotnet test tests/AgenteFinanceiro.Tests
```

A suíte não precisa de chave da OpenAI. Para subir a Api na porta 5100 e o simulador, grave a chave só em User Secrets:

```powershell
dotnet user-secrets -p src/AgenteFinanceiro.Api set "OpenAI:ApiKey" "<chave>"
dotnet user-secrets -p src/AgenteFinanceiro.Api set "OpenAI:Model" "<modelo>"
dotnet run --project src/AgenteFinanceiro.Api
dotnet run --project src/Sofia.Simulator
```

Não copie a chave para `appsettings.json` nem para o Git.
