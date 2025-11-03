namespace Venda.Domain.Interfaces;

public interface IPoliticaDesconto
{
    decimal Calcular(int quantidadeTotal);
    bool PermiteVenda(int quantidadeTotal);
}
