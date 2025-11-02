using Venda.Domain.Exceptions;
using Venda.Domain.Interfaces;

namespace Venda.Domain.Services;

public class PoliticaDesconto : IPoliticaDesconto
{
    public decimal Calcular(int quantidadeTotal)
    {
        return quantidadeTotal switch
        {
            < 4 => 0m,
            >= 4 and < 10 => 0.10m,
            >= 10 and <= 20 => 0.20m,
            > 20 => throw new DomainException("Não é permitido vender mais de 20 unidades do mesmo produto.")
        };
    }

    public bool PermiteVenda(int quantidadeTotal)
        => quantidadeTotal <= 20;
}
