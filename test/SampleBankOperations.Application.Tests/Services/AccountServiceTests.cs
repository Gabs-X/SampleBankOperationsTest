using Xunit;
using Moq;
using FluentAssertions;
using Bogus;
using SampleBankOperations.Application.Services;
using SampleBankOperations.Application.Interfaces;
using SampleBankOperations.Core.Entities;
using SampleBankOperations.Core.Interfaces;
using SampleBankOperations.Application.Services.Helpers; 
using System;

namespace SampleBankOperations.Application.Tests.Services; 

public class AccountServiceTests
{
    private readonly Faker _faker = new();
    private readonly Mock<IAccountRepository> _mockAccountRepository;
    private readonly Mock<ILogger> _mockLogger;
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockLogger = new Mock<ILogger>();
        _sut = new AccountService(_mockAccountRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeDependenciesCorrectly()
    {
        var mockRepo = new Mock<IAccountRepository>();
        var mockLogger = new Mock<ILogger>();

        var service = new AccountService(mockRepo.Object, mockLogger.Object);

        service.Should().NotBeNull();
    }

    [Fact]
    public void CalculateInterest_ShouldReturnCalculatedInterestAndLog()
    {
        var account = new Account(_faker.Finance.Account(), _faker.Random.Decimal(100, 1000), Core.Enums.AccountType.Checking);
        decimal rate = 0.05m;
        Func<decimal, decimal, decimal> interestCalculator = (balance, r) => balance * r;
        decimal expectedInterest = account.Balance * rate;

        var result = _sut.CalculateInterest(account, interestCalculator, rate);

        result.Should().Be(expectedInterest);
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains($"Calculated interest: {expectedInterest:C}"))), Times.Once);
    }

    [Theory]
    [InlineData(100, 50)]
    [InlineData(200, 0)]
    public void Deposit_ShouldUpdateAccountBalanceAndLog_WhenAmountIsPositive(decimal initialBalance, decimal depositAmount)
    {
        var account = new Account(_faker.Finance.Account(), initialBalance, Core.Enums.AccountType.Checking);
        Action<decimal> onDeposit = (amount) => { };

        _mockAccountRepository.Setup(r => r.Update(account)).Verifiable();

        _sut.Deposit(account, depositAmount, onDeposit);

        account.Balance.Should().Be(initialBalance + depositAmount);
        _mockAccountRepository.Verify(r => r.Update(account), Times.Once);
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains($"Deposited: {depositAmount:C}"))), Times.Once);
    }

    [Fact]
    public void Deposit_ShouldCallOnDepositAction()
    {
        var account = new Account(_faker.Finance.Account(), 100, Core.Enums.AccountType.Checking);
        decimal depositAmount = 50;
        bool onDepositCalled = false;
        Action<decimal> onDeposit = (amount) => { onDepositCalled = true; };

        _mockAccountRepository.Setup(r => r.Update(account)).Verifiable();

        _sut.Deposit(account, depositAmount, onDeposit);

        onDepositCalled.Should().BeTrue();
    }

    [Theory]
    [InlineData(100, 50, true, true)]
    [InlineData(100, 100, true, true)]
    [InlineData(100, 150, true, false)] // canWithdraw is true, but balance < amount
    [InlineData(100, 50, false, false)] // canWithdraw is false
    public void Withdraw_ShouldReturnExpectedResult_AndUpdateAccountAndLog(decimal initialBalance, decimal withdrawAmount, bool canWithdrawPredicateResult, bool expectedServiceResult)
    {
        var account = new Account(_faker.Finance.Account(), initialBalance, Core.Enums.AccountType.Checking);
        Predicate<decimal> canWithdraw = (balance) => canWithdrawPredicateResult;

        if (expectedServiceResult && canWithdrawPredicateResult && initialBalance >= withdrawAmount)
        {
            _mockAccountRepository.Setup(r => r.Update(account)).Verifiable();
        }

        var result = _sut.Withdraw(account, withdrawAmount, canWithdraw);

        result.Should().Be(expectedServiceResult);

        if (expectedServiceResult)
        {
            account.Balance.Should().Be(initialBalance - withdrawAmount);
            _mockAccountRepository.Verify(r => r.Update(account), Times.Once);
            _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains($"Withdraw: {withdrawAmount:C}"))), Times.Once);
        }
        else
        {
            account.Balance.Should().Be(initialBalance); // Balance should not change
            _mockAccountRepository.Verify(r => r.Update(It.IsAny<Account>()), Times.Never); // Update should not be called
            _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains($"Failed to withdraw: {withdrawAmount:C}"))), Times.Once);
        }
    }

    [Fact]
    public void GetBalance_ShouldReturnBalance_WhenAccountExists()
    {
        var expectedAccount = new Account(_faker.Finance.Account(), _faker.Random.Decimal(100, 1000), Core.Enums.AccountType.Checking);
        var accountIdToSearch = expectedAccount.AccountId;

        _mockAccountRepository.Setup(r => r.GetById(accountIdToSearch)).Returns(expectedAccount);

        var result = _sut.GetBalance(expectedAccount);

        result.Should().Be(expectedAccount.Balance);
        _mockAccountRepository.Verify(r => r.GetById(accountIdToSearch), Times.Once);
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains($"Checked balance for account {expectedAccount.AccountNumber}"))), Times.Once);
    }

    [Fact]
    public void GetBalance_ShouldReturnZeroAndLog_WhenAccountDoesNotExist()
    {
        var nonExistentAccount = new Account(_faker.Finance.Account(), 0, Core.Enums.AccountType.Checking);
        var accountIdToSearch = nonExistentAccount.AccountId;

        _mockAccountRepository.Setup(r => r.GetById(accountIdToSearch)).Returns((Account)null);

        var result = _sut.GetBalance(nonExistentAccount);

        result.Should().Be(0);
        _mockAccountRepository.Verify(r => r.GetById(accountIdToSearch), Times.Once);
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains($"Account {nonExistentAccount.AccountNumber} not found."))), Times.Once);
    }

    [Fact]
    public void Transfer_ShouldReturnTrueAndExecuteTransfer_WhenConditionsMet()
    {
        var fromAccount = new Account(_faker.Finance.Account(), 200, Core.Enums.AccountType.Checking);
        var toAccount = new Account(_faker.Finance.Account(), 50, Core.Enums.AccountType.Savings);
        decimal amount = 100;

        _mockAccountRepository.Setup(r => r.GetById(fromAccount.AccountId)).Returns(fromAccount);
        _mockAccountRepository.Setup(r => r.GetById(toAccount.AccountId)).Returns(toAccount);

        Predicate<decimal> canWithdraw = (balance) => true;
        Predicate<decimal> canTransfer = (balance) => true;

        // Note: As _transferHelper é instanciada internamente, não podemos mocká-la diretamente aqui.
        // Estamos testando a AccountService como um todo, incluindo sua interação com TransferHelper.
        // Os Verify calls serão para IAccountRepository e ILogger, que são as dependências de TransferHelper também.

        var result = _sut.Transfer(fromAccount, toAccount, amount, canWithdraw, canTransfer);

        result.Should().BeTrue();
        fromAccount.Balance.Should().Be(100); // 200 - 100
        toAccount.Balance.Should().Be(150);   // 50 + 100

        // As chamadas a Update são feitas dentro de TransferHelper.ExecuteTransfer
        _mockAccountRepository.Verify(r => r.Update(fromAccount), Times.Once);
        _mockAccountRepository.Verify(r => r.Update(toAccount), Times.Once);
        _mockLogger.Verify(l => l.Log(It.IsAny<string>()), Times.AtLeastOnce); // Verify any logging by AccountService or TransferHelper
    }

    [Fact]
    public void Transfer_ShouldReturnFalse_WhenSourceAccountNotFound()
    {
        var fromAccount = new Account(_faker.Finance.Account(), 200, Core.Enums.AccountType.Checking);
        var toAccount = new Account(_faker.Finance.Account(), 50, Core.Enums.AccountType.Savings);
        decimal amount = 100;

        _mockAccountRepository.Setup(r => r.GetById(fromAccount.AccountId)).Returns((Account)null); // Source not found
        _mockAccountRepository.Setup(r => r.GetById(toAccount.AccountId)).Returns(toAccount);

        Predicate<decimal> canWithdraw = (balance) => true;
        Predicate<decimal> canTransfer = (balance) => true;

        var result = _sut.Transfer(fromAccount, toAccount, amount, canWithdraw, canTransfer);

        result.Should().BeFalse();
        fromAccount.Balance.Should().Be(200); // Balance should not change
        toAccount.Balance.Should().Be(50);   // Balance should not change
        _mockAccountRepository.Verify(r => r.Update(It.IsAny<Account>()), Times.Never);
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains($"Source account {fromAccount.AccountNumber} not found."))), Times.Once);
    }

    [Fact]
    public void Transfer_ShouldReturnFalse_WhenDestinationAccountNotFound()
    {
        var fromAccount = new Account(_faker.Finance.Account(), 200, Core.Enums.AccountType.Checking);
        var toAccount = new Account(_faker.Finance.Account(), 50, Core.Enums.AccountType.Savings);
        decimal amount = 100;

        _mockAccountRepository.Setup(r => r.GetById(fromAccount.AccountId)).Returns(fromAccount);
        _mockAccountRepository.Setup(r => r.GetById(toAccount.AccountId)).Returns((Account)null); // Destination not found

        Predicate<decimal> canWithdraw = (balance) => true;
        Predicate<decimal> canTransfer = (balance) => true;

        var result = _sut.Transfer(fromAccount, toAccount, amount, canWithdraw, canTransfer);

        result.Should().BeFalse();
        fromAccount.Balance.Should().Be(200);
        toAccount.Balance.Should().Be(50);
        _mockAccountRepository.Verify(r => r.Update(It.IsAny<Account>()), Times.Never);
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains($"Destination account {toAccount.AccountNumber} not found."))), Times.Once);
    }

    [Fact]
    public void Transfer_ShouldReturnFalse_WhenInsufficientBalance()
    {
        var fromAccount = new Account(_faker.Finance.Account(), 50, Core.Enums.AccountType.Checking); // Insufficient balance
        var toAccount = new Account(_faker.Finance.Account(), 50, Core.Enums.AccountType.Savings);
        decimal amount = 100;

        _mockAccountRepository.Setup(r => r.GetById(fromAccount.AccountId)).Returns(fromAccount);
        _mockAccountRepository.Setup(r => r.GetById(toAccount.AccountId)).Returns(toAccount);

        Predicate<decimal> canWithdraw = (balance) => false; // Predicate says no
        Predicate<decimal> canTransfer = (balance) => true;

        var result = _sut.Transfer(fromAccount, toAccount, amount, canWithdraw, canTransfer);

        result.Should().BeFalse();
        fromAccount.Balance.Should().Be(50);
        toAccount.Balance.Should().Be(50);
        _mockAccountRepository.Verify(r => r.Update(It.IsAny<Account>()), Times.Never);
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Insufficient balance in account"))), Times.Once); // Log from TransferHelper
    }
}