# WorkTracker

Aplicação web para gerir produtos, encomendas, canais de venda e tarefas num único local, com foco no cálculo de lucro por produto.

## Estrutura inicial da base de dados

O repositório contém apenas a definição inicial das tabelas para servir de base ao desenvolvimento do projeto.

Tabelas atuais:

- `User`
- `Source`
- `Task`
- `TaskStatus`

A tabela `TaskStatus` permite suportar um Kanban com estados como `To Do`, `Doing`, `Testing` e `Done`, mas esses estados não são inseridos automaticamente na base de dados.

O lucro de cada produto é calculado a partir do preço de venda, preço de compra, portes e outros custos. O projeto não controla contas bancárias, cartões, transferências ou movimentos financeiros genéricos.

## Ficheiros

- `database/schema.dbml` — modelo visual para dbdiagram.io
- `database/schema.sql` — definição SQL das tabelas e relações em PostgreSQL

O projeto da aplicação ainda não foi criado. Estes ficheiros servem apenas como ponto de partida para implementares depois o backend, frontend e migrations.
