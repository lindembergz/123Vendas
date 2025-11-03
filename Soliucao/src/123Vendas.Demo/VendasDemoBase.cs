using System;
using System.Linq;
using System.Collections.Generic;
using _123Vendas.Shared.Common;
using Venda.Domain.Aggregates;

namespace _123Vendas.Demo
{
    public abstract class VendasDemoBase
    {
        protected void MostrarResumoVenda(VendaAgregado venda)
        {
            Console.WriteLine("\n╔════════════════════ RESUMO DA VENDA ═══════════════════╗");
            Console.WriteLine($"║  Venda: #{venda.NumeroVenda,-45} ║");
            Console.WriteLine($"║  Status: {venda.Status,-44} ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════╣");

            if (!venda.Produtos.Any())
            {
                Console.WriteLine("║  Nenhum item adicionado                                ║");
            }
            else
            {
                foreach (var item in venda.Produtos)
                {
                    var descInfo = item.Desconto > 0 ? $" (Desc: {item.Desconto:P0})" : "";
                    Console.WriteLine($"║  • {item.Quantidade}x R$ {item.ValorUnitario:N2}{descInfo,-30} = R$ {item.Total,8:N2} ║");
                }
            }

            Console.WriteLine("╠════════════════════════════════════════════════════════╣");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"║  TOTAL: R$ {venda.ValorTotal,44:N2} ║");
            Console.ResetColor();
            Console.WriteLine("╚════════════════════════════════════════════════════════╝");
        }

        protected void MostrarResultado(Result resultado, bool esperadoSucesso)
        {
            if (resultado.IsSuccess == esperadoSucesso)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"   ✅ Comportamento esperado!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"   ❌ Comportamento inesperado!");
            }

            Console.ResetColor();

            if (!resultado.IsSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"   📝 Mensagem: {resultado.Error}");
                Console.ResetColor();
            }
        }
    }
}
