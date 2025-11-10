namespace Venda.Infrastructure.Entities;

/// <summary>
/// Entidade para controle de sequência de números de venda por filial.
/// Garante geração atômica e thread-safe de números sequenciais.
/// </summary>
public class NumeroVendaSequence
{
    public Guid FilialId { get; set; }
    public int UltimoNumero { get; set; }
    
    /// <summary>
    /// Versão para controle de concorrência otimista.
    /// Previne condições de corrida em ambientes multi-thread.
    /// Incrementado automaticamente a cada atualização.
    /// </summary>
    public int Versao { get; set; }
}
