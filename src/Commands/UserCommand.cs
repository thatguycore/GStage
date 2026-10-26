//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Commands/UserCommand.cs: handle USER commands
//

using System;
using System.Text.RegularExpressions;

namespace GStage.Commands
{
    public class UserCommand : Command
    {
        private static readonly Regex UserRegex = new Regex(@"^([^\x20]+)\x20+([^\x20]+)\x20+([^\x20]+)\x20+:?(.*)$");

        private string _username;
        private string _hostname;
        private string _servername;
        private string _realname;

        public UserCommand(Server server, User origin, string rawArguments)
            : base(server, origin, rawArguments)
        {
            Parse(rawArguments);
        }

        private void Parse(string args)
        {
            Match m = UserRegex.Match(args ?? "");
            if (m.Success)
            {
                _username = m.Groups[1].Value.Trim();
                _hostname = m.Groups[2].Value.Trim();
                _servername = m.Groups[3].Value.Trim();
                _realname = m.Groups[4].Value;
            }
        }

        public override void Run()
        {
            if (!string.IsNullOrEmpty(Origin.Username))
            {
                Origin.Numeric(Numeric.ERR_ALREADYREGISTERED);
                return;
            }

            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_hostname) ||
                string.IsNullOrEmpty(_servername) || string.IsNullOrEmpty(_realname))
            {
                Origin.Numeric(Numeric.ERR_NEEDMOREPARAMS, "USER");
                return;
            }

            Origin.Username = _username;
            Origin.Realname = _realname;

            if (!string.IsNullOrEmpty(Origin.Nickname))
            {
                Server.Welcome(Origin);
            }
        }
    }
}
