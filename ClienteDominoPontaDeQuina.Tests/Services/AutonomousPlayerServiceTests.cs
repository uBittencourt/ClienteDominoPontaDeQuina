using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Xunit;
using FluentAssertions;
using Moq;
using ClienteDominoPontaDeQuina.Services;

namespace ClienteDominoPontaDeQuina.Tests.Services
{
    public class AutonomousPlayerServiceTests
    {
        [Fact]
        public void IsExecuting_AposCriacao_DeveSerFalso()
        {
            // Arrange: Configura o Mock para retornar uma URL válida
            var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost") };
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(c => c["URL_API"]).Returns("https://localhost/api");

            var apiService = new DominoApiService(httpClient, configurationMock.Object);
            var sut = new AutonomousPlayerService(apiService);

            // Assert
            sut.IsExecuting.Should().BeFalse();
        }

        [Fact]
        public void IniciarBot_ComTokenVazio_DeveLancarArgumentException()
        {
            // Arrange: Configura o Mock para retornar uma URL válida
            var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost") };
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(c => c["URL_API"]).Returns("https://localhost/api");

            var apiService = new DominoApiService(httpClient, configurationMock.Object);
            var sut = new AutonomousPlayerService(apiService);

            // Act
            Action act = () => sut.IniciarBot(string.Empty, "chave_valida", Guid.NewGuid(), Guid.NewGuid());

            // Assert
            act.Should().Throw<ArgumentException>()
               .WithParameterName("token");
        }
    }
}