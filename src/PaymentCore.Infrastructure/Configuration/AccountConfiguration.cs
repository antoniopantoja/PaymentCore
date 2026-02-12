using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentCore.Domain.Entities;
using PaymentCore.Domain.Enums;

namespace PaymentCore.Infrastructure.Configuration;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ExternalId)
            .HasMaxLength(100);

        builder.Property(a => a.Balance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.ReservedBalance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.CreditLimit)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion(
                v => v.ToString(),
                v => (AccountStatus)Enum.Parse(typeof(AccountStatus), v))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.RowVersion)
            .IsRowVersion();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.ExternalId)
            .IsUnique()
            .HasFilter("[ExternalId] IS NOT NULL");
    }
}
