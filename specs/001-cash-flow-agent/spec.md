# Feature Specification: AgenteFinanceiro — Análise de Fluxo de Caixa

**Feature Branch**: `001-cash-flow-agent`

**Created**: 2026-10-27

**Status**: Draft

**Input**: User description: "Crie a especificação funcional de uma aplicação chamada AgenteFinanceiro. A aplicação será um agente de IA especializado em análise de fluxo de caixa, que será utilizado posteriormente por um orquestrador chamado SOFIA através do protocolo A2A. O agente deverá receber perguntas em linguagem natural; consultar dados financeiros fictícios armazenados em memória; informar receitas, despesas e saldos de determinado período; calcular fluxo de caixa de um mês; identificar períodos com resultado negativo; comparar despesas entre categorias; gerar um parecer financeiro com base nos resultados das ferramentas; permitir continuidade de uma conversa por sessão; impedir a criação de informações financeiras inexistentes; gerar recomendações demonstrativas, claramente identificadas como tais; solicitar aprovação humana quando o usuário pedir uma recomendação de redução de despesas superior a 20%; expor suas funcionalidades por A2A; publicar um Agent Card para descoberta pela SOFIA; disponibilizar um projeto cliente chamado Sofia.Simulator para validar chamadas A2A. Defina requisitos funcionais, requisitos não funcionais, histórias de usuário, critérios de aceitação, tratamento de erros e cenários de teste. Não implemente código nesta etapa."

## Clarifications

### Session 2026-10-27

- Q: Onde a fonte financeira fica armazenada nesta aula? → A: Repositório em memória com os dados fictícios da base Aurora, somente leitura, sem banco de dados.
- Q: Onde o agente é hospedado, qual provedor de IA e qual porta local? → A: ASP.NET Core, provedor OpenAI, porta local 5100.
- Q: O que a aprovação humana autoriza, e onde ficam a sessão e o checkpoint? → A: A aprovação libera a recomendação sensível e não executa transferência financeira. Sessões ficam em memória. Checkpoints de aprovação ficam em arquivos JSON locais de demonstração.
- Q: Como o Sofia.Simulator valida o agente? → A: A comunicação A2A é real, com o SDK oficial. O simulador descobre o Agent Card e envia uma solicitação A2A.
- Q: Esta aula entrega um servidor MCP? → A: Não. O projeto fica preparado para integração MCP futura, sem servidor MCP nesta aula.

## User Scenarios & Testing *(mandatory)*

O AgenteFinanceiro responde perguntas em português sobre o fluxo de caixa
da base demonstrativa definida nesta especificação. Os critérios de
aceitação e os cenários de teste de cada história são os Acceptance
Scenarios. O tratamento de erros correspondente está em Edge Cases e em
Error Handling.

Uma história P1, entregue sozinha, já permite consultar um mês da base
sem apresentar informação financeira inexistente.

### User Story 1 - Consultar o fluxo de caixa de um mês (Priority: P1)

Uma pessoa pergunta, em linguagem natural, o fluxo de caixa de um mês
existente na base. O agente informa receitas, despesas, saldos e o
resultado daquele mês, deixando claro que a base é demonstrativa.

**Why this priority**: É o motivo da aplicação. Sem o fluxo de caixa de
um mês, as demais análises não têm valor.

**Independent Test**: Perguntar o fluxo de caixa de janeiro, fevereiro
e março de 2026 e conferir cada valor com a base desta especificação.

**Acceptance Scenarios**:

1. **Given** a base demonstrativa e uma conversa nova, **When** a
   pessoa pergunta "Qual o fluxo de caixa de janeiro de 2026?",
   **Then** a resposta identifica a base como demonstrativa, o período
   como janeiro de 2026, e informa receitas de Vendas 80000.00 e
   Serviços 20000.00 (total 100000.00), despesas de Folha 40000.00,
   Aluguel 8000.00, Fornecedores 22000.00, Impostos 12000.00 e
   Marketing 5000.00 (total 87000.00), saldo inicial 15000.00,
   resultado 13000.00 e saldo final 28000.00. A resposta não solicita
   aprovação humana.
2. **Given** a mesma base, **When** a pessoa pergunta o fluxo de caixa
   de fevereiro de 2026, **Then** a resposta informa receita total
   55000.00, despesa total 79000.00, saldo inicial 28000.00, resultado
   -24000.00 e saldo final 4000.00, além das receitas e despesas por
   origem e categoria daquele mês.
3. **Given** a mesma base, **When** a pessoa pede receitas, despesas e
   saldos de março de 2026, **Then** a resposta informa receita total
   85000.00, despesa total 77000.00, saldo inicial 4000.00, resultado
   8000.00 e saldo final 12000.00, com os itens de receita e despesa
   de março.
4. **Given** uma consulta já respondida de janeiro, **When** a mesma
   pergunta é repetida, **Then** os valores monetários permanecem os
   de janeiro definidos nesta especificação.
5. **Given** uma demonstração em que o agente consegue interpretar a
   pergunta, **When** a pessoa pede o fluxo de caixa de março de 2026,
   **Then** os valores da base ficam visíveis em até 30 segundos e não
   há espera por aprovação humana.

---

### User Story 2 - Recusar informação financeira inexistente (Priority: P1)

Uma pessoa pede período, categoria, documento, alíquota ou alteração
que a base não contém. O agente informa a lacuna e não completa a
resposta com informação financeira criada na hora.

**Why this priority**: Um fluxo de caixa com valor inventado invalida a
consulta. Esta história acompanha a P1.

**Independent Test**: Pedir abril de 2026, a categoria Viagens, uma
alíquota de imposto e a inclusão de uma despesa, e verificar que nenhum
valor novo aparece.

**Acceptance Scenarios**:

1. **Given** a base limitada a janeiro, fevereiro e março de 2026,
   **When** a pessoa pede o fluxo de caixa de abril de 2026, **Then** a
   resposta informa que abril de 2026 não existe na base, lista os três
   meses disponíveis e não apresenta receita, despesa, saldo ou
   resultado de abril.
2. **Given** a base, **When** a pessoa pede a despesa de Viagens em
   janeiro de 2026, **Then** a resposta informa que Viagens não é uma
   categoria da base, lista Folha, Aluguel, Fornecedores, Impostos e
   Marketing, e não atribui zero nem outro valor a Viagens.
3. **Given** a base, em que Impostos é um valor de despesa e não uma
   alíquota, **When** a pessoa pergunta "qual a alíquota de imposto em
   janeiro?", **Then** a resposta informa que a base não contém
   alíquota, pode citar a despesa Impostos de janeiro (12000.00) como
   valor existente e não apresenta essa despesa dividida pela receita
   como alíquota.
4. **Given** a base somente leitura, **When** a pessoa pede para lançar,
   alterar ou apagar uma receita ou despesa, **Then** a resposta recusa
   a alteração e uma consulta posterior de janeiro continua com os
   mesmos valores da base.
5. **Given** a base sem documentos fiscais, **When** a pessoa pede número
   de nota, CNPJ ou outro documento, **Then** a resposta informa que o
   documento não existe na base e não apresenta identificador algum.

---

### User Story 3 - Identificar períodos com resultado negativo (Priority: P2)

Uma pessoa pergunta em quais períodos o resultado foi negativo. O agente
aponta somente os meses em que o resultado é menor que zero.

**Why this priority**: Distingue mês deficitário de mês superavitário,
que é o uso seguinte da consulta mensal.

**Independent Test**: Pedir os períodos negativos da base e confirmar
que apenas fevereiro de 2026 é citado, com resultado -24000.00.

**Acceptance Scenarios**:

1. **Given** a base dos três meses, **When** a pessoa pergunta quais
   períodos tiveram resultado negativo, **Then** a resposta cita somente
   fevereiro de 2026, com resultado -24000.00, saldo inicial 28000.00 e
   saldo final 4000.00, e não classifica janeiro nem março como
   negativos.
2. **Given** a base, **When** a pessoa pergunta se janeiro de 2026 teve
   resultado negativo, **Then** a resposta informa que não, e apresenta
   o resultado 13000.00 daquele mês.
3. **Given** a base, **When** a pessoa pede os períodos negativos entre
   novembro de 2025 e fevereiro de 2026, **Then** a resposta informa
   novembro e dezembro de 2025 como ausentes, apresenta fevereiro de
   2026 como negativo e não preenche os meses ausentes com zero.

---

### User Story 4 - Comparar despesas entre categorias (Priority: P2)

Uma pessoa compara categorias de despesa. O agente mostra os totais
existentes e a diferença, no período pedido ou já estabelecido na
conversa.

**Why this priority**: Explica a composição da saída de caixa depois que
o resultado do mês já pode ser consultado.

**Independent Test**: Comparar Folha e Marketing em janeiro e conferir
40000.00, 5000.00 e a diferença de 35000.00.

**Acceptance Scenarios**:

1. **Given** a base, **When** a pessoa pede para comparar Folha e
   Marketing em janeiro de 2026, **Then** a resposta informa Folha
   40000.00, Marketing 5000.00, diferença Folha menos Marketing de
   35000.00 e que Folha é a maior das duas.
2. **Given** a base, **When** a pessoa pede a comparação de Folha,
   Aluguel e Marketing em janeiro de 2026, **Then** a resposta informa
   os três totais (40000.00, 8000.00 e 5000.00) e indica Folha como a
   maior, sem reduzir a comparação a um único par não pedido.
3. **Given** a base, **When** a pessoa compara a Folha de janeiro com a
   Folha de fevereiro, **Then** a resposta informa 40000.00 e 40000.00 e
   a diferença de 0.00.
4. **Given** a base, **When** a pessoa pede para tratar Vendas como
   categoria de despesa na comparação com Folha, **Then** a resposta
   informa que Vendas é receita e Folha é despesa, mostra os valores
   registrados se ambos foram pedidos e não coloca Vendas no ranking de
   despesas.

---

### User Story 5 - Continuar a conversa na mesma sessão (Priority: P2)

Uma pessoa faz perguntas encadeadas enquanto a aplicação está em
execução. A sessão fica em memória e reutiliza período, categorias e
aprovação pendente. Outra sessão não herda esse contexto. Encerrar o
processo descarta a conversa.

**Why this priority**: A análise ocorre em mais de uma pergunta. Sem
sessão, a pessoa repete o mês a cada turno.

**Independent Test**: Consultar janeiro, depois pedir a comparação de
Folha e Marketing sem repetir o mês, e repetir a segunda pergunta em
uma sessão nova.

**Acceptance Scenarios**:

1. **Given** uma sessão em que janeiro de 2026 já foi o período
   consultado, **When** a pessoa pede "compare Folha e Marketing" sem
   citar o mês, **Then** a resposta usa janeiro de 2026, declara esse
   período e informa 40000.00, 5000.00 e a diferença de 35000.00.
2. **Given** a sessão anterior, **When** a mesma pergunta sem mês é feita
   em uma sessão nova, **Then** o agente pede o período e não reutiliza
   janeiro nem qualquer aprovação pendente da outra sessão.
3. **Given** uma sessão desconhecida, **When** a pessoa tenta continuar
   nela, **Then** o agente informa que a sessão não foi encontrada e só
   prossegue em uma sessão nova, sem contexto da sessão citada.
4. **Given** uma sessão em memória em que janeiro de 2026 já foi
   consultado, **When** o processo da aplicação é encerrado e iniciado
   de novo, **Then** essa conversa não existe mais e a pergunta seguinte
   sem mês não reutiliza janeiro.

---

### User Story 6 - Receber um parecer baseado nos cálculos (Priority: P3)

Uma pessoa pede um parecer. O texto interpreta apenas os valores
calculados para aquela solicitação e não inclui recomendação, a menos
que a recomendação tenha sido pedida.

**Why this priority**: O parecer explica o mês depois que os números já
podem ser consultados e comparados.

**Independent Test**: Pedir o parecer de fevereiro e verificar que
-24000.00 aparece, que não há número de outro mês e que não há
recomendação de redução.

**Acceptance Scenarios**:

1. **Given** a base, **When** a pessoa pede um parecer do fluxo de caixa
   de fevereiro de 2026, **Then** o parecer identifica a base
   demonstrativa, inclui o resultado -24000.00 e somente números que
   pertençam ao fluxo de caixa calculado de fevereiro, e não traz
   recomendação de redução.
2. **Given** a base, **When** a pessoa pede um parecer só de janeiro de
   2026, **Then** o parecer não cita os valores exclusivos de fevereiro
   ou de março, como o resultado -24000.00 ou o saldo final 12000.00.
3. **Given** uma consulta factual de janeiro já respondida na sessão,
   **When** a pessoa pede "faça o parecer disso", **Then** o parecer usa
   janeiro de 2026 e apenas os valores calculados desse mês.

---

### User Story 7 - Recomendação demonstrativa e aprovação acima de 20% (Priority: P3)

Uma pessoa pede uma recomendação. O agente a identifica como
demonstrativa. Se o pedido for de redução de despesas superior a 20%,
a recomendação só sai depois de uma aprovação humana explícita na mesma
sessão. Essa aprovação libera o texto da recomendação. Ela não transfere
dinheiro nem altera saldos.

**Why this priority**: A recomendação é orientativa e, acima de 20%,
precisa de uma pessoa para autorizar a emissão. As consultas continuam
válidas sem essa etapa.

**Independent Test**: Pedir redução de 20% e de 25% sobre as despesas de
janeiro, recusar uma vez e aprovar em um turno seguinte.

**Acceptance Scenarios**:

1. **Given** a base e nenhuma aprovação pendente, **When** a pessoa pede
   uma recomendação para reduzir 20% das despesas de janeiro de 2026,
   **Then** a resposta contém a expressão "Recomendação demonstrativa",
   informa a redução calculada de 17400.00 sobre a base de 87000.00 e
   não pede aprovação humana.
2. **Given** a base, **When** a pessoa pede uma recomendação para reduzir
   25% das despesas de janeiro de 2026, **Then** o agente pede aprovação
   humana, informa o percentual 25% e a base de despesas 87000.00, e não
   apresenta o valor da redução, o valor restante nem a lista de ações
   da recomendação.
3. **Given** a solicitação de 25% aguardando aprovação na mesma sessão,
   **When** a pessoa responde com uma aprovação explícita, **Then** a
   resposta contém "Recomendação demonstrativa" e a redução calculada de
   21750.00, sem exigir que a pessoa repita o pedido. Os saldos da base
   permanecem os desta especificação e nenhuma transferência é feita.
4. **Given** a solicitação de 25% aguardando aprovação, **When** a pessoa
   recusa de forma explícita, **Then** o agente confirma que a
   recomendação não será emitida e não apresenta 21750.00.
5. **Given** a mesma pendência, **When** a pessoa responde "talvez" ou
   muda de assunto com uma consulta factual de março, **Then** a
   recomendação continua retida, a consulta de março é respondida com os
   valores da base e o agente lembra que a recomendação ainda aguarda
   decisão.
6. **Given** uma sessão sem período estabelecido, **When** a pessoa pede
   "recomende reduzir mais de 20% das despesas" sem percentual exato e
   sem mês, **Then** o agente pede o período, solicita aprovação humana
   antes de emitir recomendação e, mesmo após aprovar, não escolhe um
   percentual nem apresenta valor de corte até que a pessoa informe um
   percentual explícito.
7. **Given** a aprovação de 25% concedida em uma sessão, **When** outra
   sessão pede o resultado dessa recomendação sem novo pedido e sem nova
   aprovação, **Then** a segunda sessão não recebe a recomendação retida
   na primeira.
8. **Given** a base de janeiro, **When** a pessoa pede para transferir
   ou pagar um valor, **Then** o agente recusa, não altera saldos e
   informa que a aprovação humana apenas libera recomendação sensível.
9. **Given** uma redução de 25% aguardando aprovação, **When** o
   checkpoint é gravado em arquivo JSON local e o processo é encerrado,
   **Then** a conversa em memória não continua, o arquivo ainda contém
   período, percentual, base de despesas e a situação da aprovação, sem
   credencial, e a recomendação continua retida. A demonstração pode
   retomar essa pendência a partir do arquivo, sem restaurar os turnos
   anteriores. Os saldos recarregados em memória permanecem os da base.

---

### User Story 8 - Descobrir o agente e validar sem SOFIA (Priority: P2)

Uma pessoa usa o cliente Sofia.Simulator para descobrir o AgenteFinanceiro
e exercer as mesmas solicitações que o orquestrador SOFIA fará depois.
SOFIA não precisa existir para isso.

**Why this priority**: A integração futura depende de um cartão de
descoberta e de um cliente de validação. A consulta da história 1 já
entrega valor antes desse cliente.

**Independent Test**: Com SOFIA ausente, sem banco de dados e sem
servidor MCP, abrir o Sofia.Simulator, descobrir o Agent Card na porta
local 5100 e enviar uma solicitação A2A real pelo SDK oficial para obter
o fluxo de caixa de janeiro de 2026.

**Acceptance Scenarios**:

1. **Given** o agente em execução e SOFIA ausente, **When** o
   Sofia.Simulator consulta o Agent Card, **Then** o cartão identifica o
   agente como AgenteFinanceiro, descreve a análise de fluxo de caixa
   sobre base demonstrativa e lista as capacidades: consultar receitas,
   despesas e saldos; calcular o fluxo de caixa de um mês; identificar
   períodos com resultado negativo; comparar despesas entre categorias;
   gerar parecer; gerar recomendação demonstrativa com aprovação humana
   quando a redução pedida for superior a 20%.
2. **Given** o cartão localizado pelo simulador no agente em execução
   na porta local 5100, **When** o simulador envia uma solicitação A2A
   real, pelo SDK oficial, com a pergunta de fluxo de caixa de janeiro
   de 2026, **Then** a resposta traz os mesmos valores do cenário 1 da
   User Story 1. Uma chamada interna que apenas imite o protocolo não
   satisfaz este cenário.
3. **Given** o simulador em uma sessão com pedido de redução de 25% das
   despesas de janeiro, **When** o simulador envia a aprovação humana
   nessa sessão, **Then** a recomendação demonstrativa é emitida com a
   redução de 21750.00.
4. **Given** apenas o agente e o Sofia.Simulator, **When** a validação
   dos cenários 1 a 3 desta história é executada, **Then** nenhum passo
   exige que SOFIA esteja presente ou acessível.
5. **Given** uma consulta de janeiro concluída pelo simulador, **When**
   a pessoa examina o registro operacional dessa solicitação, **Then**
   percebe que ela ocorreu, quanto tempo levou e se falhou, e não
   encontra credencial nem o conteúdo integral do demonstrativo ou da
   pergunta.
6. **Given** o agente hospedado em ASP.NET Core, com OpenAI como
   provedor de interpretação, sem banco de dados e sem servidor MCP,
   **When** o Sofia.Simulator descobre o Agent Card e envia uma
   solicitação A2A, **Then** a validação termina com o fluxo de caixa de
   janeiro e não depende de MCP nem de SOFIA.

---

### Edge Cases

- Abril de 2026, ou qualquer mês fora de janeiro a março de 2026, gera
  lacuna. Os meses disponíveis são informados. Nenhum valor daquele mês
  é criado. Dezembro de 2025 também não é derivado do saldo inicial de
  janeiro.
- A categoria Viagens, ou outra fora de Folha, Aluguel, Fornecedores,
  Impostos e Marketing, gera lacuna. O valor zero não é usado no lugar
  da categoria ausente.
- Pedido de alíquota, nota fiscal, CNPJ ou documento equivalente gera
  lacuna. A despesa Impostos permanece um valor monetário, não uma
  alíquota.
- Pedido para incluir, alterar ou apagar lançamento é recusado. A base
  consultada em seguida permanece a desta especificação.
- Período obrigatório ausente, e sessão sem período, faz o agente
  perguntar o mês antes de calcular.
- Pergunta vazia ou incompreensível faz o agente pedir uma nova
  formulação, sem valores financeiros.
- Intervalo com meses dentro e fora da base devolve só os meses
  existentes e nomeia os ausentes, sem preencher ausência com zero.
- Resultado igual a zero não seria período negativo. Na base atual não
  há mês com resultado zero; janeiro é 13000.00 e março é 8000.00.
- Redução de exatamente 20% não pede aprovação. Redução superior a 20%,
  inclusive "mais de 20%" sem número exato, pede aprovação e retém a
  recomendação.
- "Um quarto" equivale a 25% e pede aprovação. "Um quinto" equivale a
  20% e não pede aprovação.
- Resposta ambígua à aprovação ("talvez", "depois") mantém a pendência e
  não libera a recomendação.
- Aprovação ou recusa em outra sessão não altera a sessão pendente.
- Consulta factual durante a pendência é respondida, e a recomendação
  continua retida.
- Sessão desconhecida não dá acesso ao contexto de outra conversa.
- Comparar receita com despesa não cria uma categoria de despesa mista.
- Se a base de despesas do escopo pedido for inexistente, o percentual
  de redução não é calculado.
- Se a interpretação da pergunta não puder ser concluída, inclusive
  quando o provedor OpenAI estiver indisponível, a pessoa é informada da
  falha e não recebe números criados para completar a resposta. Valores
  já apresentados em turno anterior bem-sucedido da mesma sessão
  continuam válidos como contexto enquanto o processo seguir em execução.
- Pedido de transferência, pagamento ou mudança de saldo é recusado. A
  aprovação humana não movimenta dinheiro. Os saldos permanecem os da
  base.
- Encerrar o processo descarta as sessões em memória. O arquivo JSON
  local do checkpoint de aprovação, se ainda existir, conserva a
  pendência para a demonstração e não restaura os turnos da conversa.
  Credenciais não entram nesse arquivo.
- Esta aula não oferece servidor MCP. Uma consulta de fluxo de caixa não
  espera integração MCP. O agente também não grava a base em banco de
  dados.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A pessoa DEVE poder fazer perguntas em português, em
  linguagem natural, sobre o fluxo de caixa da base demonstrativa.
- **FR-002**: O agente DEVE consultar somente a base fictícia definida
  em Key Entities, por um repositório em memória. Esta aula NÃO DEVE usar
  banco de dados. A base DEVE ser carregada em memória durante a
  execução, DEVE ser somente leitura para o agente e NÃO DEVE depender
  de cadastro da pessoa nem de SOFIA. A execução seguinte DEVE recarregar
  os mesmos dados fictícios.
- **FR-003**: Para um mês existente, o agente DEVE informar receitas por
  origem e o total, despesas por categoria e o total, saldo inicial,
  saldo final e resultado do mês.
- **FR-004**: O fluxo de caixa do mês DEVE ser calculado como resultado
  igual a receita total menos despesa total, e saldo final igual a
  saldo inicial mais resultado. O saldo inicial de um mês posterior
  DEVE ser o saldo final do mês anterior na base.
- **FR-005**: O agente DEVE identificar como período negativo apenas o
  mês cujo resultado seja menor que zero. Fevereiro de 2026 DEVE ser o
  único período negativo da base. Janeiro e março NÃO DEVEM ser
  classificados como negativos.
- **FR-006**: A pessoa DEVE poder comparar duas ou mais categorias de
  despesa em um mês, ou a mesma categoria em dois ou mais meses
  existentes. A resposta DEVE trazer cada total existente, a diferença
  quando duas categorias ou dois meses forem nomeados (primeiro menos
  segundo) e qual valor é maior.
- **FR-007**: Quando a pessoa pedir um parecer, o texto DEVE se basear
  nos valores calculados para aquela solicitação. Todo número do parecer
  DEVE existir nesse cálculo. O parecer DEVE identificar a base como
  demonstrativa e NÃO DEVE incluir recomendação de redução, salvo quando
  a própria pessoa a solicitar.
- **FR-008**: O agente NÃO DEVE criar receita, despesa, saldo, resultado,
  categoria, período, alíquota, documento ou identificador ausente da
  base e ausente de um cálculo pedido sobre valores da base. Dado
  ausente DEVE ser informado como lacuna.
- **FR-009**: Recomendação DEVE ser emitida somente quando a pessoa pedir
  recomendação, sugestão ou redução de despesas. O texto DEVE conter a
  expressão "Recomendação demonstrativa" e NÃO DEVE ser apresentado como
  determinação contábil oficial.
- **FR-010**: Quando a pessoa pedir recomendação de redução de despesas
  superior a 20% da base de despesas do escopo pedido, o agente DEVE
  solicitar aprovação humana e reter a recomendação até uma aprovação
  explícita na mesma sessão. Enquanto retida, a resposta NÃO DEVE
  incluir o valor da redução, o valor restante nem as ações recomendadas.
  A aprovação DEVE controlar somente a liberação dessa recomendação
  sensível. Ela NÃO DEVE executar, autorizar ou simular transferência,
  pagamento ou lançamento. Os saldos permanecem os desta especificação.
  No canal A2A, a resposta que pede essa aprovação é uma Task com estado
  `input-required` e ainda sem Artifact da recomendação. Depois da
  aprovação na mesma sessão, essa Task passa a `completed` e traz um
  Artifact de texto que contém `Recomendação demonstrativa` e o valor
  calculado.
  Consultas que não entram neste requisito continuam respondidas como
  Message.
- **FR-011**: Consulta de receitas, despesas, saldos, fluxo de caixa
  mensal, períodos negativos, comparação de categorias, parecer e
  recomendação de redução de até 20% (inclusive exatamente 20%) DISPENSAM
  aprovação humana. Essa dispensa é explícita. A aprovação humana desta
  funcionalidade ocorre somente no caso de FR-010.
- **FR-012**: Aprovação e recusa DEVEM ser decisões explícitas da pessoa
  na mesma sessão. Recusa DEVE impedir a emissão daquela recomendação.
  Resposta ambígua DEVE manter a retenção e pedir uma decisão clara.
- **FR-013**: Uma sessão DEVE conservar o período, as categorias usadas e
  a aprovação pendente para os turnos seguintes enquanto o processo
  estiver em execução. Nesta primeira versão, esse estado DEVE ficar em
  memória. Encerrar o processo DEVE descartar a conversa. Pergunta de
  acompanhamento que omitir o mês DEVE usar o período da sessão ainda em
  memória e declarar qual período foi usado.
- **FR-014**: Sessões distintas NÃO DEVEM compartilhar contexto nem
  aprovação pendente. Sessão desconhecida DEVE ser informada como não
  encontrada, e a conversa só DEVE continuar em sessão nova.
- **FR-015**: Antes de calcular, o agente DEVE validar período e
  categorias contra a base e o contexto da sessão. Item inválido ou
  obrigatório ausente DEVE seguir Error Handling e NÃO DEVE receber valor
  calculado.
- **FR-016**: Depois da validação, o agente DEVE calcular e responder. Se
  FR-010 se aplicar, o agente DEVE interromper a emissão da recomendação,
  pedir a decisão humana e conservar período, base de despesas e
  percentual já reconhecidos para continuar na mesma sessão. Esse estado
  de aprovação DEVE ser gravado em arquivo JSON local de demonstração.
  O arquivo DEVE permitir retomar a pendência e NÃO DEVE conter segredo
  nem credencial. Ele NÃO DEVE substituir o repositório financeiro nem
  restaurar os turnos da conversa.
- **FR-017**: O agente DEVE expor as capacidades listadas na User Story 8
  pelo protocolo agente a agente (A2A) e DEVE publicar um Agent Card com
  o nome AgenteFinanceiro, a descrição de análise de fluxo de caixa sobre
  base demonstrativa e essas capacidades. A comunicação A2A DEVE ser real
  e DEVE usar o SDK oficial do protocolo. Chamada interna que apenas
  imite o protocolo NÃO DEVE atender este requisito.
- **FR-018**: O agente e o cliente Sofia.Simulator DEVEM poder ser usados
  para descoberta, consulta, parecer, recomendação e aprovação humana sem
  SOFIA. SOFIA permanece um cliente futuro desse mesmo protocolo, fora
  desta entrega.
- **FR-019**: O Sofia.Simulator DEVE descobrir o Agent Card e enviar ao
  menos uma solicitação A2A real ao agente na porta local 5100. Também
  DEVE poder usar a sessão em memória e transmitir a aprovação ou a
  recusa quando o agente a solicitar.
- **FR-020**: O agente DEVE ser hospedado em ASP.NET Core e, na
  demonstração local, DEVE atender na porta 5100. A interpretação das
  perguntas DEVE usar o provedor OpenAI. O identificador do modelo e a
  credencial DEVEM permanecer fora do repositório.
- **FR-021**: Esta aula NÃO DEVE implementar servidor MCP nem exigir
  cliente MCP para consulta, parecer ou recomendação. O agente DEVE
  responder por A2A sem MCP. Integração MCP futura NÃO DEVE ser condição
  para iniciar o agente nesta aula.

### Non-Functional Requirements

- **NFR-001**: A mesma consulta factual sobre o mesmo mês DEVE devolver
  sempre os valores monetários desta especificação, até o centavo.
- **NFR-002**: Em uma demonstração em que o agente consiga interpretar a
  pergunta, a pessoa DEVE ver o resultado de uma consulta factual de um
  mês conhecido em até 30 segundos. O tempo aguardando aprovação humana
  não entra nessa medida.
- **NFR-003**: Valores monetários DEVEM ser apresentados em reais, com
  duas casas decimais. O valor numérico DEVE coincidir com a base,
  independentemente do separador de milhar exibido.
- **NFR-004**: Respostas ao usuário DEVEM estar em português.
- **NFR-005**: Registro operacional de uma solicitação DEVE permitir saber
  que a solicitação ocorreu, quanto tempo levou e se falhou. Esse
  registro NÃO DEVE guardar credenciais nem o conteúdo integral dos
  demonstrativos ou da pergunta com os valores financeiros.
- **NFR-006**: A base demonstrativa NÃO DEVE ser modificável pela
  conversa, pelo simulador ou por SOFIA nesta funcionalidade.
- **NFR-007**: Falha ao interpretar uma pergunta, inclusive por
  indisponibilidade do provedor OpenAI, NÃO DEVE ser preenchida com
  números parciais inventados. A pessoa DEVE receber um aviso de que a
  análise não foi concluída.
- **NFR-008**: A demonstração local DEVE atender na porta 5100, sem banco
  de dados e sem servidor MCP.

### Error Handling

- **EH-001**: Período ausente da base. Informar a lacuna, listar janeiro,
  fevereiro e março de 2026, e não apresentar valores do período pedido.
- **EH-002**: Categoria de despesa ausente da base. Informar a lacuna,
  listar as cinco categorias e não usar zero no lugar da categoria.
- **EH-003**: Período obrigatório ausente na pergunta e na sessão.
  Perguntar o mês e não calcular.
- **EH-004**: Pergunta vazia ou incompreensível. Pedir nova formulação e
  não apresentar valores financeiros.
- **EH-005**: Pedido de alíquota ou documento inexistente. Informar a
  lacuna. Não criar alíquota, número de documento ou identificador.
- **EH-006**: Pedido de inclusão, alteração ou exclusão de dado
  financeiro. Recusar e manter a base inalterada.
- **EH-007**: Sessão desconhecida. Informar que não foi encontrada e
  continuar apenas em sessão nova.
- **EH-008**: Decisão humana ambígua. Manter a recomendação retida e
  pedir aprovação ou recusa explícita.
- **EH-009**: Interpretação não concluída. Informar a falha e não
  apresentar números criados para aquela resposta.
- **EH-010**: Comparação que mistura receita e despesa como se fossem a
  mesma natureza. Explicar a diferença, mostrar os valores registrados
  quando tiverem sido pedidos e não montar ranking de despesa com a
  receita.
- **EH-011**: Redução percentual cujo escopo de despesa não exista na
  base. Não calcular o percentual e informar a lacuna.
- **EH-012**: Intervalo com meses ausentes e meses existentes. Devolver
  somente os meses existentes e nomear os ausentes, sem usar zero para
  ausência.
- **EH-013**: Consulta factual enquanto uma recomendação aguarda
  aprovação. Responder a consulta, lembrar a pendência e não anexar a
  recomendação retida.
- **EH-014**: Redução "superior a 20%" ou equivalente, sem percentual
  numérico explícito. Pedir aprovação antes de qualquer recomendação e,
  depois da aprovação, ainda não inventar o percentual nem o valor do
  corte. Pedir o percentual explícito para poder calcular.
- **EH-015**: Pedido de transferência, pagamento ou alteração de saldo.
  Recusar, informar que a aprovação não executa transferência e manter
  os saldos da base.
- **EH-016**: Processo encerrado. Descartar a sessão em memória. Se o
  arquivo JSON local do checkpoint ainda existir, conservar a pendência
  para retomada da demonstração, sem restaurar turnos e sem gravar
  credencial.
- **EH-017**: Provedor OpenAI indisponível. Informar que a interpretação
  não foi concluída e não apresentar números criados para a resposta.

### Key Entities *(include if feature involves data)*

- **Base demonstrativa Aurora**: Conjunto fictício e somente leitura de
  uma empresa imaginária, usado exclusivamente para demonstração. Não
  representa organização real. É a única fonte aprovada dos valores
  desta funcionalidade. Nesta aula vive em um repositório em memória,
  sem banco de dados, e é recarregada com os mesmos valores a cada
  execução.
- **Período**: Mês civil. Os períodos existentes são janeiro, fevereiro
  e março de 2026.
- **Receita**: Entrada do mês, com origem e valor. Origens: Vendas e
  Serviços.
- **Despesa**: Saída do mês, com categoria e valor. Categorias: Folha,
  Aluguel, Fornecedores, Impostos e Marketing. Impostos é valor de
  despesa, não alíquota.
- **Fluxo de caixa mensal**: Receita total, despesa total, resultado
  (receita total menos despesa total), saldo inicial e saldo final
  (saldo inicial mais resultado).
- **Sessão**: Conversa isolada mantida em memória nesta primeira versão.
  Guarda período e categorias já estabelecidos enquanto o processo está
  em execução. Encerra com o processo e não é compartilhada.
- **Checkpoint de aprovação**: Arquivo JSON local de demonstração com a
  pendência da recomendação sensível: período, escopo, percentual, base
  de despesas e situação. Não contém credencial, não é a fonte financeira
  e não guarda os turnos da conversa.
- **Parecer**: Texto explicativo sobre valores já calculados na
  solicitação, identificado como análise da base demonstrativa.
- **Recomendação demonstrativa**: Orientação pedida pela pessoa, sempre
  rotulada com a expressão "Recomendação demonstrativa". Pode citar
  apenas valores da base ou percentuais explícitos aplicados por cálculo
  a esses valores. A aprovação humana libera esse texto quando a redução
  pedida é superior a 20%. Não produz transferência.
- **Decisão humana**: Aprovação ou recusa explícita, na mesma sessão, da
  emissão de uma recomendação sensível. Não autoriza pagamento,
  transferência nem mudança de saldo.
- **Agent Card**: Descrição publicável do AgenteFinanceiro para
  descoberta real por um cliente A2A, incluindo nome, finalidade e
  capacidades.
- **Sofia.Simulator**: Cliente de validação, distinto do agente e distinto
  de SOFIA. Descobre o Agent Card e envia solicitação A2A real pelo SDK
  oficial para o agente na porta local 5100. Pode seguir a sessão em
  memória e transmitir a decisão humana.

**Base aprovada — receitas**

| Período | Vendas | Serviços | Total |
| --- | ---: | ---: | ---: |
| Janeiro de 2026 | 80000.00 | 20000.00 | 100000.00 |
| Fevereiro de 2026 | 45000.00 | 10000.00 | 55000.00 |
| Março de 2026 | 70000.00 | 15000.00 | 85000.00 |

**Base aprovada — despesas**

| Período | Folha | Aluguel | Fornecedores | Impostos | Marketing | Total |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Janeiro de 2026 | 40000.00 | 8000.00 | 22000.00 | 12000.00 | 5000.00 | 87000.00 |
| Fevereiro de 2026 | 40000.00 | 8000.00 | 18000.00 | 7000.00 | 6000.00 | 79000.00 |
| Março de 2026 | 42000.00 | 8000.00 | 15000.00 | 9000.00 | 3000.00 | 77000.00 |

**Base aprovada — saldos e resultado**

| Período | Saldo inicial | Resultado | Saldo final | Resultado negativo |
| --- | ---: | ---: | ---: | --- |
| Janeiro de 2026 | 15000.00 | 13000.00 | 28000.00 | Não |
| Fevereiro de 2026 | 28000.00 | -24000.00 | 4000.00 | Sim |
| Março de 2026 | 4000.00 | 8000.00 | 12000.00 | Não |

O saldo inicial de janeiro é dado da base. Não existe dezembro de 2025
para derivá-lo. Alterar qualquer número desta base exige emenda desta
especificação.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Consultas do fluxo de caixa de janeiro, fevereiro e março
  de 2026 apresentam, cada uma, receita total, despesa total, saldo
  inicial, resultado e saldo final iguais aos da base, até o centavo.
- **SC-002**: Ao pedir os períodos com resultado negativo, a pessoa vê
  somente fevereiro de 2026, com resultado -24000.00. Janeiro e março
  não aparecem como negativos.
- **SC-003**: Em 100% dos pareceres avaliados, todo número citado consta
  do cálculo daquela solicitação, e o texto identifica a base como
  demonstrativa.
- **SC-004**: Um pedido de redução de 25% das despesas de janeiro não
  apresenta a recomendação nem o valor 21750.00 antes da aprovação
  explícita na mesma sessão. Um pedido de redução de 20% apresenta a
  "Recomendação demonstrativa" com o valor 17400.00 sem essa espera.
- **SC-005**: Na mesma sessão, uma comparação de Folha e Marketing feita
  depois da consulta de janeiro, sem repetir o mês, usa janeiro e os
  valores 40000.00 e 5000.00. Em outra sessão, essa pergunta sem mês não
  usa janeiro.
- **SC-006**: Usando somente o Sofia.Simulator, uma pessoa descobre o
  AgenteFinanceiro pelo Agent Card e obtém o fluxo de caixa de janeiro
  de 2026 com os valores da base, sem o orquestrador SOFIA.
- **SC-007**: Pedidos sobre abril de 2026, a categoria Viagens, uma
  alíquota ou um documento fiscal recebem aviso de lacuna e nenhum valor
  ou identificador criado para o item ausente.
- **SC-008**: A comparação entre Folha e Marketing em janeiro informa
  40000.00, 5000.00 e a diferença de 35000.00.
- **SC-009**: Em demonstração com interpretação concluída, a consulta
  factual de um mês conhecido fica visível em até 30 segundos, sem contar
  a espera por aprovação humana.
- **SC-010**: Em 100% dos registros operacionais avaliados de uma
  demonstração, credenciais e o conteúdo integral dos demonstrativos estão
  ausentes, e é possível perceber ocorrência, duração e falha da
  solicitação.
- **SC-011**: Depois da aprovação de uma recomendação de redução, os
  saldos de janeiro, fevereiro e março permanecem os da base. Nenhuma
  transferência ou pagamento é realizado.

## Assumptions

- A empresa da base é fictícia e se chama Aurora apenas para a pessoa
  reconhecer a demonstração. Não há vínculo com organização real.
- A moeda é o real brasileiro. O período de análise é o mês civil. Não
  há fluxo diário nem semanal nesta funcionalidade.
- "Superior a 20%" é estritamente maior que 20%. Exatamente 20% dispensa
  aprovação humana, conforme FR-011.
- A base do percentual é a despesa total do mês quando a pessoa não nomeia
  categoria. Quando nomeia uma categoria existente, a base é o total
  dessa categoria nesse mês. "Um quarto" significa 25%. "Um quinto"
  significa 20%. "Mais de 20%", sem número exato, aciona a aprovação e
  não autoriza o agente a escolher o percentual.
- A pessoa que conversa é quem aprova ou recusa. Não há segundo papel de
  aprovador nem cadastro de usuários. O Sofia.Simulator apenas transmite
  essa decisão na mesma sessão.
- "Resultados das ferramentas", no pedido desta funcionalidade, significam
  os valores calculados a partir da base aprovada. O parecer e a
  recomendação repetem esses valores. Não criam outros.
- A conversa da sessão existe só em memória e só enquanto o processo
  está em execução. O checkpoint JSON local conserva a pendência de
  aprovação para a demonstração e não reconstrói os turnos anteriores.
- A porta 5100 vale para a demonstração local desta aula. Outras portas
  de outros ambientes não estão definidas aqui.
- O identificador do modelo OpenAI e a credencial ficam fora do
  repositório. O formato interno do arquivo JSON e o nome do pacote do
  SDK oficial ficam para o planejamento, desde que o comportamento desta
  especificação seja atendido.
- Limitações técnicas desta aula: não há banco de dados; a base financeira
  não sobrevive como dado editável entre execuções e é sempre a tabela
  fictícia; a sessão de conversa se perde ao encerrar o processo; o
  checkpoint JSON não é ledger nem cópia integral dos demonstrativos; não
  há transferência financeira; não há servidor nem cliente MCP; o agente
  não atende esta aula por um protocolo A2A simulado.
- O primeiro trimestre de 2026 equivale a janeiro, fevereiro e março, todos
  presentes na base.
- Fora desta entrega: o orquestrador SOFIA, livros contábeis reais,
  integração bancária, autenticação de várias pessoas, edição da base,
  emissão de documentos fiscais, alíquotas, outros idiomas, outras
  moedas, banco de dados, servidor MCP e execução de transferências.
- As restrições de engenharia já ratificadas na constituição do projeto
  permanecem vigentes. Esta especificação não as substitui. A dispensa de
  aprovação humana dos casos de FR-011 é a regra funcional daquela
  constituição para esta funcionalidade. A preparação para MCP futuro não
  adiciona um segundo runtime de agente nesta aula.
