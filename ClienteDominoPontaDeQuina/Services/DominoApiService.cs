using System.Text.Json;
using ClienteDominoPontaDeQuina.Models;

namespace ClienteDominoPontaDeQuina.Services;

/// <summary>
/// Serviço responsável por gerenciar a comunicação HTTP com a API oficial do Dominó Ponta de Quina.
/// Concentra as operações de identidade, gestão de jogadores, criação e interação com partidas.
/// </summary>
public class DominoApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Inicializa uma nova instância do serviço de API do dominó, configurando a URL base obtida do ambiente.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado pelo container de dependências.</param>
    /// <param name="configuration">Configurações da aplicação para leitura de parâmetros locais.</param>
    /// <exception cref="InvalidOperationException">Lançada caso a URL base não esteja configurada.</exception>
    public DominoApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;

        var baseUrl = configuration["URL_API"]
                  ?? throw new InvalidOperationException("A URL_API não foi configurada no appsettings.");

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }
    }

    /// <summary>
    /// Obtém automaticamente o Token Bearer configurado no ambiente de desenvolvimento.
    /// </summary>
    public string? ObterTokenConfigurado()
    {
        return _configuration["TOKEN_BEARER"];
    }

    /// <summary>
    /// Solicita a geração e envio de um token de acesso via e-mail corporativo/FIAP.
    /// </summary>
    /// <param name="email">E-mail cadastrado do usuário.</param>
    public async Task SolicitarTokenAsync(string email)
    {
        var request = new SolicitarTokenRequest(email);
        var response = await _httpClient.PostAsJsonAsync("/api/identidade/token", request);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Consulta os dados da sessão atual vinculada ao token Bearer fornecido.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação da conta.</param>
    /// <returns>Dados da sessão contendo o ID do usuário autenticado, ou nulo em caso de falha.</returns>
    public async Task<SessaoResponse?> ConsultarSessaoAsync(string tokenBearer)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/identidade/sessao");
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<SessaoResponse>(_jsonOptions);
    }

    /// <summary>
    /// Lista todos os perfis de jogadores associados à conta autenticada.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação da conta.</param>
    /// <returns>Lista de perfis de jogadores cadastrados.</returns>
    public async Task<List<PerfilJogador>> ListarJogadoresAsync(string tokenBearer)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/jogadores");
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode) return new();

        return await response.Content.ReadFromJsonAsync<List<PerfilJogador>>(_jsonOptions) ?? new();
    }

    /// <summary>
    /// Cria um novo perfil de jogador na conta autenticada.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação da conta.</param>
    /// <param name="nome">Nome identificador do jogador (entre 1 e 150 caracteres).</param>
    /// <returns>O perfil do jogador recém-criado, contendo seu ID gerado pela API.</returns>
    public async Task<PerfilJogador?> CriarJogadorAsync(string tokenBearer, string nome)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/jogadores")
        {
            Content = JsonContent.Create(new CriarJogadorRequest(nome))
        };
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<PerfilJogador>(_jsonOptions);
    }

    /// <summary>
    /// Realiza a ativação exclusiva de um jogador neste cliente utilizando uma chave secreta de 64 caracteres hexadecimais.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação da conta.</param>
    /// <param name="jogadorId">Identificador único do jogador.</param>
    /// <param name="chaveAtivacao">Chave gerada localmente pelo cliente (32 bytes em hex).</param>
    /// <returns>O perfil atualizado do jogador com status ativo.</returns>
    public async Task<PerfilJogador?> AtivarClienteAsync(string tokenBearer, Guid jogadorId, string chaveAtivacao)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/jogadores/{jogadorId}/ativar-cliente")
        {
            Content = JsonContent.Create(new AtivarJogadorRequest(chaveAtivacao))
        };
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<PerfilJogador>(_jsonOptions);
    }

    /// <summary>
    /// Lista as partidas que estão atualmente aguardando o segundo participante.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação da conta.</param>
    /// <returns>Coleção de partidas disponíveis para ingresso.</returns>
    public async Task<List<PartidaDisponivel>> ListarPartidasDisponiveisAsync(string tokenBearer)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/partidas");
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode) return new();

        return await response.Content.ReadFromJsonAsync<List<PartidaDisponivel>>(_jsonOptions) ?? new();
    }

    /// <summary>
    /// Cria uma nova partida de dominó definindo a pontuação alvo e registrando o jogador criador.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação da conta.</param>
    /// <param name="chaveAtivacao">Chave secreta de ativação do jogador (enviada no cabeçalho X-Jogador-Ativacao).</param>
    /// <param name="pontuacaoAlvo">Pontuação necessária para vencer a partida (ex: 50 ou 100).</param>
    /// <param name="jogadorId">ID do jogador criador.</param>
    /// <returns>O estado inicial completo da partida criada.</returns>
    public async Task<EstadoPartidaParticipante?> CriarPartidaAsync(string tokenBearer, string chaveAtivacao, int pontuacaoAlvo, Guid jogadorId)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/partidas")
        {
            Content = JsonContent.Create(new CriarPartidaRequest(pontuacaoAlvo, jogadorId, null))
        };
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);
        requestMessage.Headers.Add("X-Jogador-Ativacao", chaveAtivacao);

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<EstadoPartidaParticipante>(_jsonOptions);
    }
}