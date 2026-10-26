//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Commands/NoticeCommand.cs: handle NOTICE commands
//

namespace GStage.Commands
{
    public class NoticeCommand : PrivmsgCommand
    {
        public NoticeCommand(Server server, User origin, string rawArguments)
            : base(server, origin, rawArguments)
        {
        }

        protected override string CommandVerb { get { return "NOTICE"; } }
    }
}
