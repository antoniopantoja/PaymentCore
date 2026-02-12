using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentCore.Domain.Entities;
using PaymentCore.Domain.Enums;

namespace PaymentCore.Infrastructure.Configuration;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ReferenceId)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(t => t.ReferenceId)
            .IsUnique();

        builder.Property(t => t.OperationType)
            .HasConversion(
                v => v.ToString(),
                v => (OperationType)Enum.Parse(typeof(OperationType), v))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.AccountId)
            .IsRequired();

        builder.Property(t => t.TargetAccountId)
            .IsRequired(false);

        builder.Property(t => t.OriginalTransactionId)
            .IsRequired(false);

        builder.Property(t => t.Metadata)
            .HasMaxLength(2000);

        builder.Property(t => t.Timestamp)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion(
                v => v.ToString(),
                v => (TransactionStatus)Enum.Parse(typeof(TransactionStatus), v))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.HasOne(t => t.Account)
            .WithMany()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.TargetAccount)
            .WithMany()
            .HasForeignKey(t => t.TargetAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(t => t.AccountId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Timestamp);
        builder.HasIndex(t => t.OriginalTransactionId);
    }
}
