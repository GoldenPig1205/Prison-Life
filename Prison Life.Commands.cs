using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Features;
using Prison_Life;

namespace Prison_Life.Commands
{
	[CommandHandler(typeof(ClientCommandHandler))]
	public class Adminme : ICommand
	{
		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
		{
			bool result;
			Player player = Player.Get(sender as CommandSender);

			if (Prison_Life.Instance.Owner.Contains(player.UserId))
            {
				Server.ExecuteCommand($"/setgroup {player.Id} owner");
				response = "성공했습니다!";
				result = true;
				return result;
			}
            else
            {
				response = "실패했습니다!";
				result = false;
				return result;
			}
		}

		public string Command { get; } = "adminme";

		public string[] Aliases { get; } = Array.Empty<string>();

		public string Description { get; } = "금단의 영역입니다.";

		public bool SanitizeResponse { get; } = true;
	}
}