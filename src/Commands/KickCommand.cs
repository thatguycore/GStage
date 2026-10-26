//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Commands/KickCommand.cs: handle KICK commands
//

using System;
using System.Text.RegularExpressions;

namespace GStage.Commands
{
    public class KickCommand : Command
    {
        private static readonly Regex KickRegex = new Regex(@"^([^ ]*)\s*([^ ]*)\s*:?(.*)$");

        private string _channelName;
        private string _targetNick;
        private string _message;

        public KickCommand(Server server, User origin, string rawArguments)
            : base(server, origin, rawArguments)
        {
            Parse(rawArguments);
        }

        private void Parse(string args)
        {
            Match m = KickRegex.Match(args ?? "");
            if (m.Success)
            {
                _channelName = m.Groups[1].Value.Trim();
                _targetNick = m.Groups[2].Value.Trim();
                _message = m.Groups[3].Value;
            }
        }

        public override void Run()
        {
            if (!Origin.IsRegistered)
            {
                Origin.Numeric(Numeric.ERR_NOTREGISTERED);
                return;
            }

            if (string.IsNullOrEmpty(_channelName) || string.IsNullOrEmpty(_targetNick))
            {
                Origin.Numeric(Numeric.ERR_NEEDMOREPARAMS, "KICK");
                return;
            }

            Channel channel = Server.FindChannel(_channelName);
            if (channel == null)
            {
                Origin.Numeric(Numeric.ERR_NOSUCHCHANNEL, _channelName);
                return;
            }

            User target = Server.FindUser(_targetNick);
            if (target == null)
            {
                Origin.Numeric(Numeric.ERR_NOSUCHNICK, _targetNick);
                return;
            }

            if (!Origin.IsOn(channel) && !Origin.IsSupervisor)
            {
                Origin.Numeric(Numeric.ERR_NOTONCHANNEL, _channelName);
                return;
            }

            if (!Origin.IsOp(channel) && !Origin.IsSupervisor)
            {
                Origin.Numeric(Numeric.ERR_CHANOPRIVSNEEDED, _channelName);
                return;
            }

            if (!target.IsOn(channel))
            {
                Origin.Numeric(Numeric.ERR_USERNOTINCHANNEL, _targetNick, _channelName);
                return;
            }

            string reason = _message ?? "";
            channel.Broadcast(Origin.Prefix(string.Format("KICK {0} {1} :{2}", _channelName, _targetNick, reason)));
            channel.DeleteUser(target);

            // Global Stage feature: kicked user is automatically bozo-banned for 180 seconds!
            channel.AddBozo(target);
        }
    }
}
