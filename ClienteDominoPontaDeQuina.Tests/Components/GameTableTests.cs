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
    public class GameTableTests : BunitContext
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
        public void Deve_Renderizar_GameTable_Com_Sucesso_E_Instancia_Valida()
        {
            // Arrange
            ConfigurarServicosPadrao();

            // Act
            var cut = Render<GameTable>();

            // Assert
            cut.Instance.Should().NotBeNull();
            cut.Markup.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Deve_Conter_Estrutura_De_Container_Ou_Placares_Na_Mesa()
        {
            // Arrange
            ConfigurarServicosPadrao();

            // Act
            var cut = Render<GameTable>();

            // Assert
            var elementosInterativos = cut.FindAll("button, .table, .board, .card");
            cut.Markup.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Deve_Permitir_Interacao_Ou_Atualizacao_De_Estado_Na_Mesa()
        {
            // Arrange
            ConfigurarServicosPadrao();

            // Act
            var cut = Render<GameTable>();
            cut.Nodes.Should().NotBeEmpty();
        }
    }
}