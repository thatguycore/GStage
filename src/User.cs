//
// GStage: a modern C# rewrite of the Global Stage IRC server
// src/User.cs: client representation, socket I/O, registration, and hostmasking
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GStage
{
    public class User
    {
        public const long DefaultQLimit = 1L << 15; // 32768
        public const long MaxQLimit = 1L << 31;     // 2147483648

        private readonly object _syncRoot = new object();
        private readonly Server _server;
        private readonly TcpClient _client;
        private readonly Stream _stream;
        private readonly string _peerHost;
        private readonly string _mask;

        private string _nickname;
        private string _username;
        private string _realname;

        private long _qlimit = DefaultQLimit;
        private bool _isGagged = false;
        private bool _isPrivate = false;

        private readonly Dictionary<string, Channel> _channels = new Dictionary<string, Channel>(StringComparer.OrdinalIgnoreCase);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _disconnected = false;

        public User(Server server, Stream stream, string peerHost = "127.0.0.1")
        {
            _server = server;
            _client = null;
            _stream = stream;
            _peerHost = peerHost ?? "127.0.0.1";
            _mask = server.GenerateHostmask(_peerHost);
        }

        public User(Server server, TcpClient client)
            : this(server, client.GetStream(), ExtractPeerHost(client))
        {
            _client = client;
        }

        private static string ExtractPeerHost(TcpClient client)
        {
            try
            {
                IPEndPoint remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                if (remoteEndPoint != null)
                {
                    IPAddress ip = remoteEndPoint.Address;
                    if (ip.IsIPv4MappedToIPv6)
                    {
                        ip = ip.MapToIPv4();
                    }
                    return ip.ToString();
                }
            }
            catch { }
            return "127.0.0.1";
        }

        public Server Server { get { return _server; } }
        public TcpClient Client { get { return _client; } }
        public string PeerHost { get { return _peerHost; } }
        public string Mask { get { return _mask; } }

        public string Nickname
        {
            get { lock (_syncRoot) return _nickname; }
            set { lock (_syncRoot) _nickname = value; }
        }

        public string Username
        {
            get { lock (_syncRoot) return _username; }
            set { lock (_syncRoot) _username = value; }
        }

        public string Realname
        {
            get { lock (_syncRoot) return _realname; }
            set { lock (_syncRoot) _realname = value; }
        }

        public long QLimit
        {
            get { lock (_syncRoot) return _qlimit; }
            set
            {
                lock (_syncRoot)
                {
                    _qlimit = Math.Min(value, MaxQLimit);
                }
            }
        }

        public bool IsGagged
        {
            get { lock (_syncRoot) return _isGagged; }
            set { lock (_syncRoot) _isGagged = value; }
        }

        public bool IsPrivate
        {
            get { lock (_syncRoot) return _isPrivate; }
            set { lock (_syncRoot) _isPrivate = value; }
        }

        public bool IsRegistered
        {
            get
            {
                lock (_syncRoot)
                {
                    return !string.IsNullOrEmpty(_nickname) && !string.IsNullOrEmpty(_username);
                }
            }
        }

        public bool IsSupervisor
        {
            get { return _server.IsSupervisor(this); }
        }

        public string Match
        {
            get
            {
                string nick = Nickname;
                return !string.IsNullOrEmpty(nick) ? nick.ToLowerInvariant() : "";
            }
        }

        public string Hostmask
        {
            get
            {
                return string.Format("{0}!{1}@{2}", Nickname ?? "*", Username ?? "*", Mask);
            }
        }

        public string Prefix(string line)
        {
            return string.Format(":{0} {1}", Hostmask, line);
        }

        public static string GenerateMatch(string nickname)
        {
            return !string.IsNullOrEmpty(nickname) ? nickname.ToLowerInvariant() : "";
        }

        public void AddToChannel(Channel channel)
        {
            lock (_syncRoot)
            {
                _channels[channel.Match] = channel;
            }
        }

        public void RemoveFromChannel(Channel channel)
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

        public bool IsOn(Channel channel)
        {
            return channel.IsOn(this);
        }

        public bool IsOp(Channel channel)
        {
            return channel.IsOp(this);
        }

        public bool IsHalfOp(Channel channel)
        {
            return channel.IsHalfOp(this);
        }

        public bool IsVoice(Channel channel)
        {
            return channel.IsVoice(this);
        }

        public void Numeric(int code, params object[] args)
        {
            string nick = Nickname ?? "*";
            string formatted = global::GStage.Numeric.Format(code, args);
            string line = string.Format(":{0} {1:D3} {2} {3}", _server.Address, code, nick, formatted);
            Write(line);
        }

        public void Write(string line)
        {
            if (_disconnected) return;

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(line + "\r\n");
                lock (_stream)
                {
                    if (_client == null || _client.Connected)
                    {
                        _stream.Write(bytes, 0, bytes.Length);
                        _stream.Flush();
                    }
                }
            }
            catch
            {
                Disconnect("Write error");
            }
        }

        public async Task StartReadingAsync()
        {
            byte[] buffer = new byte[1024];
            StringBuilder fragment = new StringBuilder();

            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                    if (bytesRead == 0)
                    {
                        // Clean disconnect from remote
                        Disconnect("Connection reset by peer");
                        break;
                    }

                    string text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    fragment.Append(text);

                    string content = fragment.ToString();
                    int newlineIndex;
                    while ((newlineIndex = content.IndexOf('\n')) >= 0)
                    {
                        string line = content.Substring(0, newlineIndex);
                        content = content.Substring(newlineIndex + 1);

                        if (line.EndsWith("\r"))
                        {
                            line = line.Substring(0, line.Length - 1);
                        }

                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            _server.ProcessLine(this, line);
                        }
                    }

                    fragment.Clear();
                    fragment.Append(content);
                }
            }
            catch (Exception ex)
            {
                if (!_disconnected)
                {
                    Disconnect("Read error: " + ex.Message);
                }
            }
        }

        public void Disconnect(string reason)
        {
            lock (_syncRoot)
            {
                if (_disconnected) return;
                _disconnected = true;
            }

            try { _cts.Cancel(); } catch { }

            _server.Disconnect(this, reason);

            try { _stream.Close(); } catch { }
            if (_client != null)
            {
                try { _client.Close(); } catch { }
            }
        }
    }
}
