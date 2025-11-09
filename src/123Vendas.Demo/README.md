# 123Vendas - Aplicação Demo

Aplicação console interativa para demonstrar as funcionalidades do sistema 123Vendas.

## 🎯 Modos de Operação

### 1️⃣ Demo LOCAL (Simulação em memória)
- Executa **sem necessidade da API**
- Demonstra regras de negócio do domínio
- Ideal para entender as regras de desconto e validações
- Não persiste dados

**Funcionalidades:**
- Demonstrar Regras de Desconto
- Simulação Interativa de Venda
- Testar Cenários de Sucesso
- Testar Cenários de Erro
- Demonstrar Eventos de Domínio

### 2️⃣ Demo com API REST (Integração completa)
- Requer a **API rodando** em `http://localhost:5197`
- Integração completa com endpoints REST
- **Persiste dados no banco SQLite**
- Demonstra o fluxo completo end-to-end

**Funcionalidades:**
- ➕ Criar Nova Venda (POST /api/v1/vendas)
- 📋 Listar Todas as Vendas (GET /api/v1/vendas)
- 🔍 Consultar Venda por ID (GET /api/v1/vendas/{id})
- ✏️ Atualizar Venda (PUT /api/v1/vendas/{id})
- ❌ Cancelar Venda (DELETE /api/v1/vendas/{id})

## 🚀 Como Executar

### Pré-requisitos
- .NET 9.0 SDK instalado
- Para o modo API: API 123Vendas rodando

### Iniciar a API (necessário para modo 2)
```bash
cd src/123Vendas.Api
dotnet run
```

A API estará disponível em: `http://localhost:5197`

### Executar o Demo
```bash
cd src/123Vendas.Demo
dotnet run
```

## 📊 Regras de Desconto

O sistema aplica descontos automaticamente baseados na quantidade de itens **do mesmo produto**:

| Quantidade | Desconto | Exemplo (R$ 100,00/un) |
|------------|----------|------------------------|
| 1-3        | 0%       | 3x = R$ 300,00        |
| 4-9        | 10%      | 5x = R$ 450,00        |
| 10-20      | 20%      | 15x = R$ 1.200,00     |
| >20        | ❌ REJEITADO | Não permitido    |

## 🎬 Fluxo de Criação de Venda (Modo API)

1. **Entrada de Dados**
   - Sistema gera automaticamente ClienteId e FilialId
   - Usuário adiciona itens (produto, quantidade, valor)

2. **Envio para API**
   - Mensagem: `🚀 Enviando venda para a API...`
   - Request POST para `/api/v1/vendas`

3. **Persistência**
   - Mensagem: `✅ Venda PERSISTIDA com sucesso na API!`
   - Retorna o ID da venda criada

4. **Confirmação**
   - Sistema busca a venda criada (GET)
   - Exibe detalhes completos com descontos aplicados
   - Destaca descontos automáticos: `🎉 DESCONTOS APLICADOS AUTOMATICAMENTE PELA API`

## 🔄 Fluxo de Atualização de Venda

1. **Busca Venda Existente**
   - Mensagem: `🔍 Buscando venda na API...`
   - Exibe dados atuais

2. **Entrada de Novos Itens**
   - Usuário adiciona novos itens (substituem os atuais)

3. **Envio para API**
   - Mensagem: `🚀 Enviando atualização para a API...`
   - Request PUT para `/api/v1/vendas/{id}`

4. **Confirmação**
   - Mensagem: `✅ Venda ATUALIZADA e PERSISTIDA na API com sucesso!`
   - Exibe dados atualizados

## ❌ Fluxo de Cancelamento de Venda

1. **Busca Venda**
   - Mensagem: `🔍 Buscando venda na API...`
   - Exibe dados da venda

2. **Confirmação**
   - Solicita confirmação do usuário

3. **Envio para API**
   - Mensagem: `🚀 Enviando cancelamento para a API...`
   - Request DELETE para `/api/v1/vendas/{id}`

4. **Verificação**
   - Mensagem: `✅ Venda CANCELADA e PERSISTIDA na API com sucesso!`
   - Busca venda novamente para confirmar status "Cancelada"

## 💡 Dicas

- **Status "Ativa"**: Venda criada com sucesso e cliente validado
- **Status "Cancelada"**: Venda cancelada (soft delete)
- **Validação de Cliente**: Se o cliente não existir ou o CRM estiver indisponível, a venda será rejeitada

## 🐛 Troubleshooting

### Erro: "API não está acessível"
- Verifique se a API está rodando em `http://localhost:5197`
- Execute: `dotnet run --project src/123Vendas.Api/123Vendas.Api.csproj`

### Erro ao criar venda
- Verifique se os valores são positivos
- Quantidade máxima por produto: 20 unidades
- Valor unitário máximo: R$ 999.999,99

## 📝 Exemplos de Uso

### Criar venda com desconto de 10%
```
Produto: Notebook
Valor: 1000.00
Quantidade: 5
Resultado: R$ 4.500,00 (10% de desconto aplicado)
```

### Criar venda com desconto de 20%
```
Produto: Mouse
Valor: 50.00
Quantidade: 15
Resultado: R$ 600,00 (20% de desconto aplicado)
```

### Venda com múltiplos produtos
```
Produto A: 5 unidades × R$ 100,00 = R$ 450,00 (10% desc)
Produto B: 12 unidades × R$ 50,00 = R$ 480,00 (20% desc)
Produto C: 2 unidades × R$ 75,00 = R$ 150,00 (sem desc)
TOTAL: R$ 1.080,00
```
