using System;
using SampleBankOperations.Core.Entities;
using SampleBankOperations.Core.Enums;
using SampleBankOperations.Infrastructure.Persistence;
using Xunit;

namespace SampleBankOperations.Tests.Infrastructure.Persistence
{
    public class BankingDbContextTests
    {
        [Fact]
        public void Accounts_ShouldBeInitializedEmpty()
        {
            // Arrange
            var context = new BankingDbContext();

            // Act & Assert
            Assert.NotNull(context.Accounts);
            Assert.Empty(context.Accounts);
        }

        [Fact]
        public void Accounts_ShouldAllow_AddingNewAccount()
        {
            // Arrange
            var context = new BankingDbContext();
            var account = new Account("123", 500, AccountType.Savings);

            // Act
            context.Accounts[account.AccountId] = account;

            // Assert
            Assert.Single(context.Accounts);
            Assert.True(context.Accounts.ContainsKey(account.AccountId));
            Assert.Equal(account, context.Accounts[account.AccountId]);
        }

        [Fact]
        public void Accounts_ShouldAllow_OverwritingAccountWithSameId()
        {
            // Arrange
            var context = new BankingDbContext();
            var accountId = Guid.NewGuid();

            var original = new Account("123", 200, AccountType.Checking)
            {
                AccountId = accountId
            };
            var updated = new Account("123", 999, AccountType.Savings)
            {
                AccountId = accountId
            };

            // Act
            context.Accounts[accountId] = original;
            context.Accounts[accountId] = updated;

            // Assert
            Assert.Single(context.Accounts);
            Assert.Equal(999, context.Accounts[accountId].Balance);
        }

        [Fact]
        public void Accounts_ShouldAllow_RemovingAccount()
        {
            // Arrange
            var context = new BankingDbContext();
            var account = new Account("321", 300, AccountType.Checking);
            context.Accounts[account.AccountId] = account;

            // Act
            var removed = context.Accounts.Remove(account.AccountId);

            // Assert
            Assert.True(removed);
            Assert.Empty(context.Accounts);
        }

        [Fact]
        public void Accounts_ShouldReturnFalse_WhenRemovingNonexistentAccount()
        {
            // Arrange
            var context = new BankingDbContext();
            var nonexistentId = Guid.NewGuid();

            // Act
            var removed = context.Accounts.Remove(nonexistentId);

            // Assert
            Assert.False(removed);
        }

        [Fact]
        public void Accounts_ShouldReturnNull_WhenGettingNonexistentAccount()
        {
            // Arrange
            var context = new BankingDbContext();
            var randomId = Guid.NewGuid();

            // Act
            context.Accounts.TryGetValue(randomId, out var account);

            // Assert
            Assert.Null(account);
        }

        [Fact]
        public void Accounts_ShouldAllow_MultipleAccounts()
        {
            // Arrange
            var context = new BankingDbContext();

            var acc1 = new Account("A1", 100, AccountType.Checking);
            var acc2 = new Account("A2", 200, AccountType.Savings);
            var acc3 = new Account("A3", 300, AccountType.Checking);

            // Act
            context.Accounts[acc1.AccountId] = acc1;
            context.Accounts[acc2.AccountId] = acc2;
            context.Accounts[acc3.AccountId] = acc3;

            // Assert
            Assert.Equal(3, context.Accounts.Count);
            Assert.Contains(acc2.AccountId, context.Accounts.Keys);
            Assert.Equal(300, context.Accounts[acc3.AccountId].Balance);
        }
    }
}
