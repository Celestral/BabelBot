using BabelBot;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Discord.Webhook;
using Discord.Rest;
using Discord.API;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Text;
using System.Globalization;

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

        client.Ready += Client_Ready;
        client.SlashCommandExecuted += SlashCommandHandler;

        await Task.Delay(-1);
    }

    public async Task Client_Ready()
    {
        AlphabetBabelDictionary.FillDictionary();
        // CTS guildId
        ulong guildId = 1004490166288797768;
        var guild = client.GetGuild(guildId);

        var encryptCommand = new SlashCommandBuilder()
        .WithName("encrypt")
        .WithDescription("Encrypt a message to Babel script")
        .AddOption("message", ApplicationCommandOptionType.String, "The message you want to encrypt", isRequired: true);

        var decryptCommand = new SlashCommandBuilder()
        .WithName("decrypt")
        .WithDescription("Decrypt a message to the roman alphabet")
        .AddOption("message", ApplicationCommandOptionType.String, "The message you want to decrypt", isRequired: true);


        try
        {
            await client.CreateGlobalApplicationCommandAsync(encryptCommand.Build());
            await client.CreateGlobalApplicationCommandAsync(decryptCommand.Build());
        }
        catch(ApplicationCommandException e)
        {
            var json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }
    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        switch (command.Data.Name)
        {
            case "encrypt":
                await EncryptMessage(command);
                break;
            case "decrypt":
                await DecryptMessage(command);
                break;
        }
    }

    private async Task EncryptMessage(SocketSlashCommand command)
    {
        // We need to extract the message from the command. since we only have one option and it's required, we can just use the first option.
        var message = (string)command.Data.Options.First().Value;

        var encrypted = EncryptToBabel(message);

        var embedBuiler = new EmbedBuilder()
            .WithAuthor(command.User.Username.ToString(), command.User.GetAvatarUrl() ?? command.User.GetDefaultAvatarUrl())
            .WithTitle(command.User.Username.ToString() + " has left an encrypted message:")
            .WithDescription(encrypted)
            .WithColor(Color.Green)
            .WithCurrentTimestamp();

        // Now, Let's respond with the embed.
        await command.RespondAsync(embed: embedBuiler.Build());
        //await command.RespondAsync(encrypted);
    }

    private async Task DecryptMessage(SocketSlashCommand command)
    {
        // We need to extract the message from the command. since we only have one option and it's required, we can just use the first option.
        var message = (string)command.Data.Options.First().Value;

        var decrypted = DecryptFromBabel(message);

        var embedBuiler = new EmbedBuilder()
            .WithAuthor(command.User.Username.ToString(), command.User.GetAvatarUrl() ?? command.User.GetDefaultAvatarUrl())
            .WithTitle(command.User.Username.ToString() + "has decrypted the following message:")
            .WithDescription(decrypted)
            .WithColor(Color.Green)
            .WithCurrentTimestamp();

        // Now, Let's respond with the embed.
        await command.RespondAsync(embed: embedBuiler.Build());
        //await command.RespondAsync(encrypted);
    }

    public string DecryptFromBabel(string message)
    {
        string decrypted = "";

        string pattern = @"\<(.*?)\>";
        string[] split = System.Text.RegularExpressions.Regex.Split(message, pattern);

        foreach (var sequence in split)
        {

            if (sequence.StartsWith(":babel"))
            {
                var character = AlphabetBabelDictionary.alphaBabelDictionary.FirstOrDefault(x => x.Value == sequence.Split(':')[1]).Key;
                decrypted += character;
            }

            else
            {
                decrypted += sequence;
            }

        }

        return decrypted;
    }


    public string EncryptToBabel(string message)
    {
        string encrypted = "";
        message = message.ToLower();
        message = RemoveDiacritics(message);

        foreach (char character in message.ToLower())
        {
            string babel = "";
            AlphabetBabelDictionary.alphaBabelDictionary.TryGetValue(character, out babel);

            if (!string.IsNullOrEmpty(babel))
            {
                var emote = client.Guilds
                            .SelectMany(x => x.Emotes)
                            .FirstOrDefault(x => x.Name.IndexOf(babel, StringComparison.OrdinalIgnoreCase) != -1);

                encrypted += emote;

            }
            else
            {
                encrypted += character;
            }
        }

        return encrypted;
    }

    public string RemoveDiacritics(string message)
    {
        var normalizedString = message.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        for (int i = 0; i < normalizedString.Length; i++)
        {
            char c = normalizedString[i];
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }
}