//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Commands/WhoisCommand.cs: handle WHOIS commands
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace GStage.Commands
{
    public class WhoisCommand : Command
    {
        private string _target;

        public WhoisCommand(Server server, User origin, string rawArguments)
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

            if (string.IsNullOrEmpty(_target))
            {
                Origin.Numeric(Numeric.ERR_NONICKNAMEGIVEN);
                return;
            }

            User user = Server.FindUser(_target);
            if (user == null)
            {
                Origin.Numeric(Numeric.ERR_NOSUCHNICK, _target);
                return;
            }

            var channelNames = user.GetChannels().Select(c => c.Name).ToList();

            Origin.Numeric(Numeric.RPL_WHOISUSER, user.Nickname, user.Username ?? "", user.Mask ?? "", user.Realname ?? "");
            Origin.Numeric(Numeric.RPL_WHOISCHANNELS, user.Nickname, string.Join(" ", channelNames));
            Origin.Numeric(Numeric.RPL_WHOISSERVER, user.Nickname, Server.Name, Server.Address);
            Origin.Numeric(Numeric.RPL_WHOISIDLE, user.Nickname, 0);
            Origin.Numeric(Numeric.RPL_ENDOFWHOIS, user.Nickname);
        }
    }
}
