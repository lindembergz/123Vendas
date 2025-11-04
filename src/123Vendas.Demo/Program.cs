using _123Vendas.Demo;

namespace _123Vendas.ConsoleApp
{
    internal static class Program
    {
        private static async Task Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Clear();

            ConsoleUIHelper.MostrarBannerInicial();
            ConsoleUIHelper.MostrarMenuPrincipal();

            var opcao = Console.ReadLine();
            Console.Clear();

            await ExecutarOpcaoAsync(opcao);
        }

        private static async Task ExecutarOpcaoAsync(string? opcao)
        {
            switch (opcao)
            {
                case "1":
                    new VendasDemo().Executar();
                    break;

                case "2":
                    await new VendasDemoComApi().ExecutarAsync();
                    break;

                case "0":
                    ConsoleUIHelper.MostrarMensagemSaida();
                    break;

                default:
                    ConsoleUIHelper.MostrarMensagemErro("Opção inválida!");
                    break;
            }
        }
    }
}
