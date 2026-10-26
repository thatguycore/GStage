//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Commands/NickCommand.cs: handle NICK commands
//

using System;
using System.Collections.Generic;

namespace GStage.Commands
{
    public class NickCommand : Command
    {
        private string _nickname;

        public NickCommand(Server server, User origin, string rawArguments)
            : base(server, origin, rawArguments)
        {
            Parse(rawArguments);
        }

        private void Parse(string args)
        {
            string[] parts = (args ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                _nickname = StripLeadingColon(parts[0]);
            }
        }

        public override void Run()
        {
            if (string.IsNullOrEmpty(_nickname))
            {
                Origin.Numeric(Numeric.ERR_NONICKNAMEGIVEN);
                return;
            }

            User existing = Server.FindUser(_nickname);
            if (existing != null && existing != Origin)
            {
                Origin.Numeric(Numeric.ERR_NICKNAMEINUSE, _nickname);
                return;
            }

            // Global Stage quirk: Once registered, normal users CANNOT change their nickname!
            // Only supervisors can change nickname.
            if (!string.IsNullOrEmpty(Origin.Nickname))
            {
                if (Origin.IsSupervisor)
                {
                    string oldHostmask = Origin.Hostmask;
                    Server.Register(Origin, _nickname);
                    string message = string.Format(":{0} NICK {1}", oldHostmask, _nickname);

                    var sentUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var channel in Origin.GetChannels())
                    {
                        foreach (var u in channel.GetUsers())
                        {
                            if (!sentUsers.Contains(u.Match))
                            {
                                sentUsers.Add(u.Match);
                                u.Write(message);
                            }
                        }
                    }
                }
                else
                {
                    Origin.Numeric(Numeric.ERR_ERRONEOUSNICKNAME, _nickname);
                }
            }
            else
            {
                Server.Register(Origin, _nickname);

                if (!string.IsNullOrEmpty(Origin.Username))
                {
                    Server.Welcome(Origin);
                }
            }
        }
    }
}
