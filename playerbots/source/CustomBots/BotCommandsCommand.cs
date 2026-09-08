// =========================================================================
// BotCommandsCommand.cs — discoverable player-facing PlayerBot help.
// =========================================================================

using Server;
using Server.Commands;

namespace Server.CustomBots
{
    public static class BotCommandsCommand
    {
        public static void Configure()
        {
            CommandSystem.Register("BotCommands", AccessLevel.Player, OnCommand);
            CommandSystem.Register("BotHelp", AccessLevel.Player, OnCommand);
        }

        [Usage("BotCommands")]
        [Description("Shows the speech commands understood by player-led PlayerBots.")]
        private static void OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;
            if (from == null)
            {
                return;
            }

            from.SendMessage(0x35, "=== PlayerBot Commands ===");
            from.SendMessage("Say the bot's first name before every order.");
            from.SendMessage("NAME follow me  - join/follow your party");
            from.SendMessage("NAME stay       - hold the current position");
            from.SendMessage("NAME come       - resume following");
            from.SendMessage("NAME leave      - leave your party");
            from.SendMessage("NAME help       - bot repeats the short command list");
            from.SendMessage("Party followers automatically assist your fights.");
        }
    }
}
