# 123Vendas - API de Vendas

API RESTful desenvolvida em .NET 9 para gerenciamento de vendas com arquitetura em camadas e eventos de domínio.

## 📋 Sobre o Projeto

Sistema de vendas com CRUD completo, regras de negócio centralizadas e eventos de domínio. Implementa os requisitos do teste técnico com foco em qualidade de código, testes e boas práticas.

### Funcionalidades Principais

- ✅ **CRUD de Vendas**: Criar, listar, atualizar e cancelar vendas
- ✅ **Regras de Desconto**: Descontos automáticos baseados em quantidade
- ✅ **Eventos de Domínio**: CompraCriada, CompraAlterada, CompraCancelada, ItemCancelado
- ✅ **Validações**: FluentValidation para comandos
- ✅ **Logs Estruturados**: Serilog com JSON
- ✅ **Health Checks**: Monitoramento de saúde da aplicação
- ✅ **Testes**: 103 testes (unitários, aplicação e integração)

### Regras de Negócio

- **< 4 itens iguais**: Sem desconto
- **4 a 9 itens iguais**: 10% de desconto
- **10 a 20 itens iguais**: 20% de desconto
- **> 20 itens iguais**: Venda não permitida

### Princípios Aplicados

- **Clean Code**: Código limpo e legível
- **SOLID**: Princípios de design orientado a objetos
- **DRY**: Don't Repeat Yourself
- **YAGNI**: You Aren't Gonna Need It
- **Object Calisthenics**: Regras para código mais expressivo

## 🏗️ Estrutura do Projeto

```
123Vendas.sln
├── src/
│   ├── 123Vendas.Api/                          → API Layer (Minimal APIs)
│   ├── 123Vendas.Shared/                       → Shared components
│   └── Modules/
│       ├── Venda/
│       │   ├── Venda.Domain/                   → Domain Layer
│       │   ├── Venda.Application/              → Application Layer (CQRS)
│       │   └── Venda.Infrastructure/           → Infrastructure Layer (EF Core)
│       ├── Estoque/
│       │   └── Estoque.Application/            → Simulated module
│       └── CRM/
│           └── CRM.Application/                → Simulated module
└── tests/
    ├── Venda.Domain.Tests/                     → Unit tests
    ├── Venda.Application.Tests/                → Application tests
    └── Venda.Integration.Tests/                → Integration tests
```

## Decisões Arquiteturais

### Interfaces de Integração no Shared
As interfaces `IClienteService` e `IProdutoService` estão no projeto Shared
para evitar acoplamento direto entre módulos. Isso permite que:
- Venda não precise referenciar CRM/Estoque diretamente
- Os contratos sejam compartilhados e versionados centralmente
- Facilite a transição futura para microserviços (substituir por HTTP clients)

Alternativa considerada: Cada módulo expor suas próprias interfaces.
Trade-off: Maior autonomia vs. maior complexidade de dependências.


## 🚀 Como Executar

### Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Passo a Passo

1. **Clone o repositório**
   ```bash
   git clone <repository-url>
   cd 123Vendas/API
   ```

2. **Restaurar dependências**
   ```bash
   dotnet restore
   ```

3. **Executar a aplicação**
   ```bash
   dotnet run --project src/123Vendas.Api
   ```

4. **Acessar a API**
   - Swagger UI: `https://localhost:5001/swagger`
   - Health Check: `https://localhost:5001/health`
   - Endpoints: `https://localhost:5001/api/v1/vendas`

### Banco de Dados

O projeto usa **SQLite** (arquivo `vendas.db`), criado automaticamente na primeira execução. Não é necessária configuração adicional.

## 🧪 Executar Testes

### Todos os testes

```bash
dotnet test
```

### Testes unitários (Domain)

```bash
dotnet test tests/Venda.Domain.Tests
```

### Testes de aplicação

```bash
dotnet test tests/Venda.Application.Tests
```

### Testes de integração

```bash
dotnet test tests/Venda.Integration.Tests
```

### Com cobertura de código

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 📦 Build

```bash
dotnet build --configuration Release
```

## 📝 Convenções de Commits (Semantic Commits)

Este projeto segue a convenção de commits semânticos:

- `feat:` Nova funcionalidade
  ```bash
  git commit -m "feat: adicionar endpoint de criação de vendas"
  ```

- `fix:` Correção de bug
  ```bash
  git commit -m "fix: corrigir cálculo de desconto em itens"
  ```

- `docs:` Alterações em documentação
  ```bash
  git commit -m "docs: atualizar README com instruções de setup"
  ```

- `test:` Adição ou modificação de testes
  ```bash
  git commit -m "test: adicionar testes unitários para VendaAgregado"
  ```

- `refactor:` Refatoração de código
  ```bash
  git commit -m "refactor: extrair lógica de desconto para método privado"
  ```

- `chore:` Tarefas de manutenção
  ```bash
  git commit -m "chore: atualizar dependências do projeto"
  ```

- `style:` Formatação de código
  ```bash
  git commit -m "style: aplicar formatação padrão do EditorConfig"
  ```

- `perf:` Melhorias de performance
  ```bash
  git commit -m "perf: adicionar índice em ClienteId para otimizar queries"
  ```

## 🌿 Git Flow

O projeto utiliza Git Flow para gerenciamento de branches:

### Branches principais

- `main`: Código em produção (sempre estável)
- `develop`: Branch de integração para desenvolvimento

### Branches de suporte

- `feature/*`: Novas funcionalidades
  ```bash
  git checkout -b feature/criar-endpoint-vendas develop
  ```

- `bugfix/*`: Correções de bugs em desenvolvimento
  ```bash
  git checkout -b bugfix/corrigir-calculo-desconto develop
  ```

- `hotfix/*`: Correções urgentes em produção
  ```bash
  git checkout -b hotfix/corrigir-validacao-cliente main
  ```

- `release/*`: Preparação para release
  ```bash
  git checkout -b release/v1.0.0 develop
  ```

### Workflow típico

1. Criar feature branch a partir de `develop`:
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/minha-funcionalidade
   ```

2. Desenvolver e commitar usando commits semânticos:
   ```bash
   git add .
   git commit -m "feat: implementar nova funcionalidade"
   ```

3. Fazer push da branch:
   ```bash
   git push origin feature/minha-funcionalidade
   ```

4. Criar Pull Request para `develop`

5. Após aprovação e merge, deletar a branch:
   ```bash
   git branch -d feature/minha-funcionalidade
   ```

## 🛠️ Tecnologias e Padrões

### Stack Técnica

- **.NET 9**: Framework principal
- **ASP.NET Core Minimal APIs**: Endpoints RESTful
- **Entity Framework Core**: ORM com SQLite
- **MediatR**: CQRS e eventos de domínio
- **FluentValidation**: Validação de comandos
- **Serilog**: Logging estruturado (Console + File)
- **Polly**: Resiliência (Circuit Breaker, Retry)
- **Swagger/OpenAPI**: Documentação interativa

### Testes

- **xUnit**: Framework de testes
- **FluentAssertions**: Asserções expressivas
- **NSubstitute**: Mocking
- **WebApplicationFactory**: Testes de integração
- **Cobertura**: 103 testes (unitários, aplicação, integração)

### Arquitetura

- **Camadas**: API → Application → Domain → Infrastructure
- **CQRS**: Comandos e Queries separados
- **Domain Events**: Comunicação entre módulos
- **Result Pattern**: Tratamento de erros sem exceções
- **Outbox Pattern**: Garantia de entrega de eventos
- **Repository Pattern**: Abstração de acesso a dados

## 📊 Cobertura de Testes

```bash
dotnet test /p:CollectCoverage=true
```

**Resultados**: 103 testes passando (100%)
- 11 testes unitários (Domain)
- 82 testes de aplicação (Handlers, Validators)
- 10 testes de integração (API endpoints)

## 📚 Documentação Adicional

- [Design Document](.kiro/specs/api-vendas-123vendas/design.md) - Decisões arquiteturais
- [Requirements Document](.kiro/specs/api-vendas-123vendas/requirements.md) - Requisitos funcionais
- [Implementation Tasks](.kiro/specs/api-vendas-123vendas/tasks.md) - Tarefas implementadas

## 📝 Logs

Os logs são gravados em:
- **Console**: Logs em tempo real durante execução
- **Arquivo**: `logs/123vendas-YYYY-MM-DD.log` (rotação diária, 30 dias de retenção)

Formato estruturado com propriedades JSON para facilitar análise.

## 🎯 Destaques do Projeto

### Além dos Requisitos

- ✅ **Outbox Pattern**: Garantia de entrega de eventos
- ✅ **Health Checks**: Monitoramento de saúde (DB, Outbox)
- ✅ **Circuit Breaker**: Resiliência em integrações
- ✅ **Idempotência**: Prevenção de duplicação de vendas
- ✅ **Logs Estruturados**: Serilog com JSON
- ✅ **Arquitetura Modular**: Preparado para microserviços

### Boas Práticas Aplicadas

- ✅ Injeção de Dependência
- ✅ Testes com alta cobertura
- ✅ Validações centralizadas
- ✅ Tratamento de erros consistente
- ✅ Código limpo e documentado
- ✅ Commits semânticos
- ✅ Git Flow

## 📄 Licença

Projeto desenvolvido como teste técnico para vaga de desenvolvedor .NET.
