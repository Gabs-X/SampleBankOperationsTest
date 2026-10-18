# SampleBankOperations

Aplicação de console em C#/.NET 9 que simula operações bancárias — abertura de conta, consulta de saldo, depósito, saque, transferência entre contas e cálculo de juros — organizada em camadas e coberta por testes automatizados.

## Arquitetura

```text
src/
├── SampleBankOperations.Core            # entidades (Account), enums, interfaces e regras de domínio
├── SampleBankOperations.Application     # serviços (AccountService), DTOs, validações e TransferHelper
├── SampleBankOperations.Infrastructure  # persistência em memória (BankingDbContext, AccountRepository) e logging
└── SampleBankOperations.App             # interface de console e injeção de dependência
test/
├── SampleBankOperations.Core.Tests
├── SampleBankOperations.Application.Tests
└── SampleBankOperations.App.Tests
```

As dependências apontam para dentro: `Core` não conhece nenhuma outra camada, `Application` depende só de `Core`, e `App` monta tudo com `Microsoft.Extensions.DependencyInjection`.

## Destaques

- Regras de saque e transferência recebidas como `Predicate<decimal>`, permitindo trocar a política sem alterar o serviço
- Cálculo de juros injetado como `Func<decimal, decimal, decimal>`
- Transferência valida as contas de origem e destino, verifica saldo e persiste as duas contas em um único ponto (`TransferHelper`)
- Repositório e logger atrás de interfaces, substituídos por mocks nos testes

## Testes

63 testes com **xUnit**, **Moq**, **FluentAssertions** e **Bogus** (dados fictícios), mais cobertura com **coverlet**.

```bash
dotnet test SampleBankOperations.sln
```

Relatório de cobertura gerado com ReportGenerator:

![Relatório de cobertura](Print%20relatorio.png)

## Como executar

Requer o [.NET 9 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/SampleBankOperations.App
```

## Tecnologias

C# · .NET 9 · xUnit · Moq · FluentAssertions · Bogus · coverlet
