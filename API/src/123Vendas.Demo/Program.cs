using _123Vendas.Demo;
using Venda.Domain.Aggregates;
using Venda.Domain.Services;
using Venda.Domain.ValueObjects;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.Clear();

// Menu de seleção de modo
MostrarBannerInicial();
Console.WriteLine("Escolha o modo de demonstração:\n");
Console.WriteLine("  1️⃣  Demo LOCAL (Simulação em memória - sem API)");
Console.WriteLine("  2️⃣  Demo com API REST (Integração completa)");
Console.WriteLine("  0️⃣  Sair\n");
Console.Write("👉 Opção: ");

var opcao = Console.ReadLine();
Console.Clear();

switch (opcao)
{
    case "1":
        var demoLocal = new VendasDemo();
        demoLocal.Executar();
        break;
    case "2":
        var demoApi = new VendasDemoComApi();
        await demoApi.ExecutarAsync();
        break;
    case "0":
        Console.WriteLine("\n👋 Até logo!\n");
        break;
    default:
        Console.WriteLine("\n❌ Opção inválida!\n");
        break;
}

static void MostrarBannerInicial()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                                                            ║");
    Console.WriteLine("║              🛒  SISTEMA 123VENDAS - DEMO  🛒              ║");
    Console.WriteLine("║                                                            ║");
    Console.WriteLine("║          Demonstração Interativa de Regras de Negócio     ║");
    Console.WriteLine("║                                                            ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine();
}


public class VendasDemo
{
    private readonly PoliticaDesconto _politicaDesconto = new();
    
    public void Executar()
    {
        MostrarBanner();
        
        while (true)
        {
            MostrarMenu();
            var opcao = Console.ReadLine();
            
            Console.Clear();
            MostrarBanner();
            
            switch (opcao)
            {
                case "1":
                    DemonstrarRegraDesconto();
                    break;
                case "2":
                    SimularVendaInterativa();
                    break;
                case "3":
                    TestarCenariosSucesso();
                    break;
                case "4":
                    TestarCenariosErro();
                    break;
                case "5":
                    DemonstrarEventos();
                    break;
                case "0":
                    Console.Clear();
                    Console.WriteLine("\n👋 Obrigado por testar o sistema 123Vendas!\n");
                    return;
                default:
                    Console.WriteLine("\n❌ Opção inválida!\n");
                    break;
            }
            
            Console.WriteLine("\n\nPressione qualquer tecla para continuar...");
            Console.ReadKey();
            Console.Clear();
        }
    }
    
    private void MostrarBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                                            ║");
        Console.WriteLine("║              🛒  SISTEMA 123VENDAS - DEMO  🛒              ║");
        Console.WriteLine("║                                                            ║");
        Console.WriteLine("║          Demonstração Interativa de Regras de Negócio     ║");
        Console.WriteLine("║                                                            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }
    
    private void MostrarMenu()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("═══════════════════ MENU PRINCIPAL ═══════════════════\n");
        Console.ResetColor();
        
        Console.WriteLine("  1️⃣  Demonstrar Regras de Desconto");
        Console.WriteLine("  2️⃣  Simulação Interativa de Venda");
        Console.WriteLine("  3️⃣  Testar Cenários de Sucesso");
        Console.WriteLine("  4️⃣  Testar Cenários de Erro");
        Console.WriteLine("  5️⃣  Demonstrar Eventos de Domínio");
        Console.WriteLine("  0️⃣  Sair");
        
        Console.WriteLine("\n═════════════════════════════════════════════════════");
        Console.Write("\n👉 Escolha uma opção: ");
    }

    
    private void DemonstrarRegraDesconto()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           📊 REGRAS DE DESCONTO - 123VENDAS 📊            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();
        
        Console.WriteLine("As regras de desconto são aplicadas por PRODUTO baseadas na quantidade:\n");
        
        var cenarios = new[]
        {
            (Qtd: 1, Desc: "0%", Cor: ConsoleColor.White),
            (Qtd: 3, Desc: "0%", Cor: ConsoleColor.White),
            (Qtd: 4, Desc: "10%", Cor: ConsoleColor.Yellow),
            (Qtd: 9, Desc: "10%", Cor: ConsoleColor.Yellow),
            (Qtd: 10, Desc: "20%", Cor: ConsoleColor.Green),
            (Qtd: 20, Desc: "20%", Cor: ConsoleColor.Green),
        };
        
        Console.WriteLine("┌──────────────┬─────────────┬──────────────────────────────┐");
        Console.WriteLine("│  Quantidade  │  Desconto   │         Exemplo              │");
        Console.WriteLine("├──────────────┼─────────────┼──────────────────────────────┤");
        
        foreach (var (qtd, desc, cor) in cenarios)
        {
            var valorUnit = 100m;
            var desconto = _politicaDesconto.Calcular(qtd);
            var total = qtd * valorUnit * (1 - desconto);
            
            Console.Write("│ ");
            Console.ForegroundColor = cor;
            Console.Write($"{qtd,12}");
            Console.ResetColor();
            Console.Write(" │ ");
            Console.ForegroundColor = cor;
            Console.Write($"{desc,11}");
            Console.ResetColor();
            Console.WriteLine($" │ R$ {total,8:N2} ({qtd}x R$100,00) │");
        }
        
        Console.WriteLine("└──────────────┴─────────────┴──────────────────────────────┘\n");
        
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("⚠️  LIMITE MÁXIMO: 20 unidades do mesmo produto");
        Console.WriteLine("    Tentativas acima de 20 unidades serão REJEITADAS!");
        Console.ResetColor();
        
        Console.WriteLine("\n\n💡 Exemplo Prático:");
        Console.WriteLine("   • Produto A: 5 unidades  → Desconto de 10%");
        Console.WriteLine("   • Produto B: 12 unidades → Desconto de 20%");
        Console.WriteLine("   • Produto C: 2 unidades  → Sem desconto");
    }

    
    private void SimularVendaInterativa()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║          🛍️  SIMULAÇÃO INTERATIVA DE VENDA  🛍️            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        venda.DefinirNumeroVenda(1001);
        
        // Dicionário para mapear nome do produto -> ProdutoId (para consolidação)
        var produtosPorNome = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        // Dicionário para mapear nome do produto -> Valor Unitário
        var valoresPorNome = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        
        Console.WriteLine($"✅ Venda #{venda.NumeroVenda} criada com sucesso!\n");
        
        while (true)
        {
            Console.WriteLine("\n┌─────────────────────────────────────────────────────┐");
            Console.WriteLine("│  [A] Adicionar Item    [R] Remover Item    [S] Sair │");
            Console.WriteLine("└─────────────────────────────────────────────────────┘");
            Console.Write("\n👉 Opção: ");
            
            var opcao = Console.ReadLine()?.ToUpper();
            
            if (opcao == "S") break;
            
            if (opcao == "A")
            {
                Console.Write("\n📦 Nome do Produto: ");
                var nomeProduto = Console.ReadLine() ?? "Produto";
                
                // Verifica se o produto já existe
                bool produtoExistente = produtosPorNome.ContainsKey(nomeProduto);
                decimal valor;
                
                if (produtoExistente)
                {
                    // Produto já existe - reutiliza o valor unitário
                    valor = valoresPorNome[nomeProduto];
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"ℹ️  Produto já existe. Valor unitário: R$ {valor:N2}");
                    Console.ResetColor();
                }
                else
                {
                    // Produto novo - solicita o valor unitário
                    Console.Write("💰 Valor Unitário (R$): ");
                    if (!decimal.TryParse(Console.ReadLine(), out valor) || valor <= 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Valor inválido!");
                        Console.ResetColor();
                        continue;
                    }
                }
                
                Console.Write("🔢 Quantidade: ");
                if (!int.TryParse(Console.ReadLine(), out var quantidade) || quantidade <= 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Quantidade inválida!");
                    Console.ResetColor();
                    continue;
                }
                
                // Reutiliza o mesmo ProdutoId se o produto já existe (para consolidação)
                if (!produtosPorNome.TryGetValue(nomeProduto, out var produtoId))
                {
                    produtoId = Guid.NewGuid();
                    produtosPorNome[nomeProduto] = produtoId;
                    valoresPorNome[nomeProduto] = valor;
                }
                
                var item = new ItemVenda(produtoId, quantidade, valor);
                var resultado = venda.AdicionarItem(item);
                
                if (resultado.IsSuccess)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n✅ Item '{nomeProduto}' adicionado com sucesso!");
                    Console.ResetColor();
                    
                    var itemAdicionado = venda.Produtos.First(p => p.ProdutoId == produtoId);
                    if (itemAdicionado.Desconto > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"🎉 Desconto aplicado: {itemAdicionado.Desconto:P0}");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n❌ Erro: {resultado.Error}");
                    Console.ResetColor();
                }
            }
            else if (opcao == "R" && venda.Produtos.Any())
            {
                Console.WriteLine("\n📋 Produtos na venda:");
                var produtos = venda.Produtos.ToList();
                for (int i = 0; i < produtos.Count; i++)
                {
                    var p = produtos[i];
                    Console.WriteLine($"  {i + 1}. Qtd: {p.Quantidade} | Valor: R$ {p.ValorUnitario:N2} | Total: R$ {p.Total:N2}");
                }
                
                Console.Write("\n🔢 Número do item: ");
                if (int.TryParse(Console.ReadLine(), out var indice) && indice > 0 && indice <= produtos.Count)
                {
                    var produtoSelecionado = produtos[indice - 1];
                    
                    Console.Write($"🔢 Quantidade a remover (1-{produtoSelecionado.Quantidade}, Enter=Tudo): ");
                    var qtdInput = Console.ReadLine();
                    
                    _123Vendas.Shared.Common.Result resultado;
                    
                    if (string.IsNullOrWhiteSpace(qtdInput))
                    {
                        // Remove tudo
                        resultado = venda.RemoverItem(produtoSelecionado.ProdutoId);
                    }
                    else if (int.TryParse(qtdInput, out var qtdRemover) && qtdRemover > 0)
                    {
                        // Remove quantidade específica
                        resultado = venda.RemoverItem(produtoSelecionado.ProdutoId, qtdRemover);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Quantidade inválida!");
                        Console.ResetColor();
                        continue;
                    }
                    
                    if (resultado.IsSuccess)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ Item removido com sucesso!");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"❌ Erro: {resultado.Error}");
                        Console.ResetColor();
                    }
                }
            }
            
            MostrarResumoVenda(venda);
        }
    }
    
    private void MostrarResumoVenda(VendaAgregado venda)
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

    
    private void TestarCenariosSucesso()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║            ✅ CENÁRIOS DE SUCESSO - TESTES ✅             ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();
        
        var cenarios = new[]
        {
            new { Nome = "Venda com 1 item (sem desconto)", Qtd = 1, ValorUnit = 100m },
            new { Nome = "Venda com 4 itens (10% desconto)", Qtd = 4, ValorUnit = 50m },
            new { Nome = "Venda com 10 itens (20% desconto)", Qtd = 10, ValorUnit = 75m },
            new { Nome = "Venda no limite (20 itens)", Qtd = 20, ValorUnit = 30m },
        };
        
        int contador = 1;
        foreach (var cenario in cenarios)
        {
            Console.WriteLine($"📋 Cenário {contador}: {cenario.Nome}");
            Console.WriteLine("   ─────────────────────────────────────────────────");
            
            var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
            venda.DefinirNumeroVenda(2000 + contador);
            
            var item = new ItemVenda(Guid.NewGuid(), cenario.Qtd, cenario.ValorUnit);
            var resultado = venda.AdicionarItem(item);
            
            if (resultado.IsSuccess)
            {
                var itemAdicionado = venda.Produtos.First();
                var subtotal = cenario.Qtd * cenario.ValorUnit;
                var desconto = itemAdicionado.Desconto;
                var total = itemAdicionado.Total;
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"   ✅ Sucesso!");
                Console.ResetColor();
                Console.WriteLine($"   📊 Quantidade: {cenario.Qtd} unidades");
                Console.WriteLine($"   💵 Valor Unitário: R$ {cenario.ValorUnit:N2}");
                Console.WriteLine($"   📈 Subtotal: R$ {subtotal:N2}");
                
                if (desconto > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"   🎉 Desconto: {desconto:P0} (R$ {subtotal * desconto:N2})");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"   📝 Desconto: Nenhum");
                }
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"   💰 Total Final: R$ {total:N2}");
                Console.ResetColor();
            }
            
            Console.WriteLine();
            contador++;
        }
        
        Console.WriteLine("\n🎯 Teste de Múltiplos Produtos:");
        Console.WriteLine("   ─────────────────────────────────────────────────");
        
        var vendaMultipla = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        vendaMultipla.DefinirNumeroVenda(3000);
        
        vendaMultipla.AdicionarItem(new ItemVenda(Guid.NewGuid(), 5, 100m));  // 10% desconto
        vendaMultipla.AdicionarItem(new ItemVenda(Guid.NewGuid(), 12, 50m));  // 20% desconto
        vendaMultipla.AdicionarItem(new ItemVenda(Guid.NewGuid(), 2, 75m));   // Sem desconto
        
        Console.WriteLine($"   📦 Produto A: 5 unidades × R$ 100,00 = R$ {vendaMultipla.Produtos.ElementAt(0).Total:N2} (10% desc)");
        Console.WriteLine($"   📦 Produto B: 12 unidades × R$ 50,00 = R$ {vendaMultipla.Produtos.ElementAt(1).Total:N2} (20% desc)");
        Console.WriteLine($"   📦 Produto C: 2 unidades × R$ 75,00 = R$ {vendaMultipla.Produtos.ElementAt(2).Total:N2} (sem desc)");
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n   💰 TOTAL DA VENDA: R$ {vendaMultipla.ValorTotal:N2}");
        Console.ResetColor();
        
        Console.WriteLine("\n\n🎯 Teste de Remoção Parcial:");
        Console.WriteLine("   ─────────────────────────────────────────────────");
        
        var vendaRemocao = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        vendaRemocao.DefinirNumeroVenda(3001);
        
        var produtoId = Guid.NewGuid();
        vendaRemocao.AdicionarItem(new ItemVenda(produtoId, 15, 100m));  // 20% desconto
        
        Console.WriteLine($"   📦 Produto adicionado: 15 unidades × R$ 100,00");
        Console.WriteLine($"   🎉 Desconto: {vendaRemocao.Produtos.First().Desconto:P0}");
        Console.WriteLine($"   💰 Total: R$ {vendaRemocao.Produtos.First().Total:N2}");
        
        Console.WriteLine("\n   ➡️  Removendo 5 unidades...");
        vendaRemocao.RemoverItem(produtoId, 5);
        
        Console.WriteLine($"   📦 Produto atualizado: {vendaRemocao.Produtos.First().Quantidade} unidades × R$ 100,00");
        Console.WriteLine($"   🎉 Desconto recalculado: {vendaRemocao.Produtos.First().Desconto:P0}");
        Console.WriteLine($"   💰 Total atualizado: R$ {vendaRemocao.Produtos.First().Total:N2}");
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n   💡 Desconto foi recalculado automaticamente de 20% para 20%!");
        Console.ResetColor();
    }

    
    private void TestarCenariosErro()
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║             ❌ CENÁRIOS DE ERRO - VALIDAÇÕES ❌           ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        venda.DefinirNumeroVenda(4000);
        
        Console.WriteLine("📋 Teste 1: Tentativa de adicionar mais de 20 unidades");
        Console.WriteLine("   ─────────────────────────────────────────────────");
        var resultado1 = venda.AdicionarItem(new ItemVenda(Guid.NewGuid(), 21, 100m));
        MostrarResultado(resultado1, false);
        
        Console.WriteLine("\n📋 Teste 2: Quantidade inválida (zero)");
        Console.WriteLine("   ─────────────────────────────────────────────────");
        var resultado2 = venda.AdicionarItem(new ItemVenda(Guid.NewGuid(), 0, 100m));
        MostrarResultado(resultado2, false);
        
        Console.WriteLine("\n📋 Teste 3: Valor unitário inválido (negativo)");
        Console.WriteLine("   ─────────────────────────────────────────────────");
        var resultado3 = venda.AdicionarItem(new ItemVenda(Guid.NewGuid(), 5, -50m));
        MostrarResultado(resultado3, false);
        
        Console.WriteLine("\n📋 Teste 4: Valor unitário acima do limite");
        Console.WriteLine("   ─────────────────────────────────────────────────");
        var resultado4 = venda.AdicionarItem(new ItemVenda(Guid.NewGuid(), 1, 1000000m));
        MostrarResultado(resultado4, false);
        
        Console.WriteLine("\n📋 Teste 5: Adicionar item a venda cancelada");
        Console.WriteLine("   ─────────────────────────────────────────────────");
        venda.Cancelar();
        var resultado5 = venda.AdicionarItem(new ItemVenda(Guid.NewGuid(), 5, 100m));
        MostrarResultado(resultado5, false);
        
        Console.WriteLine("\n📋 Teste 6: Consolidação de itens ultrapassando limite");
        Console.WriteLine("   ─────────────────────────────────────────────────");
        var vendaNova = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        vendaNova.DefinirNumeroVenda(4001);
        
        var produtoId = Guid.NewGuid();
        vendaNova.AdicionarItem(new ItemVenda(produtoId, 15, 100m));
        Console.WriteLine("   ✅ Primeira adição: 15 unidades (sucesso)");
        
        var resultado6 = vendaNova.AdicionarItem(new ItemVenda(produtoId, 6, 100m));
        Console.WriteLine("   ❌ Segunda adição: +6 unidades (total seria 21)");
        MostrarResultado(resultado6, false);
        
        Console.WriteLine("\n\n💡 Resumo das Validações:");
        Console.WriteLine("   • Limite máximo: 20 unidades por produto");
        Console.WriteLine("   • Quantidade deve ser > 0");
        Console.WriteLine("   • Valor unitário: 0 < valor ≤ 999.999,99");
        Console.WriteLine("   • Não é possível modificar vendas canceladas");
        Console.WriteLine("   • Consolidação de itens respeita o limite");
    }
    
    private void MostrarResultado(_123Vendas.Shared.Common.Result resultado, bool esperadoSucesso)
    {
        if (resultado.IsSuccess == esperadoSucesso)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"   ✅ Comportamento esperado!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"   ❌ Comportamento inesperado!");
            Console.ResetColor();
        }
        
        if (!resultado.IsSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"   📝 Mensagem: {resultado.Error}");
            Console.ResetColor();
        }
    }

    
    private void DemonstrarEventos()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║          📡 EVENTOS DE DOMÍNIO - DEMONSTRAÇÃO 📡          ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();
        
        Console.WriteLine("Os eventos de domínio são disparados automaticamente durante");
        Console.WriteLine("as operações e são usados para comunicação entre módulos.\n");
        
        var venda = VendaAgregado.Criar(Guid.NewGuid(), Guid.NewGuid(), _politicaDesconto);
        venda.DefinirNumeroVenda(5000);
        
        Console.WriteLine("🎬 Cenário: Criação e manipulação de uma venda\n");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");
        
        // Evento 1: Criação
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("📤 Evento 1: CompraCriada");
        Console.ResetColor();
        Console.WriteLine($"   • Venda ID: {venda.Id}");
        Console.WriteLine($"   • Número: {venda.NumeroVenda}");
        Console.WriteLine($"   • Cliente ID: {venda.ClienteId}");
        Console.WriteLine($"   ➜ Módulo CRM será notificado");
        Console.WriteLine($"   ➜ Histórico do cliente será atualizado\n");
        
        // Evento 2: Adição de item
        var produtoId1 = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId1, 5, 100m));
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("📤 Evento 2: CompraAlterada");
        Console.ResetColor();
        Console.WriteLine($"   • Venda ID: {venda.Id}");
        Console.WriteLine($"   • Produto adicionado: {produtoId1}");
        Console.WriteLine($"   • Quantidade: 5 unidades");
        Console.WriteLine($"   ➜ Módulo Estoque será notificado");
        Console.WriteLine($"   ➜ Reserva de estoque será criada\n");
        
        // Evento 3: Adição de outro item
        var produtoId2 = Guid.NewGuid();
        venda.AdicionarItem(new ItemVenda(produtoId2, 12, 50m));
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("📤 Evento 3: CompraAlterada");
        Console.ResetColor();
        Console.WriteLine($"   • Venda ID: {venda.Id}");
        Console.WriteLine($"   • Produto adicionado: {produtoId2}");
        Console.WriteLine($"   • Quantidade: 12 unidades");
        Console.WriteLine($"   ➜ Módulo Estoque será notificado novamente\n");
        
        // Evento 4: Remoção de item
        venda.RemoverItem(produtoId1);
        
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("📤 Evento 4: ItemCancelado");
        Console.ResetColor();
        Console.WriteLine($"   • Venda ID: {venda.Id}");
        Console.WriteLine($"   • Produto removido: {produtoId1}");
        Console.WriteLine($"   ➜ Módulo Estoque liberará a reserva\n");
        
        // Evento 5: Cancelamento
        venda.Cancelar();
        
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine("📤 Evento 5: CompraCancelada");
        Console.ResetColor();
        Console.WriteLine($"   • Venda ID: {venda.Id}");
        Console.WriteLine($"   • Motivo: Cancelado pelo usuário");
        Console.WriteLine($"   ➜ Módulo CRM atualizará histórico");
        Console.WriteLine($"   ➜ Módulo Estoque liberará todas as reservas\n");
        
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"📊 Total de eventos gerados: {venda.DomainEvents.Count}");
        Console.ResetColor();
        
        Console.WriteLine("\n💡 Arquitetura Event-Driven:");
        Console.WriteLine("   • Desacoplamento entre módulos");
        Console.WriteLine("   • Comunicação assíncrona via Outbox Pattern");
        Console.WriteLine("   • Rastreabilidade completa de operações");
        Console.WriteLine("   • Facilita auditoria e debugging");
    }
}


public class VendasDemoComApi
{
    private readonly VendaApiClient _apiClient;
    private readonly string _apiUrl = "http://localhost:5197";

    public VendasDemoComApi()
    {
        _apiClient = new VendaApiClient(_apiUrl);
    }

    /// <summary>
    /// Calcula o desconto baseado na quantidade usando a mesma política da API.
    /// </summary>
    private static decimal CalcularDesconto(int quantidade)
    {
        var politicaDesconto = new PoliticaDesconto();
        return politicaDesconto.Calcular(quantidade);
    }

    /// <summary>
    /// Calcula o total líquido de um item (quantidade * valor * (1 - desconto))
    /// </summary>
    private static decimal CalcularTotalLiquido(int quantidade, decimal valorUnitario, decimal desconto)
    {
        return quantidade * valorUnitario * (1 - desconto);
    }

    /// <summary>
    /// Obtém a quantidade total de um produto considerando todos os itens
    /// </summary>
    private static int ObterQuantidadeTotalPorProduto(List<ItemVendaDto> itens, Guid produtoId)
    {
        return itens.Where(i => i.ProdutoId == produtoId).Sum(i => i.Quantidade);
    }

    public async Task ExecutarAsync()
    {
        // Verifica se a API está rodando
        if (!await VerificarApiAsync())
        {
            return;
        }

        MostrarBanner();

        while (true)
        {
            MostrarMenu();
            var opcao = Console.ReadLine();

            Console.Clear();
            MostrarBanner();

            switch (opcao)
            {
                case "1":
                    await CriarNovaVendaAsync();
                    break;
                case "2":
                    await ListarVendasAsync();
                    break;
                case "3":
                    await ConsultarVendaAsync();
                    break;
                case "4":
                    await AtualizarVendaAsync();
                    break;
                case "5":
                    await CancelarVendaAsync();
                    break;
                case "0":
                    Console.Clear();
                    Console.WriteLine("\n👋 Obrigado por testar o sistema 123Vendas!\n");
                    return;
                default:
                    Console.WriteLine("\n❌ Opção inválida!\n");
                    break;
            }

            Console.WriteLine("\n\nPressione qualquer tecla para continuar...");
            Console.ReadKey();
            Console.Clear();
        }
    }

    private async Task<bool> VerificarApiAsync()
    {
        Console.WriteLine("🔍 Verificando conexão com a API...\n");

        if (await _apiClient.VerificarApiAsync())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ API está rodando em {_apiUrl}");
            Console.ResetColor();
            Console.WriteLine("\nPressione qualquer tecla para continuar...");
            Console.ReadKey();
            return true;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ API não está acessível em {_apiUrl}");
        Console.ResetColor();
        Console.WriteLine("\n📝 Para iniciar a API, execute:");
        Console.WriteLine("   cd src/123Vendas.Api");
        Console.WriteLine("   dotnet run");
        Console.WriteLine("\nPressione qualquer tecla para sair...");
        Console.ReadKey();
        return false;
    }

    private void MostrarBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                                            ║");
        Console.WriteLine("║              🛒  SISTEMA 123VENDAS - DEMO  🛒              ║");
        Console.WriteLine("║                                                            ║");
        Console.WriteLine("║          Cliente Console Integrado com API REST           ║");
        Console.WriteLine("║                                                            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    private void MostrarMenu()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("═══════════════════ MENU PRINCIPAL ═══════════════════\n");
        Console.ResetColor();

        Console.WriteLine("  1️⃣  Criar Nova Venda");
        Console.WriteLine("  2️⃣  Listar Todas as Vendas");
        Console.WriteLine("  3️⃣  Consultar Venda por ID");
        Console.WriteLine("  4️⃣  Atualizar Venda");
        Console.WriteLine("  5️⃣  Cancelar Venda");
        Console.WriteLine("  0️⃣  Sair");

        Console.WriteLine("\n═════════════════════════════════════════════════════");
        Console.Write("\n👉 Escolha uma opção: ");
    }

    private async Task CriarNovaVendaAsync()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  ➕ CRIAR NOVA VENDA ➕                    ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var itens = new List<ItemVendaDto>();
        
        // Dicionários para rastrear produtos já adicionados
        var produtosPorNome = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var valoresPorNome = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("📋 Informações da Venda:");
        Console.WriteLine($"   Cliente ID: {clienteId}");
        Console.WriteLine($"   Filial ID: {filialId}\n");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("➕ Adicione os itens da venda (Enter sem digitar para finalizar)\n");
        Console.ResetColor();

        while (true)
        {
            Console.Write("📦 Nome do Produto: ");
            var nome = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(nome))
            {
                if (itens.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n📝 Finalizando entrada de itens...\n");
                    Console.ResetColor();
                }
                break;
            }

            // Verifica se o produto já existe
            bool produtoExistente = produtosPorNome.ContainsKey(nome);
            decimal valor;
            
            if (produtoExistente)
            {
                // Produto já existe - reutiliza o valor unitário
                valor = valoresPorNome[nome];
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"ℹ️  Produto já existe. Valor unitário: R$ {valor:N2}");
                Console.ResetColor();
            }
            else
            {
                // Produto novo - solicita o valor unitário
                Console.Write("💰 Valor Unitário (R$): ");
                if (!decimal.TryParse(Console.ReadLine(), out valor) || valor <= 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Valor inválido!\n");
                    Console.ResetColor();
                    continue;
                }
            }

            Console.Write("🔢 Quantidade: ");
            if (!int.TryParse(Console.ReadLine(), out var qtd) || qtd <= 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Quantidade inválida!\n");
                Console.ResetColor();
                continue;
            }

            // Reutiliza o mesmo ProdutoId se o produto já existe (para consolidação)
            if (!produtosPorNome.TryGetValue(nome, out var produtoId))
            {
                produtoId = Guid.NewGuid();
                produtosPorNome[nome] = produtoId;
                valoresPorNome[nome] = valor;
            }

            // Calcular quantidade total do produto (considerando itens já adicionados)
            var quantidadeTotalProduto = ObterQuantidadeTotalPorProduto(itens, produtoId) + qtd;
            
            // Calcular desconto baseado na quantidade total
            var desconto = CalcularDesconto(quantidadeTotalProduto);
            
            // Calcular total líquido (com desconto)
            var totalBruto = qtd * valor;
            var totalLiquido = CalcularTotalLiquido(qtd, valor, desconto);
            
            itens.Add(new ItemVendaDto(produtoId, qtd, valor, desconto, totalLiquido));
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"✅ Item '{nome}' adicionado! ");
            Console.ResetColor();
            Console.Write($"(Qtd: {qtd} x R$ {valor:N2} = R$ {totalBruto:N2}");
            
            if (desconto > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($" - {desconto:P0} desconto");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($" = R$ {totalLiquido:N2}");
                Console.ResetColor();
            }
            Console.WriteLine(")\n");
        }

        if (!itens.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  Nenhum item adicionado. Venda não criada.");
            Console.ResetColor();
            return;
        }

        // Mostrar resumo antes de enviar
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════ RESUMO DA VENDA ═══════════════════");
        Console.ResetColor();
        Console.WriteLine($"Total de itens: {itens.Count}");
        Console.WriteLine($"Valor total (sem descontos): R$ {itens.Sum(i => i.Total):N2}\n");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("🚀 Enviando venda para a API...");
        Console.ResetColor();

        var request = new CriarVendaRequest(clienteId, filialId, itens);
        var vendaId = await _apiClient.CriarVendaAsync(request);

        if (vendaId.HasValue)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ Venda PERSISTIDA com sucesso na API!");
            Console.WriteLine($"   ID: {vendaId.Value}");
            Console.ResetColor();
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n🔍 Buscando detalhes da venda criada...");
            Console.ResetColor();
            
            // Buscar a venda criada para mostrar detalhes
            var venda = await _apiClient.ObterVendaAsync(vendaId.Value);
            if (venda != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Venda recuperada da API com sucesso!\n");
                Console.ResetColor();
                
                MostrarDetalheVenda(venda);
                
                // Destacar descontos aplicados
                var itensComDesconto = venda.Itens.Where(i => i.Desconto > 0).ToList();
                if (itensComDesconto.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n🎉 DESCONTOS APLICADOS AUTOMATICAMENTE PELA API:");
                    foreach (var item in itensComDesconto)
                    {
                        Console.WriteLine($"   • {item.Quantidade} unidades → {item.Desconto:P0} de desconto");
                    }
                    Console.ResetColor();
                }
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Falha ao criar venda na API!");
            Console.ResetColor();
        }
    }

    private async Task ListarVendasAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  📋 LISTAR VENDAS 📋                       ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        var resultado = await _apiClient.ListarVendasAsync();

        if (resultado == null || !resultado.Items.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  Nenhuma venda encontrada.");
            Console.ResetColor();
            return;
        }

        foreach (var venda in resultado.Items)
        {
            Console.WriteLine($"Venda #{venda.Numero} | Status: {venda.Status} | Total: R$ {venda.ValorTotal:N2}");
        }

        Console.WriteLine($"\n📊 Total: {resultado.TotalCount} venda(s) | Página {resultado.PageNumber}/{resultado.TotalPages}");
    }

    private async Task ConsultarVendaAsync()
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  🔍 CONSULTAR VENDA 🔍                     ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        Console.Write("Digite o ID da venda: ");
        if (!Guid.TryParse(Console.ReadLine(), out var vendaId))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ ID inválido!");
            Console.ResetColor();
            return;
        }

        var venda = await _apiClient.ObterVendaAsync(vendaId);

        if (venda != null)
        {
            MostrarDetalheVenda(venda);
        }
    }

    private async Task AtualizarVendaAsync()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  ✏️  ATUALIZAR VENDA ✏️                    ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        Console.Write("Digite o ID da venda: ");
        if (!Guid.TryParse(Console.ReadLine(), out var vendaId))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ ID inválido!");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n🔍 Buscando venda na API...");
        Console.ResetColor();

        var vendaAtual = await _apiClient.ObterVendaAsync(vendaId);
        if (vendaAtual == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Venda não encontrada!");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Venda encontrada!\n");
        Console.ResetColor();
        MostrarDetalheVenda(vendaAtual);

        // Verificar se a venda está cancelada
        if (vendaAtual.Status.Equals("Cancelada", StringComparison.OrdinalIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n⚠️  ATENÇÃO: Esta venda está CANCELADA!");
            Console.WriteLine("   Não é possível atualizar uma venda cancelada.");
            Console.ResetColor();
            Console.WriteLine("\nPressione qualquer tecla para voltar ao menu...");
            Console.ReadKey();
            return;
        }

        // Copia os itens existentes para uma lista mutável
        var itens = vendaAtual.Itens.Select(i => new ItemVendaDto(
            i.ProdutoId,
            i.Quantidade,
            i.ValorUnitario,
            i.Desconto,
            i.Total
        )).ToList();

        // Dicionário para mapear ProdutoId -> Nome (para exibição)
        var nomesProdutos = new Dictionary<Guid, string>();
        int contadorProduto = 1;
        foreach (var item in itens)
        {
            nomesProdutos[item.ProdutoId] = $"Produto {contadorProduto++}";
        }

        bool modificado = false;

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    OPÇÕES DE ATUALIZAÇÃO                   ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine("1️⃣  - Adicionar novo item");
            Console.WriteLine("2️⃣  - Remover item por quantidade");
            Console.WriteLine("3️⃣  - Remover item completamente");
            Console.WriteLine("4️⃣  - Ver itens atuais");
            Console.WriteLine("5️⃣  - Finalizar e enviar para API");
            Console.WriteLine("0️⃣  - Cancelar atualização");
            Console.Write("\n👉 Escolha uma opção: ");

            var opcao = Console.ReadLine();

            switch (opcao)
            {
                case "1": // Adicionar item
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n➕ ADICIONAR NOVO ITEM\n");
                    Console.ResetColor();

                    Console.Write("📦 Nome do Produto: ");
                    var nomeProduto = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(nomeProduto))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Nome inválido!");
                        Console.ResetColor();
                        break;
                    }

                    Console.Write("💰 Valor Unitário (R$): ");
                    if (!decimal.TryParse(Console.ReadLine(), out var valorUnitario) || valorUnitario <= 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Valor inválido!");
                        Console.ResetColor();
                        break;
                    }

                    Console.Write("🔢 Quantidade: ");
                    if (!int.TryParse(Console.ReadLine(), out var quantidade) || quantidade <= 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Quantidade inválida!");
                        Console.ResetColor();
                        break;
                    }

                    var novoProdutoId = Guid.NewGuid();
                    
                    // Calcular desconto e total líquido
                    var quantidadeTotal = ObterQuantidadeTotalPorProduto(itens, novoProdutoId) + quantidade;
                    var descontoItem = CalcularDesconto(quantidadeTotal);
                    var totalBrutoItem = quantidade * valorUnitario;
                    var totalLiquidoItem = CalcularTotalLiquido(quantidade, valorUnitario, descontoItem);
                    
                    itens.Add(new ItemVendaDto(novoProdutoId, quantidade, valorUnitario, descontoItem, totalLiquidoItem));
                    nomesProdutos[novoProdutoId] = nomeProduto;
                    modificado = true;

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"✅ Item '{nomeProduto}' adicionado! ");
                    Console.ResetColor();
                    Console.Write($"(Qtd: {quantidade} x R$ {valorUnitario:N2} = R$ {totalBrutoItem:N2}");
                    
                    if (descontoItem > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($" - {descontoItem:P0} desconto");
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($" = R$ {totalLiquidoItem:N2}");
                        Console.ResetColor();
                    }
                    Console.WriteLine(")");
                    break;

                case "2": // Remover por quantidade
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n➖ REMOVER ITEM POR QUANTIDADE\n");
                    Console.ResetColor();

                    if (!itens.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("⚠️  Não há itens na venda.");
                        Console.ResetColor();
                        break;
                    }

                    MostrarItensComIndice(itens, nomesProdutos);

                    Console.Write("\n👉 Número do item: ");
                    if (!int.TryParse(Console.ReadLine(), out var indiceRemover) || indiceRemover < 1 || indiceRemover > itens.Count)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Número inválido!");
                        Console.ResetColor();
                        break;
                    }

                    var itemRemover = itens[indiceRemover - 1];
                    Console.Write($"🔢 Quantidade a remover (disponível: {itemRemover.Quantidade}): ");
                    if (!int.TryParse(Console.ReadLine(), out var qtdRemover) || qtdRemover <= 0 || qtdRemover > itemRemover.Quantidade)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Quantidade inválida!");
                        Console.ResetColor();
                        break;
                    }

                    if (qtdRemover == itemRemover.Quantidade)
                    {
                        itens.RemoveAt(indiceRemover - 1);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✅ Item removido completamente!");
                        Console.ResetColor();
                    }
                    else
                    {
                        var novaQuantidade = itemRemover.Quantidade - qtdRemover;
                        
                        // Recalcular desconto e total com a nova quantidade
                        var novoDesconto = CalcularDesconto(novaQuantidade);
                        var novoTotalItem = CalcularTotalLiquido(novaQuantidade, itemRemover.ValorUnitario, novoDesconto);
                        
                        itens[indiceRemover - 1] = new ItemVendaDto(
                            itemRemover.ProdutoId,
                            novaQuantidade,
                            itemRemover.ValorUnitario,
                            novoDesconto,
                            novoTotalItem
                        );
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✅ Removidas {qtdRemover} unidades. Quantidade restante: {novaQuantidade}");
                        Console.ResetColor();
                    }
                    modificado = true;
                    break;

                case "3": // Remover completamente
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n🗑️  REMOVER ITEM COMPLETAMENTE\n");
                    Console.ResetColor();

                    if (!itens.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("⚠️  Não há itens na venda.");
                        Console.ResetColor();
                        break;
                    }

                    MostrarItensComIndice(itens, nomesProdutos);

                    Console.Write("\n👉 Número do item: ");
                    if (!int.TryParse(Console.ReadLine(), out var indiceRemoverCompleto) || indiceRemoverCompleto < 1 || indiceRemoverCompleto > itens.Count)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Número inválido!");
                        Console.ResetColor();
                        break;
                    }

                    itens.RemoveAt(indiceRemoverCompleto - 1);
                    modificado = true;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ Item removido completamente!");
                    Console.ResetColor();
                    break;

                case "4": // Ver itens
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("\n📋 ITENS ATUAIS DA VENDA\n");
                    Console.ResetColor();
                    MostrarItensComIndice(itens, nomesProdutos);
                    break;

                case "5": // Finalizar
                    if (!modificado)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\n⚠️  Nenhuma modificação foi feita. Venda não será atualizada.");
                        Console.ResetColor();
                        return;
                    }

                    if (!itens.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n❌ A venda não pode ficar sem itens!");
                        Console.ResetColor();
                        break;
                    }

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n🚀 Enviando atualização para a API...");
                    Console.ResetColor();

                    var request = new AtualizarVendaRequest(itens);
                    var venda = await _apiClient.AtualizarVendaAsync(vendaId, request);

                    if (venda != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✅ Venda #{venda.Numero} ATUALIZADA e PERSISTIDA na API com sucesso!\n");
                        Console.ResetColor();
                        MostrarDetalheVenda(venda);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Falha ao atualizar venda na API!");
                        Console.ResetColor();
                    }
                    return;

                case "0": // Cancelar
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n⚠️  Atualização cancelada.");
                    Console.ResetColor();
                    return;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Opção inválida!");
                    Console.ResetColor();
                    break;
            }
        }
    }

    private void MostrarItensComIndice(List<ItemVendaDto> itens, Dictionary<Guid, string> nomesProdutos)
    {
        if (!itens.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  Nenhum item na lista.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine("┌────┬────────────┬─────────────────────┬──────────┬──────────────┬──────────────┐");
        Console.WriteLine("│ Nº │ ID Produto │ Produto             │ Qtd      │ Valor Unit.  │ Total        │");
        Console.WriteLine("├────┼────────────┼─────────────────────┼──────────┼──────────────┼──────────────┤");

        for (int i = 0; i < itens.Count; i++)
        {
            var item = itens[i];
            var nome = nomesProdutos.ContainsKey(item.ProdutoId) 
                ? nomesProdutos[item.ProdutoId] 
                : $"Produto {i + 1}";
            var produtoId = item.ProdutoId.ToString().Substring(0, 8);
            
            Console.Write($"│ {i + 1,-2} │ ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{produtoId,-10}");
            Console.ResetColor();
            Console.WriteLine($" │ {nome,-19} │ {item.Quantidade,-8} │ R$ {item.ValorUnitario,8:N2} │ R$ {item.Total,8:N2} │");
        }

        Console.WriteLine("└────┴────────────┴─────────────────────┴──────────┴──────────────┴──────────────┘");
        
        var totalGeral = itens.Sum(i => i.Total);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n💰 Total da Venda: R$ {totalGeral:N2}");
        Console.ResetColor();
    }

    private async Task CancelarVendaAsync()
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  ❌ CANCELAR VENDA ❌                      ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        Console.Write("Digite o ID da venda: ");
        if (!Guid.TryParse(Console.ReadLine(), out var vendaId))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ ID inválido!");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n🔍 Buscando venda na API...");
        Console.ResetColor();

        var venda = await _apiClient.ObterVendaAsync(vendaId);
        if (venda == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Venda não encontrada!");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Venda encontrada!\n");
        Console.ResetColor();
        MostrarDetalheVenda(venda);

        // Verificar se a venda já está cancelada
        if (venda.Status.Equals("Cancelada", StringComparison.OrdinalIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n⚠️  ATENÇÃO: Esta venda já está CANCELADA!");
            Console.WriteLine("   Não é necessário cancelar novamente.");
            Console.ResetColor();
            Console.WriteLine("\nPressione qualquer tecla para voltar ao menu...");
            Console.ReadKey();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("\n⚠️  Confirma o cancelamento desta venda? (S/N): ");
        Console.ResetColor();
        var confirmacao = Console.ReadLine()?.ToUpper();

        if (confirmacao != "S")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n📝 Cancelamento abortado.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n🚀 Enviando cancelamento para a API...");
        Console.ResetColor();

        if (await _apiClient.CancelarVendaAsync(vendaId))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Venda CANCELADA e PERSISTIDA na API com sucesso!");
            Console.ResetColor();
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n🔍 Verificando status atualizado...");
            Console.ResetColor();
            
            var vendaCancelada = await _apiClient.ObterVendaAsync(vendaId);
            if (vendaCancelada != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ Status confirmado: {vendaCancelada.Status}\n");
                Console.ResetColor();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Falha ao cancelar venda na API!");
            Console.ResetColor();
        }
    }

    private void MostrarDetalheVenda(VendaResponse venda)
    {
        Console.WriteLine("\n╔════════════════════ DETALHES DA VENDA ═════════════════════╗");
        Console.WriteLine($"║  Venda: #{venda.Numero,-49} ║");
        Console.WriteLine($"║  ID: {venda.Id,-52} ║");
        Console.WriteLine($"║  Status: {venda.Status,-48} ║");
        Console.WriteLine($"║  Data: {venda.Data:dd/MM/yyyy HH:mm,-50} ║");
        Console.WriteLine("╠════════════════════════════════════════════════════════════╣");

        int itemNumero = 1;
        foreach (var item in venda.Itens)
        {
            var descInfo = item.Desconto > 0 ? $" (Desc: {item.Desconto:P0})" : "";
            var produtoId = item.ProdutoId.ToString().Substring(0, 8); // Primeiros 8 caracteres do GUID
            
            Console.Write($"║  {itemNumero}. ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"[{produtoId}]");
            Console.ResetColor();
            Console.WriteLine($" {item.Quantidade}x R$ {item.ValorUnitario:N2}{descInfo,-15} = R$ {item.Total,8:N2} ║");
            
            itemNumero++;
        }

        Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"║  TOTAL: R$ {venda.ValorTotal,48:N2} ║");
        Console.ResetColor();
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
    }
}
