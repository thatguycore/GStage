//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Commands/PartCommand.cs: handle PART commands
//

using System;

namespace GStage.Commands
{
    public class PartCommand : Command
    {
        private string _channelName;

        public PartCommand(Server server, User origin, string rawArguments)
            : base(server, origin, rawArguments)
        {
            Parse(rawArguments);
        }

        private void Parse(string args)
        {
            string[] parts = (args ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                _channelName = StripLeadingColon(parts[0]);
            }
        }

        public override void Run()
        {
            if (!Origin.IsRegistered)
            {
                Origin.Numeric(Numeric.ERR_NOTREGISTERED);
                return;
            }

            if (string.IsNullOrEmpty(_channelName))
            {
                Origin.Numeric(Numeric.ERR_NEEDMOREPARAMS, "PART");
                return;
            }

            Channel channel = Server.FindChannel(_channelName);
            if (channel == null)
            {
                Origin.Numeric(Numeric.ERR_NOSUCHCHANNEL, _channelName);
                return;
            }

            if (!Origin.IsOn(channel))
            {
                Origin.Numeric(Numeric.ERR_NOTONCHANNEL, _channelName);
                return;
            }

            channel.Broadcast(Origin.Prefix("PART " + _channelName));
            channel.DeleteUser(Origin);
        }
    }
}
