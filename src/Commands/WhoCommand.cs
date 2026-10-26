//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Commands/WhoCommand.cs: handle WHO commands
//

using System;
using System.Collections.Generic;

namespace GStage.Commands
{
    public class WhoCommand : Command
    {
        private string _target;

        public WhoCommand(Server server, User origin, string rawArguments)
            : base(server, origin, rawArguments)
        {
            _target = StripLeadingColon((rawArguments ?? "").Trim());
        }

        public override void Run()
        {
            if (!Origin.IsRegistered)
            {
                Origin.Numeric(Numeric.ERR_NOTREGISTERED);
                return;
            }

            List<User> users;
            string targetName = _target;

            if (string.IsNullOrEmpty(_target) || _target == "0")
            {
                if (!Origin.IsSupervisor)
                {
                    return;
                }
                users = Server.GetUsers();
                targetName = "0";
            }
            else
            {
                Channel channel = Server.FindChannel(_target);
                if (channel == null)
                {
                    Origin.Numeric(Numeric.ERR_NOSUCHCHANNEL, _target);
                    return;
                }
                users = channel.GetUsers();
            }

            foreach (var user in users)
            {
                Origin.Numeric(Numeric.RPL_WHOREPLY, targetName, user.Username ?? "", user.Mask ?? "",
                               Server.Name, user.Nickname ?? "", user.Realname ?? "");
            }

            Origin.Numeric(Numeric.RPL_ENDOFWHO, targetName);
        }
    }
}
