using BabelBot;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public class Program
{
    private DiscordSocketClient _client;
    private InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;

    public Program()
    {
        _serviceProvider = CreateProvider();
    }

    static IServiceProvider CreateProvider()
    {
        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildPresences
        };

        var collection = new ServiceCollection()
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>();

        return collection.BuildServiceProvider();
    }

    static void Main(string[] args) => new Program().RunAsync(args).GetAwaiter().GetResult();


    async Task RunAsync(string[] args)
    {
        _client = _serviceProvider.GetRequiredService<DiscordSocketClient>();

        _client.Log += async (msg) =>
        {
            await Task.CompletedTask;
            Console.WriteLine(msg);
        };

        var token = File.ReadAllText("token.txt");

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        _client.Ready += Client_Ready;

        await Task.Delay(-1);
    }

    public async Task Client_Ready()
    {
        AlphabetBabelDictionary.FillDictionary();
        var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

        /*var encryptCommand = new SlashCommandBuilder()
        .WithName("encrypt")
        .WithDescription("Encrypt a message to Babel script")
        .AddOption("message", ApplicationCommandOptionType.String, "The message you want to encrypt", isRequired: true);

        var decryptCommand = new SlashCommandBuilder()
        .WithName("decrypt")
        .WithDescription("Decrypt a message to the roman alphabet")
        .AddOption("message", ApplicationCommandOptionType.String, "The message you want to decrypt", isRequired: true);*/

        try
        {
            _interactionService = new InteractionService(_client);
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
            await _interactionService.RegisterCommandsToGuildAsync(config.Server.GuildID);

            _client.InteractionCreated += async interaction =>
            {
                var scope = _serviceProvider.CreateScope();
                var ctx = new SocketInteractionContext(_client, interaction);
                await _interactionService.ExecuteCommandAsync(ctx, scope.ServiceProvider);
            };
            _client.Ready -= Client_Ready;
        }
        catch (ApplicationCommandException e)
        {
            var json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }

}