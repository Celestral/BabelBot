using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace BabelBot
{
    public enum ePermissionType {Encryption, Decryption, ButtonDecryption};

    public class BabelTranslationCommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        private DiscordSocketClient _client;
        private static Config _config;
        private static BabelDictionary _dict;

        /// <summary>
        /// Reads config file and dictionary during startup
        /// </summary>
        /// <param name="client"></param>
        public BabelTranslationCommandModule(DiscordSocketClient client)
        {
            _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            _dict = JsonConvert.DeserializeObject<BabelDictionary>(File.ReadAllText("dictionary.json"));

            _client = client;
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
            var interaction = Context.Interaction;
            /// IF encrypt was called in the WeAbove server and encrypting is limited to certain roles
            /// we check whether someone has an allowed role and then post the encrypted message, and otherwise an ephemeral message that they can not use encryption.
            if (interaction.GuildId == _config.Server.GuildID && _config.Server.LimitEncryption)
            {
                bool hasRolePermissions = CheckPermissions(interaction, ePermissionType.Encryption);

                if (hasRolePermissions)
                {
                    await interaction.RespondAsync("Encrypting your message...", ephemeral: true);
                    await Task.Run(()=> SendEncryptedMessage(interaction, message));
                }

                else
                {
                    await DeferAsync();
                    await Task.Run(()=> SendInsufficientRolePermissionsErrorMessage(interaction, ePermissionType.Encryption));                   
                }
            }
            else
            {
                await interaction.RespondAsync("Encrypting your message...", ephemeral: true);
                await Task.Run(() => SendEncryptedMessage(interaction, message));
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
            var interaction = Context.Interaction;
            /// IF decrypt was called in the WeAbove server and decrypting is limited to certain roles
            /// we check whether someone has an allowed role and then give them the decrypted message ephemerally if so, and otherwise an ephemeral message that they can not use decryption.
            if (interaction.GuildId == _config.Server.GuildID && _config.Server.LimitDecryption)
            {
                bool hasRolePermissions = CheckPermissions(Context.Interaction, ePermissionType.Decryption);

                if (hasRolePermissions)
                {
                    await interaction.RespondAsync("Decrypting message...", ephemeral: true);
                    await Task.Run(() => SendDecryptedMessage(interaction, message, true));
                }

                else
                {
                    await DeferAsync();
                    await Task.Run(() => SendInsufficientRolePermissionsErrorMessage(interaction, ePermissionType.Decryption));
                }
            }

            /// If decrypt was called in any other server or in DMs we don't check for Altari role
            else
            {
                await interaction.RespondAsync("Decrypting message...", ephemeral: true);
                await Task.Run(() => SendDecryptedMessage(interaction, message, true));
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
            var interaction = (SocketMessageComponent)Context.Interaction;
            /// IF the button was activated in the WeAbove server and decrypting through button is limited to certain roles
            /// we check whether someone has an allowed role and then give them the decrypted message ephemerally if so, and otherwise an ephemeral message that they can not use button decryption.
            if (interaction.GuildId == _config.Server.GuildID && _config.Server.LimitButtonDecryption)
            {
                bool hasRolePermissions = CheckPermissions(Context.Interaction, ePermissionType.ButtonDecryption);

                if (hasRolePermissions)
                {
                    var message = interaction.Message.Embeds.FirstOrDefault().Description;

                    await interaction.RespondAsync("Decrypting message...", ephemeral: true);
                    await Task.Run(() => SendDecryptedMessage(interaction, message, true));
                }

                else
                {
                    await DeferAsync();
                    await Task.Run(() => SendInsufficientRolePermissionsErrorMessage(interaction, ePermissionType.ButtonDecryption));
                }
            }

            /// If the button was activated in any other server or in DMs we don't check for Altari role
            else
            {
                var message = interaction.Message.Embeds.FirstOrDefault().Description;

                await interaction.RespondAsync("Decrypting message...", ephemeral: true);
                await Task.Run(() => SendDecryptedMessage(interaction, message, true));
            }
        }

        #endregion

        #region Helper methods

        private PropertyInfo GetPermissionProperty(ePermissionType permissionType)
        {
            switch (permissionType)
            {
                case ePermissionType.Encryption:
                    return typeof(Role).GetProperty("CanEncrypt");
                    break;
                case ePermissionType.Decryption:
                    return typeof(Role).GetProperty("CanDecrypt");
                    break;
                case ePermissionType.ButtonDecryption:
                    return typeof(Role).GetProperty("CanButtonDecrypt");
                    break;
                default:
                    return null;
                    break;
            }
        }

        /// <summary>
        /// Check whether the user has the required role permissions
        /// </summary>
        /// <param name="interaction"></param>
        /// <param name="permissionType">which permissions to check for</param>
        /// <returns>true if they have permission, false otherwise</returns>
        private bool CheckPermissions(SocketInteraction interaction, ePermissionType permissionType)
        {
            PropertyInfo permissionProperty = GetPermissionProperty(permissionType);

            var member = _client.GetGuild(_config.Server.GuildID).Users.FirstOrDefault(x => x.Id == interaction.User.Id);

            foreach (var role in _config.Roles.Where(x => ((bool)permissionProperty.GetValue(x)) == true))
            {
                if (member.Roles.FirstOrDefault(x => x.Id == role.ID) != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sends a message telling the user that they don't have the required roles for the action they're trying to do
        /// </summary>
        /// <param name="interaction"></param>
        /// <param name="permissionType"></param>
        /// <returns></returns>
        private async Task SendInsufficientRolePermissionsErrorMessage(SocketInteraction interaction, ePermissionType permissionType)
        {
            PropertyInfo permissionProperty = GetPermissionProperty(permissionType);
            string permissionActionString = "";
            switch (permissionType)
            {
                case ePermissionType.Encryption:
                    permissionActionString = "encrypt to Babel script";
                    break;
                case ePermissionType.Decryption:
                    permissionActionString = "decrypt from Babel script";
                    break;
                case ePermissionType.ButtonDecryption:
                    permissionActionString = "decrypt Babel script automatically";
                    break;
                default:
                    break;
            }

            List<Role> allowedRoles = _config.Roles.Where(x => ((bool)permissionProperty.GetValue(x)) == true).ToList();
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
                .WithTitle("You cannot currently " + permissionActionString)
                .WithDescription("Only " + allowedRolesString + " can " + permissionActionString)
                .WithColor(Color.Green)
                .WithCurrentTimestamp();

            await interaction.FollowupAsync(embed: embedBuiler.Build(), ephemeral: true);
        }

        /// <summary>
        /// Encrypts and sends message to the channel the command was used in
        /// </summary>
        /// <param name="interaction"></param>
        /// <param name="message">message to encrypt</param>
        /// <returns></returns>
        private async Task SendEncryptedMessage(SocketInteraction interaction, string message)
        {
            var encryptedMessage = EncryptToBabel(message);

            var builder = new ComponentBuilder()
            .WithButton("Decrypt", "decrypt-button");

            var embedBuilder = new EmbedBuilder()
                .WithTitle(interaction.User.Username.ToString() + " has left an encrypted message:")
                .WithDescription(encryptedMessage)
                .WithColor(Color.Green)
                .WithCurrentTimestamp();

            //Sets author avatar to server profile avatar if the user has one and the command was used inside a server.
            if (interaction.GuildId != null)
            {
                var user = _client.GetGuild((ulong)interaction.GuildId).GetUser(interaction.User.Id);
                embedBuilder.Author = new EmbedAuthorBuilder()
                    .WithName(interaction.User.Username.ToString())
                    .WithIconUrl(user.GetDisplayAvatarUrl() ?? user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
            }
            else
            {
                var user = interaction.User;
                embedBuilder.Author = new EmbedAuthorBuilder()
                    .WithName(interaction.User.Username.ToString())
                    .WithIconUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
            }

            await interaction.Channel.SendMessageAsync(embed: embedBuilder.Build(), components: builder.Build());
        }

        /// <summary>
        /// Decrypts and sends message to the channel the command was used in
        /// </summary>
        /// <param name="interaction"></param>
        /// <param name="message">The message to decrypt</param>
        /// <param name="isEphemeral">Whether the message should only be visible to the user that did the command</param>
        /// <returns></returns>
        private async Task SendDecryptedMessage(SocketInteraction interaction, string message, bool isEphemeral)
        {
            var decrypted = DecryptFromBabel(message);

            var embedBuiler = new EmbedBuilder()
                .WithTitle("You decrypted the following message:")
                .WithDescription(decrypted)
                .WithColor(Color.Green)
                .WithCurrentTimestamp();

            await interaction.FollowupAsync(embed: embedBuiler.Build(), ephemeral: isEphemeral);
        }

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
