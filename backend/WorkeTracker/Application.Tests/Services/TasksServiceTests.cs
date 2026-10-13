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

    private static CreateTaskDTO CreateValidDto() => new()
    {
        SourceId = SourceId,
        TaskStatusId = StatusId,
        Title = "Tarefa de teste",
        Priority = 1,
        SortOrder = 0
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