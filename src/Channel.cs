//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Channel.cs: channel management, modes, bozo list, and spoof matching
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GStage
{
    public class Channel
    {
        public const int BozoTimeoutSeconds = 180;

        private readonly object _syncRoot = new object();
        private readonly Server _server;
        private readonly string _name;
        private readonly string _match;

        private string _key;
        private int? _limit;
        private bool _messageLocked = true; // +n (default on)
        private bool _topicLocked = true;   // +t (default on)
        private bool _moderated = false;    // +m
        private bool _permanent = false;    // +f
        private bool _secret = false;       // +s
        private string _topic = "(No topic set)";

        private readonly Dictionary<string, User> _userList = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _opList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _halfOpList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _voiceList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> _bozoList = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        // Famous Quarterdeck Global Stage memory leak / extended ASCII character lookup table
        private static readonly byte[] Spoofs = new byte[]
        {
            0,   1,   2,   3,   4,   5,   6,   7,   8,   9,   10,  11,  12,  13,  14,  15,
            16,  17,  18,  19,  20,  21,  22,  23,  24,  25,  26,  27,  28,  29,  30,  31,
            32,  33,  34,  35,  36,  37,  38,  39,  40,  41,  42,  43,  44,  45,  46,  47,
            48,  49,  50,  51,  52,  53,  54,  55,  56,  57,  58,  59,  60,  61,  62,  63,
            64,  65,  66,  67,  68,  69,  70,  71,  72,  73,  74,  75,  76,  77,  78,  79,
            80,  81,  82,  83,  84,  85,  86,  87,  88,  89,  90,  91,  92,  93,  94,  95,
            96,  97,  98,  99,  100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111,
            112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127,
            128, 129, 105, 114, 116, 117, 97,  108, 136, 137, 105, 109, 101, 114, 142, 143,
            120, 112, 105, 114, 101, 100, 150, 151, 114, 111, 102, 105, 108, 105, 110, 103,
            160, 161, 105, 109, 101, 114, 166, 167, 120, 112, 105, 114, 101, 100, 174, 175,
            112, 117, 178, 179, 105, 109, 105, 116, 184, 185, 120, 99,  101, 101, 100, 101,
            100, 193, 194, 105, 108, 101, 198, 199, 105, 122, 101, 203, 204, 105, 109, 105,
            116, 209, 210, 120, 99,  101, 101, 100, 101, 100, 218, 219, 111, 221, 114, 117,
            110, 110, 97,  98,  108, 101, 230, 108, 119, 112, 234, 235, 110, 116, 101, 114,
            240, 108, 119, 112, 244, 115, 105, 103, 110, 97,  108, 251, 252, 253, 254, 255
        };

        public Channel(Server server, string name)
        {
            _server = server;
            _name = name;
            _match = GenerateMatch(name);
        }

        public Server Server { get { return _server; } }
        public string Name { get { return _name; } }
        public string Match { get { return _match; } }

        public string Key
        {
            get { lock (_syncRoot) return _key; }
            set { lock (_syncRoot) _key = value; }
        }

        public int? Limit
        {
            get { lock (_syncRoot) return _limit; }
            set { lock (_syncRoot) _limit = value; }
        }

        public bool MessageLocked
        {
            get { lock (_syncRoot) return _messageLocked; }
            set { lock (_syncRoot) _messageLocked = value; }
        }

        public bool TopicLocked
        {
            get { lock (_syncRoot) return _topicLocked; }
            set { lock (_syncRoot) _topicLocked = value; }
        }

        public bool Moderated
        {
            get { lock (_syncRoot) return _moderated; }
            set { lock (_syncRoot) _moderated = value; }
        }

        public bool Permanent
        {
            get { lock (_syncRoot) return _permanent; }
            set { lock (_syncRoot) _permanent = value; }
        }

        public bool Secret
        {
            get { lock (_syncRoot) return _secret; }
            set { lock (_syncRoot) _secret = value; }
        }

        public string Topic
        {
            get { lock (_syncRoot) return _topic; }
            set { lock (_syncRoot) _topic = value ?? ""; }
        }

        public static string GenerateMatch(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string lower = name.ToLowerInvariant();
            byte[] bytes = Encoding.GetEncoding(28591).GetBytes(lower); // ISO-8859-1 for full byte fidelity
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Spoofs[bytes[i]];
            }
            return Encoding.GetEncoding(28591).GetString(bytes);
        }

        public void UpdateUserMatch(string oldMatch, string newMatch, User user)
        {
            lock (_syncRoot)
            {
                if (_userList.ContainsKey(oldMatch))
                {
                    _userList.Remove(oldMatch);
                    _userList[newMatch] = user;
                }
                if (_opList.Contains(oldMatch))
                {
                    _opList.Remove(oldMatch);
                    _opList.Add(newMatch);
                }
                if (_halfOpList.Contains(oldMatch))
                {
                    _halfOpList.Remove(oldMatch);
                    _halfOpList.Add(newMatch);
                }
                if (_voiceList.Contains(oldMatch))
                {
                    _voiceList.Remove(oldMatch);
                    _voiceList.Add(newMatch);
                }
            }
        }

        public void AddUser(User user)
        {
            lock (_syncRoot)
            {
                _userList[user.Match] = user;
            }
            user.AddToChannel(this);
        }

        public bool IsOn(User user)
        {
            lock (_syncRoot)
            {
                return _userList.ContainsKey(user.Match);
            }
        }

        public void DeleteUser(User user)
        {
            bool shouldDestroy = false;
            lock (_syncRoot)
            {
                _userList.Remove(user.Match);
                _opList.Remove(user.Match);
                _halfOpList.Remove(user.Match);
                _voiceList.Remove(user.Match);

                if (_userList.Count == 0 && !_permanent)
                {
                    shouldDestroy = true;
                }
            }

            user.RemoveFromChannel(this);

            if (shouldDestroy)
            {
                _server.DestroyChannel(this);
            }
        }

        public void AddOp(User user)
        {
            lock (_syncRoot) _opList.Add(user.Match);
        }

        public bool IsOp(User user)
        {
            lock (_syncRoot) return _opList.Contains(user.Match);
        }

        public void DeleteOp(User user)
        {
            lock (_syncRoot) _opList.Remove(user.Match);
        }

        public void AddHalfOp(User user)
        {
            lock (_syncRoot) _halfOpList.Add(user.Match);
        }

        public bool IsHalfOp(User user)
        {
            lock (_syncRoot) return _halfOpList.Contains(user.Match);
        }

        public void DeleteHalfOp(User user)
        {
            lock (_syncRoot) _halfOpList.Remove(user.Match);
        }

        public void AddVoice(User user)
        {
            lock (_syncRoot) _voiceList.Add(user.Match);
        }

        public bool IsVoice(User user)
        {
            lock (_syncRoot) return _voiceList.Contains(user.Match);
        }

        public void DeleteVoice(User user)
        {
            lock (_syncRoot) _voiceList.Remove(user.Match);
        }

        public void AddBozo(User user)
        {
            lock (_syncRoot)
            {
                _bozoList[user.PeerHost] = DateTime.UtcNow.AddSeconds(BozoTimeoutSeconds);
            }
        }

        public bool IsBozo(User user)
        {
            lock (_syncRoot)
            {
                DateTime exp;
                if (_bozoList.TryGetValue(user.PeerHost, out exp))
                {
                    if (DateTime.UtcNow <= exp) return true;
                    _bozoList.Remove(user.PeerHost);
                }
                return false;
            }
        }

        public void RemoveBozo(string ipOrHost)
        {
            lock (_syncRoot)
            {
                _bozoList.Remove(ipOrHost);
            }
        }

        public List<string> GetActiveBozos()
        {
            lock (_syncRoot)
            {
                var now = DateTime.UtcNow;
                var list = new List<string>();
                var expired = new List<string>();
                foreach (var kvp in _bozoList)
                {
                    if (now <= kvp.Value)
                    {
                        list.Add(kvp.Key);
                    }
                    else
                    {
                        expired.Add(kvp.Key);
                    }
                }
                foreach (var exp in expired) _bozoList.Remove(exp);
                return list;
            }
        }

        public List<User> GetUsers()
        {
            lock (_syncRoot)
            {
                return new List<User>(_userList.Values);
            }
        }

        public int UserCount
        {
            get
            {
                lock (_syncRoot) return _userList.Count;
            }
        }

        public void Broadcast(string message, User skipOrigin = null)
        {
            List<User> recipients;
            lock (_syncRoot)
            {
                recipients = new List<User>(_userList.Values);
            }

            foreach (var user in recipients)
            {
                if (skipOrigin != null && user == skipOrigin) continue;
                user.Write(message);
            }
        }
    }
}
