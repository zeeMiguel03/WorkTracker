using Application.DTOs.TaskStatus;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using NSubstitute;

namespace Application.Tests.Services;

public class TasksStatusServiceTests
{
    private const int UserId = 42;
    private const int OtherUserId = 99;
    private const int StatusId = 10;

    private readonly ITasksStatusRepository _statusRepository = Substitute.For<ITasksStatusRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TasksStatusService _sut;

    public TasksStatusServiceTests()
    {
        _currentUserService.GetUserId().Returns(UserId);
        _sut = new TasksStatusService(_statusRepository, _currentUserService, _unitOfWork);
    }

    [Fact]
    public async Task CreateTaskStatusAsync_WhenNameIsAvailable_CreatesAndSavesStatus()
    {
        var dto = new CreateTaskStatusDTO { Name = "  Em progresso  ", Color = " #abc123 ", SortOrder = 2 };

        _statusRepository.GetByNameAsync(UserId, dto.Name, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(null));

        var result = await _sut.CreateTaskStatusAsync(dto);

        Assert.Equal(UserId, result.UserId);
        Assert.Equal("Em progresso", result.Name);
        Assert.Equal("#ABC123", result.Color);
        Assert.Equal(2, result.SortOrder);
        Assert.Equal(UserId, result.UtCreation);

        await _statusRepository.Received(1).GetByNameAsync(UserId, dto.Name, Arg.Any<CancellationToken>());
        await _statusRepository.Received(1).AddAsync(
            Arg.Is<TasksStatus>(status =>
                status.UserId == UserId && status.Name == "Em progresso" && status.Color == "#ABC123" && status.SortOrder == 2),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTaskStatusAsync_WhenNameAlreadyExists_ThrowsAndDoesNotSave()
    {
        var dto = CreateCreateDto();
        _statusRepository.GetByNameAsync(UserId, dto.Name, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus("A fazer", UserId, StatusId + 1)));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.CreateTaskStatusAsync(dto));

        Assert.Equal("TASK_STATUS_NAME_ALREADY_EXISTS", exception.Code);

        await _statusRepository.DidNotReceive().AddAsync(Arg.Any<TasksStatus>(), Arg.Any<CancellationToken>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTaskStatusAsync_WhenStatusDataIsInvalid_ThrowsAndDoesNotSave()
    {
        var dto = new CreateTaskStatusDTO { Name = "A fazer", Color = "red", SortOrder = 0 };

        _statusRepository.GetByNameAsync(UserId, dto.Name, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.CreateTaskStatusAsync(dto));

        Assert.Equal("INVALID_COLOR", exception.Code);

        await _statusRepository.DidNotReceive().AddAsync(Arg.Any<TasksStatus>(), Arg.Any<CancellationToken>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WhenStatusBelongsToUser_UpdatesAndSaves()
    {
        var status = CreateStatus("A fazer", UserId, StatusId);
        var dto = CreateUpdateDto();

        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(status));
        _statusRepository.GetByNameAsync(UserId, dto.Name, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(status));

        await _sut.UpdateTaskStatusAsync(dto);

        Assert.Equal("Em progresso", status.Name);
        Assert.Equal("#00FF00", status.Color);
        Assert.Equal(3, status.SortOrder);

        _statusRepository.Received(1).Update(status);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WhenNameBelongsToAnotherStatus_ThrowsAndDoesNotSave()
    {
        var status = CreateStatus("A fazer", UserId, StatusId);

        var dto = CreateUpdateDto();

        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(status));
        _statusRepository.GetByNameAsync(UserId, dto.Name, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus(dto.Name, UserId, StatusId + 1)));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateTaskStatusAsync(dto));

        Assert.Equal("TASK_STATUS_NAME_ALREADY_EXISTS", exception.Code);
        Assert.Equal("A fazer", status.Name);

        _statusRepository.DidNotReceive().Update(Arg.Any<TasksStatus>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WhenStatusDoesNotExist_ThrowsWithoutCheckingNameOrSaving()
    {
        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateTaskStatusAsync(CreateUpdateDto()));

        Assert.Equal("TASK_STATUS_NOT_FOUND", exception.Code);

        await _statusRepository.DidNotReceive().GetByNameAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        _statusRepository.DidNotReceive().Update(Arg.Any<TasksStatus>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WhenStatusBelongsToAnotherUser_ThrowsWithoutCheckingNameOrSaving()
    {
        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus("A fazer", OtherUserId, StatusId)));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateTaskStatusAsync(CreateUpdateDto()));

        Assert.Equal("TASK_STATUS_ACCESS_DENIED", exception.Code);

        await _statusRepository.DidNotReceive().GetByNameAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        _statusRepository.DidNotReceive().Update(Arg.Any<TasksStatus>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTaskStatusAsync_WhenStatusIsUnused_RemovesAndSaves()
    {
        var status = CreateStatus("A fazer", UserId, StatusId);

        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(status));
        _statusRepository.HasAssociatedTasksAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        await _sut.DeleteTaskStatusAsync(StatusId);

        _statusRepository.Received(1).Remove(status);

        await _statusRepository.Received(1).HasAssociatedTasksAsync(StatusId, Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTaskStatusAsync_WhenStatusHasAssociatedTasks_ThrowsAndDoesNotRemoveOrSave()
    {
        var status = CreateStatus("A fazer", UserId, StatusId);

        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(status));
        _statusRepository.HasAssociatedTasksAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.DeleteTaskStatusAsync(StatusId));

        Assert.Equal("TASK_STATUS_IN_USE", exception.Code);

        _statusRepository.DidNotReceive().Remove(Arg.Any<TasksStatus>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTaskStatusAsync_WhenStatusDoesNotExist_ThrowsWithoutCheckingUsageOrSaving()
    {
        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.DeleteTaskStatusAsync(StatusId));

        Assert.Equal("TASK_STATUS_NOT_FOUND", exception.Code);

        await _statusRepository.DidNotReceive().HasAssociatedTasksAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());

        _statusRepository.DidNotReceive().Remove(Arg.Any<TasksStatus>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTaskStatusAsync_WhenStatusBelongsToAnotherUser_ThrowsWithoutCheckingUsageOrSaving()
    {
        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus("A fazer", OtherUserId, StatusId)));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.DeleteTaskStatusAsync(StatusId));

        Assert.Equal("TASK_STATUS_ACCESS_DENIED", exception.Code);

        await _statusRepository.DidNotReceive().HasAssociatedTasksAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());

        _statusRepository.DidNotReceive().Remove(Arg.Any<TasksStatus>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListTaskStatusByIdAsync_WhenStatusBelongsToUser_ReturnsMappedStatus()
    {
        var status = CreateStatus("Em progresso", UserId, StatusId);

        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(status));

        var result = await _sut.ListTaskStatusByIdAsync(StatusId);

        Assert.Equal(StatusId, result.Id);
        Assert.Equal(UserId, result.UserId);
        Assert.Equal(status.Name, result.Name);
        Assert.Equal(status.Color, result.Color);
        Assert.Equal(status.SortOrder, result.SortOrder);
        Assert.Equal(status.CreatedAt, result.CreatedAt);
        Assert.Equal(status.UtCreation, result.UtCreation);
    }

    [Fact]
    public async Task ListTaskStatusByIdAsync_WhenStatusDoesNotExist_ThrowsNotFound()
    {
        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(null));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.ListTaskStatusByIdAsync(StatusId));

        Assert.Equal("TASK_STATUS_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task ListTaskStatusByIdAsync_WhenStatusBelongsToAnotherUser_ThrowsAccessDenied()
    {
        _statusRepository.GetByIdAsync(StatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TasksStatus?>(CreateStatus("A fazer", OtherUserId, StatusId)));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _sut.ListTaskStatusByIdAsync(StatusId));

        Assert.Equal("TASK_STATUS_ACCESS_DENIED", exception.Code);
    }

    [Fact]
    public async Task ListTaskStatusesByUserAsync_ReturnsMappedStatusesForCurrentUser()
    {
        var statuses = new[]
        {
            CreateStatus("A fazer", UserId, StatusId),
            CreateStatus("Concluído", UserId, StatusId + 1)
        };
        _statusRepository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TasksStatus>>(statuses));

        var result = await _sut.ListTaskStatusesByUserAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("A fazer", result[0].Name);
        Assert.Equal("Concluído", result[1].Name);
        Assert.All(result, item => Assert.Equal(UserId, item.UserId));
        Assert.Equal(statuses[0].Color, result[0].Color);
        Assert.Equal(statuses[1].SortOrder, result[1].SortOrder);

        await _statusRepository.Received(1).GetByUserIdAsync(UserId, Arg.Any<CancellationToken>());
    }

    private static CreateTaskStatusDTO CreateCreateDto() => new()
    {
        Name = "A fazer",
        Color = "#465FFF",
        SortOrder = 0
    };

    private static UpdateTaskStatusDTO CreateUpdateDto() => new()
    {
        Id = StatusId,
        Name = "Em progresso",
        Color = "#00FF00",
        SortOrder = 3
    };

    private static TasksStatus CreateStatus(string name, int userId, int id)
    {
        var status = TasksStatus.Create(name, "#465FFF", 0, userId, userId);
        typeof(TasksStatus).GetProperty(nameof(TasksStatus.Id))!.SetValue(status, id);
        return status;
    }
}
