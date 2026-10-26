//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Commands/PongCommand.cs: handle PONG commands
//

using System;

namespace GStage.Commands
{
    public class PongCommand : Command
    {
        private string _parameter;

        public PongCommand(Server server, User origin, string rawArguments)
            : base(server, origin, rawArguments)
        {
            _parameter = StripLeadingColon(rawArguments ?? "");
        }

        public override void Run()
        {
            Console.WriteLine("[PONG] Origin {0} replied to {1}", Origin.Nickname, _parameter);
        }
    }
}
