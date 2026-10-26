//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Commands/QuitCommand.cs: handle QUIT commands
//

namespace GStage.Commands
{
    public class QuitCommand : Command
    {
        private readonly string _reason;

        public QuitCommand(Server server, User origin, string rawArguments)
            : base(server, origin, rawArguments)
        {
            _reason = StripLeadingColon(rawArguments ?? "");
        }

        public override void Run()
        {
            Origin.Disconnect(string.IsNullOrEmpty(_reason) ? "Client Quit" : _reason);
        }
    }
}
