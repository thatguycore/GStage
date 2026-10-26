//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Commands/ListCommand.cs: handle LIST commands
//

using System;
using System.Collections.Generic;

namespace GStage.Commands
{
    public class ListCommand : Command
    {
        public ListCommand(Server server, User origin, string rawArguments)
            : base(server, origin, rawArguments)
        {
        }

        public override void Run()
        {
            if (!Origin.IsRegistered)
            {
                Origin.Numeric(Numeric.ERR_NOTREGISTERED);
                return;
            }

            Origin.Numeric(Numeric.RPL_LISTSTART);

            foreach (var channel in Server.GetChannels())
            {
                if (channel.Secret && !Origin.IsSupervisor)
                {
                    continue;
                }

                Origin.Numeric(Numeric.RPL_LIST, channel.Name, channel.UserCount, channel.Topic ?? "");
            }

            Origin.Numeric(Numeric.RPL_LISTEND);
        }
    }
}
