using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using Bunit;
using Moq;
using ClienteDominoPontaDeQuina.Components.Shared;
using ClienteDominoPontaDeQuina.Services;

namespace ClienteDominoPontaDeQuina.Tests.Components
{
    public class MatchesSidebarTests : BunitContext
    {
        [Fact]
        void Deve_Clicar_Em_Botao_Da_MatchesSidebar()
        {
            // Arrange
            Services.AddSingleton(new HttpClient { BaseAddress = new Uri("https://localhost") });
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["URL_API"]).Returns("https://localhost/api");
            Services.AddSingleton(configMock.Object);
            Services.AddScoped<DominoApiService>();
            Services.AddScoped<AutonomousPlayerService>();

            var cut = Render<MatchesSidebar>();

            // Act
            var botao = cut.Find("button");
            botao.Click();

            // Assert
            cut.Instance.Should().NotBeNull();
        }
    }
}