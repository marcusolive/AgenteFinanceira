# Quickstart: AgenteFinanceiro

Guia de validação da spec. Não substitui a implementação. Os valores
esperados estão em [data-model.md](./data-model.md). O formato das
mensagens está em [contracts/sofia-a2a.md](./contracts/sofia-a2a.md).

## Prerequisites

- SDK .NET 10.
- Chave da OpenAI disponível só fora do Git: User Secrets do projeto
  `AgenteFinanceiro.Api` ou variável de ambiente `OpenAI__ApiKey`.
- Nome do modelo em `OpenAI:Model` ou `OpenAI__Model`, também fora do
  repositório. O plano não fixa um identificador de modelo.
- Porta local 5100 livre.
- Nenhum banco de dados e nenhum servidor MCP.

Na primeira implementação, o diretório de checkpoints locais entra no
ignore do Git. Segredos não são gravados em `appsettings.json`.

## Setup

Na raiz do repositório, depois que os projetos existirem:

```powershell
dotnet restore AgenteFinanceiro.sln
dotnet build AgenteFinanceiro.sln
dotnet user-secrets -p src/AgenteFinanceiro.Api set "OpenAI:ApiKey" "<chave>"
dotnet user-secrets -p src/AgenteFinanceiro.Api set "OpenAI:Model" "<modelo>"
```

A ausência da chave deve impedir a Api de subir e deve explicar qual
configuração falta.

## Run

Terminal 1, Api na porta 5100:

```powershell
$env:ASPNETCORE_URLS = "http://localhost:5100"
dotnet run --project src/AgenteFinanceiro.Api
```

Terminal 2, simulador. O programa descobre o Agent Card e envia uma
solicitação A2A com a pergunta de janeiro de 2026:

```powershell
dotnet run --project src/Sofia.Simulator
```

SOFIA não é iniciado.

## Automated tests

```powershell
dotnet test tests/AgenteFinanceiro.Tests
```

Essa suíte cobre cálculos, ferramentas, aprovação e a integração A2A com
`IChatClient` substituto. Ela não precisa da chave OpenAI.

## Validation scenarios

### 1. Saúde

`GET http://localhost:5100/health` retorna HTTP 200 e estado saudável.

### 2. Descoberta

`GET http://localhost:5100/.well-known/agent-card.json` retorna o cartão
`AgenteFinanceiro`, a interface
`http://localhost:5100/a2a/agente-financeiro` e as seis skills do
contrato. O simulador imprime esse nome antes da resposta financeira.

### 3. Fluxo de caixa de janeiro

A solicitação A2A "Qual o fluxo de caixa de janeiro de 2026?" mostra
total de receitas 100000.00, total de despesas 87000.00, saldo inicial
15000.00, resultado 13000.00 e saldo final 28000.00. Não há pedido de
aprovação.

### 4. Lacuna

Perguntar abril de 2026 lista janeiro, fevereiro e março e não mostra
valor de abril.

### 5. Sessão

Na mesma execução do processo, uma segunda pergunta sem mês, "compare
Folha e Marketing", usa janeiro: 40000.00, 5000.00 e diferença 35000.00.
Depois de encerrar e subir a Api de novo, a mesma pergunta sem mês pede
o período.

### 6. Aprovação sem transferência

Pedir redução de 25% das despesas de janeiro recebe pedido de aprovação
e não mostra 21750.00. No `message:send`, essa espera é uma Task A2A
com estado `TASK_STATE_INPUT_REQUIRED` (input-required no protocolo) e
sem Artifact. Responder `aprovo` na mesma sessão mostra
`Recomendação demonstrativa` e 21750.00, na mesma Task com estado
`TASK_STATE_COMPLETED` e um Artifact de texto. Consultar janeiro de novo
ainda mostra saldo final 28000.00.

Pedir redução de 20% mostra `Recomendação demonstrativa` e 17400.00 sem
espera.

### 7. Checkpoint

Com a aprovação de 25% ainda pendente, encerrar a Api deixa a conversa
indisponível e mantém arquivo JSON no diretório local de checkpoints.
Esse arquivo não contém a chave da OpenAI. Subir de novo recarrega a
base Aurora com os mesmos saldos. A pendência pode ser retomada pelo
workflow sem recuperar os turnos anteriores.

### 8. Independência

Os cenários 2 e 3 terminam com a Api e o Sofia.Simulator apenas. Nenhum
passo exige SOFIA ou um endpoint MCP.
