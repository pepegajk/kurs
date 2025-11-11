using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace kursecondapi.Models;

public partial class CarPlatformContext : DbContext
{
    public CarPlatformContext()
    {
    }

    public CarPlatformContext(DbContextOptions<CarPlatformContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Car> Cars { get; set; }

    public virtual DbSet<CarImage> CarImages { get; set; }

    public virtual DbSet<Deal> Deals { get; set; }

    public virtual DbSet<Favorite> Favorites { get; set; }

    public virtual DbSet<Model> Models { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<UserSetting> UserSettings { get; set; }

    public virtual DbSet<VwActiveCar> VwActiveCars { get; set; }

    public virtual DbSet<VwCarsFull> VwCarsFulls { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=car_platform;Username=postgres;Password=1");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("AspNetRoles_pkey");

            entity.HasIndex(e => e.Name, "AspNetRoles_Name_key").IsUnique();

            entity.HasIndex(e => e.NormalizedName, "AspNetRoles_NormalizedName_key").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasMaxLength(256);
        });

        modelBuilder.Entity<AspNetRoleClaim>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("AspNetRoleClaims_pkey");

            entity.HasOne(d => d.Role).WithMany(p => p.AspNetRoleClaims)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("AspNetRoleClaims_RoleId_fkey");
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("AspNetUsers_pkey");

            entity.HasIndex(e => e.Email, "AspNetUsers_Email_key").IsUnique();

            entity.HasIndex(e => e.NormalizedEmail, "AspNetUsers_NormalizedEmail_key").IsUnique();

            entity.HasIndex(e => e.NormalizedUserName, "AspNetUsers_NormalizedUserName_key").IsUnique();

            entity.HasIndex(e => e.UserName, "AspNetUsers_UserName_key").IsUnique();

            entity.Property(e => e.AccessFailedCount).HasDefaultValue(0);
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.EmailConfirmed).HasDefaultValue(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastLoginAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.LockoutEnabled).HasDefaultValue(true);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.PhoneNumberConfirmed).HasDefaultValue(false);
            entity.Property(e => e.TwoFactorEnabled).HasDefaultValue(false);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRoles",
                    r => r.HasOne<AspNetRole>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("AspNetUserRoles_RoleId_fkey"),
                    l => l.HasOne<AspNetUser>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("AspNetUserRoles_UserId_fkey"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("AspNetUserRoles_pkey");
                        j.ToTable("AspNetUserRoles");
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("AspNetUserClaims_pkey");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("AspNetUserClaims_UserId_fkey");
        });

        modelBuilder.Entity<AspNetUserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey }).HasName("AspNetUserLogins_pkey");

            entity.Property(e => e.LoginProvider).HasMaxLength(450);
            entity.Property(e => e.ProviderKey).HasMaxLength(450);

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserLogins)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("AspNetUserLogins_UserId_fkey");
        });

        modelBuilder.Entity<AspNetUserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name }).HasName("AspNetUserTokens_pkey");

            entity.Property(e => e.LoginProvider).HasMaxLength(450);
            entity.Property(e => e.Name).HasMaxLength(450);

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("AspNetUserTokens_UserId_fkey");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("AuditLogs_pkey");

            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.RecordId).HasMaxLength(50);
            entity.Property(e => e.TableName).HasMaxLength(100);
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.UserName).HasMaxLength(256);
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Brands_pkey");

            entity.HasIndex(e => e.Name, "Brands_Name_key").IsUnique();

            entity.Property(e => e.Country).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LogoUrl).HasMaxLength(200);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Car>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Cars_pkey");

            entity.HasIndex(e => e.Vin, "Cars_VIN_key").IsUnique();

            entity.HasIndex(e => e.ModelId, "IX_Cars_ModelId");

            entity.HasIndex(e => e.Price, "IX_Cars_Price");

            entity.HasIndex(e => e.SellerId, "IX_Cars_SellerId");

            entity.HasIndex(e => e.Status, "IX_Cars_Status");

            entity.HasIndex(e => e.Year, "IX_Cars_Year");

            entity.Property(e => e.BodyType).HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(50);
            entity.Property(e => e.Condition)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Used'::character varying");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DriveType).HasMaxLength(50);
            entity.Property(e => e.EngineVolume).HasPrecision(3, 1);
            entity.Property(e => e.FuelType).HasMaxLength(50);
            entity.Property(e => e.IsFeatured).HasDefaultValue(false);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.RegistrationNumber).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Active'::character varying");
            entity.Property(e => e.Transmission).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.ViewsCount).HasDefaultValue(0);
            entity.Property(e => e.Vin)
                .HasMaxLength(20)
                .HasColumnName("VIN");

            entity.HasOne(d => d.Model).WithMany(p => p.Cars)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Cars_ModelId_fkey");

            entity.HasOne(d => d.Seller).WithMany(p => p.Cars)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Cars_SellerId_fkey");
        });

        modelBuilder.Entity<CarImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CarImages_pkey");

            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsMain).HasDefaultValue(false);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Car).WithMany(p => p.CarImages)
                .HasForeignKey(d => d.CarId)
                .HasConstraintName("CarImages_CarId_fkey");
        });

        modelBuilder.Entity<Deal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Deals_pkey");

            entity.HasIndex(e => e.CarId, "IX_Deals_CarId");

            entity.HasIndex(e => e.Status, "IX_Deals_Status");

            entity.Property(e => e.CommissionAmount).HasPrecision(18, 2);
            entity.Property(e => e.CommissionPercent).HasPrecision(5, 2);
            entity.Property(e => e.CompletedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Pending'::character varying");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.DealApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("Deals_ApprovedBy_fkey");

            entity.HasOne(d => d.Buyer).WithMany(p => p.DealBuyers)
                .HasForeignKey(d => d.BuyerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Deals_BuyerId_fkey");

            entity.HasOne(d => d.Car).WithMany(p => p.Deals)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Deals_CarId_fkey");

            entity.HasOne(d => d.Seller).WithMany(p => p.DealSellers)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Deals_SellerId_fkey");
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Favorites_pkey");

            entity.HasIndex(e => new { e.UserId, e.CarId }, "Favorites_UserId_CarId_key").IsUnique();

            entity.HasIndex(e => e.UserId, "IX_Favorites_UserId");

            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(d => d.Car).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.CarId)
                .HasConstraintName("Favorites_CarId_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("Favorites_UserId_fkey");
        });

        modelBuilder.Entity<Model>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Models_pkey");

            entity.HasIndex(e => new { e.BrandId, e.Name }, "UQ_Models_BrandName").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Brand).WithMany(p => p.Models)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Models_Brands");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Reviews_pkey");

            entity.HasIndex(e => e.DealId, "IX_Reviews_DealId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.IsApproved).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Author).WithMany(p => p.ReviewAuthors)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Reviews_AuthorId_fkey");

            entity.HasOne(d => d.Deal).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.DealId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Reviews_DealId_fkey");

            entity.HasOne(d => d.ModeratedByNavigation).WithMany(p => p.ReviewModeratedByNavigations)
                .HasForeignKey(d => d.ModeratedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("Reviews_ModeratedBy_fkey");
        });

        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("UserSettings_pkey");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .HasDefaultValueSql("'RUB'::character varying");
            entity.Property(e => e.DateFormat)
                .HasMaxLength(20)
                .HasDefaultValueSql("'dd.MM.yyyy'::character varying");
            entity.Property(e => e.EmailNotifications).HasDefaultValue(true);
            entity.Property(e => e.FavoriteLocations).HasMaxLength(500);
            entity.Property(e => e.Language)
                .HasMaxLength(10)
                .HasDefaultValueSql("'ru'::character varying");
            entity.Property(e => e.PageSize).HasDefaultValue(20);
            entity.Property(e => e.PushNotifications).HasDefaultValue(false);
            entity.Property(e => e.Theme)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Light'::character varying");
            entity.Property(e => e.TimeFormat)
                .HasMaxLength(20)
                .HasDefaultValueSql("'HH:mm'::character varying");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.User).WithOne(p => p.UserSetting)
                .HasForeignKey<UserSetting>(d => d.UserId)
                .HasConstraintName("UserSettings_UserId_fkey");
        });

        modelBuilder.Entity<VwActiveCar>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_active_cars");

            entity.Property(e => e.BodyType).HasMaxLength(50);
            entity.Property(e => e.BrandName).HasMaxLength(100);
            entity.Property(e => e.Color).HasMaxLength(50);
            entity.Property(e => e.Condition).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.DriveType).HasMaxLength(50);
            entity.Property(e => e.EngineVolume).HasPrecision(3, 1);
            entity.Property(e => e.FuelType).HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.SellerEmail).HasMaxLength(256);
            entity.Property(e => e.SellerFirstName).HasMaxLength(100);
            entity.Property(e => e.SellerLastName).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Transmission).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.Vin)
                .HasMaxLength(20)
                .HasColumnName("VIN");
        });

        modelBuilder.Entity<VwCarsFull>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_cars_full");

            entity.Property(e => e.BodyType).HasMaxLength(50);
            entity.Property(e => e.BrandName).HasMaxLength(100);
            entity.Property(e => e.Color).HasMaxLength(50);
            entity.Property(e => e.Condition).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.DriveType).HasMaxLength(50);
            entity.Property(e => e.EngineVolume).HasPrecision(3, 1);
            entity.Property(e => e.FuelType).HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.SellerEmail).HasMaxLength(256);
            entity.Property(e => e.SellerFirstName).HasMaxLength(100);
            entity.Property(e => e.SellerLastName).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Transmission).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.Vin)
                .HasMaxLength(20)
                .HasColumnName("VIN");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
