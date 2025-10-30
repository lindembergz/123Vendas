using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Venda.Domain.Aggregates;
using Venda.Domain.Enums;
using Venda.Domain.ValueObjects;

namespace Venda.Infrastructure.Configurations;

public class VendaConfiguration : IEntityTypeConfiguration<VendaAgregado>
{
    public void Configure(EntityTypeBuilder<VendaAgregado> builder)
    {
        builder.ToTable("Vendas");
        
        // Chave primária
        builder.HasKey(v => v.Id);
        
        // Propriedades
        builder.Property(v => v.Id)
            .IsRequired()
            .ValueGeneratedNever(); // Guid gerado pela aplicação
        
        builder.Property(v => v.NumeroVenda)
            .IsRequired();
        
        builder.Property(v => v.Data)
            .IsRequired();
        
        builder.Property(v => v.ClienteId)
            .IsRequired();
        
        builder.Property(v => v.Filial)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(v => v.Status)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<StatusVenda>(v))
            .HasMaxLength(50);
        
        // Propriedade calculada (não mapeada)
        builder.Ignore(v => v.ValorTotal);
        
        // Configurar owned entity para ItemVenda
        builder.OwnsMany(v => v.Produtos, produtos =>
        {
            produtos.ToTable("ItensVenda");
            
            // Chave primária da tabela de itens
            produtos.WithOwner().HasForeignKey("VendaId");
            produtos.Property<int>("Id").ValueGeneratedOnAdd();
            produtos.HasKey("Id");
            
            // Propriedades do ItemVenda
            produtos.Property(i => i.ProdutoId)
                .IsRequired()
                .HasColumnName("ProdutoId");
            
            produtos.Property(i => i.Quantidade)
                .IsRequired()
                .HasColumnName("Quantidade");
            
            produtos.Property(i => i.ValorUnitario)
                .IsRequired()
                .HasPrecision(18, 2)
                .HasColumnName("ValorUnitario");
            
            produtos.Property(i => i.Desconto)
                .IsRequired()
                .HasPrecision(5, 2)
                .HasColumnName("Desconto");
            
            // Ignorar propriedade calculada
            produtos.Ignore(i => i.Total);
        });
        
        // Índices para performance
        builder.HasIndex(v => v.ClienteId)
            .HasDatabaseName("IX_Vendas_ClienteId");
        
        builder.HasIndex(v => v.Data)
            .HasDatabaseName("IX_Vendas_Data");
        
        builder.HasIndex(v => v.Status)
            .HasDatabaseName("IX_Vendas_Status");
        
        // Índice composto para queries comuns
        builder.HasIndex(v => new { v.ClienteId, v.Data })
            .HasDatabaseName("IX_Vendas_ClienteId_Data");
    }
}
