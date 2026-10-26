//
// GStage: automated unit test suite
// tests/TestRunner.cs: tests all GStage IRC features and Quarterdeck exploits in-memory
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GStage.Tests
{
    class MemoryPipeStream : Stream
    {
        private readonly Queue<byte> _queue = new Queue<byte>();
        private readonly object _lock = new object();

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override long Length { get { lock (_lock) return _queue.Count; } }
        public override long Position
        {
            get { return 0; }
            set { throw new NotSupportedException(); }
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (_lock)
            {
                int read = 0;
                while (read < count && _queue.Count > 0)
                {
                    buffer[offset + read] = _queue.Dequeue();
                    read++;
                }
                return read;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_lock)
            {
                for (int i = 0; i < count; i++)
                {
                    _queue.Enqueue(buffer[offset + i]);
                }
            }
        }

        public string ReadAllText()
        {
            lock (_lock)
            {
                byte[] bytes = _queue.ToArray();
                _queue.Clear();
                return Encoding.UTF8.GetString(bytes);
            }
        }

        public List<string> ReadLines()
        {
            string text = ReadAllText();
            if (string.IsNullOrEmpty(text)) return new List<string>();

            return text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
    }

    class TestClient
    {
        public User User { get; private set; }
        public MemoryPipeStream OutStream { get; private set; }
        private readonly Server _server;

        public TestClient(Server server, string ip = "192.168.1.100")
        {
            _server = server;
            OutStream = new MemoryPipeStream();
            User = new User(server, OutStream, ip);
        }

        public void Send(string line)
        {
            _server.ProcessLine(User, line);
        }

        public List<string> ReceiveLines()
        {
            return OutStream.ReadLines();
        }
    }

    class TestRunner
    {
        static int passed = 0;
        static int failed = 0;

        static void Assert(bool condition, string testName)
        {
            if (condition)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  [PASS] " + testName);
                passed++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  [FAIL] " + testName);
                failed++;
            }
            Console.ResetColor();
        }

        public static int Main(string[] args)
        {
            Console.WriteLine("=================================================");
            Console.WriteLine(" GStage In-Memory Comprehensive Test Suite      ");
            Console.WriteLine("=================================================");

            Server server = new Server("GStage", "local", 6667, null, "god");

            // Test 1: Handshake and Welcome Sequence
            TestClient client1 = new TestClient(server, "192.168.1.50");
            client1.Send("NICK sycobuny");
            client1.Send("USER syco localhost GStage :Stephen Belcher");

            var welcomeLines = client1.ReceiveLines();
            Assert(welcomeLines.Any(l => l.Contains("001 sycobuny :Welcome to the Internet Relay Network, sycobuny")),
                   "RPL_WELCOME (001) formatted correctly");
            Assert(welcomeLines.Any(l => l.Contains("002 sycobuny :Your host is GStage.local, running on glorious steam")),
                   "RPL_YOURHOST (002) has Quarterdeck 'glorious steam' signature");
            Assert(welcomeLines.Any(l => l.Contains("WELCOME TO THE DOMINION")),
                   "SciFi Channel #dominion MOTD banner received");

            // Test 2: Authentic 4-tier hostmask calculation
            Assert(client1.User.Mask.EndsWith(".this.chat.server"),
                   "Hostmask ends with .this.chat.server");
            Assert(client1.User.Mask.Split('.').Length == 7,
                   "Hostmask has 4 reversed-octet tiers + this.chat.server");

            // Test 3: Channel Creation & First Join Auto-Op
            client1.Send("JOIN #dominion");
            var joinLines = client1.ReceiveLines();
            Assert(joinLines.Any(l => l.Contains("JOIN #dominion")),
                   "Client receives JOIN broadcast");
            Assert(joinLines.Any(l => l.Contains("353 sycobuny = #dominion :@sycobuny")),
                   "First user to join channel is automatically granted operator (@sycobuny)");

            // Test 4: Second client joins
            TestClient client2 = new TestClient(server, "10.0.0.1");
            client2.Send("NICK rakaur");
            client2.Send("USER rakaur localhost GStage :Ruby Hacker");
            client2.ReceiveLines(); // clear welcome

            client2.Send("JOIN #dominion");
            var c2Join = client2.ReceiveLines();
            var c1Heard = client1.ReceiveLines();

            Assert(c1Heard.Any(l => l.Contains(":rakaur!") && l.Contains("JOIN #dominion")),
                   "Client 1 receives join broadcast of Client 2");
            Assert(c2Join.Any(l => l.Contains("353 rakaur = #dominion :@sycobuny rakaur")),
                   "Client 2 receives names list with @sycobuny and non-op rakaur");

            // Test 5: PRIVMSG between channel members
            client2.Send("PRIVMSG #dominion :Greetings sycobuny!");
            var c1Msg = client1.ReceiveLines();
            Assert(c1Msg.Any(l => l.Contains("PRIVMSG #dominion :Greetings sycobuny!")),
                   "PRIVMSG broadcast delivered to channel member");

            // Test 6: Global Stage Nickname Change Prohibition
            // Normal registered users CANNOT change their nickname!
            client2.Send("NICK newrakaur");
            var nickErr = client2.ReceiveLines();
            Assert(nickErr.Any(l => l.Contains("432 rakaur newrakaur :Erroneous nickname")),
                   "Normal registered user blocked from changing nickname (432 ERR_ERRONEOUSNICKNAME)");

            // Test 7: Supervisor Mode Elevation & Nickname Change
            client1.Send("MODE sycobuny +s god");
            var superLines = client1.ReceiveLines();
            Assert(superLines.Any(l => l.Contains("MODE sycobuny :+s")),
                   "User successfully elevated to supervisor using server password");
            Assert(client1.User.IsSupervisor, "IsSupervisor returns true");

            // Supervisor CAN change nickname
            client1.Send("NICK ChiefSyco");
            var superNickLines = client1.ReceiveLines();
            Assert(client1.User.Nickname == "ChiefSyco",
                   "Supervisor permitted to change nickname to ChiefSyco");

            // Test 8: Famous Quarterdeck '-k' OP EXPLOIT!
            // ChiefSyco sets channel key
            client1.Send("MODE #dominion +k scifirocks");
            client1.ReceiveLines();

            // R休憩r (non-op) issues: MODE #dominion -k scifirocks
            client2.Send("MODE #dominion -k scifirocks");
            var exploitLines = client2.ReceiveLines();
            Assert(exploitLines.Any(l => l.Contains("MODE #dominion :+o rakaur")),
                   "Authentic Global Stage exploit: 'MODE -k <key>' successfully grants OP!");
            Channel domChan = server.FindChannel("#dominion");
            Assert(domChan.IsOp(client2.User), "Channel records rakaur as operator");

            // Test 9: De-opping a supervisor triggers immediate auto-reop!
            client2.Send("MODE #dominion -o ChiefSyco");
            var deopLines = client1.ReceiveLines();
            Assert(deopLines.Any(l => l.Contains("MODE #dominion :+o ChiefSyco")),
                   "Supervisor de-op immunity: Server immediately re-ops supervisor!");
            Assert(domChan.IsOp(client1.User), "ChiefSyco remains operator");

            // Test 10: Kicking and 180-second Bozo Ban
            client1.Send("KICK #dominion rakaur :Behave!");
            var kickLines = client2.ReceiveLines();
            Assert(kickLines.Any(l => l.Contains("KICK #dominion rakaur :Behave!")),
                   "Client 2 receives KICK notice");
            Assert(!domChan.IsOn(client2.User), "Kicked user removed from channel");

            // R休憩r tries to rejoin #dominion immediately -> should be blocked by Bozo ban (474)
            client2.Send("JOIN #dominion");
            var banLines = client2.ReceiveLines();
            Assert(banLines.Any(l => l.Contains("474 rakaur #dominion :Cannot join channel (+b)")),
                   "Kicked user's IP placed on 180s Bozo ban list (474 ERR_BANNEDFROMCHAN)");

            // Test 11: Extended ASCII Channel Spoof Table
            string matchNorm = Channel.GenerateMatch("#test");
            string matchExtended = Channel.GenerateMatch("#test\x82");
            Assert(matchExtended.Contains("i"), "Extended ASCII character correctly maps using @spoofs lookup table");

            // Test 12: Permanent Channel (+f) Mode
            client1.Send("MODE #dominion +f");
            client1.ReceiveLines();
            Assert(domChan.Permanent, "Channel marked as permanent");

            // ChiefSyco parts #dominion
            client1.Send("PART #dominion");
            client1.ReceiveLines();
            Assert(server.FindChannel("#dominion") != null,
                   "Permanent channel remains alive even when user count reaches zero");

            Console.WriteLine("=================================================");
            Console.ForegroundColor = failed == 0 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine(string.Format(" Test Results: {0} Passed, {1} Failed", passed, failed));
            Console.ResetColor();
            Console.WriteLine("=================================================");

            return failed == 0 ? 0 : 1;
        }
    }
}
