using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SampleBankOperations.App.Interfaces;
using SampleBankOperations.App.Services.Operations;
using SampleBankOperations.App.Services.UI;
using SampleBankOperations.Application.Interfaces;
using SampleBankOperations.Application.Services;
using SampleBankOperations.Core.Interfaces;
using SampleBankOperations.Infrastructure.Logging;
using SampleBankOperations.Infrastructure.Persistence;
using SampleBankOperations.Infrastructure.Persistence.Repositories;

namespace SampleBankOperations.App.Tests
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void ServiceProvider_ShouldResolve_UserInterface_AndDependencies()
        {
            // Arrange
            var serviceProvider = new ServiceCollection()
                .AddSingleton<ILogger, Logger>()
                .AddSingleton<BankingDbContext>()
                .AddScoped<IAccountRepository, AccountRepository>()
                .AddScoped<IAccountService, AccountService>()
                .AddScoped<IBankOperations, BankOperations>()
                .AddTransient<UserInterface>()
                .BuildServiceProvider();

            // Act
            var userInterface = serviceProvider.GetService<UserInterface>();

            // Assert
            Assert.NotNull(userInterface);
        }

        [Fact]
        public void ServiceProvider_ShouldResolve_IBankOperations_As_BankOperations()
        {
            // Arrange
            var serviceProvider = new ServiceCollection()
                .AddSingleton<ILogger, Logger>()
                .AddSingleton<BankingDbContext>()
                .AddScoped<IAccountRepository, AccountRepository>()
                .AddScoped<IAccountService, AccountService>()
                .AddScoped<IBankOperations, BankOperations>()
                .AddTransient<UserInterface>()
                .BuildServiceProvider();

            // Act
            var bankOps = serviceProvider.GetService<IBankOperations>();

            // Assert
            Assert.NotNull(bankOps);
            Assert.IsType<BankOperations>(bankOps);
        }

        [Fact]
        public void ServiceProvider_ShouldResolve_IAccountRepository_As_AccountRepository()
        {
            // Arrange
            var serviceProvider = new ServiceCollection()
                .AddSingleton<Logger>()
                .AddSingleton<BankingDbContext>()
                .AddScoped<IAccountRepository, AccountRepository>()
                .AddScoped<IAccountService, AccountService>()
                .AddScoped<IBankOperations, BankOperations>()
                .AddTransient<UserInterface>()
                .BuildServiceProvider();

            // Act
            var repository = serviceProvider.GetService<IAccountRepository>();

            // Assert
            Assert.NotNull(repository);
            Assert.IsType<AccountRepository>(repository);
        }
    }
}
