# 123Vendas - API de Vendas

API RESTful desenvolvida em .NET 9 para gerenciamento de vendas com arquitetura em camadas e eventos de domínio.

## 📋 Sobre o Projeto

Sistema de vendas com CRUD completo, regras de negócio centralizadas e eventos de domínio. Implementa os requisitos do teste técnico com foco em qualidade de código, testes e boas práticas.

### Funcionalidades Principais

- **CRUD de Vendas**: Criar, listar, atualizar e cancelar vendas
- **Regras de Desconto**: Descontos automáticos baseados em quantidade
- **Eventos de Domínio**: CompraCriada, CompraAlterada, CompraCancelada, ItemCancelado
- **Validações**: FluentValidation para comandos
- **Logs Estruturados**: Serilog com JSON
- **Health Checks**: Monitoramento de saúde da aplicação
- **Testes**: 204 testes (unitários, aplicação e integração)

### Regras de Negócio

- **< 4 itens iguais**: Sem desconto
- **4 a 9 itens iguais**: 10% de desconto
- **10 a 20 itens iguais**: 20% de desconto
- **> 20 itens iguais**: Venda não permitida

### Princípios Aplicados

- **Clean Code**: Código limpo e legível
- **SOLID**: Princípios de design orientado a objetos
      -SRP: Cada classe tem uma única responsabilidade. `PoliticaDesconto` isolada do agregado. 
      -OCP: IPoliticaDesconto permite novas políticas sem modificar código existente. 
      -LSP: Interfaces bem definidas, implementações substituíveis. 
      -ISP: Interfaces pequenas e focadas IClienteService, IProdutoService). 
      -DIP:  Domain não depende de Infrastructure. Abstrações no Domain, implementações na Infrastructure. 
- **DRY**: Don't Repeat Yourself
- **YAGNI**: You Aren't Gonna Need It
- **Object Calisthenics**: Regras para código mais expressivo
- **DDD** :
    - Aggregate Root: VendaAgregado controla acesso aos ItemVenda
    - Value Objects: ItemVenda é imutável (record) sem identidade própria
    - Domain Events: CompraCriada, CompraAlterada, CompraCancelada, ItemCancelado
    - Ubiquitous Language: Termos de negócio no código
    - Bounded Contexts: Módulos Venda, CRM e Estoque

## 🏗️ Estrutura do Projeto


<img width="313" height="758" alt="image" src="https://github.com/user-attachments/assets/2c3ea2b2-533a-4adb-8589-e706411ec30a" />



## 🏛️ Decisões Arquiteturais

### Modular Monolith
O projeto foi estruturado como um monólito modular, onde cada módulo (Venda, CRM, Estoque) é independente e se comunica através de eventos de domínio. Esta abordagem oferece:
- **Simplicidade inicial**: Deploy único, sem complexidade de microserviços
- **Evolução gradual**: Módulos podem ser extraídos para microserviços quando necessário
- **Baixo acoplamento**: Comunicação via eventos, sem dependências diretas

### Interfaces de Integração no Shared
As interfaces `IClienteService` e `IProdutoService` estão no projeto Shared para evitar acoplamento direto entre módulos:
- Venda não precisa referenciar CRM/Estoque diretamente
- Contratos compartilhados e versionados centralmente
- Facilita transição futura para microserviços (substituir por HTTP clients)
- **Trade-off**: Maior autonomia vs. complexidade de dependências

### Outbox Pattern
Implementado para garantir consistência entre persistência e publicação de eventos:
- Eventos salvos na mesma transação do banco de dados
- Processamento assíncrono em background
- Retry automático em caso de falha
- **Benefício**: Garantia de entrega de eventos sem perda de dados

### CQRS com MediatR
Separação clara entre comandos (escrita) e queries (leitura):
- Comandos validados com FluentValidation
- Queries otimizadas para leitura
- Handlers isolados e testáveis
- **Benefício**: Código mais organizado e escalável

## 🚀 Como Executar

### Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Passo a Passo

1. **Clone o repositório**   
   
   git clone https://github.com/lindembergz/123Vendas

2. **Restaurar dependências**

   dotnet restore      

3. **build e Executar a aplicação**
   
   dotnet build --project src/123Vendas.Api
   dotnet run --project src/123Vendas.Api   

4. **Acessar a API**
   - **Swagger UI**: `https://localhost:5001/swagger` - Documentação interativa
   - **Health Check**: `https://localhost:5001/health` - Status da aplicação
   - **Endpoints**: `https://localhost:5001/api/v1/vendas` - API de vendas
  
     **Criar Venda com Desconto**
     
     **Endpoint**: POST /api/v1/vendas
     
     **Payload:**
          {
            "clienteId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "filialId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
            "itens": [
              {
                "produtoId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
                "quantidade": 10,
                "valorUnitario": 100.00
              }
            ]
          }
      - 10 unidades = 20% de desconto automático
      - ValorTotal = 10 × 100 × 0.8 = R$ 800,00
      - Evento `CompraCriada` gerado automaticamente
      - Número sequencial gerado por filial
    
     **Listar Vendas**
     
     **Endpoint**: GET /api/v1/vendas
     
     **Demonstrar:**
      - Paginação (pageNumber, pageSize)
      - Filtros (clienteId, filialId, status, dataInicio, dataFim)
      - Ordenação por data (mais recentes primeiro)
  

5. **Executar Testes**

    dotnet test
   
    dotnet test tests/Venda.Domain.Tests
   
    dotnet test tests/Venda.Application.Tests
   
    dotnet test tests/Venda.Infrastructure.Tests
   
    dotnet test tests/Venda.Integration.Tests
   
    dotnet test tests/Shared.Tests

5. **Executar Demo (Opcional)**

   dotnet run --project src/123Vendas.Demo

   Console interativo demonstrando funcionalidades da API

   <img width="779" height="574" alt="image" src="https://github.com/user-attachments/assets/bc5dfec9-c58b-4168-b9db-6a2d1d103703" />


### Banco de Dados

O projeto usa **SQLite** (arquivo `vendas.db`) com configuração zero:

- **Criação Automática**: Na primeira execução, o banco é criado automaticamente com todas as tabelas
- **Migrations Automáticas**: Entity Framework Core aplica todas as migrations no startup
- **Sem Configuração**: Não é necessário instalar ou configurar nada
- **Localização**: O arquivo `vendas.db` é criado na raiz do projeto da API

**Tabelas criadas automaticamente:**
- `Vendas` - Dados principais das vendas
- `Produtos` - Itens das vendas
- `OutboxEvents` - Eventos de domínio (Outbox Pattern)
- `IdempotencyKeys` - Controle de idempotência

Basta executar `dotnet run` e o banco estará pronto para uso!

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
- **NSubstitute**: Mocking para testes unitários
- **WebApplicationFactory**: Testes de integração end-to-end
- **SQLite In-Memory**: Banco de dados isolado para testes de integração
- **Bogus**: Geração de dados fake realistas
- **Cobertura**: 204 testes (unitários, aplicação, infraestrutura e integração)

**Nota**
A implementação atual usa SQLite in-memory para os testes de integração, que é uma abordagem mais simples e adequada para este projeto porque:

Vantagens do SQLite in-memory (usado atualmente):

- Extremamente rápido (execução em memória)
- Sem dependências externas (Docker não necessário)
- Configuração simples
- Isolamento perfeito entre testes
- Funciona em qualquer ambiente (CI/CD, Windows, Linux, Mac)

### Padrões Arquiteturais
- **Clean Architecture**: Separação clara de responsabilidades em camadas
- **Modular Monolith**: Módulos independentes (Venda, CRM, Estoque) preparados para microserviços
- **CQRS**: Comandos e Queries separados com MediatR
- **Domain Events**: Comunicação assíncrona entre módulos
- **Result Pattern**: Tratamento de erros de negócio sem exceções
- **Global Exception Handling**: Tratamento centralizado de exceções técnicas com Exception Filter
- **Outbox Pattern**: Garantia de entrega de eventos (transactional messaging)
- **Repository Pattern**: Abstração de acesso a dados
- **Unit of Work**: Gerenciamento de transações com EF Core

## Cobertura de Testes

dotnet test /p:CollectCoverage=true

**Resultados**: 204 testes passando (100%)
- 47 testes unitários (Domain)
- 49 testes de aplicação (Handlers, Validators)
- 63 testes de integração (API endpoints)
- 18 testes compartilhados (Shared)
- 27 testes de infraestrutura (Infrastructure)

<img width="696" height="322" alt="image" src="https://github.com/user-attachments/assets/2bef8e5b-78bb-4b04-947c-c3b1b02985f6" />


### Distribuição dos Testes

| Categoria | Quantidade | Descrição |
|-----------|------------|-----------|
| **API**    |  14 | Testes de tratamento de exceções | 
| **Domain** | 47 | Testes unitários de entidades, value objects e regras de negócio |
| **Application** | 49 | Testes de handlers (23), validators (19) e services (7) |
| **Infrastructure** | 27 | Testes de repositórios, outbox pattern e persistência |
| **Integration** | 63 | Testes end-to-end dos endpoints da API e infraestrutura |
| **Shared** | 18 | Testes de componentes compartilhados, integração e services |
| **Total** | **218** | **100% de aprovação** |


### Implementação dos Testes de Integração

Os testes de integração foram implementados seguindo as melhores práticas de testes end-to-end:

#### Arquitetura dos Testes

**CustomWebApplicationFactory**
- Utiliza `WebApplicationFactory<Program>` para subir a aplicação completa em memória
- Substitui o banco SQLite por uma instância in-memory para isolamento total
- Implementa `IAsyncLifetime` para gerenciar o ciclo de vida da conexão
- Mantém a conexão aberta durante toda a execução dos testes da classe
- Garante limpeza automática de recursos após os testes

**TestDataBuilder**
- Usa a biblioteca **Bogus** para gerar dados fake realistas
- Fornece métodos para criar vendas válidas com diferentes cenários
- Gera itens com descontos de 10% (4-9 unidades) e 20% (10-20 unidades)
- Facilita a criação de dados de teste consistentes

**EventValidationHelper**
- Helper especializado para validar eventos de domínio
- Verifica se eventos foram persistidos corretamente no Outbox
- Valida estrutura completa dos eventos (tipo, dados, status, timestamps)
- Suporta validação de eventos com produtos e múltiplos eventos

#### Padrões Aplicados

**Padrão AAA (Arrange-Act-Assert)**
csharp
[Fact]
public async Task Post_VendaValida_DeveRetornar201EIdDaVenda()
{
    // Arrange - Preparar dados
    var request = _builder.GerarVendaValida();
    
    // Act - Executar ação
    var response = await _client.PostAsJsonAsync("/api/v1/vendas", request);
    
    // Assert - Validar resultado
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}


**Nomenclatura Consistente**
- Formato: `[HttpMethod]_[Scenario]_[ExpectedResult]`
- Exemplos: `Post_VendaValida_DeveRetornar201`, `Get_VendaInexistente_DeveRetornar404`

**Isolamento de Testes**
- Cada classe de teste usa `IClassFixture<CustomWebApplicationFactory>`
- Banco de dados SQLite in-memory único por classe
- Isolamento completo entre classes de teste
- Sem interferência entre testes

#### Cobertura dos Testes de Integração

Os 63 testes de integração cobrem:
-  **CRUD Completo**: Criar, consultar, listar, atualizar e cancelar vendas
-  **Regras de Desconto**: Validação de 10% e 20% de desconto
-  **Eventos de Domínio**: Verificação de CompraCriada, CompraAlterada, CompraCancelada, ItemCancelado
-  **Validações**: Cenários de erro (400, 404, 500)
-  **Persistência**: Verificação de dados salvos no banco
-  **Health Checks**: Monitoramento de saúde da aplicação
-  **Cenários Complexos**: Atualização de vendas, confirmação, cancelamento de itens

#### Tecnologias Utilizadas

- **WebApplicationFactory**: Subir aplicação ASP.NET Core em memória
- **SQLite In-Memory**: Banco de dados isolado e rápido
- **Bogus**: Geração de dados fake com nomes, valores e IDs realistas
- **FluentAssertions**: Asserções expressivas e legíveis
- **xUnit**: Framework de testes com suporte a fixtures

## 📚 Documentação Adicional

- Documento de Design.txt - Decisões arquiteturais detalhadas


### Endpoints da API

#### Vendas
- `POST /api/v1/vendas` - Criar nova venda
  - **Sucesso**: 201 Created com Location header
  - **Erro**: 400 Bad Request (validação/regra de negócio)
- `GET /api/v1/vendas` - Listar todas as vendas
  - **Sucesso**: 200 OK com lista paginada
- `GET /api/v1/vendas/{id}` - Buscar venda por ID
  - **Sucesso**: 200 OK com dados da venda
  - **Erro**: 404 Not Found (venda não existe)
- `PUT /api/v1/vendas/{id}` - Atualizar venda existente
  - **Sucesso**: 200 OK com venda atualizada
  - **Erro**: 404 Not Found ou 400 Bad Request
- `DELETE /api/v1/vendas/{id}` - Cancelar venda
  - **Sucesso**: 204 No Content
  - **Erro**: 404 Not Found

#### Monitoramento
- `GET /health` - Health check da aplicação
- `GET /swagger` - Documentação OpenAPI

#### Respostas de Erro

Todos os endpoints retornam erros no formato **ProblemDetails** (RFC 7807) com:
- `type`: URI do tipo de erro
- `title`: Resumo do erro
- `status`: Código HTTP
- `detail`: Descrição específica
- `traceId`: Identificador para rastreamento (sempre incluído)

## 🛡️ Tratamento de Erros

A API implementa um sistema robusto de tratamento de erros que combina duas abordagens:

### Result Pattern (Erros de Negócio)
Erros previsíveis de regras de negócio são tratados via **Result Pattern**, sem uso de exceções:
-  Validações de entrada
-  Regras de negócio violadas
-  Recursos não encontrados
-  Retorna status 400 (Bad Request) ou 404 (Not Found)

### Global Exception Filter (Erros Técnicos)
Exceções técnicas inesperadas são capturadas automaticamente por um **Exception Filter centralizado**:
-  Falhas de banco de dados (DbUpdateException) → 500
-  Timeouts de operação (TimeoutException) → 504
-  Erros de comunicação externa (HttpRequestException) → 502
-  Requisições canceladas (TaskCanceledException) → 499
-  Exceções genéricas → 500

### Formato de Resposta (RFC 7807)

Todas as respostas de erro seguem o padrão **ProblemDetails** (RFC 7807):

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Erro ao criar venda",
  "status": 400,
  "detail": "Não é permitido vender mais de 20 unidades do mesmo produto",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00"
}
```

### Códigos de Status HTTP

| Código | Descrição | Cenário |
|--------|-----------|---------|
| **400** | Bad Request | Validação ou regra de negócio violada |
| **404** | Not Found | Recurso não encontrado |
| **499** | Client Closed Request | Cliente cancelou a requisição |
| **500** | Internal Server Error | Erro técnico inesperado |
| **502** | Bad Gateway | Falha em serviço externo |
| **504** | Gateway Timeout | Operação excedeu tempo limite |

### Segurança

- **Produção**: Stack traces e detalhes técnicos **NÃO** são expostos
- **Desenvolvimento**: Stack traces incluídos para facilitar debugging
- **Rastreabilidade**: TraceId incluído em todas as respostas para correlação de logs

### Benefícios

-  **Código limpo**: Endpoints sem blocos try/catch duplicados
-  **Consistência**: Todas as respostas de erro seguem o mesmo formato
-  **Observabilidade**: Logs estruturados com TraceId para rastreamento
-  **Segurança**: Proteção contra vazamento de informações sensíveis
-  **Manutenibilidade**: Tratamento centralizado em um único ponto

## Logs

Os logs são gravados em:
- **Console**: Logs em tempo real durante execução
- **Arquivo**: `logs/123vendas-YYYY-MM-DD.log` (rotação diária, 30 dias de retenção)

Formato estruturado com propriedades JSON para facilitar análise.

### Logging de Exceções

Todas as exceções técnicas são logadas automaticamente com:
-  Tipo da exceção
-  Mensagem de erro
-  Stack trace completo
-  TraceId para correlação
-  Path da requisição HTTP
-  Timestamp

## 🎯 Destaques do Projeto

### Diferenciais Técnicos

-  **218 Testes Automatizados**: Api, Cobertura completa (unitários, aplicação, infraestrutura e integração)
-  **Outbox Pattern**: Garantia de entrega de eventos com consistência transacional
-  **Health Checks**: Monitoramento de saúde (Database, Outbox, Integrações)
-  **Circuit Breaker**: Resiliência em integrações externas com Polly
-  **Idempotência**: Prevenção de duplicação de vendas
-  **Logs Estruturados**: Serilog com JSON e rotação automática
-  **Arquitetura Modular**: Preparado para evolução para microserviços
-  **Domain Events**: Comunicação desacoplada entre módulos
-  **Validações Robustas**: FluentValidation em todos os comandos

### Boas Práticas Aplicadas

-  **Clean Architecture**: Separação clara de responsabilidades
-  **SOLID**: Princípios aplicados em todo o código
-  **DDD**: Domain-Driven Design com entidades ricas
-  **Injeção de Dependência**: Inversão de controle nativa do .NET
-  **Testes Abrangentes**: 218 testes com 100% de aprovação
-  **Tratamento de Erros**: Result Pattern sem exceções de negócio
-  **Código Limpo**: Seguindo Object Calisthenics e Clean Code
-  **Documentação**: Swagger/OpenAPI completo
-  **Versionamento**: Git Flow com commits semânticos

## 📄 Licença

Projeto desenvolvido como teste técnico .NET.
