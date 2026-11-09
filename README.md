# SampleBankOperations

[Português](#português) · [English](#english)

![.NET 9](https://img.shields.io/badge/.NET-9-512BD4) ![testes](https://img.shields.io/badge/testes-80%20passando-2ea44f) ![cobertura](https://img.shields.io/badge/cobertura%20de%20linhas-89%25-2ea44f)

---

## Português

Aplicação de console em C#/.NET 9 que simula operações bancárias (abertura de conta, saldo, depósito, saque, transferência e cálculo de juros), organizada em camadas e coberta por testes automatizados.

> **Origem:** a aplicação base é o projeto [rfulgencio3/SampleBankOperations](https://github.com/rfulgencio3/SampleBankOperations) (MIT). Neste repositório o foco é a **suíte de testes**: projetos de teste separados por camada (80 testes), relatório de cobertura e correções encontradas pelos testes (transferência com conta inexistente agora retorna `false`, as contas são persistidas uma única vez na transferência e o menu termina sem `Environment.Exit`).

### Arquitetura em camadas

```text
src/
├── SampleBankOperations.Core            # entidade Account, enum AccountType, interfaces (IRepository, IAccountRepository, ILogger)
├── SampleBankOperations.Application     # AccountService, TransferHelper, AccountValidator, DTOs
├── SampleBankOperations.Infrastructure  # persistência em memória (BankingDbContext, AccountRepository) e Logger
└── SampleBankOperations.App             # menu de console, BankOperations e injeção de dependência
test/
├── SampleBankOperations.Core.Tests
├── SampleBankOperations.Application.Tests
├── SampleBankOperations.Infrastructure.Tests
└── SampleBankOperations.App.Tests
```

- As dependências apontam para dentro: `Core` não conhece nenhuma outra camada; `Application` depende só de `Core`; `Infrastructure` implementa as interfaces de `Core`.
- `App` é a raiz de composição: registra tudo com `Microsoft.Extensions.DependencyInjection`.
- Repositório e logger ficam atrás de interfaces e são substituídos por mocks (Moq) nos testes.

### Regras de negócio

| Operação | Regra |
| --- | --- |
| Abrir conta | Número da conta e saldo inicial informados pelo usuário; a conta recebe um `Guid` como identificador. |
| Saque | Só acontece se a política recebida (`Predicate<decimal>`) aprovar o saldo **e** o valor for menor ou igual ao saldo. Caso contrário, retorna `false` e registra a falha no log. |
| Transferência | Origem e destino precisam existir no repositório; o saldo da origem precisa atender às duas políticas (saldo mínimo e valor solicitado). O débito e o crédito são persistidos em um único ponto (`TransferHelper.ExecuteTransfer`). Conta inexistente ou saldo insuficiente retornam `false`. |
| Juros | Fórmula injetada como `Func<decimal, decimal, decimal>`; no app, juros simples `saldo × taxa / 100`. O valor é calculado e exibido, não creditado. |
| Saldo | Consultado no repositório; conta inexistente retorna `0` e registra no log. |

As políticas de saque e transferência vêm de `AccountValidator` (`MinimumBalanceValidator`, `RequestedAmountValidator`), então dá para trocar a regra sem alterar o serviço.

### Como rodar

Requer o [.NET 9 SDK](https://dotnet.microsoft.com/download).

```bash
# aplicação de console
dotnet run --project src/SampleBankOperations.App

# testes
dotnet test SampleBankOperations.sln
```

### Testes e cobertura

80 testes com **xUnit**, **Moq**, **FluentAssertions** e **Bogus** (dados fictícios), divididos por camada.

| Camada | Cobertura de linhas |
| --- | --- |
| Core | 77,7% |
| Application | 96,5% |
| Infrastructure | 100% |
| App | 85,3% |
| **Total** | **89,3%** (ramos: 88,6%) |

Para gerar o relatório HTML localmente (ele não é versionado):

```bash
dotnet test SampleBankOperations.sln --collect:"XPlat Code Coverage" --results-directory ./TestResults
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"./TestResults/*/coverage.cobertura.xml" -targetdir:"./coveragereport" -reporttypes:Html
```

### Limitações conhecidas

- Persistência só em memória: os dados se perdem ao fechar o app.
- Depósito e saque não rejeitam valores negativos ou zero.

### Tecnologias

C# · .NET 9 · Microsoft.Extensions.DependencyInjection · xUnit · Moq · FluentAssertions · Bogus · coverlet · ReportGenerator

---

## English

C#/.NET 9 console application that simulates banking operations (account opening, balance, deposit, withdrawal, transfer and interest calculation), organized in layers and covered by automated tests.

> **Origin:** the base application is [rfulgencio3/SampleBankOperations](https://github.com/rfulgencio3/SampleBankOperations) (MIT). This repository focuses on the **test suite**: one test project per layer (80 tests), a coverage report and fixes uncovered by the tests (transfer with a missing account now returns `false`, accounts are persisted only once per transfer and the menu exits without `Environment.Exit`).

### Layered architecture

```text
src/
├── SampleBankOperations.Core            # Account entity, AccountType enum, interfaces (IRepository, IAccountRepository, ILogger)
├── SampleBankOperations.Application     # AccountService, TransferHelper, AccountValidator, DTOs
├── SampleBankOperations.Infrastructure  # in-memory persistence (BankingDbContext, AccountRepository) and Logger
└── SampleBankOperations.App             # console menu, BankOperations and dependency injection
test/
├── SampleBankOperations.Core.Tests
├── SampleBankOperations.Application.Tests
├── SampleBankOperations.Infrastructure.Tests
└── SampleBankOperations.App.Tests
```

- Dependencies point inwards: `Core` knows no other layer; `Application` depends only on `Core`; `Infrastructure` implements the `Core` interfaces.
- `App` is the composition root and wires everything with `Microsoft.Extensions.DependencyInjection`.
- Repository and logger sit behind interfaces and are replaced by mocks (Moq) in tests.

### Business rules

| Operation | Rule |
| --- | --- |
| Open account | Account number and initial balance come from the user; the account gets a `Guid` identifier. |
| Withdrawal | Only happens if the injected policy (`Predicate<decimal>`) approves the balance **and** the amount is less than or equal to the balance. Otherwise it returns `false` and logs the failure. |
| Transfer | Source and destination must exist in the repository; the source balance must satisfy both policies (minimum balance and requested amount). Debit and credit are persisted in a single place (`TransferHelper.ExecuteTransfer`). A missing account or insufficient balance returns `false`. |
| Interest | Formula injected as `Func<decimal, decimal, decimal>`; the app uses simple interest `balance × rate / 100`. The value is calculated and displayed, not credited. |
| Balance | Read from the repository; a missing account returns `0` and is logged. |

Withdrawal and transfer policies come from `AccountValidator` (`MinimumBalanceValidator`, `RequestedAmountValidator`), so the rule can change without touching the service.

### How to run

Requires the [.NET 9 SDK](https://dotnet.microsoft.com/download).

```bash
# console app
dotnet run --project src/SampleBankOperations.App

# tests
dotnet test SampleBankOperations.sln
```

### Tests and coverage

80 tests with **xUnit**, **Moq**, **FluentAssertions** and **Bogus** (fake data), split by layer.

| Layer | Line coverage |
| --- | --- |
| Core | 77.7% |
| Application | 96.5% |
| Infrastructure | 100% |
| App | 85.3% |
| **Total** | **89.3%** (branches: 88.6%) |

To generate the HTML report locally (it is not versioned):

```bash
dotnet test SampleBankOperations.sln --collect:"XPlat Code Coverage" --results-directory ./TestResults
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"./TestResults/*/coverage.cobertura.xml" -targetdir:"./coveragereport" -reporttypes:Html
```

### Known limitations

- In-memory persistence only: data is lost when the app closes.
- Deposit and withdrawal do not reject negative or zero amounts.

### Tech stack

C# · .NET 9 · Microsoft.Extensions.DependencyInjection · xUnit · Moq · FluentAssertions · Bogus · coverlet · ReportGenerator

## Licença / License

[MIT](LICENSE.txt). Aplicação base / base application: © rfulgencio3.
