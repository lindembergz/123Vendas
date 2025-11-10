using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Venda.Infrastructure.Entities;

namespace Venda.Infrastructure.Configurations;

public class NumeroVendaSequenceConfiguration : IEntityTypeConfiguration<NumeroVendaSequence>
{
    public void Configure(EntityTypeBuilder<NumeroVendaSequence> builder)
    {
        builder.ToTable("NumeroVendaSequences");
        
        // Chave primária
        builder.HasKey(s => s.FilialId);
        
        // Propriedades
        builder.Property(s => s.FilialId)
            .IsRequired();
        
        builder.Property(s => s.UltimoNumero)
            .IsRequired()
            .HasDefaultValue(0);
        
        // Versão para concorrência otimista (compatível com SQLite)
        builder.Property(s => s.Versao)
            .IsRequired()
            .HasDefaultValue(0)
            .IsConcurrencyToken();
    }
}
