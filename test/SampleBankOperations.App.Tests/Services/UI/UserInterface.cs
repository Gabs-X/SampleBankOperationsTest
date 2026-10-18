using Moq;
using SampleBankOperations.App.Interfaces;
using SampleBankOperations.App.Services.UI;
using SampleBankOperations.Core.Entities;
using Xunit;

namespace SampleBankOperations.App.Tests.Services.UI
{
    public class UserInterfaceTests
    {
        private readonly Mock<IBankOperations> _mockBankOperations;
        private readonly UserInterface _userInterface;

        public UserInterfaceTests()
        {
            _mockBankOperations = new Mock<IBankOperations>();
            _userInterface = new UserInterface(_mockBankOperations.Object);
        }

        [Fact]
        public void Should_OpenAccount_WhenOptionIs1()
        {
            // Arrange
            var inputs = new Queue<string>(new[] { "1", "9" });
            Console.SetIn(new StringReader(string.Join(Environment.NewLine, inputs)));
            Console.SetOut(new StringWriter());

            // Act
            _userInterface.Run();

            // Assert
            _mockBankOperations.Verify(x => x.OpenAccount(), Times.Once);
        }

        [Fact]
        public void Should_ViewBalance_WhenOptionIs2_AndAccountExists()
        {
            // Arrange
            var account = new Account("123", 100, Core.Enums.AccountType.Checking);
            _mockBankOperations.Setup(x => x.GetAccountByNumber("123")).Returns(account);
            var inputs = new Queue<string>(new[] { "2", "123", "9" });
            Console.SetIn(new StringReader(string.Join(Environment.NewLine, inputs)));
            Console.SetOut(new StringWriter());

            // Act
            _userInterface.Run();

            // Assert
            _mockBankOperations.Verify(x => x.ViewBalance(account), Times.Once);
        }

        [Fact]
        public void Should_NotExecuteAction_WhenAccountNotFound()
        {
            // Arrange
            _mockBankOperations.Setup(x => x.GetAccountByNumber("000")).Returns((Account)null);
            var inputs = new Queue<string>(new[] { "2", "000", "9" });
            Console.SetIn(new StringReader(string.Join(Environment.NewLine, inputs)));
            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            _userInterface.Run();

            // Assert
            var result = output.ToString();
            Assert.Contains("Conta não encontrada", result);
            _mockBankOperations.Verify(x => x.ViewBalance(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public void Should_Transfer_WhenAccountsExist()
        {
            // Arrange
            var fromAccount = new Account("001", 1000, Core.Enums.AccountType.Checking);
            var toAccount = new Account("002", 500, Core.Enums.AccountType.Checking);

            _mockBankOperations.Setup(x => x.GetAccountByNumber("001")).Returns(fromAccount);
            _mockBankOperations.Setup(x => x.GetAccountByNumber("002")).Returns(toAccount);

            var inputs = new Queue<string>(new[] { "5", "001", "002", "9" });
            Console.SetIn(new StringReader(string.Join(Environment.NewLine, inputs)));
            Console.SetOut(new StringWriter());

            // Act
            _userInterface.Run();

            // Assert
            _mockBankOperations.Verify(x => x.Transfer(fromAccount, toAccount), Times.Once);
        }

        [Fact]
        public void Should_Exit_WhenOptionIs9()
        {
            // Arrange
            var input = new Queue<string>(new[] { "9" });
            Console.SetIn(new StringReader(string.Join(Environment.NewLine, input)));
            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            _userInterface.Run();

            // Assert
            Assert.Contains("Obrigado por utilizar o SampleBankOperations!", output.ToString());
        }
    }
}
