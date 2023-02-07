using MeDirectCurrencyExchange.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeDirectCurrencyExchange.Api.Data.Configurations;

public class RateConfiguration : IEntityTypeConfiguration<Rate>
{
    public void Configure(EntityTypeBuilder<Rate> entity)
    {
        entity.ToTable("currency_rate");

        entity.HasKey(o => o.Id);

        entity.Property(o => o.Id).HasColumnName("id")
            .UseIdentityAlwaysColumn();

        entity.Property(o => o.DateCreated).HasColumnName("date_created")
            .IsRequired();

        entity.Property(o => o.ClientId).HasColumnName("client_id")
            .IsRequired();

        entity.Property(o => o.RateProvider).HasColumnName("rate_provider")
            .IsRequired()
            .HasMaxLength(25);

        entity.Property(o => o.BaseCurrency).HasColumnName("base_currency")
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength(true);

        entity.Property(o => o.TargetCurrency).HasColumnName("target_currency")
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength(true);

        entity.Property(o => o.Value).HasColumnName("value")
            .IsRequired();

        entity.Property(o => o.RateTimestamp).HasColumnName("rate_timestamp")
            .IsRequired();
    }
}
