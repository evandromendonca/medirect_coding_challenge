using MeDirectCurrencyExchange.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeDirectCurrencyExchange.Api.Data.Configurations;

public class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> entity)
    {
        entity.ToTable("currency_trade");

        entity.HasKey(o => o.Id);

        entity.Property(o => o.Id).HasColumnName("id")
            .UseIdentityAlwaysColumn();

        entity.Property(o => o.DateCreated).HasColumnName("date_created")
            .IsRequired();

        entity.Property(o => o.ClientId).HasColumnName("client_id")
            .IsRequired();

        entity.Property(o => o.BaseCurrency).HasColumnName("base_currency")
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        entity.Property(o => o.TargetCurrency).HasColumnName("target_currency")
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        entity.Property(o => o.Rate).HasColumnName("rate")
            .IsRequired();

        entity.Property(o => o.BaseCurrencyAmount).HasColumnName("base_currency_amount")
            .IsRequired();

        entity.Property(o => o.Fees).HasColumnName("fees")
            .IsRequired()
            .HasDefaultValue(0);

        entity.Property(o => o.TargetCurrencyAmount).HasColumnName("target_currency_amount")
            .IsRequired();

        entity.Property(o => o.RateId).HasColumnName("rate_id")
            .IsRequired();
    }
}
