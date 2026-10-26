//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Command.cs: abstract base command and command dispatcher
//

using System;
using System.Collections.Generic;

namespace GStage
{
    public abstract class Command
    {
        private readonly Server _server;
        private readonly User _origin;
        private readonly string _rawArguments;

        private static readonly Dictionary<string, Func<Server, User, string, Command>> CommandFactories =
            new Dictionary<string, Func<Server, User, string, Command>>(StringComparer.OrdinalIgnoreCase);

        static Command()
        {
            CommandFactories["JOIN"]    = (s, u, a) => new Commands.JoinCommand(s, u, a);
            CommandFactories["KICK"]    = (s, u, a) => new Commands.KickCommand(s, u, a);
            CommandFactories["LIST"]    = (s, u, a) => new Commands.ListCommand(s, u, a);
            CommandFactories["MODE"]    = (s, u, a) => new Commands.ModeCommand(s, u, a);
            CommandFactories["NICK"]    = (s, u, a) => new Commands.NickCommand(s, u, a);
            CommandFactories["NOTICE"]  = (s, u, a) => new Commands.NoticeCommand(s, u, a);
            CommandFactories["PART"]    = (s, u, a) => new Commands.PartCommand(s, u, a);
            CommandFactories["PONG"]    = (s, u, a) => new Commands.PongCommand(s, u, a);
            CommandFactories["PRIVMSG"] = (s, u, a) => new Commands.PrivmsgCommand(s, u, a);
            CommandFactories["QUIT"]    = (s, u, a) => new Commands.QuitCommand(s, u, a);
            CommandFactories["TOPIC"]   = (s, u, a) => new Commands.TopicCommand(s, u, a);
            CommandFactories["USER"]    = (s, u, a) => new Commands.UserCommand(s, u, a);
            CommandFactories["WHO"]     = (s, u, a) => new Commands.WhoCommand(s, u, a);
            CommandFactories["WHOIS"]   = (s, u, a) => new Commands.WhoisCommand(s, u, a);
        }

        protected Command(Server server, User origin, string rawArguments)
        {
            _server = server;
            _origin = origin;
            _rawArguments = rawArguments ?? "";
        }

        public Server Server { get { return _server; } }
        public User Origin { get { return _origin; } }
        public string RawArguments { get { return _rawArguments; } }

        public static string StripLeadingColon(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.StartsWith(":") ? value.Substring(1) : value;
        }

        public static Command Create(string verb, Server server, User origin, string arguments)
        {
            Func<Server, User, string, Command> factory;
            if (CommandFactories.TryGetValue(verb, out factory))
            {
                return factory(server, origin, arguments);
            }
            return null;
        }

        public abstract void Run();
    }
}
