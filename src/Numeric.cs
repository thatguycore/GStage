//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Numeric.cs: RFC 1459 numeric codes and response formats
//

using System;
using System.Collections.Generic;

namespace GStage
{
    public static class Numeric
    {
        // WELCOME
        public const int RPL_WELCOME           = 1;
        public const int RPL_YOURHOST          = 2;
        public const int RPL_CREATED           = 3;
        public const int RPL_MYINFO            = 4;
        public const int RPL_BOUNCE            = 5;

        // TRACE / STATS
        public const int RPL_TRACELINK         = 200;
        public const int RPL_TRACECONNECTING   = 201;
        public const int RPL_TRACEHANDSHAKE    = 202;
        public const int RPL_TRACEUNKNOWN      = 203;
        public const int RPL_TRACEOPERATOR     = 204;
        public const int RPL_TRACEUSER         = 205;
        public const int RPL_TRACESERVER       = 206;
        public const int RPL_TRACENEWTYPE      = 208;
        public const int RPL_TRACECLASS        = 209;
        public const int RPL_STATSLINKINFO     = 211;
        public const int RPL_STATSCOMMANDS     = 212;
        public const int RPL_STATSCLINE        = 213;
        public const int RPL_STATSNLINE        = 214;
        public const int RPL_STATSILINE        = 215;
        public const int RPL_STATSKLINE        = 216;
        public const int RPL_STATSQLINE        = 217;
        public const int RPL_STATSYLINE        = 218;
        public const int RPL_ENDOFSTATS        = 219;
        public const int RPL_UMODEIS           = 221;
        public const int RPL_SERVICEINFO       = 231;
        public const int RPL_ENDOFSERVICES     = 232;
        public const int RPL_SERVICE           = 233;
        public const int RPL_SERVLIST          = 234;
        public const int RPL_SERVICELISTEND    = 235;
        public const int RPL_STATSLLINE        = 241;
        public const int RPL_STATSUPTIME       = 242;
        public const int RPL_STATSOLINE        = 243;
        public const int RPL_STATSHLINE        = 244;
        public const int RPL_LUSERCLIENT       = 251;
        public const int RPL_LUSEROP           = 252;
        public const int RPL_LUSERUNKNOWN      = 253;
        public const int RPL_LUSERCHANNELS     = 254;
        public const int RPL_LUSERME           = 255;
        public const int RPL_ADMINME           = 256;
        public const int RPL_ADMINLOC1         = 257;
        public const int RPL_ADMINLOC2         = 258;
        public const int RPL_ADMINEMAIL        = 259;
        public const int RPL_TRACELOG          = 261;

        // COMMAND REPLIES
        public const int RPL_NONE              = 300;
        public const int RPL_AWAY              = 301;
        public const int RPL_USERHOST          = 302;
        public const int RPL_ISON              = 303;
        public const int RPL_UNAWAY            = 305;
        public const int RPL_NOWAWAY           = 306;
        public const int RPL_WHOISUSER         = 311;
        public const int RPL_WHOISSERVER       = 312;
        public const int RPL_WHOISOPERATOR     = 313;
        public const int RPL_WHOWASUSER        = 314;
        public const int RPL_ENDOFWHO          = 315;
        public const int RPL_WHOISCHANOP       = 316;
        public const int RPL_WHOISIDLE         = 317;
        public const int RPL_ENDOFWHOIS        = 318;
        public const int RPL_WHOISCHANNELS     = 319;
        public const int RPL_LISTSTART         = 321;
        public const int RPL_LIST              = 322;
        public const int RPL_LISTEND           = 323;
        public const int RPL_CHANNELMODEIS     = 324;
        public const int RPL_NOTOPIC           = 331;
        public const int RPL_TOPIC             = 332;
        public const int RPL_INVITING          = 341;
        public const int RPL_SUMMONING         = 342;
        public const int RPL_VERSION           = 351;
        public const int RPL_WHOREPLY          = 352;
        public const int RPL_NAMEREPLY         = 353;
        public const int RPL_KILLDONE          = 361;
        public const int RPL_CLOSING           = 362;
        public const int RPL_CLOSEEND          = 363;
        public const int RPL_LINKS             = 364;
        public const int RPL_ENDOFLINKS        = 365;
        public const int RPL_ENDOFNAMES        = 366;
        public const int RPL_BANLIST           = 367;
        public const int RPL_ENDOFBANLIST      = 368;
        public const int RPL_ENDOFWHOWAS       = 369;
        public const int RPL_INFO              = 371;
        public const int RPL_MOTD              = 372;
        public const int RPL_INFOSTART         = 373;
        public const int RPL_ENDOFINFO         = 374;
        public const int RPL_MOTDSTART         = 375;
        public const int RPL_ENDOFMOTD         = 376;
        public const int RPL_YOUREOPER         = 381;
        public const int RPL_REHASHING         = 382;
        public const int RPL_MYPORTIS          = 384;
        public const int RPL_TIME              = 391;
        public const int RPL_USERSSTART        = 392;
        public const int RPL_USERS             = 393;
        public const int RPL_ENDOFUSERS        = 394;
        public const int RPL_NOUSERS           = 395;

        // ERRORS
        public const int ERR_NOSUCHNICK        = 401;
        public const int ERR_NOSUCHSERVER      = 402;
        public const int ERR_NOSUCHCHANNEL     = 403;
        public const int ERR_CANNOTSENDTOCHAN  = 404;
        public const int ERR_TOOMANYCHANNELS   = 405;
        public const int ERR_WASNOSUCHNICK     = 406;
        public const int ERR_TOOMANYTARGETS    = 407;
        public const int ERR_NOORIGIN          = 409;
        public const int ERR_NORECIPIENT       = 411;
        public const int ERR_NOTEXTTOSEND      = 412;
        public const int ERR_NOTOPLEVEL        = 413;
        public const int ERR_WILDTOPLEVEL      = 414;
        public const int ERR_UNKNOWNCOMMAND    = 421;
        public const int ERR_NOMOTD            = 422;
        public const int ERR_NOADMININFO       = 423;
        public const int ERR_FILEERROR         = 424;
        public const int ERR_NONICKNAMEGIVEN   = 431;
        public const int ERR_ERRONEOUSNICKNAME = 432;
        public const int ERR_NICKNAMEINUSE     = 433;
        public const int ERR_NICKCOLLISION     = 436;
        public const int ERR_USERNOTINCHANNEL  = 441;
        public const int ERR_NOTONCHANNEL      = 442;
        public const int ERR_USERONCHANNEL     = 443;
        public const int ERR_NOLOGIN           = 444;
        public const int ERR_SUMMONDISABLED    = 445;
        public const int ERR_USERSDISABLED     = 446;
        public const int ERR_NOTREGISTERED     = 451;
        public const int ERR_NEEDMOREPARAMS    = 461;
        public const int ERR_ALREADYREGISTERED = 462;
        public const int ERR_NOPERMFORHOST     = 463;
        public const int ERR_PASSWDMISTMATCH   = 464;
        public const int ERR_YOUREBANNEDCREEP  = 465;
        public const int ERR_YOUWILLBEBANNED   = 466;
        public const int ERR_KEYSET            = 467;
        public const int ERR_CHANNELISFULL     = 471;
        public const int ERR_UNKNOWNMODE       = 472;
        public const int ERR_INVITEONLYCHAN    = 473;
        public const int ERR_BANNEDFROMCHAN    = 474;
        public const int ERR_BADCHANNELKEY     = 475;
        public const int ERR_BADCHANMASK       = 476;
        public const int ERR_NOPRIVILEGES      = 481;
        public const int ERR_CHANOPRIVSNEEDED  = 482;
        public const int ERR_CANTKILLSERVER    = 483;
        public const int ERR_NOOPERHOST        = 491;
        public const int ERR_NOSERVICEHOST     = 492;
        public const int ERR_UMODEUNKNOWNFLAG  = 501;
        public const int ERR_USERSDONTMATCH    = 502;

        private static readonly Dictionary<int, string> Formats = new Dictionary<int, string>
        {
            // WELCOME
            { RPL_WELCOME          , ":Welcome to the Internet Relay Network, {0}" },
            { RPL_YOURHOST         , ":Your host is {0}, running on glorious steam" },
            { RPL_CREATED          , ":This server was created on {0}" },
            { RPL_MYINFO           , ":{0} {1} +{2} +{3}" },
            { RPL_BOUNCE           , ":Try server {0}, port {1}" },

            // ERRORS
            { ERR_NOSUCHNICK       , "{0} :No such nick/channel" },
            { ERR_NOSUCHSERVER     , "{0} :No such server" },
            { ERR_NOSUCHCHANNEL    , "{0} :No such channel" },
            { ERR_CANNOTSENDTOCHAN , "{0} :Cannot send to channel" },
            { ERR_TOOMANYCHANNELS  , "{0} :You have joined too many channels" },
            { ERR_WASNOSUCHNICK    , "{0} :There was no such nickname" },
            { ERR_TOOMANYTARGETS   , "{0} :Duplicate recipients. No message delivered" },
            { ERR_NOORIGIN         , ":No origin specified" },
            { ERR_NORECIPIENT      , ":No recipient given ({0})" },
            { ERR_NOTEXTTOSEND     , ":No text to send" },
            { ERR_NOTOPLEVEL       , "{0} :No toplevel domain specified" },
            { ERR_WILDTOPLEVEL     , "{0} :Wildcard in toplevel domain" },
            { ERR_UNKNOWNCOMMAND   , "{0} :Unknown command" },
            { ERR_NOMOTD           , ":MOTD File is missing" },
            { ERR_NOADMININFO      , "{0} :No administrative info available" },
            { ERR_FILEERROR        , ":File error doing {0} on {1}" },
            { ERR_NONICKNAMEGIVEN  , ":No nickname given" },
            { ERR_ERRONEOUSNICKNAME, "{0} :Erroneous nickname" },
            { ERR_NICKNAMEINUSE    , "{0} :Nickname is already in use" },
            { ERR_NICKCOLLISION    , "{0} :Nickname collision KILL" },
            { ERR_USERNOTINCHANNEL , "{0} {1} :They aren't on that channel" },
            { ERR_NOTONCHANNEL     , "{0} :You're not on that channel" },
            { ERR_USERONCHANNEL    , "{0} {1} :is already on channel" },
            { ERR_NOLOGIN          , "{0} :User not logged in" },
            { ERR_SUMMONDISABLED   , ":SUMMON has been disabled" },
            { ERR_USERSDISABLED    , ":USERS has been disabled" },
            { ERR_NOTREGISTERED    , ":You have not registered" },
            { ERR_NEEDMOREPARAMS   , "{0} :Not enough parameters" },
            { ERR_ALREADYREGISTERED, ":You may not reregister" },
            { ERR_NOPERMFORHOST    , ":Your host isn't among the privileged" },
            { ERR_PASSWDMISTMATCH  , ":Password incorrect" },
            { ERR_YOUREBANNEDCREEP , ":You are banned from this server" },
            { ERR_KEYSET           , "{0} :Channel key already set" },
            { ERR_CHANNELISFULL    , "{0} :Cannot join channel (+l)" },
            { ERR_UNKNOWNMODE      , "{0} :is unknown mode char to me" },
            { ERR_INVITEONLYCHAN   , "{0} :Cannot join channel (+i)" },
            { ERR_BANNEDFROMCHAN   , "{0} :Cannot join channel (+b)" },
            { ERR_BADCHANNELKEY    , "{0} :Cannot join channel (+k)" },
            { ERR_NOPRIVILEGES     , ":Permission Denied-You're not an IRC operator" },
            { ERR_CHANOPRIVSNEEDED , "{0} :You're not channel operator" },
            { ERR_CANTKILLSERVER   , ":You cant kill a server!" },
            { ERR_NOOPERHOST       , "No O-lines for your host" },
            { ERR_UMODEUNKNOWNFLAG , ":Unknown MODE flag" },
            { ERR_USERSDONTMATCH   , ":Cant change mode for other users" },

            // COMMAND REPLIES
            { RPL_NONE             , "" },
            { RPL_USERHOST         , ":{0}" },
            { RPL_ISON             , ":{0}" },
            { RPL_AWAY             , "{0} :{1}" },
            { RPL_UNAWAY           , ":You are no longer marked as being away" },
            { RPL_NOWAWAY          , ":You have been marked as being away" },
            { RPL_WHOISUSER        , "{0} {1} {2} * :{3}" },
            { RPL_WHOISSERVER      , "{0} {1} :{2}" },
            { RPL_WHOISOPERATOR    , "{0} :is an IRC operator" },
            { RPL_WHOISIDLE        , "{0} {1} :seconds idle" },
            { RPL_ENDOFWHOIS       , "{0} :End of /WHOIS list" },
            { RPL_WHOISCHANNELS    , "{0} :{1}" },
            { RPL_WHOWASUSER       , "{0} {1} {2} * :{3}" },
            { RPL_ENDOFWHOWAS      , "{0} :End of WHOWAS" },
            { RPL_LISTSTART        , "Channel :Users Name" },
            { RPL_LIST             , "{0} {1} :{2}" },
            { RPL_LISTEND          , ":End of /LIST" },
            { RPL_CHANNELMODEIS    , "{0} {1}" },
            { RPL_NOTOPIC          , "{0} :No topic is set" },
            { RPL_TOPIC            , "{0} :{1}" },
            { RPL_INVITING         , "{0} {1}" },
            { RPL_SUMMONING        , "{0} :Summoning user to IRC" },
            { RPL_VERSION          , "{0}.{1} {2} :{3}" },
            { RPL_WHOREPLY         , "{0} {1} {2} {3} {4} H :0 {5}" },
            { RPL_ENDOFWHO         , "{0} :End of /WHO list" },
            { RPL_NAMEREPLY        , "= {0} :{1}" },
            { RPL_ENDOFNAMES       , "{0} :End of /NAMES" },
            { RPL_LINKS            , "{0} {1} :{2} {3}" },
            { RPL_ENDOFLINKS       , "{0} :End of /LINKS list" },
            { RPL_BANLIST          , "{0} {1}" },
            { RPL_ENDOFBANLIST     , "{0} :End of channel ban list" },
            { RPL_INFO             , ":{0}" },
            { RPL_ENDOFINFO        , ":End of /INFO list" },
            { RPL_MOTDSTART        , ":- {0} Message of the day - " },
            { RPL_MOTD             , ":- {0}" },
            { RPL_ENDOFMOTD        , ":End of /MOTD command" },
            { RPL_YOUREOPER        , ":You are now an IRC operator" },
            { RPL_REHASHING        , "{0} :Rehashing" },
            { RPL_TIME             , "{0} :{1}" },
            { RPL_UMODEIS          , "{0}" }
        };

        public static string Format(int numeric, params object[] args)
        {
            string fmt;
            if (Formats.TryGetValue(numeric, out fmt))
            {
                if (args == null || args.Length == 0) return fmt;
                try
                {
                    return string.Format(fmt, args);
                }
                catch
                {
                    return fmt;
                }
            }
            return args != null && args.Length > 0 ? string.Join(" ", args) : string.Empty;
        }
    }
}
