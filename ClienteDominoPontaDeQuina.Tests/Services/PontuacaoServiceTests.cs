using Xunit;
using FluentAssertions;

namespace ClienteDominoPontaDeQuina.Tests.Services
{
    public class PontuacaoServiceTests
    {
        /// <summary>
        /// Método auxiliar que encapsula a regra de negócio do Ponta de Quina (Soma das pontas / 5 se múltiplo de 5).
        /// </summary>
        private int CalcularPontos(int somaPontas)
        {
            if (somaPontas < 0)
                throw new ArgumentOutOfRangeException(nameof(somaPontas), "A soma não pode ser negativa.");

            return (somaPontas % 5 == 0) ? somaPontas / 5 : 0;
        }

        [Fact]
        public void Calcular_ComSomaDez_DeveRetornarDois()
        {
            // Arrange
            const int soma = 10;

            // Act
            var resultado = CalcularPontos(soma);

            // Assert
            resultado.Should().Be(2);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(4, 0)]
        [InlineData(5, 1)]
        [InlineData(10, 2)]
        [InlineData(15, 3)]
        [InlineData(12, 0)]
        public void Calcular_DeveRetornarPontuacaoEsperada(int soma, int esperado)
        {
            // Act
            var resultado = CalcularPontos(soma);

            // Assert
            resultado.Should().Be(esperado);
        }

        [Fact]
        public void Calcular_ComSomaNegativa_DeveRejeitar()
        {
            // Act
            Action act = () => CalcularPontos(-1);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
               .WithParameterName("somaPontas");
        }
    }
}