using Xunit;
using FluentAssertions;
using SampleBankOperations.Application.Validations; // Classe a ser testada
using System;

namespace SampleBankOperations.Application.Tests.Validations; // Namespace ajustado

public class AccountValidatorTests
{
    [Theory]
    [InlineData(100, 50, true)]  // Balance >= MinimumBalance
    [InlineData(100, 100, true)] // Balance == MinimumBalance
    [InlineData(100, 150, false)]// Balance < MinimumBalance
    [InlineData(0, 0, true)]    // Zero balance, zero minimum
    [InlineData(-10, 0, false)] // Negative balance, positive minimum (or zero)
    public void MinimumBalanceValidator_ShouldReturnCorrectPredicate(decimal balance, decimal minimumBalance, bool expectedResult)
    {
        // Arrange
        var validator = AccountValidator.MinimumBalanceValidator(minimumBalance);

        // Act
        var result = validator(balance);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(100, 50, true)]  // Balance >= RequestedAmount
    [InlineData(100, 100, true)] // Balance == RequestedAmount
    [InlineData(100, 150, false)]// Balance < RequestedAmount
    [InlineData(0, 0, true)]    // Zero balance, zero requested
    [InlineData(50, 0, true)]   // Positive balance, zero requested
    public void RequestedAmountValidator_ShouldReturnCorrectPredicate(decimal balance, decimal requestedAmount, bool expectedResult)
    {
        // Arrange
        var validator = AccountValidator.RequestedAmountValidator(requestedAmount);

        // Act
        var result = validator(balance);

        // Assert
        result.Should().Be(expectedResult);
    }
}