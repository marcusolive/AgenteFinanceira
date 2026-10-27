# Implementation Plan: AgenteFinanceiro — Análise de Fluxo de Caixa

**Branch**: `001-cash-flow-agent` | **Date**: 2026-10-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-cash-flow-agent/spec.md`

## Summary

O AgenteFinanceiro responde em português sobre o fluxo de caixa da base
demonstrativa Aurora (janeiro a março de 2026). Os números saem de código
C# com `decimal`. O modelo OpenAI só interpreta a pergunta, escolhe
ferramentas e redige o texto. Recomendação de redução superior a 20% fica
retida até aprovação humana e não movimenta saldo.

A hospedagem é ASP.NET Core em .NET 10, na porta local 5100, com o
Microsoft Agent Framework 1.26.0. O agente concreto é um `ChatClientAgent`
(`AIAgent`) sobre `IChatClient`. A sessão de conversa fica em memória. O
workflow de validação, cálculo, parecer e aprovação usa `RequestPort` e
grava checkpoint em JSON local com `FileSystemJsonCheckpointStore`. O
Agent Card é publicado em `/.well-known/agent-card.json`. O
Sofia.Simulator descobre esse cartão com `A2ACardResolver` e envia uma
solicitação A2A real. Não há banco de dados nem servidor MCP.

## Technical Context

**Language/Version**: C#, .NET 10 (`net10.0`)

**Primary Dependencies**: Microsoft Agent Framework 1.26.0
(`Microsoft.Agents.AI`, `Microsoft.Agents.AI.OpenAI`,
`Microsoft.Agents.AI.Workflows`); hospedagem A2A
`Microsoft.Agents.AI.Hosting.A2A.AspNetCore` 1.26.0-preview.261023.1;
cliente A2A `Microsoft.Agents.AI.A2A` 1.26.0-preview.261023.1; SDK de
protocolo `A2A` e `A2A.AspNetCore` 1.0.0-preview3 (transitivos);
`Microsoft.Extensions.AI` 10.10.x e `OpenAI` 2.14.x (transitivos)

**Storage**: Repositório em memória com a base Aurora. Sessão `AgentSession`
em memória, ligada ao `contextId` A2A. Checkpoint de aprovação em arquivos
JSON locais via `FileSystemJsonCheckpointStore`. Sem banco de dados.

**Testing**: xUnit e `Microsoft.AspNetCore.Mvc.Testing`. Cálculos e
ferramentas não chamam OpenAI. Integração A2A usa `WebApplicationFactory`
e um `IChatClient` substituto. O quickstart cobre a chamada real com OpenAI.

**Target Platform**: Kestrel local em `http://localhost:5100`. O simulador
é um console .NET 10 no mesmo computador.

**Project Type**: Serviço web ASP.NET Core mais cliente console.

**Performance Goals**: Consulta factual de um mês visível em até 30
segundos quando o provedor OpenAI responde, sem contar a espera humana
(NFR-002).

**Constraints**: Somente leitura da base. Sem segredos no repositório.
Logs sem credencial, sem demonstrativo integral e sem texto da pergunta.
Aprovação humana não executa transferência. Porta local 5100. Sem servidor
MCP. Agente utilizável sem SOFIA.

**Scale/Scope**: Uma instância local, uma base de três meses, uma conversa
por `contextId` enquanto o processo vive. Seis projetos na solução.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Exigência | Resultado |
| --- | --- | --- |
| I. SDD | Plano compatível com `spec.md` e com a constituição | Pass |
| II. Stack oficial | C#, .NET 10, ASP.NET Core, Microsoft Agent Framework oficial | Pass |
| III. IChatClient | OpenAI somente por `IChatClient` substituível em teste | Pass |
| IV. decimal | Cálculos no Domain, tipo `decimal` | Pass |
| V. Instructions e Function Calling | `ChatClientAgent` com Instructions e `AIFunction` | Pass |
| VI. AgentSession | `CreateSessionAsync` / `RunAsync(..., session)`; memória por conversa | Pass |
| VII. Provedor de contexto | `AIContextProvider` com catálogo da base, sem valores monetários | Pass |
| VIII. Middleware | `AIAgentBuilder.Use` registra operação, duração e falha, sem payload | Pass |
| IX. Segredos | User Secrets ou variável de ambiente; falha explícita se a chave falta | Pass |
| X. Workflow e checkpoints | Validação, cálculo, parecer e `RequestPort`; JSON oficial em disco | Pass |
| XI. A2A sem SOFIA | `MapA2AHttpJson` e simulador próprio; SOFIA não é referência | Pass |
| XII. Dados fictícios | Tabelas da spec no repositório em memória; lacuna sem invenção | Pass |
| XIII. Testes e execução | Projeto de testes e `quickstart.md` | Pass |

Nenhum gate falhou antes da pesquisa.

## Project Structure

### Documentation (this feature)

```text
specs/001-cash-flow-agent/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── sofia-a2a.md
└── tasks.md             # criado depois por /speckit-tasks
```

### Source Code (repository root)

```text
AgenteFinanceiro.sln
src/
├── AgenteFinanceiro.Domain/
│   ├── Finance/
│   ├── Calculations/
│   └── Abstractions/
├── AgenteFinanceiro.Application/
│   ├── Agent/
│   ├── Tools/
│   ├── Context/
│   ├── Middleware/
│   ├── Sessions/
│   └── Workflow/
├── AgenteFinanceiro.Infrastructure/
│   ├── Data/
│   └── Checkpoints/
├── AgenteFinanceiro.Api/
│   ├── Program.cs
│   ├── Health/
│   └── Hosting/
└── Sofia.Simulator/
    └── Program.cs
tests/
└── AgenteFinanceiro.Tests/
    ├── Calculations/
    ├── Tools/
    ├── Approval/
    └── Integration/
```

**Structure Decision**: Seis projetos, como pedido para a aula. Domain não
referencia o framework de agente. Application referencia Domain e o
framework. Infrastructure implementa o repositório em memória e o store de
checkpoint. Api compõe OpenAI, saúde e A2A. Sofia.Simulator referencia só
o cliente A2A oficial e não referencia a Api. Testes referenciam Api,
Application, Domain e Infrastructure.

Dependências de projeto:

- Application → Domain
- Infrastructure → Domain
- Api → Application, Infrastructure
- Sofia.Simulator → nenhum projeto interno
- AgenteFinanceiro.Tests → Api, Application, Domain, Infrastructure

## Architecture

### Composição em execução

1. A Api lê `OpenAI:ApiKey` e `OpenAI:Model` de User Secrets ou variável
   de ambiente. Chave ausente interrompe a inicialização.
2. `OpenAIClient.GetChatClient(model).AsIChatClient()` produz o
   `IChatClient`.
3. A Application monta o `ChatClientAgent` com Instructions, ferramentas
   `AIFunctionFactory.Create`, `FinancialContextProvider` e middleware de
   execução. O `Id` estável é `agente-financeiro` e o `Name` é
   `AgenteFinanceiro`.
4. O workflow envolve cada solicitação: validar, calcular pelas
   ferramentas e serviços de domínio, redigir o parecer com o agente e,
   só se a redução pedida for superior a 20%, abrir `RequestPort` de
   aprovação. Os demais casos dispensam o portão, de forma explícita.
5. `CheckpointManager.CreateJson(FileSystemJsonCheckpointStore)` grava o
   superstep em disco. A conversa em si não é restaurada desse arquivo.
6. `AddA2AServer` e `MapA2AHttpJson` publicam o agente em
   `/a2a/agente-financeiro`. `MapWellKnownAgentCard` publica o cartão.
   A resposta comum é uma Message. Só a espera de aprovação humana
   (Human-in-the-Loop, `RequestPort`) devolve uma Task `input-required`
   sem Artifact; a aprovação marca essa Task `completed` com um Artifact
   de texto. Não há `tasks/send`, polling nem outro endpoint.
7. `MapHealthChecks("/health")` responde o estado do processo.
8. O simulador usa `A2ACardResolver` em `http://localhost:5100` e
   `GetAIAgentAsync()`, depois `RunAsync`.

### Ferramentas

Cada ferramenta delega ao Domain e devolve o resultado calculado. Nenhuma
ferramenta chama o modelo.

| Função | Papel |
| --- | --- |
| `ObterFluxoCaixaMes` | Receitas, despesas, saldos e resultado de um mês |
| `ListarPeriodosNegativos` | Meses com resultado menor que zero, no intervalo pedido |
| `CompararCategorias` | Totais e diferença entre categorias no mesmo mês |
| `CompararCategoriaEntreMeses` | A mesma categoria em meses existentes |
| `PrepararRecomendacaoReducao` | Percentual explícito sobre a base de despesas |

`PrepararRecomendacaoReducao` devolve o valor calculado quando o
percentual é de até 20%. Quando é superior a 20%, devolve apenas
`AprovacaoPendente`, o percentual e a base, sem o valor do corte. O
workflow conserva esse valor no estado do checkpoint e só o emite depois
da aprovação, na frase montada em C# com o prefixo
`Recomendação demonstrativa`.

### MCP

Nenhum pacote MCP entra na solução. As ferramentas já são uma lista de
`AITool`. Uma aula futura pode acrescentar ferramentas nessa lista sem
ligar um servidor MCP para o agente responder.

## Package baseline

Versões conferidas no NuGet em 2026-10-27. A publicação dessas versões é
2026-10-23.

| Pacote | Versão | Projeto |
| --- | --- | --- |
| `Microsoft.Agents.AI` | 1.26.0 | Application |
| `Microsoft.Agents.AI.OpenAI` | 1.26.0 | Api |
| `Microsoft.Agents.AI.Workflows` | 1.26.0 | Application, Infrastructure |
| `Microsoft.Agents.AI.Hosting.A2A.AspNetCore` | 1.26.0-preview.261023.1 | Api |
| `Microsoft.Agents.AI.A2A` | 1.26.0-preview.261023.1 | Sofia.Simulator |
| `A2A` e `A2A.AspNetCore` | >= 1.0.0-preview3, transitivos | Api e Simulator |

A pesquisa das assinaturas está em [research.md](./research.md). O modelo
de dados está em [data-model.md](./data-model.md). O contrato do simulador
está em [contracts/sofia-a2a.md](./contracts/sofia-a2a.md). A validação
manual está em [quickstart.md](./quickstart.md).

## Constitution Check — Post-Design

| Princípio | Conferência depois do desenho | Resultado |
| --- | --- | --- |
| I a XIII | Contratos, modelo e quickstart repetem a base Aurora, o portão de 20%, a sessão em memória e o A2A real | Pass |
| VIII e IX | O contrato de log e o checkpoint excluem credencial e o demonstrativo integral | Pass |
| X | O JSON do framework é o checkpoint de demonstração; não é um segundo ledger | Pass |
| XI | O simulador não referencia SOFIA nem o projeto da Api | Pass |
| A2A Message, Task, Artifact | Message no `message:send` comum; Task e um Artifact de texto só na aprovação acima de 20%. Sem fila, polling ou `tasks/send` | Pass |
| MCP | Nenhum servidor MCP no desenho; a lista de `AITool` permanece o único encaixe futuro | Pass |

## Complexity Tracking

Nenhuma violação da constituição precisa de justificativa. Os seis
projetos são a separação pedida para a aula: domínio testável sem o
modelo, composição do agente, dados fictícios, hospedagem A2A, cliente
oficial e testes.
