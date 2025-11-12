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
        
        builder.Property(v => v.FilialId)
            .IsRequired();
        
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

            // Índice em ProdutoId para otimizar queries de itens por produto
            // Uso: Buscar todos os itens vendidos de um produto específico
            // Query: SELECT * FROM ItensVenda WHERE ProdutoId = @id
            produtos.HasIndex(i => i.ProdutoId)
                .HasDatabaseName("IX_ItensVenda_ProdutoId");
            
            // Ignorar propriedade calculada
            produtos.Ignore(i => i.Total);
        });
        
        // ========================================================================
        // ÍNDICES DE PERFORMANCE - Otimizados para queries de listagem e filtro
        // ========================================================================
        
        // Índices simples para filtros básicos
        builder.HasIndex(v => v.ClienteId)
            .HasDatabaseName("IX_Vendas_ClienteId");
        
        builder.HasIndex(v => v.FilialId)
            .HasDatabaseName("IX_Vendas_FilialId");
        
        // Índice em Data com ordem descendente (queries ordenam por data DESC)
        builder.HasIndex(v => v.Data)
            .IsDescending()
            .HasDatabaseName("IX_Vendas_Data");
        
        builder.HasIndex(v => v.Status)
            .HasDatabaseName("IX_Vendas_Status");
        
        // ========================================================================
        // ÍNDICES COMPOSTOS - Otimizam queries com múltiplos filtros
        // ========================================================================
        
        // Índice composto ClienteId + Data (DESC)
        // Uso: Listar vendas de um cliente ordenadas por data
        // Query: WHERE ClienteId = @id ORDER BY Data DESC
        builder.HasIndex(v => new { v.ClienteId, v.Data })
            .IsDescending(false, true)  // ClienteId ASC, Data DESC
            .HasDatabaseName("IX_Vendas_ClienteId_Data");
        
        // Índice composto FilialId + Data (DESC)
        // Uso: Listar vendas de uma filial ordenadas por data
        // Query: WHERE FilialId = @id ORDER BY Data DESC
        builder.HasIndex(v => new { v.FilialId, v.Data })
            .IsDescending(false, true)  // FilialId ASC, Data DESC
            .HasDatabaseName("IX_Vendas_FilialId_Data");
        
        // Índice composto Status + Data (DESC)
        // Uso: Listar vendas por status ordenadas por data
        // Query: WHERE Status = @status ORDER BY Data DESC
        builder.HasIndex(v => new { v.Status, v.Data })
            .IsDescending(false, true)  // Status ASC, Data DESC
            .HasDatabaseName("IX_Vendas_Status_Data");
        
        // Índice composto FilialId + Status
        // Uso: Listar vendas de uma filial com status específico
        // Query: WHERE FilialId = @id AND Status = @status
        builder.HasIndex(v => new { v.FilialId, v.Status })
            .HasDatabaseName("IX_Vendas_FilialId_Status");
        
        // ========================================================================
        // ÍNDICE COVERING - Otimiza listagem sem JOIN
        // ========================================================================
        
        // Índice covering para queries de listagem (evita lookup na tabela principal)
        // Uso: Listagem geral com filtros e ordenação
        // Query: SELECT * FROM Vendas WHERE [filtros] ORDER BY Data DESC
        // Inclui todas as colunas frequentemente usadas em listagens
        builder.HasIndex(v => new { v.Data, v.Status, v.ClienteId, v.FilialId })
            .IsDescending(true, false, false, false)  // Data DESC, demais ASC
            .HasDatabaseName("IX_Vendas_Covering_List");
        
        // ========================================================================
        // ÍNDICE ÚNICO - Garante integridade de dados
        // ========================================================================
        
        // CRÍTICO: Índice único composto para prevenir duplicação de números por filial
        // Este índice também otimiza ObterUltimoNumeroPorFilialAsync
        builder.HasIndex(v => new { v.FilialId, v.NumeroVenda })
            .IsUnique()
            .HasDatabaseName("IX_Vendas_FilialId_NumeroVenda_Unique");
    }
}
