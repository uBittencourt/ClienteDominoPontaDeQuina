using Xunit;
using FluentAssertions;
using Bunit;
using ClienteDominoPontaDeQuina.Components.Shared;

namespace ClienteDominoPontaDeQuina.Tests.Components
{
    public class DominoTileTests : BunitContext
    {
        [Fact]
        public void Deve_Renderizar_Peca_Com_Valores_Corretos()
        {
            // Arrange & Act: Renderiza o componente DominoTile passando parâmetros
            var cut = Render<DominoTile>(parameters => parameters
                .Add(p => p.ValorA, 4)
                .Add(p => p.ValorB, 6)
            );

            // Assert
            cut.Markup.Should().Contain("4");
            cut.Markup.Should().Contain("6");
        }
    }
}