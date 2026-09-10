using API.Middleware;
using API.Services;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Interfaces;
using Infrastructure.Options;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "The connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IUnitOfWork>(serviceProvider =>
    serviceProvider.GetRequiredService<AppDbContext>());

builder.Services
    .AddOptions<UploadOptions>()
    .Bind(builder.Configuration.GetSection(UploadOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ISourceService, SourceService>();
builder.Services.AddScoped<ITransactionTypeService, TransactionTypeService>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IEntryRepository, EntryRepository>();
builder.Services.AddScoped<ISourceRepository, SourceRepository>();
builder.Services.AddScoped<ITasksRepository, TasksRepository>();
builder.Services.AddScoped<ITasksStatusRepository, TasksStatusRepository>();
builder.Services.AddScoped<ITransactionTypeRepository, TransactionTypeRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
