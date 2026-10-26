//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Commands/ModeCommand.cs: handle MODE commands (including Quarterdeck exploits)
//

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GStage.Commands
{
    public class ModeCommand : Command
    {
        private static readonly Regex ModeRegex = new Regex(@"^([^\x20]+)(?:\x20+([^\x20]+))?(?:\x20+(.*))?$");

        private string _target;
        private char? _operation; // '+' or '-'
        private char? _modeChar;
        private string _parameter;

        public ModeCommand(Server server, User origin, string rawArguments)
            : base(server, origin, rawArguments)
        {
            Parse(rawArguments);
        }

        private void Parse(string args)
        {
            Match m = ModeRegex.Match(args ?? "");
            if (m.Success)
            {
                _target = m.Groups[1].Value.Trim();
                string modeStr = StripLeadingColon(m.Groups[2].Value.Trim());
                _parameter = m.Groups[3].Value.Trim();

                if (!string.IsNullOrEmpty(modeStr))
                {
                    if (modeStr[0] == '+' || modeStr[0] == '-')
                    {
                        _operation = modeStr[0];
                        if (modeStr.Length > 1)
                        {
                            _modeChar = modeStr[1];
                        }
                    }
                    else
                    {
                        _modeChar = modeStr[0];
                    }
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

            if (string.IsNullOrEmpty(_target))
            {
                Origin.Numeric(Numeric.ERR_NEEDMOREPARAMS, "MODE");
                return;
            }

            bool isChannel = _target.StartsWith("#") || _target.StartsWith("&");

            if (isChannel)
            {
                RunChannelMode();
            }
            else
            {
                RunUserMode();
            }
        }

        private void RunChannelMode()
        {
            Channel channel = Server.FindChannel(_target);
            if (channel == null)
            {
                Origin.Numeric(Numeric.ERR_NOSUCHCHANNEL, _target);
                return;
            }

            // If no operation, return channel modes
            if (!_operation.HasValue || !_modeChar.HasValue)
            {
                string modes = "";
                var paramsList = new List<string>();

                if (channel.Permanent) modes += "f";
                if (!string.IsNullOrEmpty(channel.Key))
                {
                    modes += "k";
                    paramsList.Add(channel.Key);
                }
                if (channel.Limit.HasValue)
                {
                    modes += "l";
                    paramsList.Add(channel.Limit.Value.ToString());
                }
                if (channel.Moderated) modes += "m";
                if (channel.MessageLocked) modes += "n";
                if (channel.Secret) modes += "s";
                if (channel.TopicLocked) modes += "t";

                string modeStr = "+" + modes;
                if (paramsList.Count > 0)
                {
                    modeStr += " " + string.Join(" ", paramsList);
                }

                Origin.Numeric(Numeric.RPL_CHANNELMODEIS, channel.Name, modeStr);
                return;
            }

            bool isUp = _operation.Value == '+';
            char m = _modeChar.Value;

            switch (m)
            {
                case 'o': // Operator
                    {
                        if (!Origin.IsOp(channel) && !Origin.IsSupervisor)
                        {
                            Origin.Numeric(Numeric.ERR_CHANOPRIVSNEEDED, channel.Name);
                            return;
                        }

                        if (string.IsNullOrEmpty(_parameter))
                        {
                            Origin.Numeric(Numeric.ERR_NEEDMOREPARAMS, "MODE");
                            return;
                        }

                        User targetUser = Server.FindUser(_parameter);
                        if (targetUser == null || !targetUser.IsOn(channel))
                        {
                            Origin.Numeric(Numeric.ERR_USERNOTINCHANNEL, _parameter, channel.Name);
                            return;
                        }

                        if (isUp)
                        {
                            channel.AddOp(targetUser);
                            channel.Broadcast(Origin.Prefix(string.Format("MODE {0} :+o {1}", channel.Name, targetUser.Nickname)));
                        }
                        else
                        {
                            channel.DeleteOp(targetUser);
                            channel.Broadcast(Origin.Prefix(string.Format("MODE {0} :-o {1}", channel.Name, targetUser.Nickname)));

                            // Quarterdeck quirk: If supervisor is de-opped, immediately auto re-op!
                            if (targetUser.IsSupervisor)
                            {
                                channel.AddOp(targetUser);
                                channel.Broadcast(targetUser.Prefix(string.Format("MODE {0} :+o {1}", channel.Name, targetUser.Nickname)));
                            }
                        }
                        break;
                    }

                case 'h': // HalfOp
                    {
                        if (!Origin.IsOp(channel) && !Origin.IsHalfOp(channel) && !Origin.IsSupervisor)
                        {
                            Origin.Numeric(Numeric.ERR_CHANOPRIVSNEEDED, channel.Name);
                            return;
                        }

                        if (string.IsNullOrEmpty(_parameter))
                        {
                            Origin.Numeric(Numeric.ERR_NEEDMOREPARAMS, "MODE");
                            return;
                        }

                        User targetUser = Server.FindUser(_parameter);
                        if (targetUser == null || !targetUser.IsOn(channel))
                        {
                            Origin.Numeric(Numeric.ERR_USERNOTINCHANNEL, _parameter, channel.Name);
                            return;
                        }

                        if (isUp)
                        {
                            channel.AddHalfOp(targetUser);
                            channel.Broadcast(Origin.Prefix(string.Format("MODE {0} :+h {1}", channel.Name, targetUser.Nickname)));
                        }
                        else
                        {
                            channel.DeleteHalfOp(targetUser);
                            channel.Broadcast(Origin.Prefix(string.Format("MODE {0} :-h {1}", channel.Name, targetUser.Nickname)));
                        }
                        break;
                    }

                case 'v': // Voice
                    {
                        if (!Origin.IsOp(channel) && !Origin.IsHalfOp(channel) && !Origin.IsSupervisor)
                        {
                            Origin.Numeric(Numeric.ERR_CHANOPRIVSNEEDED, channel.Name);
                            return;
                        }

                        if (string.IsNullOrEmpty(_parameter))
                        {
                            Origin.Numeric(Numeric.ERR_NEEDMOREPARAMS, "MODE");
                            return;
                        }

                        User targetUser = Server.FindUser(_parameter);
                        if (targetUser == null || !targetUser.IsOn(channel))
                        {
                            Origin.Numeric(Numeric.ERR_USERNOTINCHANNEL, _parameter, channel.Name);
                            return;
                        }

                        if (isUp)
                        {
                            channel.AddVoice(targetUser);
                            channel.Broadcast(Origin.Prefix(string.Format("MODE {0} :+v {1}", channel.Name, targetUser.Nickname)));
                        }
                        else
                        {
                            channel.DeleteVoice(targetUser);
                            channel.Broadcast(Origin.Prefix(string.Format("MODE {0} :-v {1}", channel.Name, targetUser.Nickname)));
                        }
                        break;
                    }

                case 'k': // Key (Contains the famous Global Stage exploit!)
                    {
                        if (isUp)
                        {
                            if (!Origin.IsOp(channel) && !Origin.IsSupervisor)
                            {
                                Origin.Numeric(Numeric.ERR_CHANOPRIVSNEEDED, channel.Name);
                                return;
                            }

                            if (string.IsNullOrEmpty(_parameter))
                            {
                                Origin.Numeric(Numeric.ERR_NEEDMOREPARAMS, "MODE");
                                return;
                            }

                            channel.Key = _parameter;
                            Origin.Write(Origin.Prefix(string.Format("MODE {0} :+k {1}", channel.Name, _parameter)));
                        }
                        else
                        {
                            // THE FAMOUS QUARTERDECK EXPLOIT:
                            // Anyone can issue "MODE <chan> -k <key>". If the key matches, it OPS them!
                            if (!string.IsNullOrEmpty(channel.Key) && channel.Key == _parameter)
                            {
                                channel.AddOp(Origin);
                                channel.Broadcast(Origin.Prefix(string.Format("MODE {0} :+o {1}", channel.Name, Origin.Nickname)));
                            }
                        }
                        break;
                    }

                case 'l': // Limit
                    {
                        if (!Origin.IsOp(channel) && !Origin.IsSupervisor)
                        {
                            Origin.Numeric(Numeric.ERR_CHANOPRIVSNEEDED, channel.Name);
                            return;
                        }

                        if (isUp)
                        {
                            int limitVal;
                            if (int.TryParse(_parameter, out limitVal) && limitVal > 0)
                            {
                                channel.Limit = limitVal;
                                channel.Broadcast(Origin.Prefix(string.Format("MODE {0} :+l {1}", channel.Name, limitVal)));
                            }
                        }
                        else
                        {
                            channel.Limit = null;
                            channel.Broadcast(Origin.Prefix(string.Format("MODE {0} :-l", channel.Name)));
                        }
                        break;
                    }

                case 'm': // Moderated
                    {
                        if (!Origin.IsOp(channel) && !Origin.IsSupervisor)
                        {
                            Origin.Numeric(Numeric.ERR_CHANOPRIVSNEEDED, channel.Name);
                            return;
                        }

                        channel.Moderated = isUp;
                        channel.Broadcast(Origin.Prefix(string.Format("MODE {0} :{1}m", channel.Name, _operation.Value)));
                        break;
                    }

                case 'n': // Message Locked
                    {
                        if (!Origin.IsOp(channel) && !Origin.IsSupervisor)
                        {
                            Origin.Numeric(Numeric.ERR_CHANOPRIVSNEEDED, channel.Name);
                            return;
                        }

                        channel.MessageLocked = isUp;
                        channel.Broadcast(Origin.Prefix(string.Format("MODE {0} :{1}n", channel.Name, _operation.Value)));
                        break;
                    }

                case 't': // Topic Locked
                    {
                        if (!Origin.IsOp(channel) && !Origin.IsSupervisor)
                        {
                            Origin.Numeric(Numeric.ERR_CHANOPRIVSNEEDED, channel.Name);
                            return;
                        }

                        channel.TopicLocked = isUp;
                        channel.Broadcast(Origin.Prefix(string.Format("MODE {0} :{1}t", channel.Name, _operation.Value)));
                        break;
                    }

                case 's': // Secret
                    {
                        if (!Origin.IsOp(channel) && !Origin.IsSupervisor)
                        {
                            Origin.Numeric(Numeric.ERR_CHANOPRIVSNEEDED, channel.Name);
                            return;
                        }

                        channel.Secret = isUp;
                        channel.Broadcast(Origin.Prefix(string.Format("MODE {0} :{1}s", channel.Name, _operation.Value)));
                        break;
                    }

                case 'b': // Bozo filter
                    {
                        if (!isUp && !string.IsNullOrEmpty(_parameter))
                        {
                            channel.RemoveBozo(_parameter);
                        }
                        else
                        {
                            foreach (var ip in channel.GetActiveBozos())
                            {
                                Origin.Numeric(Numeric.RPL_BANLIST, channel.Name, ip);
                            }
                            Origin.Numeric(Numeric.RPL_ENDOFBANLIST, channel.Name);
                        }
                        break;
                    }

                case 'f': // Permanent (Supervisor only)
                    {
                        if (!Origin.IsSupervisor)
                        {
                            Origin.Numeric(Numeric.ERR_NOPRIVILEGES);
                            return;
                        }

                        channel.Permanent = isUp;
                        channel.Broadcast(Origin.Prefix(string.Format("MODE {0} :{1}f", channel.Name, _operation.Value)));
                        break;
                    }

                default:
                    Origin.Numeric(Numeric.ERR_UNKNOWNMODE, m.ToString());
                    break;
            }
        }

        private void RunUserMode()
        {
            User targetUser = Server.FindUser(_target);
            if (targetUser == null)
            {
                Origin.Numeric(Numeric.ERR_NOSUCHNICK, _target);
                return;
            }

            if (Origin != targetUser && !Origin.IsSupervisor)
            {
                Origin.Numeric(Numeric.ERR_USERSDONTMATCH);
                return;
            }

            // Query user modes
            if (!_operation.HasValue || !_modeChar.HasValue)
            {
                string modes = "";
                if (targetUser.IsGagged) modes += "g";
                if (targetUser.IsPrivate) modes += "p";
                if (targetUser.IsSupervisor) modes += "s";

                Origin.Numeric(Numeric.RPL_UMODEIS, "+" + modes);
                return;
            }

            bool isUp = _operation.Value == '+';
            char m = _modeChar.Value;

            switch (m)
            {
                case 'p': // Private
                    targetUser.IsPrivate = isUp;
                    string pMsg = Origin.Prefix(string.Format("MODE {0} :{1}p", targetUser.Nickname, _operation.Value));
                    targetUser.Write(pMsg);
                    if (Origin != targetUser) Origin.Write(pMsg);
                    break;

                case 'q': // Qlimit
                    if (isUp)
                    {
                        long qlimit;
                        if (long.TryParse(_parameter, out qlimit))
                        {
                            targetUser.QLimit = qlimit;
                        }
                    }
                    else
                    {
                        targetUser.QLimit = User.DefaultQLimit;
                    }
                    break;

                case 's': // Supervisor
                    if (isUp)
                    {
                        if (Origin == targetUser)
                        {
                            if (string.IsNullOrEmpty(_parameter))
                            {
                                Origin.Numeric(Numeric.ERR_NEEDMOREPARAMS, "MODE");
                                return;
                            }
                            if (Server.Superpass != _parameter)
                            {
                                Origin.Numeric(Numeric.ERR_PASSWDMISTMATCH);
                                return;
                            }
                        }

                        Server.AddSupervisor(targetUser);
                        string sMsg = Origin.Prefix(string.Format("MODE {0} :+s", targetUser.Nickname));
                        targetUser.Write(sMsg);
                        if (Origin != targetUser) Origin.Write(sMsg);
                    }
                    else
                    {
                        Server.DeleteSupervisor(targetUser);
                        string sMsg = Origin.Prefix(string.Format("MODE {0} :-s", targetUser.Nickname));
                        targetUser.Write(sMsg);
                        if (Origin != targetUser) Origin.Write(sMsg);
                    }
                    break;

                case 'z': // Kill
                    targetUser.Disconnect(string.Format("KILL from {0}", Origin.Nickname));
                    break;

                default:
                    Origin.Numeric(Numeric.ERR_UMODEUNKNOWNFLAG);
                    break;
            }
        }
    }
}
