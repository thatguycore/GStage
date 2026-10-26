//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Commands/TopicCommand.cs: handle TOPIC commands
//

using System;
using System.Text.RegularExpressions;

namespace GStage.Commands
{
    public class TopicCommand : Command
    {
        private static readonly Regex TopicRegex = new Regex(@"^(\S+)\s*(.*)$");

        private string _channelName;
        private string _newTopic;
        private bool _isQuery;

        public TopicCommand(Server server, User origin, string rawArguments)
            : base(server, origin, rawArguments)
        {
            Parse(rawArguments);
        }

        private void Parse(string args)
        {
            Match m = TopicRegex.Match(args ?? "");
            if (m.Success)
            {
                _channelName = m.Groups[1].Value.Trim();
                string topicPart = m.Groups[2].Value;
                if (!string.IsNullOrEmpty(topicPart))
                {
                    _newTopic = StripLeadingColon(topicPart.Trim());
                    _isQuery = false;
                }
                else
                {
                    _isQuery = true;
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

            if (string.IsNullOrEmpty(_channelName))
            {
                Origin.Numeric(Numeric.ERR_NEEDMOREPARAMS, "TOPIC");
                return;
            }

            Channel channel = Server.FindChannel(_channelName);
            if (channel == null)
            {
                Origin.Numeric(Numeric.ERR_NOSUCHCHANNEL, _channelName);
                return;
            }

            if (_isQuery)
            {
                if (string.IsNullOrEmpty(channel.Topic) || channel.Topic == "(No topic set)")
                {
                    Origin.Numeric(Numeric.RPL_NOTOPIC, channel.Name);
                }
                else
                {
                    Origin.Numeric(Numeric.RPL_TOPIC, channel.Name, channel.Topic);
                }
                return;
            }

            if (!Origin.IsOn(channel) && !Origin.IsSupervisor)
            {
                Origin.Numeric(Numeric.ERR_NOTONCHANNEL, _channelName);
                return;
            }

            if (channel.TopicLocked && !Origin.IsOp(channel) && !Origin.IsSupervisor)
            {
                Origin.Numeric(Numeric.ERR_CHANOPRIVSNEEDED, _channelName);
                return;
            }

            channel.Topic = _newTopic;
            channel.Broadcast(Origin.Prefix(string.Format("TOPIC {0} :{1}", channel.Name, _newTopic)));
        }
    }
}
