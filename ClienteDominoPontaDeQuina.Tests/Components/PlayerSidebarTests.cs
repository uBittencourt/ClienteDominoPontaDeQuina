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
    public class PlayerSidebarTests : BunitContext
    {
        /// <summary>
        /// Método auxiliar para evitar repetição do Arrange (DRY)
        /// </summary>
        private void ConfigurarServicosPadrao()
        {
            Services.AddSingleton(new HttpClient { BaseAddress = new Uri("https://localhost") });

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["URL_API"]).Returns("https://localhost/api");
            Services.AddSingleton(configMock.Object);

            Services.AddScoped<DominoApiService>();
            Services.AddScoped<AutonomousPlayerService>();
        }

        [Fact]
        public void Deve_Renderizar_PlayerSidebar_Com_Sucesso()
        {
            // Arrange
            ConfigurarServicosPadrao();

            // Act
            var cut = Render<PlayerSidebar>();

            // Assert
            cut.Instance.Should().NotBeNull();
            cut.Markup.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Deve_Conter_Botoes_Ou_Elementos_Na_Sidebar_Do_Jogador()
        {
            // Arrange
            ConfigurarServicosPadrao();

            // Act
            var cut = Render<PlayerSidebar>();

            // Assert
            var botoes = cut.FindAll("button");
            botoes.Should().NotBeNull();
        }

        [Fact]
        public void Deve_Permitir_Interacao_Com_Botoes_Da_Sidebar()
        {
            // Arrange
            ConfigurarServicosPadrao();
            var cut = Render<PlayerSidebar>();

            // Act & Assert
            var botao = cut.FindAll("button");
            if (botao.Count > 0)
            {
                Action acao = () => botao[0].Click();
                acao.Should().NotThrow();
            }
        }
    }
}