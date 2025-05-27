using Xunit;
using Moq;
using FluentAssertions;
using Bogus;
using SampleBankOperations.Application.Services.Helpers;
using SampleBankOperations.Core.Entities;
using SampleBankOperations.Core.Interfaces;
using System;

namespace SampleBankOperations.Application.Tests.Helpers; 
public class TransferHelperTests
{
    private readonly Faker _faker = new();
    private readonly Mock<IAccountRepository> _mockAccountRepository;
    private readonly Mock<ILogger> _mockLogger;
    private readonly TransferHelper _sut; 

    public TransferHelperTests()
    {
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockLogger = new Mock<ILogger>();
        _sut = new TransferHelper(_mockAccountRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeDependenciesCorrectly()
    {
        var mockRepo = new Mock<IAccountRepository>();
        var mockLogger = new Mock<ILogger>();

        var helper = new TransferHelper(mockRepo.Object, mockLogger.Object);

        helper.Should().NotBeNull();
    }

    [Fact]
    public void GetValidAccount_ShouldReturnAccount_WhenAccountExists()
    {
        var account = new Account(_faker.Finance.Account(), _faker.Random.Decimal(100, 1000), Core.Enums.AccountType.Checking);
        var expectedAccount = account;
        string accountType = "Source";

        _mockAccountRepository.Setup(r => r.GetById(account.AccountId)).Returns(expectedAccount);

        var result = _sut.GetValidAccount(account, accountType);

        result.Should().Be(expectedAccount);
        _mockAccountRepository.Verify(r => r.GetById(account.AccountId), Times.Once);
        _mockLogger.Verify(l => l.Log(It.IsAny<string>()), Times.Never); // No log on success
    }

    [Fact]
    public void GetValidAccount_ShouldThrowInvalidOperationExceptionAndLog_WhenAccountDoesNotExist()
    {
        var account = new Account(_faker.Finance.Account(), _faker.Random.Decimal(100, 1000), Core.Enums.AccountType.Checking);
        string accountType = "Source";

        _mockAccountRepository.Setup(r => r.GetById(account.AccountId)).Returns((Account)null);

        Action act = () => _sut.GetValidAccount(account, accountType);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"{accountType} account not found.");
        _mockAccountRepository.Verify(l => l.GetById(account.AccountId), Times.Once);
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains($"{accountType} account {account.AccountNumber} not found."))), Times.Once);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void HasSufficientBalance_ShouldReturnCorrectResultAndLog(bool canWithdrawResult, bool canTransferResult, bool expectedResult)
    {
        var account = new Account(_faker.Finance.Account(), 500, Core.Enums.AccountType.Checking);
        decimal amount = 100;
        Predicate<decimal> canWithdraw = (balance) => canWithdrawResult;
        Predicate<decimal> canTransfer = (balance) => canTransferResult;

        var result = _sut.HasSufficientBalance(account, amount, canWithdraw, canTransfer);

        result.Should().Be(expectedResult);

        if (!expectedResult)
        {
            _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains($"Insufficient balance in account {account.AccountNumber} for transfer of {amount:C}."))), Times.Once);
        }
        else
        {
            _mockLogger.Verify(l => l.Log(It.IsAny<string>()), Times.Never); 
        }
    }

    [Fact]
    public void ExecuteTransfer_ShouldCallActionsAndUpdateRepositoryAndLog()
    {
        var fromAccount = new Account(_faker.Finance.Account(), 500, Core.Enums.AccountType.Checking);
        var toAccount = new Account(_faker.Finance.Account(), 100, Core.Enums.AccountType.Savings);
        decimal amount = 100;

        bool withdrawActionCalled = false;
        bool depositActionCalled = false;


        Action<decimal> withdrawAction = (amt) => {
            fromAccount.Withdraw(amt);
            withdrawActionCalled = true;
        };
        Action<decimal> depositAction = (amt) => {
            toAccount.Deposit(amt);
            depositActionCalled = true;
        };

        _mockAccountRepository.Setup(r => r.Update(fromAccount)).Verifiable();
        _mockAccountRepository.Setup(r => r.Update(toAccount)).Verifiable();

        _sut.ExecuteTransfer(fromAccount, toAccount, amount, withdrawAction, depositAction);

        withdrawActionCalled.Should().BeTrue();
        depositActionCalled.Should().BeTrue();
        fromAccount.Balance.Should().Be(400); // 500 - 100
        toAccount.Balance.Should().Be(200);   // 100 + 100
        _mockAccountRepository.Verify(r => r.Update(fromAccount), Times.Once);
        _mockAccountRepository.Verify(r => r.Update(toAccount), Times.Once);
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains($"Transferred {amount:C} from account {fromAccount.AccountNumber} to account {toAccount.AccountNumber}."))), Times.Once);
    }
}