using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

public class Program
{
    private readonly IServiceProvider _serviceProvider;
    DiscordSocketClient client;

    public Program()
    {
        _serviceProvider = CreateProvider();
    }

    static void Main(string[] args) => new Program().RunAsync(args).GetAwaiter().GetResult();

    static IServiceProvider CreateProvider()
    {
        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged
        };

        var xConfig = new InteractionServiceConfig()
        {

        };

        var cConfig = new CommandServiceConfig()
        {

        };

        var collection = new ServiceCollection()
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(xConfig)
            .AddSingleton<InteractionService>()
            .AddSingleton(cConfig)
            .AddSingleton<CommandService>();

        return collection.BuildServiceProvider();
    }

    async Task RunAsync(string[] args)
    {
        client = _serviceProvider.GetRequiredService<DiscordSocketClient>();

        client.Log += async (msg) =>
        {
            await Task.CompletedTask;
            Console.WriteLine(msg);
        };

        var token = File.ReadAllText("token.txt");

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        await Task.Delay(-1);

        client.Ready += Client_Ready;
        client.SlashCommandExecuted += SlashCommandHandler;
    }

    public async Task Client_Ready()
    {
        // CTS guildId
        ulong guildId = 1004490166288797768;
        var guild = client.GetGuild(guildId);

        var guildCommand = new SlashCommandBuilder();
        guildCommand.WithName("first-command");
        guildCommand.WithDescription("Let's try it this way");

        var globalCommand = new SlashCommandBuilder();
        globalCommand.WithName("first-global-command");
        globalCommand.WithDescription("This is my first global slash command");

        try
        {
            await guild.CreateApplicationCommandAsync(guildCommand.Build());
            await client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
        }
        catch(ApplicationCommandException e)
        {
            var json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }
    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        await command.RespondAsync($"You executed {command.Data.Name}");
    }
}