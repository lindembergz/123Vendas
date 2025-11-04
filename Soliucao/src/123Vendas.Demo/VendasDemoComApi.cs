using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Venda.Domain.Services;

namespace _123Vendas.Demo
{
    public class VendasDemoComApi : VendasDemoBase
    {
        private readonly VendaApiClient _apiClient;
        private readonly string _apiUrl = "http://localhost:5197";
        private readonly PoliticaDesconto _politicaDesconto = new();

        // Filiais fixas para o Demo (3 filiais)
        private static readonly Guid[] Filiais = new[]
        {
            Guid.Parse("11111111-1111-1111-1111-111111111111"), // Filial Centro
            Guid.Parse("22222222-2222-2222-2222-222222222222"), // Filial Norte
            Guid.Parse("33333333-3333-3333-3333-333333333333")  // Filial Sul
        };

        // Clientes fixos para o Demo (5 clientes)
        private static readonly Guid[] Clientes = new[]
        {
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), // Cliente A
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), // Cliente B
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), // Cliente C
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), // Cliente D
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")  // Cliente E
        };

        private static readonly Random _random = new Random();

        public VendasDemoComApi()
        {
            _apiClient = new VendaApiClient(_apiUrl);
        }

        public async Task ExecutarAsync()
        {
            if (!await VerificarApiAsync()) return;

            ConsoleUIHelper.MostrarBannerInicial();

            while (true)
            {
                MostrarMenu();

                var opcao = Console.ReadLine();
                Console.Clear();
                ConsoleUIHelper.MostrarBannerInicial();

                switch (opcao)
                {
                    case "1": await CriarVendaAsync(); break;
                    case "2": await ListarVendasAsync(); break;
                    case "3": await ConsultarVendaAsync(); break;
                    case "4": await AtualizarVendaAsync(); break;
                    case "5": await CancelarVendaAsync(); break;
                    case "0": ConsoleUIHelper.MostrarMensagemSaida(); return;
                    default: ConsoleUIHelper.MostrarMensagemErro("Opção inválida!"); break;
                }

                ConsoleUIHelper.Pausar();
            }
        }

        #region Menus e UI

        private static void MostrarMenu()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("═══════════════════ MENU API REST ═══════════════════\n");
            Console.ResetColor();

            Console.WriteLine("  1️⃣  Criar Nova Venda");
            Console.WriteLine("  2️⃣  Listar Vendas");
            Console.WriteLine("  3️⃣  Consultar Venda");
            Console.WriteLine("  4️⃣  Atualizar Venda");
            Console.WriteLine("  5️⃣  Cancelar Venda");
            Console.WriteLine("  0️⃣  Sair\n");
            Console.Write("👉 Escolha uma opção: ");
        }

        private async Task<bool> VerificarApiAsync()
        {
            Console.WriteLine("🔍 Verificando conexão com a API...\n");

            if (await _apiClient.VerificarApiAsync())
            {
                ConsoleUIHelper.MostrarMensagemSucesso($"API online em {_apiUrl}");
                return true;
            }

            ConsoleUIHelper.MostrarMensagemErro($"API não acessível em {_apiUrl}");
            Console.WriteLine("Execute: cd src/123Vendas.Api && dotnet run\n");
            ConsoleUIHelper.Pausar("Pressione qualquer tecla para sair...");
            return false;
        }

        #endregion

        #region CRUD Operações

        private async Task CriarVendaAsync()
        {
            // Selecionar filial e cliente aleatoriamente do conjunto fixo
            var filialId = Filiais[_random.Next(Filiais.Length)];
            var clienteId = Clientes[_random.Next(Clientes.Length)];
            var itens = new List<ItemVendaDto>();
            var produtosAdicionados = new Dictionary<string, (Guid ProdutoId, decimal ValorUnitario)>();

            ConsoleUIHelper.MostrarMensagemSucesso("Iniciando criação de nova venda...");
            
            Console.Write("Filial:  ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{filialId.ToString().Substring(0, 8)} ({GetNomeFilial(filialId)})");
            Console.ResetColor();
            
            Console.Write("Cliente: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{clienteId.ToString().Substring(0, 8)} ({GetNomeCliente(clienteId)})");
            Console.ResetColor();
            Console.WriteLine();

            while (true)
            {
                Console.Write("📦 Nome do Produto, Enter p/ finalizar ou [V] p/ Voltar: ");
                var nome = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(nome)) break;
                
                if (nome.Trim().Equals("V", StringComparison.OrdinalIgnoreCase))
                {
                    ConsoleUIHelper.MostrarMensagemAviso("Criação de venda cancelada. Voltando ao menu principal.");
                    return;
                }

                Guid produtoId;
                decimal valor;

                // Verificar se o produto já foi adicionado
                if (produtosAdicionados.ContainsKey(nome.ToLower()))
                {
                    var produtoExistente = produtosAdicionados[nome.ToLower()];
                    produtoId = produtoExistente.ProdutoId;
                    valor = produtoExistente.ValorUnitario;
                    
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"   ℹ️  Produto já adicionado (R$ {valor:N2})");
                    Console.ResetColor();
                }
                else
                {
                    // Produto novo - solicitar valor unitário
                    Console.Write("💰 Valor Unitário (R$): ");
                    if (!decimal.TryParse(Console.ReadLine(), out valor) || valor <= 0)
                    {
                        ConsoleUIHelper.MostrarMensagemErro("Valor inválido!");
                        continue;
                    }

                    produtoId = Guid.NewGuid();
                    produtosAdicionados[nome.ToLower()] = (produtoId, valor);
                }

                int qtd;
                while (true)
                {
                    Console.Write("🔢 Quantidade: ");
                    if (!int.TryParse(Console.ReadLine(), out qtd) || qtd <= 0)
                    {
                        ConsoleUIHelper.MostrarMensagemErro("Quantidade inválida!");
                        continue;
                    }

                    // Validar regra de negócio: máximo 20 unidades
                    if (qtd > 20)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("   ❌ Não é permitido vender mais de 20 unidades do mesmo produto.");
                        Console.ResetColor();
                        Console.WriteLine("   💡 Digite uma quantidade entre 1 e 20.\n");
                        continue;
                    }

                    break; // Quantidade válida
                }

                var desconto = CalcularDesconto(qtd);
                var total = CalcularTotalLiquido(qtd, valor, desconto);

                itens.Add(new ItemVendaDto(produtoId, qtd, valor, desconto, total));

                ConsoleUIHelper.MostrarMensagemSucesso($"Item '{nome}' adicionado com {desconto:P0} de desconto.");
            }

            if (!itens.Any())
            {
                ConsoleUIHelper.MostrarMensagemAviso("Nenhum item adicionado. Venda não criada.");
                return;
            }

            var request = new CriarVendaRequest(clienteId, filialId, itens);
            var vendaId = await _apiClient.CriarVendaAsync(request);

            if (vendaId.HasValue)
            {
                ConsoleUIHelper.MostrarMensagemSucesso($"Venda criada com ID {vendaId}");
                var venda = await _apiClient.ObterVendaAsync(vendaId.Value);
                if (venda != null) MostrarDetalheVenda(venda);
            }
        }

        private async Task ListarVendasAsync()
        {
            ConsoleUIHelper.MostrarMensagemSucesso("Listando vendas...\n");
            var resultado = await _apiClient.ListarVendasAsync();

            if (resultado?.Items == null || !resultado.Items.Any())
            {
                ConsoleUIHelper.MostrarMensagemAviso("Nenhuma venda encontrada.");
                return;
            }

            Console.WriteLine("┌────────────┬───────┬────────────┬────────────┬──────────────┬──────────────┐");
            Console.WriteLine("│ Filial     │ Venda │ Cliente    │ Status     │ Data         │ Total        │");
            Console.WriteLine("├────────────┼───────┼────────────┼────────────┼──────────────┼──────────────┤");

            foreach (var venda in resultado.Items)
            {
                var clienteId = venda.ClienteId.ToString().Substring(0, 8);
                var filialId = venda.FilialId.ToString().Substring(0, 8);
                var dataFormatada = venda.Data.ToString("dd/MM HH:mm");
                
                // Filial (cyan)
                Console.Write($"│ ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{filialId,-10}");
                Console.ResetColor();
                
                // Venda
                Console.Write($" │ #{venda.Numero,-4} │ ");
                
                // Cliente (cyan)
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{clienteId,-10}");
                Console.ResetColor();
                
                // Status (colorido)
                Console.Write($" │ ");
                if (venda.Status.Equals("Ativa", StringComparison.OrdinalIgnoreCase))
                    Console.ForegroundColor = ConsoleColor.Green;
                else if (venda.Status.Equals("Cancelada", StringComparison.OrdinalIgnoreCase))
                    Console.ForegroundColor = ConsoleColor.Red;
                else
                    Console.ForegroundColor = ConsoleColor.Yellow;
                
                Console.Write($"{venda.Status,-10}");
                Console.ResetColor();
                
                // Data e Total
                Console.WriteLine($" │ {dataFormatada,-12} │ R$ {venda.ValorTotal,8:N2} │");
            }

            Console.WriteLine("└───────┴────────────┴────────────┴────────────┴──────────────┴──────────────┘");
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n📊 Total: {resultado.TotalCount} vendas | Página {resultado.PageNumber}/{resultado.TotalPages}");
            Console.ResetColor();
        }

        private async Task ConsultarVendaAsync()
        {
            Console.Write("Digite o ID da venda: ");
            if (!Guid.TryParse(Console.ReadLine(), out var id))
            {
                ConsoleUIHelper.MostrarMensagemErro("ID inválido!");
                return;
            }

            var venda = await _apiClient.ObterVendaAsync(id);
            if (venda != null)
                MostrarDetalheVenda(venda);
            else
                ConsoleUIHelper.MostrarMensagemErro("Venda não encontrada!");
        }

        private async Task AtualizarVendaAsync()
        {
            Console.Write("Digite o ID da venda para atualizar: ");
            if (!Guid.TryParse(Console.ReadLine(), out var id))
            {
                ConsoleUIHelper.MostrarMensagemErro("ID inválido!");
                return;
            }

            var vendaAtual = await _apiClient.ObterVendaAsync(id);
            if (vendaAtual == null)
            {
                ConsoleUIHelper.MostrarMensagemErro("Venda não encontrada!");
                return;
            }

            if (vendaAtual.Status.Equals("Cancelada", StringComparison.OrdinalIgnoreCase))
            {
                ConsoleUIHelper.MostrarMensagemAviso("Venda já cancelada. Não pode ser atualizada.");
                return;
            }

            var itens = vendaAtual.Itens.ToList();
            ConsoleUIHelper.MostrarMensagemSucesso("Venda carregada. Você pode adicionar ou remover itens.");

            bool salvar = false;
            while (true)
            {
                Console.WriteLine("\n[A] Adicionar Item   [R] Remover Item   [Q] Remover Quantidade   [S] Salvar   [V] Voltar");
                Console.Write("👉 Opção: ");
                var opcao = Console.ReadLine()?.ToUpper();

                if (opcao == "S")
                {
                    salvar = true;
                    break;
                }
                if (opcao == "V")
                {
                    ConsoleUIHelper.MostrarMensagemAviso("Alterações descartadas. Voltando ao menu principal.");
                    return;
                }
                if (opcao == "A") AdicionarItem(itens);
                else if (opcao == "R") RemoverItem(itens);
                else if (opcao == "Q") RemoverQuantidadeItem(itens);
            }

            if (salvar)
            {
                var request = new AtualizarVendaRequest(itens);
                var venda = await _apiClient.AtualizarVendaAsync(id, request);
                if (venda != null)
                {
                    ConsoleUIHelper.MostrarMensagemSucesso("Venda atualizada com sucesso!");
                    MostrarDetalheVenda(venda);
                }
            }
        }

        private async Task CancelarVendaAsync()
        {
            Console.Write("Digite o ID da venda para cancelar: ");
            if (!Guid.TryParse(Console.ReadLine(), out var id))
            {
                ConsoleUIHelper.MostrarMensagemErro("ID inválido!");
                return;
            }

            var venda = await _apiClient.ObterVendaAsync(id);
            if (venda == null)
            {
                ConsoleUIHelper.MostrarMensagemErro("Venda não encontrada!");
                return;
            }

            if (venda.Status.Equals("Cancelada", StringComparison.OrdinalIgnoreCase))
            {
                ConsoleUIHelper.MostrarMensagemAviso("Venda já está cancelada.");
                return;
            }

            Console.Write("Confirmar cancelamento (S/N)? ");
            if (Console.ReadLine()?.ToUpper() != "S") return;

            if (await _apiClient.CancelarVendaAsync(id))
                ConsoleUIHelper.MostrarMensagemSucesso("Venda cancelada com sucesso!");
            else
                ConsoleUIHelper.MostrarMensagemErro("Falha ao cancelar venda!");
        }

        #endregion

        #region Helpers

        private static decimal CalcularDesconto(int qtd) =>
            new PoliticaDesconto().Calcular(qtd);

        private static decimal CalcularTotalLiquido(int qtd, decimal valor, decimal desconto) =>
            qtd * valor * (1 - desconto);

        private void AdicionarItem(List<ItemVendaDto> itens)
        {
            Console.Write("� VNome do Produto: ");
            var nome = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(nome))
            {
                ConsoleUIHelper.MostrarMensagemErro("Nome do produto é obrigatório!");
                return;
            }

            // Verificar se o produto já existe na venda (pelo valor unitário)
            // Como não temos o nome armazenado, vamos perguntar se é um produto existente
            Guid produtoId;
            decimal valor;

            // Verificar se já existe um item com o mesmo ProdutoId (simulação)
            var produtoExistente = itens.FirstOrDefault(i => 
                // Aqui seria ideal comparar por nome, mas como não temos, 
                // vamos sempre solicitar o valor para novos itens
                false);

            if (produtoExistente != null)
            {
                produtoId = produtoExistente.ProdutoId;
                valor = produtoExistente.ValorUnitario;
                
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"   ℹ️  Produto já existe na venda (R$ {valor:N2})");
                Console.ResetColor();
            }
            else
            {
                Console.Write("💰 Valor Unitário (R$): ");
                if (!decimal.TryParse(Console.ReadLine(), out valor) || valor <= 0)
                {
                    ConsoleUIHelper.MostrarMensagemErro("Valor inválido!");
                    return;
                }
                produtoId = Guid.NewGuid();
            }

            Console.Write("🔢 Quantidade: ");
            if (!int.TryParse(Console.ReadLine(), out var qtd) || qtd <= 0)
            {
                ConsoleUIHelper.MostrarMensagemErro("Quantidade inválida!");
                return;
            }

            var desconto = CalcularDesconto(qtd);
            var total = CalcularTotalLiquido(qtd, valor, desconto);
            itens.Add(new ItemVendaDto(produtoId, qtd, valor, desconto, total));

            ConsoleUIHelper.MostrarMensagemSucesso($"Item '{nome}' adicionado ({qtd}x R$ {valor:N2}) com {desconto:P0} de desconto.");
        }

        private void RemoverItem(List<ItemVendaDto> itens)
        {
            if (!itens.Any())
            {
                ConsoleUIHelper.MostrarMensagemAviso("Nenhum item para remover.");
                return;
            }

            for (int i = 0; i < itens.Count; i++)
                Console.WriteLine($"{i + 1}. {itens[i].Quantidade}x R$ {itens[i].ValorUnitario:N2} = R$ {itens[i].Total:N2}");

            Console.Write("Número do item a remover: ");
            if (!int.TryParse(Console.ReadLine(), out var idx) || idx < 1 || idx > itens.Count)
            {
                ConsoleUIHelper.MostrarMensagemErro("Número inválido!");
                return;
            }

            itens.RemoveAt(idx - 1);
            ConsoleUIHelper.MostrarMensagemSucesso("Item removido completamente!");
        }

        private void RemoverQuantidadeItem(List<ItemVendaDto> itens)
        {
            if (!itens.Any())
            {
                ConsoleUIHelper.MostrarMensagemAviso("Nenhum item na venda.");
                return;
            }

            Console.WriteLine("\nItens da venda:");
            for (int i = 0; i < itens.Count; i++)
            {
                var item = itens[i];
                Console.WriteLine($"  {i + 1}. {item.Quantidade}x R$ {item.ValorUnitario:N2} → R$ {item.Total:N2}");
            }

            Console.Write("\nNúmero do item: ");
            if (!int.TryParse(Console.ReadLine(), out var idx) || idx < 1 || idx > itens.Count)
            {
                ConsoleUIHelper.MostrarMensagemErro("Número inválido!");
                return;
            }

            var itemSelecionado = itens[idx - 1];
            
            Console.Write($"\n🔢 Quantidade a remover (disponível: {itemSelecionado.Quantidade}): ");
            if (!int.TryParse(Console.ReadLine(), out var quantidade) || quantidade <= 0)
            {
                ConsoleUIHelper.MostrarMensagemErro("Quantidade inválida!");
                return;
            }

            if (quantidade > itemSelecionado.Quantidade)
            {
                ConsoleUIHelper.MostrarMensagemErro($"Quantidade a remover ({quantidade}) é maior que a disponível ({itemSelecionado.Quantidade})!");
                return;
            }

            var novaQuantidade = itemSelecionado.Quantidade - quantidade;

            if (novaQuantidade == 0)
            {
                // Remove o item completamente
                itens.RemoveAt(idx - 1);
                ConsoleUIHelper.MostrarMensagemSucesso("Item removido completamente da venda!");
            }
            else
            {
                // Atualiza a quantidade e recalcula desconto e total
                var novoDesconto = CalcularDesconto(novaQuantidade);
                var novoTotal = CalcularTotalLiquido(novaQuantidade, itemSelecionado.ValorUnitario, novoDesconto);
                
                itens[idx - 1] = new ItemVendaDto(
                    itemSelecionado.ProdutoId,
                    novaQuantidade,
                    itemSelecionado.ValorUnitario,
                    novoDesconto,
                    novoTotal
                );
                
                ConsoleUIHelper.MostrarMensagemSucesso($"{quantidade} unidade(s) removida(s). Restam {novaQuantidade} unidade(s). [S] Para salvar!");
            }
        }

        private void MostrarDetalheVenda(VendaResponse venda)
        {
            Console.WriteLine("\n╔═══════════════════ DETALHE DA VENDA ═══════════════════╗");
            Console.WriteLine($"║  ID: {venda.Id,-52} ║");
            Console.WriteLine($"║  Nº: {venda.Numero,-52} ║");
            Console.WriteLine($"║  Status: {venda.Status,-48} ║");
            Console.WriteLine($"║  Data: {venda.Data:dd/MM/yyyy HH:mm,-50} ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════╣");

            foreach (var item in venda.Itens)
            {
                var descInfo = item.Desconto > 0 ? $" ({item.Desconto:P0})" : "";
                Console.WriteLine($"║  • {item.Quantidade}x R$ {item.ValorUnitario:N2}{descInfo,-10} = R$ {item.Total,8:N2} ║");
            }

            Console.WriteLine("╠════════════════════════════════════════════════════════╣");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"║  TOTAL: R$ {venda.ValorTotal,44:N2} ║");
            Console.ResetColor();
            Console.WriteLine("╚════════════════════════════════════════════════════════╝");
        }

        #endregion

        #region Métodos Helper

        private static string GetNomeFilial(Guid filialId)
        {
            if (filialId == Filiais[0]) return "Centro";
            if (filialId == Filiais[1]) return "Norte";
            if (filialId == Filiais[2]) return "Sul";
            return "Desconhecida";
        }

        private static string GetNomeCliente(Guid clienteId)
        {
            if (clienteId == Clientes[0]) return "Cliente A";
            if (clienteId == Clientes[1]) return "Cliente B";
            if (clienteId == Clientes[2]) return "Cliente C";
            if (clienteId == Clientes[3]) return "Cliente D";
            if (clienteId == Clientes[4]) return "Cliente E";
            return "Desconhecido";
        }

        #endregion
    }
}
