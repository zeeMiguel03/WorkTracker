# WorkTracker

Aplicação web para registar rendimentos, despesas, fontes de trabalho, contas e tarefas num único local.

## Modelo inicial

A primeira versão da base de dados inclui:

- `User` — utilizadores da aplicação
- `Source` — origem do rendimento/despesa, por exemplo Vinted, Padeiro ou Freelance
- `Entry` — movimento financeiro
- `TransactionType` — tipo de movimento (`Income` / `Expense`)
- `Account` — local onde o dinheiro entra ou sai
- `AccountType` — tipo de conta
- `Task` — tarefas do utilizador
- `TaskStatus` — colunas do Kanban

## Kanban inicial

Cada novo utilizador deverá começar com estas colunas:

1. To Do
2. Doing
3. Testing
4. Done

A posição das colunas é controlada por `TaskStatus.SortOrder` e a posição das tarefas dentro de cada coluna por `Task.SortOrder`.

## Ficheiros da base de dados

- `database/schema.dbml` — modelo para dbdiagram.io
- `database/seed-template.sql` — dados iniciais de referência e template para criar os estados do Kanban de um utilizador
