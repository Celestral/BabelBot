using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabelBot
{
    public class BabelTranslationCommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        private DiscordSocketClient _client;
        private static SocketGuild _guild;
        private static Config _config;
        private static BabelDictionary _dict;

        /// <summary>
        /// Reads config file during startup
        /// </summary>
        /// <param name="client"></param>
        public BabelTranslationCommandModule(DiscordSocketClient client)
        {
            _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            _dict = JsonConvert.DeserializeObject<BabelDictionary>(File.ReadAllText("dictionary.json"));

            _client = client;
            _guild = _client.GetGuild(_config.Server.GuildID);
        }

        #region Slash Commands

        /// <summary>
        /// The command to Encrypt a Babel message
        /// </summary>
        /// <param name="command">The slash command to encrypt a message, including the message as the first (and only) parameter</param>
        /// <returns>An embed containing the encrypted message with the original User as author OR an embed saying you can't encrypt a message depending on guild and role</returns>
        [SlashCommand("encrypt", "Encrypt a message to Babel script")]
        private async Task EncryptMessage(string message)
        {
            var command = Context.Interaction;
            /// IF decrypt was called in the WeAbove server and decrypting is limited to certain roles
            /// we check whether someone has an allowed role and then give them the decrypted message ephemerally if so, and otherwise a message that they can not decrypt.
            if (command.GuildId == _config.Server.GuildID && _config.Server.LimitButtonDecryption)
            {
                var member = _client.GetGuild(_config.Server.GuildID).Users.FirstOrDefault(x => x.Id == command.User.Id);
                bool hasRolePermission = false;

                foreach (var role in _config.Roles.Where(x => x.CanEncrypt))
                {
                    if (member.Roles.FirstOrDefault(x => x.Id == role.ID) != null)
                    {
                        hasRolePermission = true;
                        break;
                    }
                }

                if (hasRolePermission)
                {
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
                    await command.RespondAsync("Your encrypted message has been sent", ephemeral: true);
                    await command.Channel.SendMessageAsync(embed: embedBuiler.Build(), components: builder.Build());
                }

                else
                {
                    List<Role> allowedRoles = _config.Roles.Where(x => x.CanEncrypt).ToList();
                    string allowedRolesString = "";
                    for (int i = 0; i < allowedRoles.Count(); i++)
                    {
                        if (i == 0)
                        {
                            allowedRolesString += _client.GetGuild(_config.Server.GuildID).Roles.FirstOrDefault(x => x.Id == allowedRoles[i].ID).Name;
                        }
                        else if (i == allowedRoles.Count - 1)
                        {
                            allowedRolesString += " and " + _client.GetGuild(_config.Server.GuildID).Roles.FirstOrDefault(x => x.Id == allowedRoles[i].ID).Name;
                        }
                        else
                        {
                            allowedRolesString += ", " + _client.GetGuild(_config.Server.GuildID).Roles.FirstOrDefault(x => x.Id == allowedRoles[i].ID).Name;
                        }
                    }

                    var embedBuiler = new EmbedBuilder()
                        .WithTitle("You cannot currently encrypt a message.")
                        .WithDescription("Only " + allowedRolesString + " can encrypt to Babel script")
                        .WithColor(Color.Green)
                        .WithCurrentTimestamp();

                    await command.RespondAsync(embed: embedBuiler.Build(), ephemeral: true);
                }
            }
            else
            {
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
                await command.RespondAsync("Your encrypted message has been sent", ephemeral: true);
                await command.Channel.SendMessageAsync(embed: embedBuiler.Build(), components: builder.Build());
            }


        }

        /// <summary>
        /// The command to Decrypt a Babel message
        /// </summary>
        /// <param name="command">The slash command to decrypt a message, including the message as the first (and only) parameter</param>
        /// <returns>An ephemeral embed with the decrypted message OR an embed saying you can't decrypt the message depending on guild and role/returns>
        [SlashCommand("decrypt", "Decrypt a message to the roman alphabet")]
        private async Task DecryptMessage(string message)
        {
            var command = Context.Interaction;
            /// IF decrypt was called in the WeAbove server and decrypting is limited to certain roles
            /// we check whether someone has an allowed role and then give them the decrypted message ephemerally if so, and otherwise a message that they can not decrypt.
            if (command.GuildId == _config.Server.GuildID && _config.Server.LimitButtonDecryption)
            {
                var member = _client.GetGuild(_config.Server.GuildID).Users.FirstOrDefault(x => x.Id == command.User.Id);
                bool hasRolePermission = false;

                foreach (var role in _config.Roles.Where(x => x.CanDecrypt))
                {
                    if (member.Roles.FirstOrDefault(x => x.Id == role.ID) != null)
                    {
                        hasRolePermission = true;
                        break;
                    }
                }

                if (hasRolePermission)
                {
                    var decrypted = DecryptFromBabel(message);

                    var embedBuiler = new EmbedBuilder()
                        .WithTitle("You decrypted the following message:")
                        .WithDescription(decrypted)
                        .WithColor(Color.Green)
                        .WithCurrentTimestamp();

                    await command.RespondAsync(embed: embedBuiler.Build(), ephemeral: true);
                }

                else
                {
                    List<Role> allowedRoles = _config.Roles.Where(x => x.CanDecrypt).ToList();
                    string allowedRolesString = "";
                    for (int i = 0; i < allowedRoles.Count(); i++)
                    {
                        if (i == 0)
                        {
                            allowedRolesString += _client.GetGuild(_config.Server.GuildID).Roles.FirstOrDefault(x => x.Id == allowedRoles[i].ID).Name;
                        }
                        else if (i == allowedRoles.Count - 1)
                        {
                            allowedRolesString += " and " + _client.GetGuild(_config.Server.GuildID).Roles.FirstOrDefault(x => x.Id == allowedRoles[i].ID).Name;
                        }
                        else
                        {
                            allowedRolesString += ", " + _client.GetGuild(_config.Server.GuildID).Roles.FirstOrDefault(x => x.Id == allowedRoles[i].ID).Name;
                        }
                    }

                    var embedBuiler = new EmbedBuilder()
                        .WithTitle("You cannot currently decrypt this message.")
                        .WithDescription("Only " + allowedRolesString + " can decrypt Babel script")
                        .WithColor(Color.Green)
                        .WithCurrentTimestamp();

                    await command.RespondAsync(embed: embedBuiler.Build(), ephemeral: true);
                }
            }

            /// If decrypt was called in any other server or in DMs we don't check for Altari role
            else
            {
                var decrypted = DecryptFromBabel(message);

                var embedBuiler = new EmbedBuilder()
                    .WithTitle("You decrypted the following message:")
                    .WithDescription(decrypted)
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();

                await command.RespondAsync(embed: embedBuiler.Build(), ephemeral: true);
            }
        }

        #endregion

        #region Component Interactions

        /// <summary>
        /// Decrypts a message after pushing the "Decrypt" button underneath an Encrypted embed
        /// </summary>
        /// <param name="component">The button that was pressed</param>
        /// <returns>An ephemeral embed with the decrypted message OR an embed saying you can't decrypt the message depending on guild and role</returns>
        [ComponentInteraction("decrypt-button")]
        private async Task DecryptMessageThroughButton()
        {
            var component = (SocketMessageComponent)Context.Interaction;
            /// IF the button was activated in the WeAbove server and decrypting through button is limited to certain roles
            /// we check whether someone has an allowed role and then give them the decrypted message ephemerally if so, and otherwise a message that they can not decrypt.
            if (component.GuildId == _config.Server.GuildID && _config.Server.LimitButtonDecryption)
            {
                var member = _client.GetGuild(_config.Server.GuildID).Users.FirstOrDefault(x => x.Id == component.User.Id);
                bool hasRolePermission = false;

                foreach (var role in _config.Roles.Where(x => x.CanButtonDecrypt))
                {
                    if (member.Roles.FirstOrDefault(x => x.Id == role.ID) != null)
                    {
                        hasRolePermission = true;
                        break;
                    }
                }

                if (hasRolePermission)
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
                    List<Role> allowedRoles = _config.Roles.Where(x => x.CanButtonDecrypt).ToList();
                    string allowedRolesString = "";
                    for (int i = 0; i < allowedRoles.Count(); i++)
                    {
                        if (i == 0)
                        {
                            allowedRolesString += _client.GetGuild(_config.Server.GuildID).Roles.FirstOrDefault(x => x.Id == allowedRoles[i].ID).Name;
                        }
                        else if (i == allowedRoles.Count - 1)
                        {
                            allowedRolesString += " and " + _client.GetGuild(_config.Server.GuildID).Roles.FirstOrDefault(x => x.Id == allowedRoles[i].ID).Name;
                        }
                        else
                        {
                            allowedRolesString += ", " + _client.GetGuild(_config.Server.GuildID).Roles.FirstOrDefault(x => x.Id == allowedRoles[i].ID).Name;
                        }
                    }

                    var embedBuiler = new EmbedBuilder()
                        .WithTitle("You cannot currently decrypt this message.")
                        .WithDescription("Only " + allowedRolesString + " can auto-decrypt Babel script")
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
                var tryGet = _dict.Pairings.FirstOrDefault(x => x.Character == character);
                if (tryGet != null)
                {
                    babel = tryGet.EmoteName;
                }

                if (!string.IsNullOrEmpty(babel))
                {
                    var emote = _client.Guilds
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
                    var character = _dict.Pairings.FirstOrDefault(x => x.EmoteName == sequence.Split(':')[1]).Character;
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
}
