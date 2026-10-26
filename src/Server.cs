//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/Server.cs: central server engine, connection listener, and state management
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GStage
{
    public class Server
    {
        private readonly object _syncRoot = new object();
        private readonly string _name;
        private readonly string _network;
        private readonly int _port;
        private readonly IPAddress _bindAddress;
        private readonly DateTime _created;
        private readonly string _superpass;

        private readonly List<string> _motd = new List<string>();
        private readonly List<string[]> _hostmasks = new List<string[]>();

        private readonly Dictionary<string, User> _registeredUsers = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<User> _unregisteredUsers = new HashSet<User>();
        private readonly Dictionary<string, Channel> _channels = new Dictionary<string, Channel>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<User> _supervisors = new HashSet<User>();

        private TcpListener _listener;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _isRunning = false;

        private static readonly Regex CommandRegex = new Regex(@"^\s*(\S+)(?:\s+(.*))?$");

        public Server(string name = "GStage", string network = "local", int port = 6667,
                      IPAddress bindAddress = null, string superpass = "god", string motdPath = null)
        {
            _name = name;
            _network = network;
            _port = port;
            _bindAddress = bindAddress ?? IPAddress.Any;
            _superpass = superpass;
            _created = DateTime.Now;

            LoadMotd(motdPath);
            LoadHostmasks();
        }

        public string Name { get { return _name; } }
        public string Network { get { return _network; } }
        public string Address { get { return string.Format("{0}.{1}", _name, _network); } }
        public int Port { get { return _port; } }
        public string Created { get { return _created.ToString("ddd MMM dd yyyy 'at' HH:mm:ss"); } }
        public string Superpass { get { return _superpass; } }

        public void AddSupervisor(User user)
        {
            lock (_syncRoot)
            {
                _supervisors.Add(user);
            }
        }

        public bool IsSupervisor(User user)
        {
            lock (_syncRoot)
            {
                return _supervisors.Contains(user);
            }
        }

        public void DeleteSupervisor(User user)
        {
            lock (_syncRoot)
            {
                _supervisors.Remove(user);
            }
        }

        public Channel FindChannel(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string match = Channel.GenerateMatch(name);
            lock (_syncRoot)
            {
                Channel chan;
                _channels.TryGetValue(match, out chan);
                return chan;
            }
        }

        public Channel CreateChannel(string name)
        {
            Channel chan = new Channel(this, name);
            lock (_syncRoot)
            {
                _channels[chan.Match] = chan;
            }
            return chan;
        }

        public void DestroyChannel(Channel channel)
        {
            lock (_syncRoot)
            {
                _channels.Remove(channel.Match);
            }
        }

        public List<Channel> GetChannels()
        {
            lock (_syncRoot)
            {
                return new List<Channel>(_channels.Values);
            }
        }

        public User FindUser(string nickname)
        {
            if (string.IsNullOrEmpty(nickname)) return null;
            string match = User.GenerateMatch(nickname);
            lock (_syncRoot)
            {
                User user;
                _registeredUsers.TryGetValue(match, out user);
                return user;
            }
        }

        public List<User> GetUsers()
        {
            lock (_syncRoot)
            {
                return new List<User>(_registeredUsers.Values);
            }
        }

        public void Register(User user, string nickname)
        {
            lock (_syncRoot)
            {
                _unregisteredUsers.Remove(user);

                // If user already had a nickname and was registered, update channels
                if (!string.IsNullOrEmpty(user.Nickname))
                {
                    string oldMatch = user.Match;
                    string newMatch = User.GenerateMatch(nickname);

                    _registeredUsers.Remove(oldMatch);

                    user.Nickname = nickname;
                    _registeredUsers[newMatch] = user;

                    foreach (var channel in user.GetChannels())
                    {
                        channel.UpdateUserMatch(oldMatch, newMatch, user);
                    }
                    return;
                }

                user.Nickname = nickname;
                _registeredUsers[user.Match] = user;
            }
        }

        public void Disconnect(User user, string message)
        {
            string quitMsg = string.IsNullOrEmpty(message) ? "(signed off)" : message;
            string formattedQuit = user.Prefix("QUIT :" + quitMsg);

            List<Channel> channels;
            lock (_syncRoot)
            {
                _unregisteredUsers.Remove(user);
                _registeredUsers.Remove(user.Match);
                _supervisors.Remove(user);
                channels = user.GetChannels();
            }

            var broadcastUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var chan in channels)
            {
                foreach (var u in chan.GetUsers())
                {
                    if (u != user && !broadcastUsers.Contains(u.Match))
                    {
                        broadcastUsers.Add(u.Match);
                        u.Write(formattedQuit);
                    }
                }
                chan.DeleteUser(user);
            }

            Console.WriteLine("[Client Disconnected] {0} ({1})", user.Nickname ?? user.PeerHost, quitMsg);
        }

        public void Welcome(User user)
        {
            user.Numeric(Numeric.RPL_WELCOME, user.Nickname);
            user.Numeric(Numeric.RPL_YOURHOST, Address);
            user.Numeric(Numeric.RPL_CREATED, Created);
            user.Numeric(Numeric.RPL_MYINFO, Name, "0.1", "qs", "imnst");

            SendMotd(user);
            Console.WriteLine("[Client Registered] {0}!{1}@{2}", user.Nickname, user.Username, user.Mask);
        }

        public void SendMotd(User user)
        {
            user.Numeric(Numeric.RPL_MOTDSTART, Address);
            foreach (var line in _motd)
            {
                user.Numeric(Numeric.RPL_MOTD, line);
            }
            user.Numeric(Numeric.RPL_ENDOFMOTD);
        }

        public void ProcessLine(User user, string line)
        {
            Match m = CommandRegex.Match(line);
            if (!m.Success) return;

            string verb = m.Groups[1].Value.Trim();
            string args = m.Groups[2].Value.Trim();

            Command cmd = Command.Create(verb, this, user, args);
            if (cmd != null)
            {
                try
                {
                    cmd.Run();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Command Error] {0} from {1}: {2}", verb, user.Nickname ?? user.PeerHost, ex.Message);
                }
            }
            else
            {
                user.Numeric(Numeric.ERR_UNKNOWNCOMMAND, verb.ToUpperInvariant());
            }
        }

        public string GenerateHostmask(string peerIp)
        {
            try
            {
                IPAddress ip;
                if (!IPAddress.TryParse(peerIp, out ip))
                {
                    ip = IPAddress.Loopback;
                }
                byte[] bytes = ip.GetAddressBytes();
                if (bytes.Length == 4)
                {
                    // Reverse octets as in Quarterdeck GStage: d.c.b.a
                    Array.Reverse(bytes);
                    string mask0 = _hostmasks[0][bytes[0]];
                    string mask1 = _hostmasks[1][bytes[1]];
                    string mask2 = _hostmasks[2][bytes[2]];
                    string mask3 = _hostmasks[3][bytes[3]];
                    return string.Format("{0}.{1}.{2}.{3}.this.chat.server", mask0, mask1, mask2, mask3);
                }
            }
            catch { }

            return "local.host.chat.server";
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(_bindAddress, _port);
            _listener.Start(100);
            _isRunning = true;

            Console.WriteLine("===================================================================");
            Console.WriteLine(" GStage IRC Server - Native Windows 11 Edition");
            Console.WriteLine(" Emulating Quarterdeck 'Global Stage' IRC Server");
            Console.WriteLine(" Listening on {0}:{1} (Network: {2})", _bindAddress, _port, Address);
            Console.WriteLine("===================================================================");

            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    client.NoDelay = true;

                    User user = new User(this, client);
                    lock (_syncRoot)
                    {
                        _unregisteredUsers.Add(user);
                    }

                    Console.WriteLine("[Client Connected] from {0}", user.PeerHost);

                    // Launch async read task without blocking accept loop
                    var readTask = Task.Run(async () => await user.StartReadingAsync());
                }
            }
            catch (ObjectDisposedException)
            {
                // Expected when listener stops
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Console.WriteLine("[Listener Exception] {0}", ex.Message);
                }
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;

            Console.WriteLine("Shutting down GStage server...");
            _cts.Cancel();

            try { _listener.Stop(); } catch { }

            List<User> allUsers;
            lock (_syncRoot)
            {
                allUsers = _registeredUsers.Values.Concat(_unregisteredUsers).ToList();
            }

            foreach (var user in allUsers)
            {
                user.Disconnect("Server shutting down");
            }

            Console.WriteLine("Server stopped cleanly.");
        }

        private void LoadMotd(string customPath)
        {
            string path = customPath;
            if (string.IsNullOrEmpty(path))
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string checkPath = Path.Combine(baseDir, "etc", "MOTD");
                if (File.Exists(checkPath)) path = checkPath;
                else if (File.Exists("etc/MOTD")) path = "etc/MOTD";
            }

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    _motd.AddRange(File.ReadAllLines(path));
                    return;
                }
                catch { }
            }

            // Built-in fallback: authentic SciFi Channel Dominion MOTD
            _motd.Add("********************************************************");
            _motd.Add("********************************************************");
            _motd.Add("");
            _motd.Add("                 WELCOME TO THE DOMINION");
            _motd.Add("");
            _motd.Add("                          .eee.._:@$*e");
            _motd.Add("                        .@$$$$$@.  .e*'");
            _motd.Add("                     .,.O$$$$$$$O*''");
            _motd.Add("                    @* :*$$$$$*'");
            _motd.Add("                   '@$#*\"''***'");
            _motd.Add("");
            _motd.Add("         General chatting takes place in #dominion");
            _motd.Add("       Special events are usually in the #auditorium");
            _motd.Add("");
            _motd.Add("********************************************************");
            _motd.Add("********************************************************");
            _motd.Add("");
            _motd.Add("To find out about other places to chat on the Internet, visit http://www.globalchat.com/");
        }

        private void LoadHostmasks()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            Random rand = new Random();

            for (int i = 1; i <= 4; i++)
            {
                string path = Path.Combine(baseDir, "etc", "hostmasks." + i);
                if (!File.Exists(path) && File.Exists("etc/hostmasks." + i))
                {
                    path = "etc/hostmasks." + i;
                }

                if (File.Exists(path))
                {
                    try
                    {
                        string content = File.ReadAllText(path);
                        string[] tokens = content.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length >= 256)
                        {
                            _hostmasks.Add(tokens.Take(256).ToArray());
                            continue;
                        }
                    }
                    catch { }
                }

                // Fallback: generate 256 random 4-letter masks
                var generated = new HashSet<string>();
                while (generated.Count < 256)
                {
                    char c1 = (char)('a' + rand.Next(26));
                    char c2 = (char)('a' + rand.Next(26));
                    char c3 = (char)('a' + rand.Next(26));
                    char c4 = (char)('a' + rand.Next(26));
                    generated.Add(new string(new[] { c1, c2, c3, c4 }));
                }
                _hostmasks.Add(generated.ToArray());
            }
        }
    }
}
