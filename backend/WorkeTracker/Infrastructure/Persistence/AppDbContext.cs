using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class AppDbContext : DbContext, IUnitOfWork
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public DbSet<Product> products { get; set; }
        public DbSet<ProductImage> product_images { get; set; }
        public DbSet<PurchaseOrder> purchase_orders { get; set; }
        public DbSet<RefreshToken> refresh_tokens { get; set; }
        public DbSet<Source> sources { get; set; } 
        public DbSet<Tasks> tasks { get; set; }
        public DbSet<TasksStatus> task_status { get; set; }
        public DbSet<User> users { get; set; }

        public DbSet<ProductSale> product_sales { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PurchaseOrder>(e =>
            {
                e.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                e.HasOne(x => x.User)
                    .WithMany(x => x.PurchaseOrders)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Source)
                    .WithMany()
                    .HasForeignKey(x => x.SourceId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt, x.Id });
                e.HasIndex(x => new { x.UserId, x.CreatedAt, x.Id });
            });

            modelBuilder.Entity<Product>(e =>
            {
                e.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                e.Property(x => x.Condition)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                e.Property(x => x.RowVersion).IsRowVersion();

                e.HasOne(x => x.User)
                    .WithMany(x => x.Products)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.PurchaseOrder)
                    .WithMany(x => x.Products)
                    .HasForeignKey(x => x.PurchaseOrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.SaleSource)
                    .WithMany()
                    .HasForeignKey(x => x.SaleSourceId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(x => new { x.UserId, x.Status });
                e.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt, x.Id });
                e.HasIndex(x => new { x.UserId, x.CreatedAt, x.Id });
                e.HasIndex(x => x.PurchaseOrderId);
                e.HasIndex(x => x.SaleSourceId);
            });

            modelBuilder.Entity<ProductImage>(e =>
            {
                e.HasOne(x => x.Product)
                    .WithMany(x => x.Images)
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.ProductId, x.DisplayOrder })
                    .IsUnique();

                e.HasIndex(x => x.ProductId)
                    .HasDatabaseName("UX_product_images_cover")
                    .HasFilter("[is_cover] = 1")
                    .IsUnique();
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

            modelBuilder.Entity<RefreshToken>(e =>
            {
                e.Property(x => x.RowVersion)
                    .IsRowVersion()
                    .IsConcurrencyToken();

                e.HasOne(x => x.User)
                    .WithMany(x => x.RefreshTokens)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => x.TokenHash)
                    .IsUnique();

                e.HasIndex(x => new { x.UserId, x.ExpiresAt });
            });

            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(x => x.Email)
                    .IsUnique();
            });

            modelBuilder.Entity<ProductSale>(e =>
            {
                e.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne<Product>()
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => x.ProductId)
                    .IsUnique()
                    .HasFilter("[product_id] IS NOT NULL");

                e.HasIndex(x => new { x.UserId, x.SaleDate, x.Id });
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new ConcurrencyException("The entity was modified by another request.", exception);
            }
        }

    }
}
