using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Xsl;
using TerrariaBot;
//using TerrariaBot;
using TerrariaBot.Client;
using TerrariaBot.Entity;
using static Meina.BotRunner;

namespace Meina
{
    public class Settings
    {
        private readonly static string configFile = "default.conf";
        public readonly int botCount;
        public readonly bool testLatency;
        public readonly string workload;
        public readonly string logFile;
        public readonly int seed;
        public readonly string serverIp;
        public readonly string serverPassword;
        public readonly int serverPort;
        public readonly int batchJoinDelay;
        public readonly int batchSize;

        private Settings()
        {
            this.botCount = 40;
            this.testLatency = true;
            this.workload = "teleport";
            this.logFile = prependBaseDir("output.csv");
            this.seed = 12345;
            this.serverIp = "localhost";
            this.serverPassword = "";
            this.serverPort = 7777;
            this.batchJoinDelay = 5000;
            this.batchSize = 5;
        }

        private Settings(string path)
        {
            foreach (string line in File.ReadLines(path))
            {
                string[] keyValues = line.Split("=");
                string setting = keyValues[0];
                string value = keyValues[1].Trim();
                switch (setting)
                {
                    case "bot_count":
                        botCount = int.Parse(value);
                        break;
                    case "test_latency":
                        testLatency = bool.Parse(value);
                        break;
                    case "workload":
                        workload = value;
                        break;
                    case "log_file":
                        logFile = value;
                        break;
                    case "seed":
                        seed = int.Parse(value);
                        break;
                    case "server_ip":
                        serverIp = value;
                        break;
                    case "server_password":
                        serverPassword = value;
                        break;
                    case "server_port":
                        serverPort = int.Parse(value);
                        break;
                    case "batch_join_delay":
                        batchJoinDelay = int.Parse(value);
                        break;
                    case "batch_size":
                        batchSize = int.Parse(value);
                        break;

                }
            }
        }


        private static string prependBaseDir(string file)
        {
            return Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + "\\" + file;
        }
        public static Settings parseFromFile(string file)
        {

            string path;

            if (File.Exists(prependBaseDir(file)))
            {
                path = prependBaseDir(file);
            }
            else if (File.Exists(configFile))
            {
                path = prependBaseDir(configFile);
            }
            else
            {
                Console.Error.WriteLine("No file located at {0} please move default.conf to this location. ", prependBaseDir(configFile));
                Console.Error.WriteLine("Hardcoded settings are used!");
                return new Settings();
            }
            return new Settings(path);
        }

        public string toString()
        {
            string obj = "bot_count={0}\ntest_latency={1}\nworkload={2}\nlog_file={3}\n" +
                            "seed={4}\nserver_ip={5}\nserver_password={6}\nserver_port={7}\n" +
                            "batch_join_delay={8}\nbatch_size={9}\n";
            return String.Format(obj, botCount, testLatency, workload, logFile, 
                seed, serverIp, serverPassword, serverPort, 
                batchJoinDelay, batchSize);
        }
    }

    public class BotRunner
    {
        private Settings botSettings;


        readonly private int N = 5;
        readonly int seed = 12345;
        private string ip = "52.58.211.22";
        private string password = "";
        private AClient client;
        private readonly AutoResetEvent autoEvent = new AutoResetEvent(false);
        private Random rand = new Random();
        private AClient Client;
        private Random rand = new Random();
        private Stopwatch Stopwatch = new Stopwatch();
        long nanosecPerTick = (1000L * 1000L * 1000L) / Stopwatch.Frequency;
        private bool waitingForReceive = false;
        private int playerCount = 0; // Excludes receiver, 1 if included
        private int latencyProbesReceived = 0;
        private Vector2 spawnPos;
        private PlayerSelf receiver;

        private int botCount;


        static void Main(string[] _)
        {
            Console.Write("Define config file: (default.conf)");
            string definedConf = Console.ReadLine();
            Settings settings = Settings.parseFromFile(definedConf);
            new BotRunner(settings);
        }

        public BotRunner(Settings settings)
        {

            this.botSettings = settings;
            try
            {
                rand = new Random(Seed: this.botSettings.seed);
                if (File.Exists(this.botSettings.logFile)) File.Delete(this.botSettings.logFile);
                File.Create(this.botSettings.logFile).Close();
                TextWriter tw = new StreamWriter(this.botSettings.logFile);
                tw.WriteLine("Latency,Botcount,Workload");
                tw.Close();

                this.botCount = this.botSettings.botCount;

                if (this.botSettings.testLatency) this.botCount += 2;

                for (int i = 0; i < this.botCount; i++)
                {
                    System.Threading.Thread.Sleep(this.botSettings.batchJoinDelay);
                    //batching
                    for (int j = 0; j < this.botSettings.batchSize; j++)
                    {
                        Client = new IPClient();

                        //Naming bots
                        var name = "";
                        if (this.botSettings.testLatency) name = i == 0 ? "Receiver" : (i == 1 ? "Sender" : generateRandomName());
                        else name = generateRandomName();

                        var newChar = GenerateRandomChar(name);
                        Client.ServerJoined += BotJoined;
                        //Client.Log += Log;

                        if (i == 0 && this.botSettings.testLatency)
                        {
                            Client.PlayerPositionUpdate += ReceiverDetectMovement;
                            Client.NewPlayerJoined += incrementPlayerCount;
                        }
                        else Client.ChatMessageReceived += Chat;

                        ((IPClient)Client).ConnectWithIP(this.botSettings.serverIp, newChar, this.botSettings.serverPassword, this.botSettings.serverPort);
                        Console.WriteLine("CLIENT CONNECTED");
                        if (j < this.botSettings.batchSize - 1) i++;
                    }
                }

            }
            catch (SocketException se)
            {
                Console.WriteLine("Can't connect to server: " + se.Message + Environment.NewLine + "Make sure that your Terraria server is online.");
                Console.WriteLine("Press any key to continue");
                Console.ReadKey();
                return;
            }
        }

        private byte[] HairList = new byte[] { 11, 0, 1 };

        private void incrementPlayerCount(Player player)
        {
            playerCount++;
        }

        private void BotJoined(PlayerSelf bot)
        {

            bot.TogglePVP(false);
            if (String.Equals(bot.GetName(), "Sender"))
            {
                spawnPos = bot.GetPosition();
                while (true)
                {
                    if (!waitingForReceive)
                    {
                        Stopwatch.Reset();
                        Stopwatch.Start();
                        waitingForReceive = true;
                        if (bot.GetPosition().X <= spawnPos.X) bot.Teleport(spawnPos.X + 50, spawnPos.Y);
                        else bot.Teleport(spawnPos.X - 50, spawnPos.Y);
                    }
                    
                }
            }
            else if (String.Equals(bot.GetName(), "Receiver"))
            {
                receiver = bot;
            }
            else
            {
                switch(this.botSettings.workload)
                {
                    case "teleport":
                        runTeleportWorkload(bot);
                        break;
                    case "walking":
                        runWalkingWorkload(bot);
                        break;
                    default:
                        throw new ArgumentException("Invalid workload selected: ", this.botSettings.workload);
                };
            }

        }

        private void runTeleportWorkload(PlayerSelf bot)
        {
            while (true)
            {
                System.Numerics.Vector2 vector = bot.GetPosition();
                float xPos = vector.X;
                float yPos = vector.Y;

                float deltaX = ((float)rand.NextDouble()) * 200;
                float newXPos = (rand.NextDouble() >= 0.5) ? xPos + deltaX : xPos - deltaX;

                bot.Teleport(newXPos, yPos);
                System.Threading.Thread.Sleep(rand.Next(1, 1000));
            }
        }
        private void runWalkingWorkload(PlayerSelf bot)
        {

            PlayerAction[] actions = { PlayerAction.Right, PlayerAction.Left };
            int i = 0;
            while (true)
            {
                i++;
                var pos = bot.GetPosition();
                bot.Teleport(pos.X, spawnPos.Y - 50);
                bot.DoAction(new PlayerAction[] { actions[i % 2] });
                System.Threading.Thread.Sleep(rand.Next(2000, 4000));
            }
        }

        private string generateRandomName()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var name = new char[8];

            for (int i = 0; i < name.Length; i++)
            {
                name[i] = chars[rand.Next(chars.Length)];
            }

            return new String(name);
        }

        private void Log(LogLevel logLevel, string message)
        {
            var color = Console.ForegroundColor;
            switch (logLevel)
            {
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;

                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;

                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            Console.WriteLine(message);
            Console.ForegroundColor = color;
        }

        private void ReceiverDetectMovement(Player bot, Vector2 position)
        {
            if (String.Equals(bot.GetName(), "Sender") && bot.GetPosition().X > 5.0)
            {

                Stopwatch.Stop();
                latencyProbesReceived++;
                //long millis = Stopwatch.ElapsedMilliseconds;
                long nanos = Stopwatch.ElapsedTicks * this.nanosecPerTick;

                string logMessage = $"{nanos},{playerCount},{this.botSettings.workload}";

                //Avoid disconnecting due to inactivity
                if (latencyProbesReceived % 20 == 0)
                {
                    receiver.Teleport(spawnPos.X, spawnPos.Y);
                }

                TextWriter tw = new StreamWriter(this.botSettings.logFile, true);
                tw.WriteLine(logMessage);
                tw.Close();

                System.Threading.Thread.Sleep(20);
                waitingForReceive = false;
            }
        }

        private void Chat(Player author, string message)
        {

            //if (message.ToLower().StartsWith("meina"))
            //{
            //    message = message.Substring(5).Trim().ToLower();
            //    if (message.StartsWith("go to"))
            //    {
            //        message = message.Substring(5).Trim();
            //        Player player;
            //        if (message == "me") player = author;
            //        else player = client.GetAllPlayers().Where(x => x.GetName().ToLower() == message).FirstOrDefault();
            //        if (player == null) me.SendChatMessage("There is nobody with that name here");
            //        else
            //        {
            //            SendSuccessMessage(me);
            //            me.Teleport(player.GetPosition());
            //        }
            //    }
            //    else if (message.StartsWith("come here"))
            //    {
            //        SendSuccessMessage(me);
            //        me.Teleport(author.GetPosition());
            //    }
            //    else if (message.StartsWith("go"))
            //    {
            //        message = message.Substring(2).Trim();
            //        if (message == "left")
            //        {
            //            SendSuccessMessage(me);
            //            me.DoAction(PlayerAction.Left);
            //        }
            //        else if (message == "right")
            //        {
            //            SendSuccessMessage(me);
            //            me.DoAction(PlayerAction.Right);
            //        }
            //    }
            //    else if (message.StartsWith("stop"))
            //    {
            //        SendSuccessMessage(me);
            //        me.DoAction();
            //    }1
            //}
        }



        private void SendSuccessMessage(PlayerSelf bot)
        {
            bot.SendChatMessage("got it");
        }

        private PlayerInformation GenerateRandomChar(string name, PlayerDifficulty difficulty = PlayerDifficulty.Easy)
        {
            int hair_i = rand.Next(HairList.Count());
            var hair = HairList[hair_i];
            var hairColor = new Color(BitConverter.GetBytes((rand.Next(0, 256)))[0], BitConverter.GetBytes((rand.Next(0, 256)))[0], BitConverter.GetBytes((rand.Next(0, 256)))[0]);
            var skinColor = new Color(BitConverter.GetBytes((rand.Next(0, 256)))[0], BitConverter.GetBytes((rand.Next(0, 256)))[0], BitConverter.GetBytes((rand.Next(0, 256)))[0]);
            var eyesColor = new Color(BitConverter.GetBytes((rand.Next(0, 256)))[0], BitConverter.GetBytes((rand.Next(0, 256)))[0], BitConverter.GetBytes((rand.Next(0, 256)))[0]);
            var shirtColor = new Color(BitConverter.GetBytes((rand.Next(0, 256)))[0], BitConverter.GetBytes((rand.Next(0, 256)))[0], BitConverter.GetBytes((rand.Next(0, 256)))[0]);
            var underShirtColor = new Color(BitConverter.GetBytes((rand.Next(0, 256)))[0], BitConverter.GetBytes((rand.Next(0, 256)))[0], BitConverter.GetBytes((rand.Next(0, 256)))[0]);
            var pantsColor = new Color(BitConverter.GetBytes((rand.Next(0, 256)))[0], BitConverter.GetBytes((rand.Next(0, 256)))[0], BitConverter.GetBytes((rand.Next(0, 256)))[0]);
            var shoesColor = new Color(BitConverter.GetBytes((rand.Next(0, 256)))[0], BitConverter.GetBytes((rand.Next(0, 256)))[0], BitConverter.GetBytes((rand.Next(0, 256)))[0]);
            PlayerInformation newChar = new PlayerInformation(name: name, hairVariant: hair, hairColor: hairColor,
                skinColor: skinColor, eyesColor: eyesColor, shirtColor: shirtColor, underShirtColor: underShirtColor,
                pantsColor: pantsColor, shoesColor: shoesColor, difficulty: difficulty);
            return newChar;
        }
    }
}

