# PaymentCore Transaction Processor 💳

Sistema robusto de processamento de transações financeiras construído com .NET 9, seguindo princípios de Clean Architecture, Domain-Driven Design (DDD), e preparado para arquitetura de microsserviços.

## 📋 Sobre o Projeto

O **PaymentCore** é uma API RESTful para processamento de transações financeiras que oferece:

- ✅ Gerenciamento de contas bancárias com saldo, reservas e limites de crédito
- ✅ Processamento de múltiplos tipos de transações (crédito, débito, reserva, captura, estorno, transferência)
- ✅ Sistema de idempotência para evitar transações duplicadas
- ✅ Controle de concorrência otimista para garantir consistência de dados
- ✅ Processamento assíncrono de eventos em background
- ✅ Paginação em endpoints de listagem
- ✅ Lock distribuído para operações críticas em contas

## 🏗️ Arquitetura

O projeto segue os princípios de **Clean Architecture** e **Onion Architecture**:

```
┌─────────────────────────────────────┐
│         API Layer (Web)             │  ← Controllers, Middleware
├─────────────────────────────────────┤
│     Application Layer               │  ← DTOs, Services, Interfaces
├─────────────────────────────────────┤
│     Domain Layer (Core)             │  ← Entities, Enums, Events
├─────────────────────────────────────┤
│   Infrastructure Layer              │  ← EF Core, Repositories, Services
└─────────────────────────────────────┘
```

**Benefícios:**
- Separação clara de responsabilidades
- Testabilidade com inversão de dependências
- Independência de frameworks externos
- Facilidade de manutenção e evolução

## 🚀 Tecnologias

- **.NET 9** - Framework principal
- **ASP.NET Core Web API** - Construção da API REST
- **Entity Framework Core 9** - ORM e gerenciamento de dados
- **SQL Server** - Banco de dados relacional
- **Swagger/OpenAPI** - Documentação interativa da API
- **Background Services** - Processamento assíncrono de eventos
- **Account Lock Service** - Sincronização de operações críticas

## 📦 Estrutura do Projeto

```
PaymentCore.TransactionProcessor/
├── src/
│   ├── PaymentCore.Domain/              # Camada de Domínio
│   │   ├── Entities/                    # Account, Transaction
│   │   ├── Enums/                       # OperationType, TransactionStatus, AccountStatus
│   │   ├── Events/                      # DomainEvent, TransactionProcessedEvent
│   │   └── Interfaces/                  # IAccountRepository, ITransactionRepository
│   │
│   ├── PaymentCore.Application/         # Camada de Aplicação
│   │   ├── DTOs/                        # AccountDtos, TransactionDtos, PaginationRequest
│   │   ├── Services/                    # AccountService, TransactionService
│   │   └── Interfaces/                  # IAccountService, ITransactionService
│   │
│   ├── PaymentCore.Infrastructure/      # Camada de Infraestrutura
│   │   ├── Persistence/                 # ApplicationDbContext
│   │   ├── Repositories/                # AccountRepository, TransactionRepository
│   │   ├── Services/                    # InMemoryEventPublisher, AccountLockService
│   │   ├── BackgroundServices/          # EventProcessorBackgroundService
│   │   └── Migrations/                  # Migrações do EF Core
│   │
│   └── PaymentCore.API/                 # Camada de Apresentação
│       ├── Controllers/                 # AccountsController, TransactionsController
│       ├── Middleware/                  # GlobalExceptionMiddleware
│       └── Program.cs                   # Configuração da aplicação
│
└── tests/
    ├── PaymentCore.UnitTests/           # Testes unitários
    └── PaymentCore.IntegrationTests/    # Testes de integração
```

## 💼 Domínio

### 📊 Account (Conta)
Representa uma conta bancária no sistema com:

- **Id** - Identificador único (GUID)
- **ExternalId** - Identificador externo (cliente)
- **Balance** - Saldo disponível em reais
- **ReservedBalance** - Saldo reservado (bloqueado) em reais
- **AvailableBalance** - Saldo disponível (Balance - ReservedBalance)
- **CreditLimit** - Limite de crédito da conta
- **Status** - Status da conta (Active, Suspended, Closed)
- **RowVersion** - Controle de concorrência otimista

### 💸 Transaction (Transação)
Representa uma operação financeira:

- **ReferenceId** - Identificador único para idempotência
- **OperationType** - Tipo de operação (Credit, Debit, Reserve, Capture, Transfer, Reversal)
- **Amount** - Valor da transação em centavos
- **AccountId** - Conta de origem
- **TargetAccountId** - Conta de destino (para transferências)
- **Status** - Status da transação (Pending, Completed, Failed, Reversed)
- **Timestamp** - Data/hora da transação

### Tipos de Operação

| Operação   | Descrição                                    |
|------------|----------------------------------------------|
| **Credit** | Adiciona saldo à conta (depósito)           |
| **Debit**  | Remove saldo da conta (compra/saque)        |
| **Reserve**| Reserva saldo (pré-autorização)             |
| **Capture**| Captura uma reserva existente               |
| **Transfer**| Transfere saldo entre contas               |
| **Reversal**| Estorna uma transação existente            |

## 🔒 Funcionalidades de Segurança

### Idempotência
Todas as transações usam `ReferenceId` único para garantir que operações duplicadas não sejam processadas.

### Controle de Concorrência
- Lock por `AccountId` usando SemaphoreSlim
- RowVersion para concorrência otimista no banco
- Transações atômicas no SQL Server

### Eventos Assíncronos
Sistema de eventos para processamento assíncrono:
- `TransactionProcessedEvent`
- Background worker para consumo de eventos
- Channel-based in-memory event publisher
- Preparado para integração com Message Brokers (RabbitMQ, Kafka)

## 🚀 Como Executar

### Pré-requisitos
- .NET 9 SDK
- SQL Server (LocalDB ou SQL Server Express)

### Executar Localmente

```bash
# Clonar o repositório
git clone <repository-url>
cd PaymentCore.TransactionProcessor

# Restaurar pacotes
dotnet restore

# Configurar connection string no appsettings.Development.json
# DefaultConnection: "Server=localhost;Database=PaymentCore;..."

# Aplicar migrations
cd src/PaymentCore.API
dotnet ef database update --project ../PaymentCore.Infrastructure

# Executar API
dotnet run

# Ou executar com watch mode (rebuild automático)
dotnet watch run
```

A API estará disponível em: **http://localhost:5009**

## 📚 API Endpoints

### Swagger UI
Acesse a documentação interativa: **http://localhost:5009/swagger**

### � Autenticação (Novo!)

Todos os endpoints de Accounts e Transactions agora requerem autenticação JWT.

#### Registrar Novo Usuário
```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "joao_silva",
  "email": "joao@email.com",
  "password": "senha123",
  "role": "User"
}
```

**Resposta:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresAt": "2026-02-13T10:00:00Z",
  "user": {
    "id": "guid",
    "username": "joao_silva",
    "email": "joao@email.com",
    "role": "User"
  }
}
```

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "usernameOrEmail": "joao_silva",
  "password": "senha123"
}
```

#### Verificar Usuário Autenticado
```http
GET /api/auth/me
Authorization: Bearer {seu_token}
```

#### Usando o Token em Requisições

Para acessar endpoints protegidos, inclua o token JWT no header:

```http
GET /api/accounts
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**No Swagger UI:**
1. Clique no botão **Authorize** 🔒
2. Digite: `Bearer {seu_token}`
3. Clique em **Authorize**

💡 **Nota:** O token expira em 24 horas. Após expirar, faça login novamente.

### �📊 Accounts (Contas)

#### Criar Conta
```http
POST /api/accounts
Content-Type: application/json

{
  "externalId": "CLIENTE-001",
  "creditLimit": 5000.00
}
```

#### Listar Contas (com Paginação)
```http
GET /api/accounts?pageNumber=1&pageSize=10
```

**Resposta:**
```json
{
  "items": [
    {
      "id": "guid",
      "externalId": "CLIENTE-001",
      "balance": 1000.00,
      "reservedBalance": 0.00,
      "availableBalance": 1000.00,
      "creditLimit": 5000.00,
      "status": "Active",
      "createdAt": "2026-02-12T10:00:00Z",
      "updatedAt": "2026-02-12T10:00:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 50,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

#### Consultar Conta por ID
```http
GET /api/accounts/{id}
```

### 💸 Transactions (Transações)

#### Listar Transações (com Paginação)
```http
GET /api/transactions?pageNumber=1&pageSize=10
```

**Resposta:**
```json
{
  "items": [
    {
      "transactionId": "guid",
      "status": "success",
      "balance": 100000,
      "reservedBalance": 0,
      "availableBalance": 100000,
      "timestamp": "2026-02-12T10:00:00Z",
      "errorMessage": null
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 150,
  "totalPages": 15,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

#### Processar Transação - Crédito (Depósito)
```http
POST /api/transactions
Content-Type: application/json

{
  "operation": "credit",
  "account_id": "85d7a1d9-68b4-4e62-8957-556adbf8d996",
  "amount": 100000,
  "currency": "BRL",
  "reference_id": "DEP-001-20260212123045",
  "metadata": {
    "description": "Depósito inicial"
  }
}
```
💡 **Nota:** O valor é em centavos (100000 = R$ 1.000,00)

#### Processar Transação - Débito (Compra)
```http
POST /api/transactions
Content-Type: application/json

{
  "operation": "debit",
  "account_id": "85d7a1d9-68b4-4e62-8957-556adbf8d996",
  "amount": 15000,
  "currency": "BRL",
  "reference_id": "COMPRA-001-20260212123046"
}
```

#### Processar Transação - Transferência
```http
POST /api/transactions
Content-Type: application/json

{
  "operation": "transfer",
  "account_id": "85d7a1d9-68b4-4e62-8957-556adbf8d996",
  "amount": 10000,
  "currency": "BRL",
  "reference_id": "TRF-001-20260212123047",
  "target_account_id": "13edef1a-ee42-49c6-be4d-7b0e4ae3ba15"
}
```

#### Consultar Transação por ID
```http
GET /api/transactions/{id}
```

### Health Check
```http
GET /health
```

## 🧪 Testes

```bash
# Executar todos os testes
dotnet test

# Executar apenas testes unitários
dotnet test tests/PaymentCore.UnitTests

# Executar testes de integração
dotnet test tests/PaymentCore.IntegrationTests

# Executar com cobertura
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 🗄️ Migrations

```bash
# Criar nova migration
dotnet ef migrations add MigrationName --project src/PaymentCore.Infrastructure --startup-project src/PaymentCore.API

# Aplicar migrations
dotnet ef database update --project src/PaymentCore.Infrastructure --startup-project src/PaymentCore.API

# Reverter migration
dotnet ef database update PreviousMigrationName --project src/PaymentCore.Infrastructure --startup-project src/PaymentCore.API

# Remover última migration (não aplicada)
dotnet ef migrations remove --project src/PaymentCore.Infrastructure --startup-project src/PaymentCore.API
```

## 📊 Decisões Arquiteturais

### Clean Architecture
A aplicação é dividida em camadas com dependências unidirecionais:
- **Domain**: Núcleo da aplicação, sem dependências externas
- **Application**: Casos de uso e interfaces
- **Infrastructure**: Implementações de infraestrutura (EF Core, repositórios)
- **API**: Camada de apresentação (Controllers, Middleware)

### Controle de Concorrência
- **Optimistic Concurrency**: RowVersion no Entity Framework
- **Pessimistic Locking**: SemaphoreSlim para lock de contas
- **Ordering**: Locks ordenados por AccountId para evitar deadlocks

### Idempotência
- ReferenceId único por transação
- Verificação antes de processar
- Retorno da transação existente em caso de duplicata

### Eventos e Background Processing
- Channel-based event publisher (in-memory)
- Background service para processar eventos
- Preparado para migração para Message Brokers

### Retry e Resiliência
- Polly para retry com exponential backoff
- Transações do banco com suporte a rollback
- Logging estruturado de erros

## 🔧 Configuração

### appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PaymentCore;User Id=seu_usuario;Password=sua_senha;TrustServerCertificate=True;Connection Timeout=30;"
  }
}
```

### Variáveis de Ambiente
- `ASPNETCORE_ENVIRONMENT`: Development, Staging, Production
- `ConnectionStrings__DefaultConnection`: Connection string do SQL Server
- `ASPNETCORE_URLS`: URLs de binding (padrão: http://localhost:5009)

## 🔐 Segurança

- Middleware global de tratamento de exceções
- Validação de dados de entrada
- Transações atômicas no banco
- Logs estruturados (sem dados sensíveis)
- Health checks configurados
- Usuário não-root no Docker

## 📈 Próximos Passos / Roadmap

- [x] **Implementar autenticação e autorização (JWT/OAuth2)** ✅
- [x] **Adicionar rate limiting por cliente**
- [x] **Integrar com Message Broker (RabbitMQ/Kafka) para eventos**
- [x] **Implementar padrão CQRS**
- [x] **Adicionar cache distribuído (Redis)**
- [x] **Adicionar filtros avançados nos endpoints de listagem**
- [x] **Implementar soft delete para registros**
- [x] **Métricas e observabilidade (Prometheus, Grafana)**
- [x] **Tracing distribuído (OpenTelemetry/Jaeger)**
- [x] **Implementar audit log para transações**
- [ ] CI/CD Pipeline (GitHub Actions/Azure DevOps)
- [ ] Containerização completa (Docker/Kubernetes)
- [ ] Webhooks para notificação de eventos
- [ ] Dashboard administrativo

## ✨ Características Principais

### ✅ Implementado
- Clean Architecture com separação de camadas
- **Autenticação e autorização JWT** 🆕
- **Sistema completo de usuários e roles** 🆕
- **Proteção de endpoints com [Authorize]** 🆕
- **Paginação em endpoints de listagem**
- **Controle de concorrência otimista e pessimista**
- **Sistema de idempotência com `reference_id`**
- **Processamento assíncrono de eventos**
- **Múltiplos tipos de operações financeiras**
- **Lock distribuído para operações críticas**
- **Tratamento global de exceções**
- **Documentação Swagger com suporte JWT**
- **Health checks**
- **Migrações automáticas do banco**
- **Hashing seguro de senhas (PBKDF2)**

### 🚧 Em Desenvolvimento
- Rate limiting por cliente
- Message Broker integration
- Cache distribuído

## 📝 Licença

Este projeto é um exemplo educacional.

## 👥 Contribuindo

Contribuições são bem-vindas! Por favor:
1. Faça um Fork do projeto
2. Crie uma branch para sua feature (`git checkout -b feature/MinhaFeature`)
3. Commit suas mudanças (`git commit -m 'Adiciona MinhaFeature'`)
4. Push para a branch (`git push origin feature/MinhaFeature`)
5. Abra um Pull Request

## 📞 Suporte

Para dúvidas, sugestões ou reportar problemas, abra uma issue no repositório.

---

⭐ **Desenvolvido com .NET 9 e Clean Architecture**
