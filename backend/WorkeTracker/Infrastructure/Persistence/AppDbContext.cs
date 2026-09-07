using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class AppDbContext : DbContext, IUnitOfWork
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public DbSet<Account> accounts { get; set; }
        public DbSet<AccountType> accountsType { get; set; }
        public DbSet<Entry> entries { get; set; }
        public DbSet<Source> sources { get; set; } 
        public DbSet<Tasks> tasks { get; set; }
        public DbSet<TasksStatus> task_status { get; set; }
        public DbSet<TransactionType> transaction_types { get; set; }
        public DbSet<User> users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>(e =>
            {
                e.HasOne(x => x.User)
                    .WithMany(x => x.Accounts)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.AccountType)
                    .WithMany(x => x.Accounts)
                    .HasForeignKey(x => x.AccountTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(x => new { x.UserId, x.CreatedAt });
            });

            modelBuilder.Entity<AccountType>(e =>
            {
                e.HasIndex(x => x.Name)
                    .IsUnique();
            });

            modelBuilder.Entity<Entry>(e =>
            {
                e.HasOne(x => x.Account)
                    .WithMany(x => x.Entries)
                    .HasForeignKey(x => x.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Source)
                    .WithMany(x => x.Entries)
                    .HasForeignKey(x => x.SourceId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.TransactionType)
                    .WithMany(x => x.Entries)
                    .HasForeignKey(x => x.TransactionTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(x => new { x.AccountId, x.Date });

                e.HasIndex(x => new { x.AccountId, x.TransactionTypeId });

                e.HasIndex(x => new { x.AccountId, x.SourceId });
            });

            modelBuilder.Entity<Source>(e =>
            {
                e.HasOne(x => x.User)
                    .WithMany(x => x.Sources)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.UserId, x.IsActive });

                e.HasIndex(x => new { x.UserId, x.Name });
            });

            modelBuilder.Entity<Tasks>(e =>
            {
                e.HasOne(x => x.User)
                    .WithMany(x => x.Tasks)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.TaskStatus)
                    .WithMany(x => x.Tasks)
                    .HasForeignKey(x => x.TaskStatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Source)
                    .WithMany(x => x.Tasks)
                    .HasForeignKey(x => x.SourceId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(x => new { x.UserId, x.TaskStatusId });

                e.HasIndex(x => new { x.UserId, x.DueDate });

                e.HasIndex(x => new { x.UserId, x.CompletedAt });

                e.HasIndex(x => new { x.TaskStatusId, x.SortOrder });
            });

            modelBuilder.Entity<TasksStatus>(e =>
            {
                e.HasOne(x => x.User)
                    .WithMany(x => x.TasksStatus)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.UserId, x.SortOrder });

                e.HasIndex(x => new { x.UserId, x.Name })
                    .IsUnique();
            });

            modelBuilder.Entity<TransactionType>(e =>
            {
                e.HasIndex(x => x.Name)
                    .IsUnique();
            });

            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(x => x.Email)
                    .IsUnique();
            });
        }

    }
}
