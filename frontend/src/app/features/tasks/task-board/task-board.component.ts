import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TaskCardModel } from '../components/task-card/task-card.component';
import { TaskColumn } from '../components/task-column/task-column.component';

@Component({
  imports: [RouterLink, TaskColumn],
  selector: 'app-task-board',
  styleUrl: './task-board.component.scss',
  templateUrl: './task-board.component.html',
})
export class TaskBoard {
  protected readonly columns = [
    {
      title: 'Por fazer',
      accent: '#98a2b3',
      tasks: [
        {
          id: 1,
          title: 'Definir categorias financeiras',
          description: 'Organizar as categorias usadas nos lançamentos.',
          priority: 'Alta',
          dueDate: '18 Set',
          source: 'Planeamento',
          assignee: 'José Rocha',
          initials: 'JR',
        },
        {
          id: 2,
          title: 'Rever fontes de trabalho',
          description: 'Validar as fontes ativas e remover duplicados.',
          priority: 'Média',
          dueDate: '20 Set',
          source: 'Operações',
          assignee: 'Miguel Silva',
          initials: 'MS',
        },
      ] satisfies readonly TaskCardModel[],
    },
    {
      title: 'Em progresso',
      accent: '#7592ff',
      tasks: [
        {
          id: 3,
          title: 'Construir dashboard financeiro',
          description: 'Preparar os cartões de resumo e o gráfico mensal.',
          priority: 'Alta',
          dueDate: '22 Set',
          source: 'Produto',
          assignee: 'José Rocha',
          initials: 'JR',
        },
        {
          id: 4,
          title: 'Criar formulário de tarefa',
          description: 'Definir os campos essenciais para novas tarefas.',
          priority: 'Baixa',
          dueDate: '24 Set',
          source: 'Desenvolvimento',
          assignee: 'Ana Costa',
          initials: 'AC',
        },
      ] satisfies readonly TaskCardModel[],
    },
    {
      title: 'Em revisão',
      accent: '#f79009',
      tasks: [
        {
          id: 5,
          title: 'Ajustar responsive do perfil',
          description: 'Confirmar o comportamento em portáteis e tablets.',
          priority: 'Média',
          dueDate: '25 Set',
          source: 'Design',
          assignee: 'Miguel Silva',
          initials: 'MS',
        },
      ] satisfies readonly TaskCardModel[],
    },
    {
      title: 'Concluídas',
      accent: '#12b76a',
      tasks: [
        {
          id: 6,
          title: 'Configurar navegação principal',
          description: 'Menu base e rotas principais do WorkTracker.',
          priority: 'Baixa',
          dueDate: '12 Set',
          source: 'Fundação',
          assignee: 'José Rocha',
          initials: 'JR',
        },
      ] satisfies readonly TaskCardModel[],
    },
  ] as const;
}
