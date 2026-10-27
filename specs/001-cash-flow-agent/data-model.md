# Data Model: AgenteFinanceiro

Fonte aprovada dos valores: as tabelas de [spec.md](./spec.md). Todo campo
monetário é `decimal`. Não há entidade de transferência, pagamento ou
documento fiscal.

## MonthPeriod

Representa um mês civil.

| Campo | Tipo | Regra |
| --- | --- | --- |
| Year | int | 2026 nesta base |
| Month | int | 1 a 12; a base só contém 1, 2 e 3 |

Identidade: Year + Month. Janeiro de 2026 é o primeiro mês e o saldo
inicial 15000.00 é dado, não é derivado de 2025.

## RevenueLine

| Campo | Tipo | Regra |
| --- | --- | --- |
| Period | MonthPeriod | Mês existente |
| Source | enum | Vendas ou Servicos |
| Amount | decimal | Valor da tabela, duas casas |

## ExpenseLine

| Campo | Tipo | Regra |
| --- | --- | --- |
| Period | MonthPeriod | Mês existente |
| Category | enum | Folha, Aluguel, Fornecedores, Impostos, Marketing |
| Amount | decimal | Valor da tabela. Impostos é despesa, não alíquota |

## MonthlyCashFlow

Calculado. Não é editável pela conversa.

| Campo | Tipo | Regra |
| --- | --- | --- |
| Period | MonthPeriod | Mês existente |
| Revenues | RevenueLine[] | Itens do mês |
| Expenses | ExpenseLine[] | Itens do mês |
| TotalRevenue | decimal | Soma das receitas |
| TotalExpense | decimal | Soma das despesas |
| OpeningBalance | decimal | Saldo inicial da tabela |
| Result | decimal | TotalRevenue − TotalExpense |
| ClosingBalance | decimal | OpeningBalance + Result |
| IsNegative | bool | Result < 0. Zero não é negativo |

Valores fechados:

| Mês | Receita | Despesa | Inicial | Resultado | Final | Negativo |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| 2026-01 | 100000.00 | 87000.00 | 15000.00 | 13000.00 | 28000.00 | Não |
| 2026-02 | 55000.00 | 79000.00 | 28000.00 | -24000.00 | 4000.00 | Sim |
| 2026-03 | 85000.00 | 77000.00 | 4000.00 | 8000.00 | 12000.00 | Não |

O saldo inicial de fevereiro é o saldo final de janeiro. O de março é o
saldo final de fevereiro.

## CategoryComparison

| Campo | Tipo | Regra |
| --- | --- | --- |
| Period | MonthPeriod | Obrigatório quando a comparação é no mesmo mês |
| Items | (Category, decimal)[] | Só categorias existentes. Ausência não vira zero |
| Difference | decimal? | Primeira menos segunda, quando exatamente duas são nomeadas |
| Largest | Category? | A maior entre as citadas |

Comparar Vendas com uma despesa não produz item de despesa. A resposta
informa naturezas diferentes.

## ReductionAssessment

Pedido de recomendação de redução. Não altera `MonthlyCashFlow`.

| Campo | Tipo | Regra |
| --- | --- | --- |
| Period | MonthPeriod | Obrigatório para calcular |
| Category | ExpenseCategory? | Vazio significa a despesa total do mês |
| Percent | decimal | Percentual explícito. "Mais de 20%" sem número não preenche este campo |
| ExpenseBase | decimal | Total do escopo |
| ReductionAmount | decimal? | ExpenseBase × Percent / 100, só quando a aprovação não está pendente ou já foi concedida |
| Decision | ApprovalState | Ver máquina abaixo |

Exemplos da spec: 20% de janeiro = 17400.00 sobre 87000.00, sem aprovação.
25% de janeiro = 21750.00 sobre 87000.00, só depois da aprovação.

## ApprovalState

| Estado | Significado |
| --- | --- |
| NotRequired | Percentual menor ou igual a 20%, ou a solicitação não é recomendação de redução |
| Pending | Percentual superior a 20%. A recomendação ainda não foi emitida |
| Approved | Decisão humana explícita na conversa. O texto demonstrativo pode sair |
| Rejected | Decisão humana explícita. O valor do corte não é emitido |

Transições:

- NotRequired não passa por Pending.
- Pending → Approved somente com aprovação explícita.
- Pending → Rejected somente com recusa explícita.
- Resposta ambígua permanece Pending.
- Approved e Rejected não mudam saldos.
- Outra sessão em memória não lê o Pending desta sessão.
- Encerrar o processo apaga a sessão. O checkpoint JSON ainda pode
  devolver o fluxo a Pending para a demonstração, sem restaurar os turnos.

## ConversationSession

Estado em memória. Não é gravado no checkpoint.

| Campo | Tipo | Regra |
| --- | --- | --- |
| ContextId | string | `contextId` A2A. Chave da conversa |
| AgentSession | sessão do framework | Criada por `CreateSessionAsync` |
| EstablishedPeriod | MonthPeriod? | Mês já usado nesta conversa |
| PendingApproval | ReductionAssessment? | Espelho em memória enquanto o processo vive |

Sessão desconhecida não herda outro `ContextId`.

## ApprovalCheckpoint

Projeção do que o workflow precisa retomar. O arquivo físico é o JSON do
`FileSystemJsonCheckpointStore`, não um ledger paralelo.

| Conteúdo permitido | Conteúdo proibido |
| --- | --- |
| Período, categoria opcional, percentual, base de despesas, estado da aprovação, identificador do checkpoint | Chave de API, token, texto integral da conversa, demonstrativo completo dos três meses, saldo alterado |

O repositório financeiro não lê esse arquivo. Na subida seguinte, a base
volta a ser a tabela Aurora.

## AgentCard publicado

Documento de descoberta, não é dado financeiro.

| Campo | Valor |
| --- | --- |
| Name | AgenteFinanceiro |
| Description | Análise de fluxo de caixa sobre a base demonstrativa Aurora |
| Version | 1.0 |
| Interface | HTTP+JSON em `http://localhost:5100/a2a/agente-financeiro` |
| Skills | consultar-periodo; calcular-fluxo-caixa-mensal; identificar-resultado-negativo; comparar-categorias; gerar-parecer; recomendar-reducao |

## Message, Task e Artifact

Objetos do protocolo A2A na mesma rota `message:send`. Não são tabelas
financeiras e não têm repositório próprio.

| Objeto | Quando existe | Conteúdo |
| --- | --- | --- |
| Message | Consulta factual, lacuna, parecer e redução de até 20% | Partes de texto com os números das ferramentas |
| Task | Redução superior a 20% ainda sem aprovação | Estado `input-required`. Id igual ao do `ApprovalCheckpoint`. Sem Artifact de recomendação |
| Artifact | A Task acima depois de aprovação explícita | Uma parte de texto com `Recomendação demonstrativa` e o valor calculado |

Recusa conclui a Task sem Artifact de recomendação e sem o valor do corte.

## Relacionamentos

- Um `MonthPeriod` tem duas `RevenueLine` e cinco `ExpenseLine`.
- Um `MonthlyCashFlow` é a leitura calculada de um `MonthPeriod`.
- Um `ReductionAssessment` aponta para um `MonthPeriod` e não grava linha
  nova de despesa.
- Uma `ConversationSession` pode apontar para um `ReductionAssessment` em
  Pending.
- Um `ApprovalCheckpoint` guarda a pendência do workflow, não a sessão.

## Validação

- Mês fora de 2026-01, 2026-02 e 2026-03 produz lacuna e lista os três
  meses. Não cria linha.
- Categoria fora do enum produz lacuna e lista as cinco categorias. Não
  usa zero.
- Intervalo parcialmente coberto devolve só os meses existentes e nomeia
  os ausentes.
- Pedido de alíquota ou documento não cria campo.
- Pedido de inclusão, alteração, exclusão ou transferência não muda
  `MonthlyCashFlow`.
- Percentual não numérico em "mais de 20%" não preenche `Percent` nem
  `ReductionAmount`. O estado pode ficar Pending, e o cálculo espera o
  percentual explícito.
