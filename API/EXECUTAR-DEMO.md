# 🚀 Como Executar a Aplicação Demo

## Pré-requisitos
- .NET 9.0 SDK instalado

## Executar

### Opção 1: Via Terminal
```bash
cd src/123Vendas.Demo
dotnet run
```

### Opção 2: Via Raiz do Projeto
```bash
dotnet run --project src/123Vendas.Demo/123Vendas.Demo.csproj
```

## Menu Principal

Ao executar, você verá o menu:

```
╔════════════════════════════════════════════════════════════╗
║              🛒  SISTEMA 123VENDAS - DEMO  🛒              ║
║          Demonstração Interativa de Regras de Negócio     ║
╚════════════════════════════════════════════════════════════╝

═══════════════════ MENU PRINCIPAL ═══════════════════

  1️⃣  Demonstrar Regras de Desconto
  2️⃣  Simulação Interativa de Venda
  3️⃣  Testar Cenários de Sucesso
  4️⃣  Testar Cenários de Erro
  5️⃣  Demonstrar Eventos de Domínio
  0️⃣  Sair

═════════════════════════════════════════════════════
👉 Escolha uma opção:
```

## Funcionalidades

### 1️⃣ Demonstrar Regras de Desconto
- Mostra tabela com todas as faixas de desconto
- Exemplos práticos de cálculo
- Demonstração do limite de 20 unidades

### 2️⃣ Simulação Interativa de Venda
**Mais Interessante!** Permite:
- ✅ Adicionar produtos com quantidade e valor
- ✅ Remover quantidade parcial ou total
- ✅ Ver descontos sendo aplicados em tempo real
- ✅ Consolidação automática de produtos iguais
- ✅ Recálculo automático de descontos

**Exemplo de uso:**
1. Escolha opção `2`
2. Digite `A` para adicionar item
3. Nome: `Notebook`
4. Quantidade: `5`
5. Valor: `1000`
6. Veja o desconto de 10% sendo aplicado!
7. Digite `A` novamente e adicione mais 5 unidades do mesmo produto
8. Veja a consolidação: agora são 10 unidades com 20% de desconto!
9. Digite `R` para remover
10. Escolha remover apenas 3 unidades
11. Veja o desconto recalculado para 7 unidades (10%)

### 3️⃣ Testar Cenários de Sucesso
- Executa automaticamente vários cenários
- Mostra cálculos detalhados
- Demonstra múltiplos produtos
- Teste de remoção parcial

### 4️⃣ Testar Cenários de Erro
- Validação de limite (>20 unidades)
- Quantidade inválida
- Valor unitário inválido
- Venda cancelada
- Consolidação ultrapassando limite

### 5️⃣ Demonstrar Eventos de Domínio
- Mostra eventos disparados
- Fluxo de comunicação entre módulos
- Event-Driven Architecture

## 🎯 Cenários Recomendados para Demonstração

### Cenário 1: Desconto Progressivo
1. Adicione 3 unidades → Sem desconto
2. Adicione mais 2 unidades do mesmo produto → 10% desconto
3. Adicione mais 5 unidades → 20% desconto

### Cenário 2: Remoção Parcial
1. Adicione 15 unidades → 20% desconto
2. Remova 5 unidades → Fica com 10 unidades e 20% desconto
3. Remova mais 3 unidades → Fica com 7 unidades e 10% desconto

### Cenário 3: Limite Máximo
1. Adicione 20 unidades → Sucesso com 20% desconto
2. Tente adicionar mais 1 unidade → ERRO!

### Cenário 4: Múltiplos Produtos
1. Produto A: 5 unidades → 10% desconto
2. Produto B: 12 unidades → 20% desconto
3. Produto C: 2 unidades → Sem desconto
4. Veja o total da venda com descontos diferentes

## 💡 Dicas

- Use a opção `3` primeiro para ver todos os cenários automaticamente
- Depois use a opção `2` para testar interativamente
- A opção `1` é ótima para explicar as regras de negócio
- A opção `5` demonstra a arquitetura event-driven

## 🎨 Recursos Visuais

- ✅ Cores para destacar informações
- ✅ Emojis para melhor UX
- ✅ Tabelas formatadas
- ✅ Feedback claro de sucesso/erro
- ✅ Cálculos detalhados passo a passo

---

**Desenvolvido para demonstrar as regras de negócio do sistema 123Vendas** 🚀
