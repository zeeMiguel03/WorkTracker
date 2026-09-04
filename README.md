# WorkTracker

Aplicação web para registar rendimentos, despesas, fontes de trabalho, contas e tarefas num único local.

## Estrutura inicial da base de dados

O repositório contém apenas a definição inicial das tabelas para servir de base ao desenvolvimento do projeto.

Tabelas atuais:

- `User`
- `Source`
- `Entry`
- `TransactionType`
- `Account`
- `AccountType`
- `Task`
- `TaskStatus`

A tabela `TaskStatus` permite suportar um Kanban com estados como `To Do`, `Doing`, `Testing` e `Done`, mas esses estados não são inseridos automaticamente na base de dados.

## Ficheiros

- `database/schema.dbml` — modelo visual para dbdiagram.io
- `database/schema.sql` — definição SQL das tabelas e relações em PostgreSQL

O projeto da aplicação ainda não foi criado. Estes ficheiros servem apenas como ponto de partida para implementares depois o backend, frontend e migrations.
