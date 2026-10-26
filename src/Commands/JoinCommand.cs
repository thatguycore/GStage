//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Commands/JoinCommand.cs: handle JOIN commands
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace GStage.Commands
{
    public class JoinCommand : Command
    {
        private readonly List<string> _channels = new List<string>();
        private readonly List<string> _keys = new List<string>();

        public JoinCommand(Server server, User origin, string rawArguments)
            : base(server, origin, rawArguments)
        {
            Parse(rawArguments);
        }

        private void Parse(string args)
        {
            string[] parts = (args ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                _channels.AddRange(parts[0].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
            if (parts.Length > 1)
            {
                _keys.AddRange(parts[1].Split(','));
            }
        }

        public override void Run()
        {
            if (!Origin.IsRegistered)
            {
                Origin.Numeric(Numeric.ERR_NOTREGISTERED);
                return;
            }

            if (_channels.Count == 0)
            {
                Origin.Numeric(Numeric.ERR_NEEDMOREPARAMS, "JOIN");
                return;
            }

            for (int i = 0; i < _channels.Count; i++)
            {
                string chanName = _channels[i];
                string key = i < _keys.Count ? _keys[i] : null;

                if (string.IsNullOrEmpty(chanName) || (chanName[0] != '#' && chanName[0] != '&'))
                {
                    Origin.Numeric(Numeric.ERR_NOSUCHCHANNEL, chanName);
                    continue;
                }

                Channel channel = Server.FindChannel(chanName);
                if (channel == null)
                {
                    channel = Server.CreateChannel(chanName);
                    if (!string.IsNullOrEmpty(key))
                    {
                        channel.Key = key;
                    }
                    channel.AddOp(Origin);
                }

                if (channel.IsBozo(Origin) && !Origin.IsSupervisor)
                {
                    Origin.Numeric(Numeric.ERR_BANNEDFROMCHAN, chanName);
                    continue;
                }

                channel.AddUser(Origin);

                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(channel.Key) && channel.Key == key)
                {
                    channel.AddOp(Origin);
                }

                channel.Broadcast(Origin.Prefix("JOIN " + chanName));

                // Send names list
                var users = channel.GetUsers();
                var formattedUsers = users.Select(u =>
                {
                    string pfx = "";
                    if (u.IsOp(channel)) pfx += "@";
                    if (u.IsHalfOp(channel)) pfx += "%";
                    if (u.IsVoice(channel)) pfx += "+";
                    return pfx + u.Nickname;
                }).OrderBy(n => n).ToList();

                string namesStr = string.Join(" ", formattedUsers);

                Origin.Numeric(Numeric.RPL_TOPIC, chanName, channel.Topic);
                Origin.Numeric(Numeric.RPL_NAMEREPLY, chanName, namesStr);
                Origin.Numeric(Numeric.RPL_ENDOFNAMES, chanName);

                if (Origin.IsSupervisor)
                {
                    new ModeCommand(Server, Origin, channel.Name + " :+o " + Origin.Nickname).Run();
                }
            }
        }
    }
}
