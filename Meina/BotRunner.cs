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

namespace Meina
{
    public class BotRunner
    {
        // Configurable settings
        readonly private int botCount = 40;
        readonly private bool testLatency = true;
        readonly private string workload = "teleport";
        string logFile = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory) + "\\output.csv";
        readonly int seed = 12345;
        private string ip = "localhost";
        private string password = "";

        private readonly AutoResetEvent autoEvent = new AutoResetEvent(false);
        private Random rand = new Random();
        private AClient Client;
        private Stopwatch Stopwatch = new Stopwatch();
        private bool waitingForReceive = false;
        long nanosecPerTick = (1000L * 1000L * 1000L) / Stopwatch.Frequency;
        private System.Numerics.Vector2 spawnPos;

        private int playerCount = 0; // Excludes receiver, 1 if included
        private int latencyProbesReceived = 0;
        private PlayerSelf receiver;
        static void Main(string[] _)
        => new BotRunner();


        private PlayerAction[] BotActions = new[] { PlayerAction.Left, PlayerAction.Right };

        public BotRunner()
        {
            try
            {
                rand = new Random(Seed: seed);
                if (File.Exists(logFile)) File.Delete(logFile);
                File.Create(logFile).Close();
                TextWriter tw = new StreamWriter(logFile);
                tw.WriteLine("Latency,Botcount,Workload");
                tw.Close();

                if (testLatency) botCount += 2;

                for (int i = 0; i < botCount; i++)
                {
                    System.Threading.Thread.Sleep(5000);
                    Client = new IPClient();

                    //Naming bots
                    var name = "";
                    if (testLatency) name = i == 0 ? "Receiver" : (i == 1 ? "Sender" : generateRandomName());
                    else name = generateRandomName();

                    var newChar = GenerateRandomChar(name);
                    Client.ServerJoined += BotJoined;
                    //Client.Log += Log;

                    if (i == 0)
                    {
                        Client.PlayerPositionUpdate += ReceiverDetectMovement;
                        Client.NewPlayerJoined += incrementPlayerCount;
                    }
                    else Client.ChatMessageReceived += Chat;
                    
                    ((IPClient)Client).ConnectWithIP(ip, newChar, password);
                    Console.WriteLine("CLIENT CONNECTED");
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
                switch(workload)
                {
                    case "teleport": 
                        runTeleportWorkload(bot);
                        break;
                    default:
                        throw new ArgumentException("Invalid workload selected: ", workload);
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
                long nanos = Stopwatch.ElapsedTicks * nanosecPerTick;

                string logMessage = $"{nanos},{playerCount},{workload}";

                //Avoid disconnecting due to inactivity
                if (latencyProbesReceived % 20 == 0)
                {
                    receiver.Teleport(spawnPos.X, spawnPos.Y);
                }

                TextWriter tw = new StreamWriter(logFile, true);
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

