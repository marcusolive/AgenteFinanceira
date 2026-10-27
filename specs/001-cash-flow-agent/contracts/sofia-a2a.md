# Contract: Sofia.Simulator ↔ AgenteFinanceiro (A2A)

Contrato entre o console Sofia.Simulator e o agente hospedado na Api.
SOFIA não participa. O transporte é o binding HTTP+JSON do SDK oficial
A2A (`A2A` / `A2A.AspNetCore`), exposto por
`Microsoft.Agents.AI.Hosting.A2A.AspNetCore`.

Base local: `http://localhost:5100`.

## Discovery

O simulador não monta o cartão à mão. Ele usa:

```csharp
A2ACardResolver resolver = new(new Uri("http://localhost:5100"));
AgentCard card = await resolver.GetAgentCardAsync();
AIAgent agent = await resolver.GetAIAgentAsync();
```

Pedido HTTP equivalente:

```http
GET /.well-known/agent-card.json
```

Resposta esperada, campos normativos:

| Campo | Valor |
| --- | --- |
| name | AgenteFinanceiro |
| description | Análise de fluxo de caixa sobre a base demonstrativa Aurora |
| version | 1.0 |
| supportedInterfaces[0].url | http://localhost:5100/a2a/agente-financeiro |
| supportedInterfaces[0].protocolBinding | HTTP+JSON (`ProtocolBindingNames.HttpJson`) |
| supportedInterfaces[0].protocolVersion | 1.0 |

Skills publicadas no cartão:

| id | Nome visível | Quando a pessoa pede |
| --- | --- | --- |
| consultar-periodo | Consultar receitas, despesas e saldos | Um mês conhecido |
| calcular-fluxo-caixa-mensal | Calcular o fluxo de caixa de um mês | Resultado, saldo inicial e saldo final |
| identificar-resultado-negativo | Identificar períodos com resultado negativo | Varredura da base ou de um intervalo |
| comparar-categorias | Comparar despesas entre categorias | Duas ou mais categorias, ou a mesma em meses distintos |
| gerar-parecer | Gerar parecer financeiro | Texto só com números calculados na solicitação |
| recomendar-reducao | Gerar recomendação demonstrativa | Redução pedida; aprovação humana se o percentual for superior a 20% |

O cartão não lista SOFIA como dependência e não contém chave de API.

## Message

Uma solicitação A2A real:

```http
POST /a2a/agente-financeiro/message:send
Content-Type: application/json
```

```json
{
  "message": {
    "role": "ROLE_USER",
    "parts": [
      {
        "text": "Qual o fluxo de caixa de janeiro de 2026?"
      }
    ],
    "messageId": null,
    "contextId": "demo-aurora-001"
  }
}
```

A resposta de sucesso traz `message.role` do agente, `parts` com o texto
e o mesmo `contextId`. Para janeiro de 2026 o texto contém, com os
valores numéricos da base: receitas 80000.00 e 20000.00 (total
100000.00), despesas 40000.00, 8000.00, 22000.00, 12000.00 e 5000.00
(total 87000.00), saldo inicial 15000.00, resultado 13000.00 e saldo
final 28000.00. O texto identifica a base como demonstrativa e não pede
aprovação humana.

O simulador, pelo SDK, não precisa montar esse JSON. `RunAsync` no
`AIAgent` devolvido por `GetAIAgentAsync` envia o `message:send`. O
`contextId` da conversa é o da `AgentSession` criada no cliente.

Continuação na mesma sessão: a mensagem seguinte usa o mesmo
`contextId`. Exemplo de acompanhamento depois de janeiro: "compare Folha
e Marketing". A resposta usa janeiro e os valores 40000.00, 5000.00 e
diferença 35000.00.

Outro `contextId` não reutiliza o mês nem uma aprovação pendente.

## Approval

Redução superior a 20% também viaja como mensagem A2A, não como
transferência.

Primeira mensagem, mesmo caminho `message:send`:

```json
{
  "message": {
    "role": "ROLE_USER",
    "parts": [
      { "text": "Recomende reduzir 25% das despesas de janeiro de 2026." }
    ],
    "contextId": "demo-aurora-001"
  }
}
```

A resposta pede aprovação humana, pode citar 25% e a base 87000.00, e
não inclui 21750.00 nem uma lista de ações de corte. Nenhuma linha da
base muda. Nesse caso o corpo da resposta é uma Task A2A com estado
`input-required`. O id da Task é o id do checkpoint do workflow.
Essa Task ainda não traz Artifact.

Segunda mensagem no mesmo `contextId`, texto de aprovação explícita
(`aprovo`): a mesma Task fica `completed` e traz um Artifact de texto. O texto
contém `Recomendação demonstrativa` e o valor 21750.00. Os saldos
permanecem os da base.

Texto de recusa explícita no mesmo `contextId`: a Task conclui sem
Artifact de recomendação, a recomendação não sai e 21750.00 não aparece.

`talvez` mantém a pendência.

Redução de 20% no mesmo formato devolve `Recomendação demonstrativa` e
17400.00 na primeira resposta, como Message, sem Task e sem pedido de
aprovação.

## Health

Fora do protocolo A2A. O simulador não precisa chamá-lo para a
descoberta.

```http
GET /health
```

Resposta de processo saudável: HTTP 200 e corpo de health check do
ASP.NET Core indicando saudável.

## Failures

| Situação | Comportamento visível |
| --- | --- |
| Cartão ausente ou host fora do ar | O simulador informa a falha de descoberta e não fabrica resposta financeira |
| Abril de 2026, categoria Viagens, alíquota ou documento | Lacuna, sem valor inventado |
| Transferência ou pagamento | Recusa. Saldos intactos |
| OpenAI indisponível na execução real | Aviso de análise não concluída, sem números criados para essa resposta |
| `contextId` desconhecido depois da reinicialização | Nova conversa, sem mês herdado. Checkpoint de aprovação não restaura os turnos |

## Out of scope

- Servidor MCP e qualquer mensagem MCP.
- JSON-RPC (`MapA2AJsonRpc`) nesta aula.
- `tasks/send`, `tasks/get`, polling, streaming de Artifact e push
  notification. Task e Artifact desta aula só aparecem na resposta do
  `message:send` de aprovação.
- Chamada em processo ao `ChatClientAgent` como substituto do
  `message:send`.
- SOFIA como remetente ou como configuração obrigatória.
