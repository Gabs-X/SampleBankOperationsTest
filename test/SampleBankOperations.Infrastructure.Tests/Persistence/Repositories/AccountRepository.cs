using SampleBankOperations.Core.Entities;
using SampleBankOperations.Core.Enums;
using SampleBankOperations.Infrastructure.Persistence;
using SampleBankOperations.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SampleBankOperations.Tests.Infrastructure.Persistence
{
    public class AccountRepositoryTests
    {
        private readonly BankingDbContext _context;
        private readonly AccountRepository _repository;

        public AccountRepositoryTests()
        {
            _context = new BankingDbContext();
            _repository = new AccountRepository(_context);
        }

        [Fact]
        public void Add_ShouldAddAccountToContext()
        {
            // Arrange
            var account = CreateAccount();

            // Act
            _repository.Add(account);

            // Assert
            Assert.True(_context.Accounts.ContainsKey(account.AccountId));
        }

        [Fact]
        public void GetById_ShouldReturnCorrectAccount()
        {
            // Arrange
            var account = CreateAccount();
            _context.Accounts[account.AccountId] = account;

            // Act
            var result = _repository.GetById(account.AccountId);

            // Assert
            Assert.Equal(account, result);
        }

        [Fact]
        public void GetById_ShouldReturnNull_WhenNotFound()
        {
            // Act
            var result = _repository.GetById(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetAll_ShouldReturnAllAccounts()
        {
            // Arrange
            var account1 = CreateAccount();
            var account2 = CreateAccount();
            _context.Accounts[account1.AccountId] = account1;
            _context.Accounts[account2.AccountId] = account2;

            // Act
            var allAccounts = _repository.GetAll().ToList();

            // Assert
            Assert.Contains(account1, allAccounts);
            Assert.Contains(account2, allAccounts);
            Assert.Equal(2, allAccounts.Count);
        }

        [Fact]
        public void Update_ShouldModifyExistingAccount()
        {
            // Arrange
            var account = CreateAccount();
            _context.Accounts[account.AccountId] = account;
            account.Deposit(100);

            // Act
            _repository.Update(account);

            // Assert
            var updated = _context.Accounts[account.AccountId];
            Assert.Equal(account.Balance, updated.Balance);
        }

        [Fact]
        public void Remove_ShouldDeleteAccount()
        {
            // Arrange
            var account = CreateAccount();
            _context.Accounts[account.AccountId] = account;

            // Act
            _repository.Remove(account);

            // Assert
            Assert.False(_context.Accounts.ContainsKey(account.AccountId));
        }

        [Fact]
        public void GetByAccountNumber_ShouldReturnMatchingAccount()
        {
            // Arrange
            var account = CreateAccount();
            _context.Accounts[account.AccountId] = account;

            // Act
            var result = _repository.GetByAccountNumber(account.AccountNumber);

            // Assert
            Assert.Equal(account, result);
        }

        [Fact]
        public void GetByAccountNumber_ShouldReturnNull_IfNotExists()
        {
            // Act
            var result = _repository.GetByAccountNumber("nonexistent");

            // Assert
            Assert.Null(result);
        }

        private Account CreateAccount()
        {
            return new Account(Guid.NewGuid().ToString(), 500, AccountType.Checking);
        }
    }
}
