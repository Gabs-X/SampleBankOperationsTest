using System;
using System.IO;
using SampleBankOperations.Core.Interfaces;
using SampleBankOperations.Infrastructure.Logging;
using Xunit;

namespace SampleBankOperations.Tests.Infrastructure.Logging
{
    public class LoggerTests
    {
        [Fact]
        public void Log_ShouldWriteMessageToConsole_WithExpectedFormat()
        {
            // Arrange
            var logger = new Logger();
            var testMessage = "Test message for logging";
            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            logger.Log(testMessage);
            var result = output.ToString();

            // Assert
            Assert.Contains("[LOG]", result);
            Assert.Contains(testMessage, result);

            var parts = result.Split(": ");
            Assert.True(parts.Length >= 2);
            DateTime parsedDate;
            var timestamp = parts[0].Replace("[LOG] ", "").Trim();
            Assert.True(DateTime.TryParse(timestamp, out parsedDate));
        }

        [Fact]
        public void Log_ShouldWriteNewLineAfterMessage()
        {
            // Arrange
            var logger = new Logger();
            var testMessage = "Final line check";
            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            logger.Log(testMessage);
            var result = output.ToString();

            // Assert
            Assert.EndsWith(Environment.NewLine, result);
        }
    }
}
