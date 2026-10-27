---
description: "Tarefas de implementação do AgenteFinanceiro"
---

# Tasks: AgenteFinanceiro — Análise de Fluxo de Caixa

**Input**: Design documents from `/specs/001-cash-flow-agent/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/sofia-a2a.md, quickstart.md

**Tests**: Pedidos na spec e nesta geração. Escrever o teste primeiro e vê-lo falhar antes da implementação correspondente.

**Organization**: Fases por história de usuário, na ordem P1, P2 e P3. As etapas pedidas (solution, pacotes, domínio, agente, A2A, simulador, testes e build) estão mapeadas no fim.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode correr em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: História desta fase (US1…US8)
- Cada tarefa cita dependência, caminho de arquivo e critério de conclusão

## Path Conventions

- Solução na raiz: `AgenteFinanceiro.sln`
- Projetos em `src/` e testes em `tests/`, conforme `plan.md`
- Base numérica e regras: `specs/001-cash-flow-agent/data-model.md`
- Contrato A2A: `specs/001-cash-flow-agent/contracts/sofia-a2a.md`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Criar a solution .NET 10 e instalar as dependências oficiais.

- [X] T001 Criar `AgenteFinanceiro.sln` e os projetos `net10.0` `src/AgenteFinanceiro.Domain/AgenteFinanceiro.Domain.csproj`, `src/AgenteFinanceiro.Application/AgenteFinanceiro.Application.csproj`, `src/AgenteFinanceiro.Infrastructure/AgenteFinanceiro.Infrastructure.csproj`, `src/AgenteFinanceiro.Api/AgenteFinanceiro.Api.csproj` (SDK Web), `src/Sofia.Simulator/Sofia.Simulator.csproj` (SDK Console) e `tests/AgenteFinanceiro.Tests/AgenteFinanceiro.Tests.csproj`. Depends on: nenhuma. Done when: `dotnet sln AgenteFinanceiro.sln list` mostra os seis projetos e cada `csproj` declara `TargetFramework` `net10.0`.
- [X] T002 Declarar referências de projeto: Application → Domain; Infrastructure → Domain; Api → Application e Infrastructure; Tests → Api, Application, Domain e Infrastructure; Sofia.Simulator sem referência a projeto interno. Arquivos: os seis `csproj` da T001. Depends on: T001. Done when: `dotnet build AgenteFinanceiro.sln` restaura o grafo e o simulador não referencia `AgenteFinanceiro.Api`.
- [X] T003 Adicionar pacotes com versão fixa. Application: `Microsoft.Agents.AI` 1.26.0 e `Microsoft.Agents.AI.Workflows` 1.26.0. Infrastructure: `Microsoft.Agents.AI.Workflows` 1.26.0. Api: `Microsoft.Agents.AI.OpenAI` 1.26.0 e `Microsoft.Agents.AI.Hosting.A2A.AspNetCore` 1.26.0-preview.261023.1. Simulator: `Microsoft.Agents.AI.A2A` 1.26.0-preview.261023.1. Tests: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` e `Microsoft.AspNetCore.Mvc.Testing`. Arquivos: `csproj` correspondentes. Depends on: T002. Done when: `dotnet restore AgenteFinanceiro.sln` conclui e nenhum pacote MCP é referenciado.
- [X] T004 [P] Configurar execução local sem segredos em `.gitignore`, `src/AgenteFinanceiro.Api/appsettings.json` e `src/AgenteFinanceiro.Api/Properties/launchSettings.json`. `applicationUrl` é `http://localhost:5100`. `appsettings.json` só nomeia `OpenAI:Model` vazio e o diretório `checkpoints`, sem chave. `.gitignore` ignora `checkpoints/`, `appsettings.*.local.json` e User Secrets. Depends on: T001. Done when: a porta 5100 está no launch profile e o repositório não contém valor de `OpenAI:ApiKey`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Entidades, interfaces, base fictícia, cálculos `decimal`, Model Client, Instructions, sessão, contexto, middleware e o esqueleto do workflow. Nenhuma história começa antes desta fase.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T005 [P] Escrever testes que falham em `tests/AgenteFinanceiro.Tests/Calculations/CashFlowCalculatorTests.cs` para janeiro (receita 100000.00, despesa 87000.00, inicial 15000.00, resultado 13000.00, final 28000.00), fevereiro (55000.00, 79000.00, 28000.00, -24000.00, 4000.00, negativo) e março (85000.00, 77000.00, 4000.00, 8000.00, 12000.00). Depends on: T003. Done when: `dotnet test` desse arquivo falha por tipo ou método ausente, não por assert de valor invertido.
- [X] T006 [P] Criar `src/AgenteFinanceiro.Domain/Finance/MonthPeriod.cs`. Year é int e 2026 nesta base. Month é int de 1 a 12 e a base só contém 1, 2 e 3. Identidade é Year + Month. O saldo inicial de janeiro 15000.00 é dado e não é derivado de 2025. Depends on: T001. Done when: o tipo compila e rejeita mês fora de 1–12.
- [X] T007 [P] Criar o enum `src/AgenteFinanceiro.Domain/Finance/RevenueSource.cs` com Vendas e Servicos. Depends on: T001. Done when: só essas duas origens existem.
- [X] T008 [P] Criar o enum `src/AgenteFinanceiro.Domain/Finance/ExpenseCategory.cs` com Folha, Aluguel, Fornecedores, Impostos e Marketing. Impostos é despesa, não alíquota. Depends on: T001. Done when: o enum não tem categoria extra.
- [X] T009 Criar `src/AgenteFinanceiro.Domain/Finance/RevenueLine.cs` com Period, Source e Amount `decimal`. Depends on: T006, T007. Done when: Amount é `decimal`.
- [X] T010 Criar `src/AgenteFinanceiro.Domain/Finance/ExpenseLine.cs` com Period, Category e Amount `decimal`. Depends on: T006, T008. Done when: Amount é `decimal`.
- [X] T011 Criar `src/AgenteFinanceiro.Domain/Finance/MonthlyCashFlow.cs` com Period, Revenues, Expenses, TotalRevenue, TotalExpense, OpeningBalance, Result = TotalRevenue − TotalExpense, ClosingBalance = OpeningBalance + Result e IsNegative somente quando Result < 0. Depends on: T009, T010. Done when: zero não marca IsNegative.
- [X] T012 Criar `src/AgenteFinanceiro.Domain/Finance/CategoryComparison.cs` com Period, Items, Difference (primeira menos segunda quando há exatamente duas) e Largest. Ausência de categoria não vira zero. Depends on: T006, T008. Done when: o tipo representa a regra sem calcular ainda.
- [X] T013 [P] Criar `src/AgenteFinanceiro.Domain/Finance/ApprovalState.cs` com NotRequired, Pending, Approved e Rejected. Depends on: T001. Done when: os quatro estados compilam.
- [X] T014 Criar `src/AgenteFinanceiro.Domain/Finance/ReductionAssessment.cs` com Period, Category opcional, Percent, ExpenseBase, ReductionAmount opcional e Decision. Depends on: T006, T008, T013. Done when: ReductionAmount pode ficar vazio enquanto Pending.
- [X] T015 Criar `src/AgenteFinanceiro.Domain/Finance/ConversationSession.cs` com ContextId, EstablishedPeriod opcional e PendingApproval opcional. Depends on: T006, T014. Done when: o tipo não tem campo de persistência em disco.
- [X] T016 Criar `src/AgenteFinanceiro.Domain/Finance/ApprovalCheckpoint.cs` permitindo período, categoria opcional, percentual, base de despesas, estado e identificador do checkpoint, e proibindo chave, token, texto integral da conversa e demonstrativo completo. Depends on: T013, T014. Done when: o tipo não tem propriedade de credencial nem coleção dos três meses.
- [X] T017 Criar `src/AgenteFinanceiro.Domain/Abstractions/IFinancialDataRepository.cs` e `src/AgenteFinanceiro.Domain/Abstractions/ICashFlowCalculator.cs` somente leitura. Depends on: T011, T012, T014. Done when: nenhuma interface expõe incluir, alterar ou apagar lançamento.
- [X] T018 Gravar a base Aurora em `src/AgenteFinanceiro.Infrastructure/Data/AuroraFinancialSeed.cs` com os valores literais de `data-model.md`: janeiro Vendas 80000.00 e Serviços 20000.00, despesas 40000.00, 8000.00, 22000.00, 12000.00 e 5000.00; fevereiro 45000.00, 10000.00 e despesas 40000.00, 8000.00, 18000.00, 7000.00, 6000.00; março 70000.00, 15000.00 e despesas 42000.00, 8000.00, 15000.00, 9000.00, 3000.00; saldos iniciais 15000.00, 28000.00 e 4000.00. Depends on: T009, T010. Done when: o arquivo contém só esses números e nenhum mês fora de 2026-01 a 2026-03.
- [X] T019 Implementar `src/AgenteFinanceiro.Infrastructure/Data/InMemoryFinancialRepository.cs`. Depends on: T017, T018. Done when: cada execução recarrega a semente, consulta desconhecida devolve lacuna sem zero inventado, e não há tipo de banco de dados no projeto.
- [X] T020 Implementar `src/AgenteFinanceiro.Domain/Calculations/CashFlowCalculator.cs`. Depends on: T005, T017, T019. Done when: `dotnet test --filter CashFlowCalculatorTests` passa com os três meses e a cadeia de saldo.
- [X] T021 [P] Escrever `src/AgenteFinanceiro.Application/Agent/FinancialAgentInstructions.cs` em português: base demonstrativa Aurora, somente leitura, números apenas de ferramentas, sem alíquota inventada, recomendação só quando pedida e com o texto `Recomendação demonstrativa`, aprovação humana só para redução superior a 20%. Depends on: T001. Done when: o texto não embute chave nem os valores monetários da tabela.
- [X] T022 Implementar `src/AgenteFinanceiro.Application/Context/FinancialContextProvider.cs` como `AIContextProvider`, sobrescrevendo `ProvideAIContextAsync`. O contexto lista Aurora, meses 2026-01 a 2026-03 e as cinco categorias, e declara somente leitura. Depends on: T003, T006, T008. Done when: a mensagem de contexto não contém 80000.00 nem outra quantia, nem credencial.
- [X] T023 Implementar `src/AgenteFinanceiro.Application/Middleware/ExecutionLogMiddleware.cs` e registrá-lo com `AsBuilder().Use(runFunc, runStreamingFunc)`. O registro leva o nome AgenteFinanceiro, o identificador da sessão, a duração, o desfecho e o tipo da exceção. Depends on: T003. Done when: o middleware não lê nem grava texto da pergunta, demonstrativo ou `OpenAI:ApiKey`.
- [X] T024 Implementar `src/AgenteFinanceiro.Application/Sessions/InMemoryAgentSessionStore.cs` mapeando `contextId` para `AgentSession` criada por `CreateSessionAsync`. Depends on: T003. Done when: outro `contextId` não vê a sessão, e limpar o store descarta a conversa.
- [X] T025 Implementar `src/AgenteFinanceiro.Api/Hosting/OpenAIChatClientRegistration.cs` com `new OpenAIClient(new ApiKeyCredential(apiKey)).GetChatClient(model).AsIChatClient()`. A chave vem de User Secrets ou `OpenAI__ApiKey`. Depends on: T003, T004. Done when: chave ausente impede a inicialização com mensagem que cita a configuração faltante e não imprime a chave.
- [X] T026 Implementar `src/AgenteFinanceiro.Application/Agent/FinancialAgentFactory.cs` criando `ChatClientAgent` com `Id` `agente-financeiro`, `Name` `AgenteFinanceiro`, Instructions de T021, `AIContextProviders` de T022, middleware de T023 e `UseProvidedChatClientAsIs` desligado. Depends on: T021, T022, T023, T025. Done when: o agente compilado é um `AIAgent` e o Id permanece estável.
- [X] T027 Criar `src/AgenteFinanceiro.Application/Workflow/ValidationExecutor.cs`, `CalculationExecutor.cs`, `OpinionExecutor.cs` e `CashFlowWorkflowFactory.cs`. O fluxo valida período e categoria, calcula no Domain e monta a resposta com números das ferramentas. O `RequestPort.Create<ApprovalRequest, ApprovalDecision>("AprovacaoRecomendacao")` existe, e redução de até 20% inclusive 20% não envia mensagem a ele. Depends on: T020, T026. Done when: uma consulta de mês conhecido percorre validação, cálculo e parecer sem portão de aprovação.
- [X] T028 Compor DI e `MapHealthChecks("/health")` em `src/AgenteFinanceiro.Api/Program.cs`, registrando repositório, calculadora, store de sessão, factory do agente e workflow. Ainda sem `MapA2AHttpJson`. Depends on: T019, T024, T026, T027. Done when: a Api sobe em `http://localhost:5100` com chave configurada e `GET /health` retorna HTTP 200.

**Checkpoint**: Fundação pronta. Histórias podem começar.

---

## Phase 3: User Story 1 - Consultar o fluxo de caixa de um mês (Priority: P1) 🎯 MVP

**Goal**: Responder receitas, despesas, saldos e resultado de um mês da base, sem aprovação humana.

**Independent Test**: Perguntar janeiro, fevereiro e março de 2026 e conferir cada valor com a base. Repetir janeiro devolve os mesmos centavos.

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T029 [P] [US1] Escrever teste que falha em `tests/AgenteFinanceiro.Tests/Tools/CashFlowToolTests.cs` para `ObterFluxoCaixaMes` de 2026-01, 2026-02 e 2026-03, incluindo a repetição de janeiro e a ausência de pedido de aprovação. Depends on: T020. Done when: o teste falha porque a ferramenta ainda não existe.

### Implementation for User Story 1

- [X] T030 [US1] Implementar `ObterFluxoCaixaMes` em `src/AgenteFinanceiro.Application/Tools/CashFlowTools.cs` com `AIFunctionFactory.Create` e registrá-la em `src/AgenteFinanceiro.Application/Agent/FinancialAgentFactory.cs` e em `src/AgenteFinanceiro.Application/Workflow/CalculationExecutor.cs`. Depends on: T029, T027. Done when: `dotnet test --filter CashFlowToolTests` passa, janeiro mostra Vendas 80000.00, Serviços 20000.00, Folha 40000.00, Aluguel 8000.00, Fornecedores 22000.00, Impostos 12000.00, Marketing 5000.00, totais 100000.00 e 87000.00, saldo inicial 15000.00, resultado 13000.00 e saldo final 28000.00, e a resposta não pede aprovação.

**Checkpoint**: US1 funciona e é testável sem as outras histórias.

---

## Phase 4: User Story 2 - Recusar informação financeira inexistente (Priority: P1)

**Goal**: Informar lacuna para período, categoria, alíquota, documento ou alteração que a base não contém.

**Independent Test**: Pedir abril de 2026, Viagens, alíquota de imposto, um documento e a inclusão de despesa. Nenhum valor novo aparece e janeiro permanece igual.

### Tests for User Story 2 ⚠️

- [X] T031 [P] [US2] Escrever testes que falham em `tests/AgenteFinanceiro.Tests/Tools/GapToolTests.cs` para abril sem valores e com a lista janeiro, fevereiro e março; Viagens sem zero e com as cinco categorias; alíquota sem razão Impostos/receita, podendo citar Impostos de janeiro 12000.00; documento sem identificador; inclusão recusada com janeiro inalterado. Depends on: T019. Done when: os testes falham antes da implementação da lacuna.

### Implementation for User Story 2

- [X] T032 [US2] Completar lacunas em `src/AgenteFinanceiro.Infrastructure/Data/InMemoryFinancialRepository.cs` e `src/AgenteFinanceiro.Application/Tools/CashFlowTools.cs`, e a validação em `src/AgenteFinanceiro.Application/Workflow/ValidationExecutor.cs`. Depends on: T031, T030. Done when: `dotnet test --filter GapToolTests` passa e a base em memória continua a da semente.

**Checkpoint**: US1 e US2 passam juntas.

---

## Phase 5: User Story 3 - Identificar períodos com resultado negativo (Priority: P2)

**Goal**: Apontar só o mês com resultado menor que zero.

**Independent Test**: A varredura cita somente fevereiro de 2026 com resultado -24000.00, saldo inicial 28000.00 e saldo final 4000.00. Janeiro não é negativo e mostra 13000.00.

### Tests for User Story 3 ⚠️

- [X] T033 [P] [US3] Escrever testes que falham em `tests/AgenteFinanceiro.Tests/Tools/NegativePeriodToolTests.cs` para a base inteira, para a pergunta se janeiro é negativo, e para o intervalo novembro de 2025 a fevereiro de 2026 nomeando os meses ausentes sem preencher zero. Depends on: T020. Done when: o teste falha porque `ListarPeriodosNegativos` não existe.

### Implementation for User Story 3

- [X] T034 [US3] Implementar `ListarPeriodosNegativos` em `src/AgenteFinanceiro.Application/Tools/CashFlowTools.cs` e registrá-la em `src/AgenteFinanceiro.Application/Agent/FinancialAgentFactory.cs`. IsNegative só quando Result < 0. Depends on: T033, T030. Done when: `dotnet test --filter NegativePeriodToolTests` passa e janeiro e março não entram como negativos.

**Checkpoint**: US3 é testável sem comparação, sessão ou A2A.

---

## Phase 6: User Story 4 - Comparar despesas entre categorias (Priority: P2)

**Goal**: Mostrar totais existentes e a diferença, sem misturar receita no ranking de despesa.

**Independent Test**: Folha e Marketing em janeiro retornam 40000.00, 5000.00 e diferença 35000.00, com Folha maior.

### Tests for User Story 4 ⚠️

- [X] T035 [P] [US4] Escrever testes que falham em `tests/AgenteFinanceiro.Tests/Tools/ComparisonToolTests.cs` para Folha menos Marketing em janeiro (40000.00, 5000.00, 35000.00), para Folha, Aluguel e Marketing (40000.00, 8000.00, 5000.00, maior Folha), para Folha janeiro contra fevereiro (40000.00, 40000.00, diferença 0.00) e para Vendas contra Folha sem ranking de despesa misto. Depends on: T020. Done when: os testes falham antes das ferramentas de comparação.

### Implementation for User Story 4

- [X] T036 [US4] Implementar `CompararCategorias` e `CompararCategoriaEntreMeses` em `src/AgenteFinanceiro.Application/Tools/ComparisonTools.cs` e registrá-las em `src/AgenteFinanceiro.Application/Agent/FinancialAgentFactory.cs`. Difference é a primeira menos a segunda quando duas são nomeadas. Depends on: T035, T017. Done when: `dotnet test --filter ComparisonToolTests` passa.

**Checkpoint**: US4 não depende do simulador.

---

## Phase 7: User Story 5 - Continuar a conversa na mesma sessão (Priority: P2)

**Goal**: Reutilizar período e pendência na mesma sessão em memória, e esquecer a conversa ao encerrar o processo.

**Independent Test**: Depois de janeiro, "compare Folha e Marketing" sem mês usa janeiro. A mesma pergunta num `contextId` novo pede o período.

### Tests for User Story 5 ⚠️

- [X] T037 [P] [US5] Escrever testes que falham em `tests/AgenteFinanceiro.Tests/Sessions/SessionContinuityTests.cs` para acompanhamento no mesmo `contextId`, isolamento de outro `contextId`, sessão desconhecida e descarte depois de limpar o store. Depends on: T024. Done when: os testes falham antes do ajuste do store e do workflow.

### Implementation for User Story 5

- [X] T038 [US5] Fazer `src/AgenteFinanceiro.Application/Sessions/InMemoryAgentSessionStore.cs` e `src/AgenteFinanceiro.Application/Workflow/CashFlowWorkflowFactory.cs` gravarem `EstablishedPeriod` e declararem o mês usado no acompanhamento. Depends on: T037, T027. Done when: `dotnet test --filter SessionContinuityTests` passa e o store não grava arquivo.

**Checkpoint**: A sessão em memória está provada sem A2A.

---

## Phase 8: User Story 8 - Descobrir o agente e validar sem SOFIA (Priority: P2)

**Goal**: Publicar A2A real, Agent Card e Skills, e o console Sofia.Simulator descobrir o cartão e enviar uma solicitação.

**Independent Test**: Com SOFIA ausente, sem banco e sem MCP, o simulador lê o Agent Card na porta 5100 e obtém o fluxo de caixa de janeiro de 2026.

### Tests for User Story 8 ⚠️

- [X] T039 [P] [US8] Escrever testes que falham em `tests/AgenteFinanceiro.Tests/Integration/AgentCardDiscoveryTests.cs` e `tests/AgenteFinanceiro.Tests/Integration/A2AMessageSendTests.cs`, usando `WebApplicationFactory` e um `IChatClient` substituto em `tests/AgenteFinanceiro.Tests/Integration/AgenteFinanceiroApiFactory.cs`. O cartão deve trazer name AgenteFinanceiro, a URL `http://localhost:5100/a2a/agente-financeiro`, binding HTTP+JSON e as seis skills do contrato. O `POST /a2a/agente-financeiro/message:send` de janeiro deve devolver os valores da US1. Depends on: T028, T030. Done when: os testes falham porque o mapa A2A ainda não existe.

### Implementation for User Story 8

- [X] T040 [US8] Implementar `src/AgenteFinanceiro.Api/Hosting/A2AHostRegistration.cs` com agente chaveado `agente-financeiro`, `AddA2AServer` e `MapA2AHttpJson` em `/a2a/agente-financeiro`, e chamá-lo em `src/AgenteFinanceiro.Api/Program.cs`. A resposta imediata é uma Message A2A com partes de texto. Depends on: T039, T028. Done when: o POST `message:send` chega ao workflow, a resposta de janeiro é uma Message e não é chamada em processo fora do HTTP.
- [X] T041 [US8] Implementar `src/AgenteFinanceiro.Api/Hosting/AgentCardFactory.cs` e publicar com `MapWellKnownAgentCard` em `src/AgenteFinanceiro.Api/Program.cs`. Skills: `consultar-periodo`, `calcular-fluxo-caixa-mensal`, `identificar-resultado-negativo`, `comparar-categorias`, `gerar-parecer`, `recomendar-reducao`. Depends on: T040. Done when: `GET /.well-known/agent-card.json` corresponde a `specs/001-cash-flow-agent/contracts/sofia-a2a.md` e não cita SOFIA nem chave.
- [X] T042 [US8] Implementar `src/Sofia.Simulator/Program.cs` com `A2ACardResolver` em `http://localhost:5100`, `GetAgentCardAsync`, `GetAIAgentAsync` e `RunAsync` da pergunta de janeiro, imprimindo o nome do cartão e o texto da resposta. O `csproj` do simulador continua sem referência à Api. Depends on: T041. Done when: o console só usa o pacote `Microsoft.Agents.AI.A2A` para falar com o agente.
- [X] T043 [US8] Fazer passar `tests/AgenteFinanceiro.Tests/Integration/AgentCardDiscoveryTests.cs` e `tests/AgenteFinanceiro.Tests/Integration/A2AMessageSendTests.cs`, inclusive o registro de `src/AgenteFinanceiro.Application/Middleware/ExecutionLogMiddleware.cs` sem credencial e sem demonstrativo integral. Depends on: T040, T041, T023. Done when: `dotnet test --filter AgentCardDiscoveryTests|A2AMessageSendTests` passa sem SOFIA, sem banco e sem servidor MCP.

**Checkpoint**: Descoberta e uma solicitação A2A real estão provadas com `IChatClient` substituto.

---

## Phase 9: User Story 6 - Receber um parecer baseado nos cálculos (Priority: P3)

**Goal**: O parecer usa só números calculados na solicitação, identifica a base demonstrativa e não inclui recomendação.

**Independent Test**: Parecer de fevereiro inclui -24000.00, não cita saldo final 12000.00 de março e não recomenda redução.

### Tests for User Story 6 ⚠️

- [X] T044 [P] [US6] Escrever testes que falham em `tests/AgenteFinanceiro.Tests/Tools/OpinionTests.cs` para fevereiro, para janeiro sem -24000.00 e sem 12000.00, e para "faça o parecer disso" depois de janeiro. Depends on: T027, T038. Done when: os testes falham antes do executor de parecer.

### Implementation for User Story 6

- [X] T045 [US6] Completar `src/AgenteFinanceiro.Application/Workflow/OpinionExecutor.cs` e o trecho de parecer em `src/AgenteFinanceiro.Application/Agent/FinancialAgentInstructions.cs`. Depends on: T044. Done when: `dotnet test --filter OpinionTests` passa e o parecer não emite `Recomendação demonstrativa`.

**Checkpoint**: O parecer não inventa número de outro mês.

---

## Phase 10: User Story 7 - Recomendação demonstrativa e aprovação acima de 20% (Priority: P3)

**Goal**: Liberar recomendação sensível só depois de aprovação humana (Human-in-the-Loop), gravar checkpoint JSON, devolver essa espera como Task A2A e a frase aprovada como Artifact, e não transferir valor.

**Independent Test**: 20% de janeiro devolve `Recomendação demonstrativa` e 17400.00 sem espera. 25% não mostra 21750.00 até `aprovo`. Os saldos permanecem os da base.

### Tests for User Story 7 ⚠️

- [X] T046 [P] [US7] Escrever testes que falham em `tests/AgenteFinanceiro.Tests/Approval/ReductionApprovalTests.cs` para 20% sem portão e valor 17400.00 sobre 87000.00; 25% em Pending sem 21750.00; aprovação posterior com `Recomendação demonstrativa` e 21750.00 e saldos inalterados; recusa sem 21750.00; "talvez" permanece Pending; "mais de 20%" sem percentual não escolhe corte; outra sessão não recebe a recomendação; transferência recusada; arquivo JSON com período, percentual, base e estado, sem credencial, e conversa descartada quando o store de memória é limpo. Depends on: T014, T016. Done when: os testes falham antes das ferramentas de redução e do checkpoint.

### Implementation for User Story 7

- [X] T047 [US7] Implementar `PrepararRecomendacaoReducao` em `src/AgenteFinanceiro.Application/Tools/ReductionTools.cs`. Até 20% inclusive devolve o valor. Superior a 20% devolve `AprovacaoPendente`, percentual e base, sem ReductionAmount. Depends on: T046, T020. Done when: o método não altera `MonthlyCashFlow`.
- [X] T048 [US7] Ligar o Human-in-the-Loop no `RequestPort` `AprovacaoRecomendacao` em `src/AgenteFinanceiro.Application/Workflow/CashFlowWorkflowFactory.cs` e responder com `CreateResponse` mais `SendResponseAsync`. A frase final é montada em C# e começa com `Recomendação demonstrativa`. Depends on: T047, T027. Done when: Pending não emite 21750.00 e Approved emite sem mudar saldo.
- [X] T049 [US7] Implementar `src/AgenteFinanceiro.Infrastructure/Checkpoints/CheckpointDirectory.cs` e o uso de `FileSystemJsonCheckpointStore` com `CheckpointManager.CreateJson` na execução do workflow. Retomada usa `InProcessExecution.ResumeStreamingAsync` com o mesmo `Id` `agente-financeiro`. Depends on: T048, T016. Done when: `dotnet test --filter ReductionApprovalTests` passa, o JSON não contém `OpenAI:ApiKey` nem os turnos, e o repositório em memória não lê esse arquivo.
- [X] T053 [P] [US7] Escrever o teste que falha em `tests/AgenteFinanceiro.Tests/Integration/A2AApprovalTaskTests.cs`. Redução de 25% no `message:send` devolve uma Task A2A com estado `input-required`, com o id do checkpoint, sem Artifact de recomendação e sem 21750.00. Depois de `aprovo` no mesmo `contextId`, a Task fica `completed` e traz um Artifact de texto que contém `Recomendação demonstrativa` e 21750.00. Recusa conclui sem esse Artifact. Depends on: T040, T046. Done when: o teste falha porque a espera ainda sai só como Message.
- [X] T054 [US7] No `message:send` já publicado em `src/AgenteFinanceiro.Api/Hosting/A2AHostRegistration.cs`, mapear a espera do `RequestPort` para uma Task A2A e a frase aprovada para um Artifact de texto. O id da Task é o id do checkpoint de T049. Usar os tipos `Message`, `Task` e `Artifact` do pacote `A2A`, com alias se `Task` colidir com `System.Threading.Tasks.Task`. Depends on: T053, T048, T049. Done when: `dotnet test --filter A2AApprovalTaskTests` passa, consultas que não pedem aprovação continuam Message, e não existe rota `tasks/send`, polling nem store de Artifact.

**Checkpoint**: Aprovação, checkpoint, Task e Artifact estão provados sem SOFIA.

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Documentar execução e fechar build e testes.

- [X] T050 [P] Documentar execução em `README.md`, apontando `specs/001-cash-flow-agent/quickstart.md`, os comandos `dotnet test tests/AgenteFinanceiro.Tests`, a porta 5100, User Secrets `OpenAI:ApiKey` e `OpenAI:Model`, e a ausência de banco, MCP e SOFIA. Não copiar chave. Depends on: T043, T049. Done when: um leitor sobe a Api e o simulador só com o README e o quickstart.
- [X] T051 Alinhar comandos de `specs/001-cash-flow-agent/quickstart.md` aos caminhos reais dos projetos, sem alterar os valores esperados da base. Depends on: T050. Done when: cada comando do quickstart referencia um projeto que existe na solution.
- [X] T052 Executar `dotnet build AgenteFinanceiro.sln` e `dotnet test tests/AgenteFinanceiro.Tests`. Depends on: T051, T054. Done when: build e testes terminam com código 0 e a suíte cobre cálculos, ferramentas, aprovação, Message, Task, Artifact e A2A.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: começa imediatamente.
- **Foundational (Phase 2)**: depende da Phase 1 e bloqueia todas as histórias.
- **User Stories (Phase 3+)**: dependem da Phase 2. Ordem de prioridade: US1 e US2 (P1), US3, US4, US5 e US8 (P2), US6 e US7 (P3).
- **Polish (Phase 11)**: depende das histórias desejadas. O build final depende de todas.

### User Story Dependencies

- **US1 (P1)**: depois da Phase 2. Sem dependência de outra história. MVP.
- **US2 (P1)**: depois da Phase 2. Usa o repositório da fundação e a ferramenta da US1 se ela já existir; o teste de lacuna também roda direto no repositório.
- **US3 (P2)**: depois da Phase 2. Independente de comparação e de A2A.
- **US4 (P2)**: depois da Phase 2. Independente de US3.
- **US5 (P2)**: depois da Phase 2. O store nasce na fundação; esta fase prova o acompanhamento.
- **US8 (P2)**: depois da US1 para o `message:send` de janeiro ter números reais. Não depende de US6 nem de US7.
- **US6 (P3)**: depois da US1 e da sessão da US5 para o parecer "disso".
- **US7 (P3)**: T046–T049 vêm depois da US1 e não exigem o console. T053 e T054 vêm depois do `message:send` da US8.

### Within Each User Story

- O teste da história é escrito e falha antes da implementação.
- Entidades e semente vêm antes do cálculo.
- Cálculo vem antes da ferramenta.
- Ferramenta vem antes do mapa A2A que a expõe.
- A história fecha no filtro `dotnet test` citado nela.

### Parallel Opportunities

- T004 em paralelo com T002 depois de T001.
- T005 a T016 em paralelo depois de T001 e T003, cada um no seu arquivo.
- T021 em paralelo com a semente, pois não leva valores monetários.
- Testes T029, T031, T033, T035, T037, T039, T044 e T046 podem ser escritos em paralelo depois da fundação, em arquivos distintos. T053 espera T040 e fica em arquivo próprio.
- US3 e US4 podem ser implementadas em paralelo: a comparação fica em `ComparisonTools.cs` e os períodos negativos ficam em `CashFlowTools.cs`. US2 e US3 editam `CashFlowTools.cs` e ficam em sequência.

---

## Parallel Example: User Story 1

```powershell
# Depois da Phase 2, o teste da US1 vem antes da ferramenta:
# tests/AgenteFinanceiro.Tests/Tools/CashFlowToolTests.cs

# Entidades da fundação, em paralelo, antes do cálculo:
# src/AgenteFinanceiro.Domain/Finance/MonthPeriod.cs
# src/AgenteFinanceiro.Domain/Finance/RevenueLine.cs
# src/AgenteFinanceiro.Domain/Finance/ExpenseLine.cs
# src/AgenteFinanceiro.Domain/Finance/MonthlyCashFlow.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Phase 1 e Phase 2.
2. Completar Phase 3 (US1).
3. Parar e rodar `dotnet test --filter CashFlowCalculatorTests|CashFlowToolTests`.
4. O mês da base já responde com os centavos da spec.

### Incremental Delivery

1. Setup e fundação.
2. US1 consulta do mês.
3. US2 lacunas.
4. US3 negativos e US4 comparação.
5. US5 sessão em memória.
6. US8 cartão, A2A e simulador.
7. US6 parecer.
8. US7 aprovação, checkpoint JSON, Task e Artifact.
9. README, quickstart e `dotnet test` completo.

### Etapas pedidas

| Etapa | Tarefas |
| --- | --- |
| Criar solution .NET e projetos | T001–T002 |
| Instalar dependências oficiais | T003 |
| Criar entidades e interfaces | T006–T017 |
| Criar dados financeiros fictícios | T018–T019 |
| Implementar cálculos financeiros | T005, T020 |
| Implementar Tools e Function Calling | T030, T032, T034, T036, T047 |
| Configurar Model Client e agente | T025–T026 |
| Criar Instructions | T021, T045 |
| Implementar AgentSession | T024, T038 |
| Implementar provedor de contexto | T022 |
| Implementar Middleware de observabilidade | T023, T043 |
| Criar Workflow de análise financeira | T027, T045 |
| Implementar Checkpoints e aprovação humana | T046–T049, T053–T054 |
| Configurar servidor A2A | T040, T054 |
| Publicar Agent Card e Skills | T041 |
| Criar Sofia.Simulator | T042 |
| Implementar testes unitários | T005, T029, T031, T033, T035, T037, T044, T046 |
| Implementar testes A2A | T039, T043, T053 |
| Documentar execução | T050–T051 |
| Executar build e testes finais | T052 |

### Parallel Team Strategy

1. Todos fecham Phase 1 e Phase 2.
2. Em seguida: uma pessoa na US1 e US2, outra na US3 e US4, outra na US5.
3. US8 começa quando a US1 passar.
4. US6 e US7 fecham o parecer e a aprovação.

---

## Notes

- [P] significa outro arquivo e nenhuma dependência aberta.
- T053 e T054 foram acrescentadas depois de T052. O build final depende de T054.
- Teste novo deve falhar antes do código que o faz passar.
- Não gravar `OpenAI:ApiKey` em arquivo versionado.
- Não adicionar pacote MCP nem projeto de banco.
- O simulador não referencia a Api.
- Parar em qualquer checkpoint e rodar o filtro de teste da fase.
