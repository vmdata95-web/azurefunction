using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("NEWID()");

            builder.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(100);

            builder.Property(x => x.Email)
                .HasColumnName("email")
                .HasMaxLength(150)
                .IsRequired();

            builder.HasIndex(x => x.Email).IsUnique();

            //builder.Property(x => x.PasswordHash)
            //    .HasColumnName("password_hash");

            builder.Property(x => x.Role)
                .HasColumnName("role")
                .HasMaxLength(50);

            builder.Property(x => x.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETDATE()");

            builder.Property(x => x.SessionId)
                .HasColumnName("session_id");

            builder.Property(x => x.Designation)
                .HasColumnName("designation")
                .HasMaxLength(255)
                .IsUnicode(false);

            builder.Property(x => x.CompanyName)
                .HasColumnName("company_name")
                .HasMaxLength(255)
                .IsUnicode(false);

            builder.Property(x => x.Number_Of_Employees)
                .HasColumnName("Number_Of_Employees");

            builder.Property(x => x.MobileNo)
                .HasColumnName("mobile_no")
                .HasMaxLength(20)
                .IsUnicode(false);

            builder.Property(x => x.Country)
                .HasColumnName("country")
                .HasMaxLength(100)
                .IsUnicode(false);

            builder.Property(x => x.Registerfrom)
                .HasColumnName("Registerfrom")
                .HasColumnType("int");

            builder.Property(x => x.IpAddress)
                .HasColumnName("ip_address")
                .HasColumnType("varchar(max)")
                .IsUnicode(false);
        }
    }
}
