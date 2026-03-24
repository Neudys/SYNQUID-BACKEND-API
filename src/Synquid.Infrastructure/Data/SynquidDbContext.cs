using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Synquid.Domain.Entities;

namespace Synquid.Infrastructure.Data;

public class SynquidDbContext : DbContext
{
    public SynquidDbContext(DbContextOptions<SynquidDbContext> options) : base(options) { }

    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<NfcCard> NfcCards => Set<NfcCard>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Institution
        modelBuilder.Entity<Institution>(e =>
        {
            e.ToTable("institutions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ContactEmail).HasMaxLength(256).IsRequired();
            e.Property(x => x.Timezone).HasMaxLength(50).HasDefaultValue("Europe/Madrid");
        });

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.GoogleId).IsUnique();
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Language).HasMaxLength(5).HasDefaultValue("es");

            e.HasOne(x => x.Institution)
             .WithMany(x => x.Users)
             .HasForeignKey(x => x.InstitutionId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Group
        modelBuilder.Entity<Group>(e =>
        {
            e.ToTable("groups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();

            e.HasOne(x => x.Institution)
             .WithMany(x => x.Groups)
             .HasForeignKey(x => x.InstitutionId);

            e.HasOne(x => x.Professor)
             .WithMany()
             .HasForeignKey(x => x.ProfessorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // GroupMember
        modelBuilder.Entity<GroupMember>(e =>
        {
            e.ToTable("group_members");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => new { x.GroupId, x.UserId }).IsUnique();

            e.HasOne(x => x.Group).WithMany(x => x.Members).HasForeignKey(x => x.GroupId);
            e.HasOne(x => x.User).WithMany(x => x.GroupMemberships).HasForeignKey(x => x.UserId);
        });

        // Schedule
        modelBuilder.Entity<Schedule>(e =>
        {
            e.ToTable("schedules");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.Group).WithMany(x => x.Schedules).HasForeignKey(x => x.GroupId);
        });

        // Device
        modelBuilder.Entity<Device>(e =>
        {
            e.ToTable("devices");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ApiKeyHash).HasMaxLength(256).IsRequired();

            e.HasOne(x => x.Institution)
             .WithMany(x => x.Devices)
             .HasForeignKey(x => x.InstitutionId);
        });

        // NfcCard
        modelBuilder.Entity<NfcCard>(e =>
        {
            e.ToTable("nfc_cards");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.HashUid).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.HashUid).IsUnique();
            e.Property(x => x.Salt).HasMaxLength(64).IsRequired();

            e.HasOne(x => x.User).WithMany(x => x.NfcCards).HasForeignKey(x => x.UserId);
        });

        // AttendanceRecord
        modelBuilder.Entity<AttendanceRecord>(e =>
        {
            e.ToTable("attendance_records");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => new { x.UserId, x.TimestampUtc });
            e.HasIndex(x => x.DeviceId);

            e.HasOne(x => x.User)
             .WithMany(x => x.AttendanceRecords)
             .HasForeignKey(x => x.UserId);

            e.HasOne(x => x.Device)
             .WithMany(x => x.AttendanceRecords)
             .HasForeignKey(x => x.DeviceId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.RegisteredBy)
             .WithMany()
             .HasForeignKey(x => x.RegisteredById)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
