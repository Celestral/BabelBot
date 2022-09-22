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
    private readonly ulong _WeAbove = 971028448569073664;
    private readonly ulong _AltariRole = 1001126450646237295;

    private readonly ulong _BTS = 1021834513329946716;
    private readonly ulong _BTSAltariRole = 1022552879418060863;
    private readonly ulong _CTS = 1004490166288797768;

    DiscordSocketClient client;

    public Program()
    {
        //_serviceProvider = CreateProvider();
    }

    static void Main(string[] args) => new Program().RunAsync(args).GetAwaiter().GetResult();

    static IServiceProvider CreateProvider()
    {

        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.GuildPresences
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
        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.All
        };

        client = new DiscordSocketClient(config);

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
        client.ButtonExecuted += ButtonHandler;

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

        var decryptTestCommand = new SlashCommandBuilder()
        .WithName("decrypttestcommand")
        .WithDescription("Decrypt a message to the roman alphabet")
        .AddOption("message", ApplicationCommandOptionType.String, "The message you want to decrypt", isRequired: true);


        try
        {
            await client.CreateGlobalApplicationCommandAsync(encryptCommand.Build());
            await client.CreateGlobalApplicationCommandAsync(decryptCommand.Build());
            await client.CreateGlobalApplicationCommandAsync(decryptTestCommand.Build());
        }
        catch(ApplicationCommandException e)
        {
            var json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }

    public async Task ButtonHandler(SocketMessageComponent component)
    {
        // We can now check for our custom id
        switch (component.Data.CustomId)
        {
            case "decrypt-button":
                // Lets respond by sending a message saying they clicked the button
                await DecryptMessageThroughButton(component);
                break;
        }
    }

    private async Task DecryptMessageThroughButton(SocketMessageComponent component)
    {
        var guild = client.GetGuild(_BTS);
        var member = guild.Users.FirstOrDefault(x => x.Id == component.User.Id);

        if (member != null && member.Roles.FirstOrDefault(x => x.Id == _BTSAltariRole) != null)
        {
            var message = component.Message.Embeds.FirstOrDefault().Description;

            var decrypted = DecryptFromBabel(message);

            var embedBuiler = new EmbedBuilder()
                .WithTitle("You decrypted the following message:")
                .WithDescription(decrypted)
                .WithColor(Color.Green)
                .WithCurrentTimestamp();

            // Now, Let's respond with the embed.
            await component.RespondAsync(embed: embedBuiler.Build(), ephemeral: true);
            //await command.RespondAsync(encrypted);
        }

        else
        {
            var embedBuiler = new EmbedBuilder()
                .WithTitle("You cannot currently decrypt this message.")
                .WithDescription("Only Altari can auto-decrypt Babel script")
                .WithColor(Color.Green)
                .WithCurrentTimestamp();

            await component.RespondAsync(embed: embedBuiler.Build(), ephemeral: true);
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
            case "decrypttestcommand":
                await DecryptTestMessage(command);
                break;
        }
    }

    private async Task EncryptMessage(SocketSlashCommand command)
    {
        // We need to extract the message from the command. since we only have one option and it's required, we can just use the first option.
        var message = (string)command.Data.Options.First().Value;

        var encrypted = EncryptToBabel(message);

        var builder = new ComponentBuilder()
        .WithButton("Decrypt", "decrypt-button");

        var embedBuiler = new EmbedBuilder()
            .WithAuthor(command.User.Username.ToString(), command.User.GetAvatarUrl() ?? command.User.GetDefaultAvatarUrl())
            .WithTitle(command.User.Username.ToString() + " has left an encrypted message:")
            .WithDescription(encrypted)
            .WithColor(Color.Green)
            .WithCurrentTimestamp();

        // Now, Let's respond with the embed.
        await command.RespondAsync(embed: embedBuiler.Build(), components: builder.Build());
        //await command.RespondAsync(encrypted, components: builder.Build());
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

    private async Task DecryptTestMessage(SocketSlashCommand command)
    {
        // We need to extract the message from the command. since we only have one option and it's required, we can just use the first option.
        var message = (string)command.Data.Options.First().Value;

        var decrypted = DecryptFromBabelTEST(message);

        var embedBuiler = new EmbedBuilder()
            .WithAuthor(command.User.Username.ToString(), command.User.GetAvatarUrl() ?? command.User.GetDefaultAvatarUrl())
            .WithTitle(command.User.Username.ToString() + "has decrypted the following message:")
            .WithDescription(decrypted)
            .WithColor(Color.Green)
            .WithCurrentTimestamp();

        // Now, Let's respond with the embed.
        await command.RespondAsync(embed: embedBuiler.Build(), ephemeral: true);
        //await command.RespondAsync(encrypted);
    }


    public string DecryptFromBabelTEST(string message)
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