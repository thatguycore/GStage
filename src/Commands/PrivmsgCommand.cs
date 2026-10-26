//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Commands/PrivmsgCommand.cs: handle PRIVMSG commands
//

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GStage.Commands
{
    public class PrivmsgCommand : Command
    {
        public const int MaxTargets = 9;
        private static readonly Regex CmdRegex = new Regex(@"^([^ ]*)\s*:?(.*)$");

        private readonly List<string> _targets = new List<string>();
        private string _message;

        public PrivmsgCommand(Server server, User origin, string rawArguments)
            : base(server, origin, rawArguments)
        {
            Parse(rawArguments);
        }

        protected virtual string CommandVerb { get { return "PRIVMSG"; } }

        private void Parse(string args)
        {
            Match m = CmdRegex.Match(args ?? "");
            if (m.Success)
            {
                string targetList = m.Groups[1].Value.Trim();
                _message = m.Groups[2].Value;

                if (!string.IsNullOrEmpty(targetList))
                {
                    _targets.AddRange(targetList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
        }

        public override void Run()
        {
            if (!Origin.IsRegistered)
            {
                Origin.Numeric(Numeric.ERR_NOTREGISTERED);
                return;
            }

            // Gagged users have their messages silently dropped unless supervisor
            if (Origin.IsGagged && !Origin.IsSupervisor)
            {
                return;
            }

            if (string.IsNullOrEmpty(_message))
            {
                Origin.Numeric(Numeric.ERR_NOTEXTTOSEND);
                return;
            }

            if (_targets.Count == 0)
            {
                Origin.Numeric(Numeric.ERR_NORECIPIENT, CommandVerb);
                return;
            }

            var recipients = new List<object>();
            var nullRecipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var badRecipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var voidRecipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int targetCount = 0;
            foreach (var target in _targets)
            {
                if (++targetCount > MaxTargets) break;

                if (badRecipients.Contains(target) || voidRecipients.Contains(target))
                {
                    continue;
                }

                bool isChannel = target.StartsWith("#") || target.StartsWith("&");
                object recipient = isChannel ? (object)Server.FindChannel(target) : (object)Server.FindUser(target);

                if (recipient != null)
                {
                    bool canMessage = Origin.IsSupervisor;

                    if (!canMessage)
                    {
                        Channel chan = recipient as Channel;
                        User user = recipient as User;

                        if (chan != null)
                        {
                            if (Origin.IsOn(chan))
                            {
                                if (chan.Moderated)
                                {
                                    canMessage = chan.IsVoice(Origin) || chan.IsHalfOp(Origin) || chan.IsOp(Origin);
                                }
                                else
                                {
                                    canMessage = true;
                                }
                            }
                            else
                            {
                                canMessage = !chan.MessageLocked;
                            }
                        }
                        else if (user != null)
                        {
                            if (user.IsPrivate)
                            {
                                voidRecipients.Add(target);
                            }
                            else
                            {
                                canMessage = true;
                            }
                        }
                    }

                    if (!voidRecipients.Contains(target))
                    {
                        if (canMessage)
                        {
                            recipients.Add(recipient);
                        }
                        else
                        {
                            badRecipients.Add(target);
                        }
                    }
                }
                else
                {
                    nullRecipients.Add(target);
                }
            }

            foreach (var err in nullRecipients)
            {
                Origin.Numeric(Numeric.ERR_NOSUCHNICK, err);
            }

            foreach (var err in badRecipients)
            {
                Origin.Numeric(Numeric.ERR_CANNOTSENDTOCHAN, err);
            }

            foreach (var recipient in recipients)
            {
                Channel chan = recipient as Channel;
                User user = recipient as User;

                if (chan != null)
                {
                    chan.Broadcast(Origin.Prefix(string.Format("{0} {1} :{2}", CommandVerb, chan.Name, _message)), Origin);
                }
                else if (user != null)
                {
                    user.Write(Origin.Prefix(string.Format("{0} {1} :{2}", CommandVerb, user.Nickname, _message)));
                }
            }
        }
    }
}
