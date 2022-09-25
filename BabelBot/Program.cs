using BabelBot;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Text;
using System.Globalization;

public class Program
{
    // WeAbove Guild ID and Altari role ID
    private readonly ulong _WeAbove = 971028448569073664;
    private readonly ulong _AltariRole = 1001126450646237295;

    private const bool _ALTARIREQUIRED = true;

    DiscordSocketClient client;

    static void Main(string[] args) => new Program().RunAsync(args).GetAwaiter().GetResult();

    async Task RunAsync(string[] args)
    {
        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildPresences
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
        catch (ApplicationCommandException e)
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

    public async Task ButtonHandler(SocketMessageComponent component)
    {
        switch (component.Data.CustomId)
        {
            case "decrypt-button":
                await DecryptMessageThroughButton(component);
                break;
        }
    }

    #region Command Implementations

    /// <summary>
    /// The command to Encrypt a Babel message
    /// </summary>
    /// <param name="command">The slash command to encrypt a message, including the message as the first (and only) parameter</param>
    /// <returns>An embed containing the encrypted message with the original User as author</returns>
    private async Task EncryptMessage(SocketSlashCommand command)
    {
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

        // Sends an ephemeral response first, then sends a new message to the channel, this is to prevent the original command from showing, together with the plaintext string that was encrypted.
        await command.RespondAsync("Your encrypted message has been sent", ephemeral:true);
        await command.Channel.SendMessageAsync(embed: embedBuiler.Build(), components: builder.Build());
    }

    /// <summary>
    /// The command to Decrypt a Babel message
    /// </summary>
    /// <param name="command">The slash command to decrypt a message, including the message as the first (and only) parameter</param>
    /// <returns>An embed containing the decrypted message with the original User as author</returns>
    private async Task DecryptMessage(SocketSlashCommand command)
    {
        var message = (string)command.Data.Options.First().Value;

        var decrypted = DecryptFromBabel(message);

        var embedBuiler = new EmbedBuilder()
            .WithAuthor(command.User.Username.ToString(), command.User.GetAvatarUrl() ?? command.User.GetDefaultAvatarUrl())
            .WithTitle(command.User.Username.ToString() + " has decrypted the following message:")
            .WithDescription(decrypted)
            .WithColor(Color.Green)
            .WithCurrentTimestamp();

        await command.RespondAsync(embed: embedBuiler.Build());
    }

    /// <summary>
    /// Decrypts a message after pushing the "Decrypt" button underneath an Encrypted embed
    /// </summary>
    /// <param name="component">The button that was pressed</param>
    /// <returns>An ephemeral embed with the decrypted message OR an embed saying you can't decrypt the message depending on guild and role</returns>
    private async Task DecryptMessageThroughButton(SocketMessageComponent component)
    {
        /// IF the button was activated in the WeAbove server and the Altari role is required
        /// We check whether someone is Altari and then give them the decrypted message ephemerally if so, and otherwise a message that they can not decrypt.
        if (component.GuildId == _WeAbove && _ALTARIREQUIRED)
        {
            var member = client.GetGuild(_WeAbove).Users.FirstOrDefault(x => x.Id == component.User.Id);

            bool isAltari = member.Roles.FirstOrDefault(x => x.Id == _AltariRole) != null;

            if (isAltari)
            {
                var message = component.Message.Embeds.FirstOrDefault().Description;

                var decrypted = DecryptFromBabel(message);

                var embedBuiler = new EmbedBuilder()
                    .WithTitle("You decrypted the following message:")
                    .WithDescription(decrypted)
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();

                await component.RespondAsync(embed: embedBuiler.Build(), ephemeral: true);
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

        /// If the button was activated in any other server or in DMs we don't check for Altari role
        else
        {
            var message = component.Message.Embeds.FirstOrDefault().Description;

            var decrypted = DecryptFromBabel(message);

            var embedBuiler = new EmbedBuilder()
                .WithTitle("You decrypted the following message:")
                .WithDescription(decrypted)
                .WithColor(Color.Green)
                .WithCurrentTimestamp();

            await component.RespondAsync(embed: embedBuiler.Build(), ephemeral: true);
        }
    }

    #endregion

    #region Helper methods

    /// <summary>
    /// Encrypts a message to Babel
    /// Characters that don't have a Babel equivalent are kept in plaintext
    /// </summary>
    /// <param name="message">The message to encrypt</param>
    /// <returns>The encrypted Babel message in emoji</returns>
    public string EncryptToBabel(string message)
    {
        string encrypted = "";
        message = message.ToLower();
        message = RemoveDiacritics(message);
        bool inEmote = false;

        foreach (char character in message.ToLower())
        {
            if (character == '<')
            {
                inEmote = true;
                continue;
            }
            if (character == '>')
            {
                inEmote = false;
                continue;
            }
            if (inEmote)
            {
                continue;
            }
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

    /// <summary>
    /// Decrypts Babel text back to Roman alphabet
    /// </summary>
    /// <param name="message">The encrypted Babel message</param>
    /// <returns>The decrypted Roman alphabet message</returns>
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

    /// <summary>
    /// Removes all accents from letters (i.e 'á' 'à' 'ä' all become 'a')
    /// This is necessary because there currently no Babel equivalents of diacritics
    /// </summary>
    /// <param name="message">The unencrypted input message</param>
    /// <returns>The unencrypted input message without diacritics</returns>
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

    #endregion
}