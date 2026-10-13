using Application.DTOs.Task;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using NSubstitute;

namespace Application.Tests.Services;

public class TasksServiceTests
{
    private const int UserId = 42;
    private const int OtherUserId = 99;
    private const int StatusId = 10;
    private const int SourceId = 20;
    private const int TaskId = 7;

    private readonly ITasksRepository _tasksRepository = Substitute.For<ITasksRepository>();

    private readonly ITasksStatusRepository _statusRepository = Substitute.For<ITasksStatusRepository>();

    private readonly ISourceRepository _sourceRepository = Substitute.For<ISourceRepository>();

    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly TasksService _sut;

    public TasksServiceTests()
    {
        _currentUserService.GetUserId().Returns(UserId);

        _sut = new TasksService(
            _tasksRepository,
            _statusRepository,
            _sourceRepository,
            _currentUserService,
            _unitOfWork);
    }

    [Fact]
    public async Task CreateTaskAsync_WhenRelationsBelongToUser_CreatesAndSavesTask()
    {
        var dueDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);

        var dto = new CreateTaskDTO
        {
            SourceId = SourceId,
            TaskStatusId = StatusId,
            Title = "  Preparar relatório  ",
            Description = "  Rever os dados do mês  ",
            Priority = 2,
            SortOrder = 3,
            DueDate = dueDate
        };

        _statusRepository
            .GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus(UserId)));

        _sourceRepository
            .GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(CreateSource(UserId)));

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var result = await _sut.CreateTaskAsync(dto);

        Assert.Equal(UserId, result.UserId);
        Assert.Equal(SourceId, result.SourceId);
        Assert.Equal(StatusId, result.TaskStatusId);
        Assert.Equal("Preparar relatório", result.Title);
        Assert.Equal("Rever os dados do mês", result.Description);
        Assert.Equal(2, result.Priority);
        Assert.Equal(3, result.SortOrder);
        Assert.Equal(dueDate, result.DueDate);

        await _tasksRepository.Received(1).AddAsync(
            Arg.Is<Tasks>(task =>
                task.UserId == UserId &&
                task.SourceId == SourceId &&
                task.TaskStatusId == StatusId &&
                task.Title == "Preparar relatório"),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTaskAsync_WhenStatusBelongsToAnotherUser_ThrowsAndDoesNotSave()
    {
        var dto = CreateValidDto();

        _statusRepository
            .GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus(OtherUserId)));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.CreateTaskAsync(dto));

        Assert.Equal("TASK_STATUS_ACCESS_DENIED", exception.Code);

        await _tasksRepository.DidNotReceive().AddAsync(
            Arg.Any<Tasks>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTaskAsync_WhenSourceBelongsToAnotherUser_ThrowsAndDoesNotSave()
    {
        var dto = CreateValidDto();

        _statusRepository
            .GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus(UserId)));

        _sourceRepository
            .GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(CreateSource(OtherUserId)));


        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.CreateTaskAsync(dto));

        Assert.Equal("SOURCE_ACCESS_DENIED", exception.Code);

        await _tasksRepository.DidNotReceive().AddAsync(
            Arg.Any<Tasks>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTaskAsync_WhenStatusDoesNotExist_ThrowsAndDoesNotSave()
    {
        _statusRepository
            .GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.CreateTaskAsync(CreateValidDto()));

        Assert.Equal("TASK_STATUS_NOT_FOUND", exception.Code);

        await _sourceRepository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _tasksRepository.DidNotReceive().AddAsync(Arg.Any<Tasks>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTaskAsync_WhenSourceDoesNotExist_ThrowsAndDoesNotSave()
    {
        _statusRepository
            .GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus(UserId)));
        _sourceRepository
            .GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.CreateTaskAsync(CreateValidDto()));

        Assert.Equal("SOURCE_NOT_FOUND", exception.Code);

        await _tasksRepository.DidNotReceive().AddAsync(Arg.Any<Tasks>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTaskAsync_WithoutSource_CreatesTaskWithoutLookingUpSource()
    {
        var dto = CreateValidDto();
        dto.SourceId = null;
        _statusRepository
            .GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus(UserId)));

        var result = await _sut.CreateTaskAsync(dto);

        Assert.Null(result.SourceId);

        await _sourceRepository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _tasksRepository.Received(1).AddAsync(Arg.Is<Tasks>(task => task.SourceId == null), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskAsync_WhenTaskBelongsToUser_UpdatesAndSavesTask()
    {
        var task = CreateTask(UserId, SourceId, StatusId);

        _tasksRepository
            .GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(task));

        _statusRepository
           .GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
           .Returns(Task.FromResult<TasksStatus?>(CreateStatus(UserId)));

        _sourceRepository
          .GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
          .Returns(Task.FromResult<Source?>(CreateSource(UserId)));

        var dto = new UpdateTaskDTO
        {
            Id = TaskId,
            SourceId = SourceId,
            TaskStatusId = StatusId,
            Title = "Tarefa de teste atualizada",
            Description = "Descrição da tarefa de teste atualizada",
            Priority = 2,
            SortOrder = 1,
            DueDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc)
        };

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        await _sut.UpdateTaskAsync(dto);

        Assert.Equal("Tarefa de teste atualizada", task.Title);
        Assert.Equal("Descrição da tarefa de teste atualizada", task.Description);
        Assert.Equal(2, task.Priority);
        Assert.Equal(1, task.SortOrder);
        Assert.Equal(new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc), task.DueDate);

        _tasksRepository.Received(1).Update(task);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskAsync_WhenTaskBelongsToAnotherUser_ThrowsAndDoesNotSave()
    {
        var task = CreateTask(OtherUserId, SourceId, StatusId);

        _tasksRepository
            .GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(task));

        var dto = new UpdateTaskDTO
        {
            Id = TaskId,
            SourceId = SourceId,
            TaskStatusId = StatusId,
            Title = "Tarefa de teste atualizada",
            Description = "Descrição da tarefa de teste atualizada",
            Priority = 2,
            SortOrder = 1,
            DueDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc)
        };

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateTaskAsync(dto));

        Assert.Equal("TASK_ACCESS_DENIED", exception.Code);

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskAsync_WhenSourceBelongsToAnotherUser_ThrowsAndDoesNotSave()
    {
        var task = CreateTask(UserId, SourceId, StatusId);

        _tasksRepository
            .GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(task));

        _statusRepository
            .GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus(UserId)));

        _sourceRepository
            .GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(CreateSource(OtherUserId)));

        var dto = new UpdateTaskDTO
        {
            Id = TaskId,
            SourceId = SourceId,
            TaskStatusId = StatusId,
            Title = "Tarefa de teste atualizada",
            Description = "Descrição da tarefa de teste atualizada",
            Priority = 2,
            SortOrder = 1,
            DueDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc)
        };

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateTaskAsync(dto));

        Assert.Equal("SOURCE_ACCESS_DENIED", exception.Code);

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskAsync_WhenStatusBelongsToAnotherUser_ThrowsAndDoesNotSave()
    {
        var task = CreateTask(UserId, SourceId, StatusId);

        _tasksRepository
            .GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(task));

        _statusRepository
            .GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus(OtherUserId)));

        var dto = new UpdateTaskDTO
        {
            Id = TaskId,
            SourceId = SourceId,
            TaskStatusId = StatusId,
            Title = "Tarefa de teste atualizada",
            Description = "Descrição da tarefa de teste atualizada",
            Priority = 2,
            SortOrder = 1,
            DueDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc)
        };

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateTaskAsync(dto));

        Assert.Equal("TASK_STATUS_ACCESS_DENIED", exception.Code);

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskAsync_WhenTaskDoesNotExist_ThrowsAndDoesNotSave()
    {
        _tasksRepository
            .GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateTaskAsync(CreateUpdateDto()));

        Assert.Equal("TASK_NOT_FOUND", exception.Code);

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());
        await _statusRepository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskAsync_WhenStatusDoesNotExist_ThrowsAndDoesNotSave()
    {
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(CreateTask(UserId, SourceId, StatusId)));
        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateTaskAsync(CreateUpdateDto()));

        Assert.Equal("TASK_STATUS_NOT_FOUND", exception.Code);
        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());
        await _sourceRepository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskAsync_WhenSourceDoesNotExist_ThrowsAndDoesNotSave()
    {
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(CreateTask(UserId, SourceId, StatusId)));
        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus(UserId)));
        _sourceRepository.GetByIdAsync(SourceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Source?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateTaskAsync(CreateUpdateDto()));

        Assert.Equal("SOURCE_NOT_FOUND", exception.Code);

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskAsync_WithoutSource_UpdatesAndSavesTask()
    {
        var task = CreateTask(UserId, SourceId, StatusId);
        var dto = CreateUpdateDto();
        dto.SourceId = null;
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(task));
        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus(UserId)));

        await _sut.UpdateTaskAsync(dto);

        Assert.Null(task.SourceId);
        await _sourceRepository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        _tasksRepository.Received(1).Update(task);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTaskAsync_WhenTaskBelongsToUser_RemovesAndSavesTask()
    {
        var task = CreateTask(UserId, SourceId, StatusId);
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(task));

        await _sut.DeleteTaskAsync(TaskId);

        _tasksRepository.Received(1).Remove(task);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTaskAsync_WhenTaskDoesNotExist_ThrowsWithoutRemovingOrSaving()
    {
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.DeleteTaskAsync(TaskId));

        Assert.Equal("TASK_NOT_FOUND", exception.Code);

        _tasksRepository.DidNotReceive().Remove(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTaskAsync_WhenTaskBelongsToAnotherUser_ThrowsWithoutRemovingOrSaving()
    {
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(CreateTask(OtherUserId, SourceId, StatusId)));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.DeleteTaskAsync(TaskId));

        Assert.Equal("TASK_ACCESS_DENIED", exception.Code);

        _tasksRepository.DidNotReceive().Remove(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListTaskByIdAsync_WhenTaskBelongsToUser_ReturnsMappedTask()
    {
        var task = CreateTask(UserId, SourceId, StatusId);

        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(task));

        var result = await _sut.ListTaskByIdAsync(TaskId);

        Assert.Equal(task.UserId, result.UserId);
        Assert.Equal(task.SourceId, result.SourceId);
        Assert.Equal(task.TaskStatusId, result.TaskStatusId);
        Assert.Equal(task.Title, result.Title);
        Assert.Equal(task.Description, result.Description);
        Assert.Equal(task.Priority, result.Priority);
        Assert.Equal(task.SortOrder, result.SortOrder);
        Assert.Equal(task.CreatedAt, result.CreatedAt);
        Assert.Equal(task.UtCreation, result.UtCreation);

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListTaskByIdAsync_WhenTaskDoesNotExist_ThrowsTaskNotFound()
    {
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.ListTaskByIdAsync(TaskId));

        Assert.Equal("TASK_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task ListTaskByIdAsync_WhenTaskBelongsToAnotherUser_ThrowsAccessDenied()
    {
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(CreateTask(OtherUserId, SourceId, StatusId)));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.ListTaskByIdAsync(TaskId));

        Assert.Equal("TASK_ACCESS_DENIED", exception.Code);
    }

    [Fact]
    public async Task ListTasksByUserAsync_ForwardsFiltersAndMapsPagedItems()
    {
        var task = CreateTask(UserId, SourceId, StatusId);

        _tasksRepository.GetPageByUserIdAsync(
                UserId, 2, 5, StatusId, "relatório", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(IReadOnlyList<Tasks> Items, int TotalCount)>(([task], 11)));

        var result = await _sut.ListTasksByUserAsync(2, 5, StatusId, "relatório");

        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(11, result.TotalItems);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPrevious);
        Assert.True(result.HasNext);

        var mapped = Assert.Single(result.Items);

        Assert.Equal(task.UserId, mapped.UserId);
        Assert.Equal(task.Title, mapped.Title);
        Assert.Equal(task.Description, mapped.Description);

        await _tasksRepository.Received(1).GetPageByUserIdAsync(UserId, 2, 5, StatusId, "relatório", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListTasksByUserAsync_NormalizesPageAndPageSizeAndIgnoresNonPositiveStatus()
    {
        _tasksRepository.GetPageByUserIdAsync(
                UserId, 1, 1, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(IReadOnlyList<Tasks> Items, int TotalCount)>((Array.Empty<Tasks>(), 0)));

        var result = await _sut.ListTasksByUserAsync(0, 0, 0, null);

        Assert.Equal(1, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPrevious);
        Assert.False(result.HasNext);

        await _tasksRepository.Received(1).GetPageByUserIdAsync(UserId, 1, 1, null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListTasksByUserAsync_ClampsPageSizeToMaximum()
    {
        _tasksRepository.GetPageByUserIdAsync(
                UserId, 1, 50, null, "query", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(IReadOnlyList<Tasks> Items, int TotalCount)>((Array.Empty<Tasks>(), 0)));

        var result = await _sut.ListTasksByUserAsync(-3, 100, -1, "query");

        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);

        await _tasksRepository.Received(1).GetPageByUserIdAsync(UserId, 1, 50, null, "query", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WhenTaskAndStatusBelongToUser_UpdatesAndSaves()
    {
        var task = CreateTask(UserId, SourceId, StatusId);
        const int newStatusId = StatusId + 1;

        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(task));

        _statusRepository.GetByIdAsync(newStatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus(UserId)));

        await _sut.UpdateTaskStatusAsync(TaskId, newStatusId);

        Assert.Equal(newStatusId, task.TaskStatusId);

        _tasksRepository.Received(1).Update(task);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WhenTaskDoesNotExist_ThrowsWithoutSaving()
    {
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateTaskStatusAsync(TaskId, StatusId));

        Assert.Equal("TASK_NOT_FOUND", exception.Code);

        await _statusRepository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WhenTaskBelongsToAnotherUser_ThrowsWithoutSaving()
    {
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(CreateTask(OtherUserId, SourceId, StatusId)));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateTaskStatusAsync(TaskId, StatusId));

        Assert.Equal("TASK_ACCESS_DENIED", exception.Code);

        await _statusRepository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WhenStatusDoesNotExist_ThrowsWithoutSaving()
    {
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(CreateTask(UserId, SourceId, StatusId)));
        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateTaskStatusAsync(TaskId, StatusId));

        Assert.Equal("TASK_STATUS_NOT_FOUND", exception.Code);

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WhenStatusBelongsToAnotherUser_ThrowsWithoutSaving()
    {
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(CreateTask(UserId, SourceId, StatusId)));
        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus(OtherUserId)));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _sut.UpdateTaskStatusAsync(TaskId, StatusId));

        Assert.Equal("TASK_STATUS_ACCESS_DENIED", exception.Code);

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskSortOrderAsync_WhenTaskBelongsToUser_UpdatesAndSaves()
    {
        var task = CreateTask(UserId, SourceId, StatusId);
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(task));

        await _sut.UpdateTaskSortOrderAsync(TaskId, 8);

        Assert.Equal(8, task.SortOrder);

        _tasksRepository.Received(1).Update(task);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskSortOrderAsync_WhenTaskDoesNotExist_ThrowsWithoutSaving()
    {
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateTaskSortOrderAsync(TaskId, 8));

        Assert.Equal("TASK_NOT_FOUND", exception.Code);

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskSortOrderAsync_WhenTaskBelongsToAnotherUser_ThrowsWithoutSaving()
    {
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(CreateTask(OtherUserId, SourceId, StatusId)));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateTaskSortOrderAsync(TaskId, 8));

        Assert.Equal("TASK_ACCESS_DENIED", exception.Code);

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenTaskBelongsToUser_CompletesAndSaves()
    {
        var task = CreateTask(UserId, SourceId, StatusId);

        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(task));

        await _sut.CompleteTaskAsync(TaskId);

        Assert.NotNull(task.CompletedAt);

        _tasksRepository.Received(1).Update(task);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenTaskDoesNotExist_ThrowsWithoutSaving()
    {
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.CompleteTaskAsync(TaskId));

        Assert.Equal("TASK_NOT_FOUND", exception.Code);

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteTaskAsync_WhenTaskBelongsToAnotherUser_ThrowsWithoutSaving()
    {
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(CreateTask(OtherUserId, SourceId, StatusId)));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.CompleteTaskAsync(TaskId));

        Assert.Equal("TASK_ACCESS_DENIED", exception.Code);

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReopenTaskAsync_WhenTaskBelongsToUser_ReopensAndSaves()
    {
        var task = CreateTask(UserId, SourceId, StatusId);
        task.Complete();

        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(task));

        await _sut.ReopenTaskAsync(TaskId);

        Assert.Null(task.CompletedAt);

        _tasksRepository.Received(1).Update(task);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReopenTaskAsync_WhenTaskDoesNotExist_ThrowsWithoutSaving()
    {
        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.ReopenTaskAsync(TaskId));

        Assert.Equal("TASK_NOT_FOUND", exception.Code);

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReopenTaskAsync_WhenTaskBelongsToAnotherUser_ThrowsWithoutSaving()
    {
        var task = CreateTask(OtherUserId, SourceId, StatusId);

        task.Complete();

        _tasksRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tasks?>(task));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.ReopenTaskAsync(TaskId));

        Assert.Equal("TASK_ACCESS_DENIED", exception.Code);
        Assert.NotNull(task.CompletedAt);

        _tasksRepository.DidNotReceive().Update(Arg.Any<Tasks>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static CreateTaskDTO CreateValidDto() => new()
    {
        SourceId = SourceId,
        TaskStatusId = StatusId,
        Title = "Tarefa de teste",
        Priority = 1,
        SortOrder = 0
    };

    private static UpdateTaskDTO CreateUpdateDto() => new()
    {
        Id = TaskId,
        SourceId = SourceId,
        TaskStatusId = StatusId,
        Title = "Tarefa de teste atualizada",
        Description = "Descrição da tarefa de teste atualizada",
        Priority = 2,
        SortOrder = 1,
        DueDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc)
    };

    private static TasksStatus CreateStatus(int userId) =>
        TasksStatus.Create(
            name: "Por fazer",
            color: "#465FFF",
            sortOrder: 0,
            userId: userId,
            utCreation: userId);

    private static Source CreateSource(int userId) =>
        Source.Create(
            userId: userId,
            name: "Loja de teste",
            imageUrl: null,
            link: null,
            isActive: true,
            utCreation: userId);

    private static Tasks CreateTask(int userId, int sourceId, int statusId) =>
        Tasks.Create(
            userId: userId,
            sourceId: sourceId,
            taskStatusId: statusId,
            title: "Tarefa de teste",
            description: "Descrição da tarefa de teste",
            priority: 1,
            sortOrder: 0,
            dueDate: null,
            utCreation: userId);
}
