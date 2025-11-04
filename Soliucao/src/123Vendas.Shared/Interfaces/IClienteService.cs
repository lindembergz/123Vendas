namespace _123Vendas.Shared.Interfaces;

///<summary>
//Serviço para integração com o módulo de CRM para validação de clientes.
// 
//NOTA ARQUITETURAL:
//Esta interface está no Shared por simplicidade e para evitar acoplamento direto
//entre módulos (Venda não referencia CRM diretamente).
// 
//Em um cenário de produção com microserviços, cada módulo exporia sua própria
//interface e a comunicação seria via HTTP/gRPC ou mensageria.
//
//Para um monolito modular como este, um teste, ambas abordagens são válidas:
//- Shared: Simplicidade, contratos compartilhados
//- Por módulo: Maior autonomia, melhor para evolução independente
///</summary>
public interface IClienteService
{
    Task<bool> ClienteExisteAsync(Guid clienteId, CancellationToken ct = default);
}
