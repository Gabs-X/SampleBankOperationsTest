using Bogus;
using FluentAssertions;
using Moq;
using SampleBankOperations.App.Services.Operations;
using SampleBankOperations.Application.Interfaces;
using SampleBankOperations.Core.Entities;
using SampleBankOperations.Core.Enums;
using SampleBankOperations.Core.Interfaces;
using Xunit;

namespace SampleBankOperations.Tests;

public class BankOperationsTests
{
    private readonly Mock<IAccountService> _accountServiceMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly BankOperations _bankOperations;
    private readonly Faker _faker;

    public BankOperationsTests()
    {
        _accountServiceMock = new Mock<IAccountService>();
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _loggerMock = new Mock<ILogger>();
        _bankOperations = new BankOperations(
            _accountServiceMock.Object,
            _accountRepositoryMock.Object,
            _loggerMock.Object);
        _faker = new Faker();
    }

    [Fact]
    public void OpenAccount_ShouldAddAccount_WhenInputIsValid()
    {
        // Arrange
        var accountNumber = "123456";
        var initialBalance = 1000m;
        var input = $"{accountNumber}\n{initialBalance}\n";
        Console.SetIn(new StringReader(input));
        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        _bankOperations.OpenAccount();

        // Assert
        _accountRepositoryMock.Verify(r => r.Add(It.Is<Account>(a =>
            a.AccountNumber == accountNumber && a.Balance == initialBalance
        )), Times.Once);

        _loggerMock.Verify(l => l.Log(It.Is<string>(msg =>
            msg.Contains(accountNumber) && msg.Contains($"{initialBalance:C}")
        )), Times.Once);
    }

    [Fact]
    public void OpenAccount_ShouldShowError_WhenBalanceInputIsInvalid()
    {
        // Arrange
        var input = "123456\ninvalid_balance\n";
        Console.SetIn(new StringReader(input));
        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        _bankOperations.OpenAccount();

        // Assert
        var output = sw.ToString().Trim();
        output.Should().Contain("Valor inválido.");
        _accountRepositoryMock.Verify(r => r.Add(It.IsAny<Account>()), Times.Never);
    }

    [Fact]
    public void GetAccountByNumber_ShouldReturnAccount()
    {
        // Arrange
        var accountNumber = "999";
        var expected = new Account(accountNumber, 100, AccountType.Savings);
        _accountRepositoryMock.Setup(r => r.GetByAccountNumber(accountNumber)).Returns(expected);

        // Act
        var result = _bankOperations.GetAccountByNumber(accountNumber);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ViewBalance_ShouldPrintCorrectBalance()
    {
        // Arrange
        var account = new Account("123", 500, AccountType.Checking);
        _accountServiceMock.Setup(s => s.GetBalance(account)).Returns(500);
        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        _bankOperations.ViewBalance(account);

        // Assert
        var output = sw.ToString().Trim();
        output.Should().Contain($"Saldo atual da conta {account.AccountNumber}:");
        output.Should().Contain($"{500:C}");
    }

    [Fact]
    public void Deposit_ShouldExecute_WhenInputIsValid()
    {
        // Arrange
        var account = new Account("111", 0, AccountType.Checking);
        var deposit = 150m;
        Console.SetIn(new StringReader($"{deposit}"));
        using var sw = new StringWriter();
        Console.SetOut(sw);

        _accountServiceMock.Setup(s => s.Deposit(account, deposit, It.IsAny<Action<decimal>>()))
            .Callback<Account, decimal, Action<decimal>>((_, _, cb) => cb(deposit));

        // Act
        _bankOperations.Deposit(account);

        // Assert
        var output = sw.ToString().Trim();
        output.Should().Contain($"Depositado: {deposit:C}");
    }

    [Fact]
    public void Deposit_ShouldShowError_WhenInputInvalid()
    {
        // Arrange
        var account = new Account("111", 0, AccountType.Checking);
        Console.SetIn(new StringReader("abc"));
        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        _bankOperations.Deposit(account);

        // Assert
        var output = sw.ToString().Trim();
        output.Should().Contain("Valor inválido.");
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(9999, false)]
    public void Withdraw_ShouldHandleSuccessAndFailure(decimal amount, bool expected)
    {
        // Arrange
        var account = new Account("222", 1000, AccountType.Checking);
        Console.SetIn(new StringReader($"{amount}"));
        using var sw = new StringWriter();
        Console.SetOut(sw);

        _accountServiceMock.Setup(s =>
            s.Withdraw(account, amount, It.IsAny<Predicate<decimal>>()))
            .Returns(expected);

        // Act
        _bankOperations.Withdraw(account);

        // Assert
        var output = sw.ToString().Trim();
        output.Should().Contain(expected ? $"Sacado: {amount:C}" : "Saldo insuficiente.");
    }

    [Fact]
    public void Withdraw_ShouldShowError_WhenInputInvalid()
    {
        var account = new Account("222", 1000, AccountType.Checking);
        Console.SetIn(new StringReader("xxx"));
        using var sw = new StringWriter();
        Console.SetOut(sw);

        _bankOperations.Withdraw(account);

        var output = sw.ToString().Trim();
        output.Should().Contain("Valor inválido.");
    }

    [Theory]
    [InlineData(250, true)]
    [InlineData(9999, false)]
    public void Transfer_ShouldHandleSuccessAndFailure(decimal amount, bool expected)
    {
        var from = new Account("from", 1000, AccountType.Checking);
        var to = new Account("to", 0, AccountType.Checking);

        Console.SetIn(new StringReader($"{amount}"));
        using var sw = new StringWriter();
        Console.SetOut(sw);

        _accountServiceMock.Setup(s =>
            s.Transfer(from, to, amount,
                It.IsAny<Predicate<decimal>>(),
                It.IsAny<Predicate<decimal>>()))
            .Returns(expected);

        _bankOperations.Transfer(from, to);

        var output = sw.ToString().Trim();
        output.Should().Contain(expected
            ? $"Transferido: {amount:C} da conta {from.AccountNumber} para {to.AccountNumber}"
            : "Falha na transferência por saldo insuficiente.");
    }

    [Fact]
    public void Transfer_ShouldShowError_WhenInputInvalid()
    {
        var from = new Account("from", 1000, AccountType.Checking);
        var to = new Account("to", 0, AccountType.Checking);

        Console.SetIn(new StringReader("abc"));
        using var sw = new StringWriter();
        Console.SetOut(sw);

        _bankOperations.Transfer(from, to);

        var output = sw.ToString().Trim();
        output.Should().Contain("Valor inválido.");
    }

    [Fact]
    public void CalculateInterest_ShouldPrintCorrectInterest_WhenValid()
    {
        var account = new Account("int123", 1000, AccountType.Checking);
        var rate = 5;
        var expectedInterest = 50;

        Console.SetIn(new StringReader($"{rate}"));
        using var sw = new StringWriter();
        Console.SetOut(sw);

        _accountServiceMock.Setup(s =>
            s.CalculateInterest(account, It.IsAny<Func<decimal, decimal, decimal>>(), rate))
            .Returns(expectedInterest);

        _bankOperations.CalculateInterest(account);

        var output = sw.ToString().Trim();
        output.Should().Contain($"Juros calculado: {expectedInterest:C}");
    }

    [Fact]
    public void CalculateInterest_ShouldShowError_WhenRateInvalid()
    {
        var account = new Account("int123", 1000, AccountType.Checking);
        Console.SetIn(new StringReader("abc"));
        using var sw = new StringWriter();
        Console.SetOut(sw);

        _bankOperations.CalculateInterest(account);

        var output = sw.ToString().Trim();
        output.Should().Contain("Taxa inválida.");
    }
}
