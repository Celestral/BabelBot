using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabelBot
{
    public class BabelModule : InteractionModuleBase
    {
        private readonly InteractionService _interactionService;

        public BabelModule(InteractionService interactionService)
        {
            _interactionService = interactionService;
        }

        [SlashCommand("echo", "Echo an input")]
        public async Task Echo(string input)
        {
            await RespondAsync(input);
        }
    }
}
