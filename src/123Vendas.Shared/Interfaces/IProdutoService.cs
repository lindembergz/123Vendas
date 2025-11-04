namespace _123Vendas.Shared.Interfaces;

/// <summary>
/// Serviço para integração com o módulo de Estoque para reserva de produtos
//NOTA ARQUITETURAL:
//Esta interface está no Shared por simplicidade e para evitar acoplamento direto
//entre módulos (Venda não referencia Estoque diretamente).
// 
//Em um cenário de produção com microserviços, cada módulo exporia sua própria
//interface e a comunicação seria via HTTP/gRPC ou mensageria.
//
//Para um monolito modular como este, ambas abordagens são válidas:
//- Shared: Simplicidade, contratos compartilhados
//- Por módulo: Maior autonomia, melhor para evolução independente
///</summary>
/// </summary>
public interface IProdutoService
{
    Task<bool> ReservarEstoqueAsync(Guid produtoId, int quantidade, CancellationToken ct = default);
}
