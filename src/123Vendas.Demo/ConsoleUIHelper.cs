using System;

namespace _123Vendas.Demo
{
    public static class ConsoleUIHelper
    {
        public static void MostrarBannerInicial()
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

        public static void MostrarMenuPrincipal()
        {
            Console.WriteLine("Escolha o modo de demonstração:\n");
            Console.WriteLine("  1️⃣  Demo LOCAL (Simulação em memória - sem API)");
            Console.WriteLine("  2️⃣  Demo com API REST (Integração completa)");
            Console.WriteLine("  0️⃣  Sair\n");
            Console.Write("👉 Opção: ");
        }

        public static void MostrarMensagemErro(string mensagem)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ {mensagem}\n");
            Console.ResetColor();
        }

        public static void MostrarMensagemSucesso(string mensagem)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✅ {mensagem}\n");
            Console.ResetColor();
        }

        public static void MostrarMensagemAviso(string mensagem)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n⚠️  {mensagem}\n");
            Console.ResetColor();
        }

        public static void MostrarMensagemSaida()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n👋 Até logo!\n");
            Console.ResetColor();
        }

        public static void Pausar(string mensagem = "Pressione qualquer tecla para continuar...")
        {
            Console.WriteLine($"\n{mensagem}");
            Console.ReadKey();
            Console.Clear();
        }
    }
}
