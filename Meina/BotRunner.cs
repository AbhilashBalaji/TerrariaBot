using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using TerrariaBot;
//using TerrariaBot;
using TerrariaBot.Client;
using TerrariaBot.Entity;

namespace Meina
{
    public class BotRunner
    {
        readonly private int N = 1;
        readonly int seed = 12345;
        private string ip = "localhost";
        private string password = "";
        private AClient client;
        private readonly AutoResetEvent autoEvent = new AutoResetEvent(false);
        private Random rand = new Random();
        private AClient Client;
        static void Main(string[] _)
        => new BotRunner();


        private PlayerAction[] BotActions = new []{ PlayerAction.Left, PlayerAction.Right };

        public BotRunner()
		{
            try
            {

                rand = new Random(Seed: seed);
                for (int i = 0; i < N; ++i)

                {
                    Client = new IPClient();
                    var name = "Bot#" + i.ToString();
                    var newChar = GenerateRandomChar(name);
                    Client.ServerJoined += BotJoined;
                    Client.Log += Log;
                    Client.ChatMessageReceived += Chat;
                    ((IPClient)Client).ConnectWithIP(ip, newChar, password);
                    Console.WriteLine("CLIENT CONNECTED");

                    //Console.ReadLine();
                }

                //Console.ReadLine();
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

        private void BotJoined(PlayerSelf bot)
        {

            var pos = bot.GetPosition();
            bot.Teleport(pos.X, pos.Y - 50);
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
                    case "walking":
                        runWalkingWorkload(bot);
                        break;

                        break;
                    default:
                        throw new ArgumentException("Invalid workload selected: ", workload);
                };
            }


        private void runTeleportWorkload(PlayerSelf bot) { 

           while(true)
            {
                System.Numerics.Vector2 vector = bot.GetPosition();
                float xPos = vector.X;
                float yPos = vector.Y;

                float deltaX = ((float)rand.NextDouble()) * 200;
                float newXPos = (rand.NextDouble() >= 0.5) ? xPos + deltaX : xPos - deltaX;

                bot.Teleport(newXPos, 5200);
                System.Threading.Thread.Sleep(rand.Next(1, 4000));
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


        private void Chat(Player author, string message)
        {
            //var me = author.
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
            //    }
            //}
        }



        private void SendSuccessMessage(PlayerSelf bot)
        {
            bot.SendChatMessage("got it");
        }

        private PlayerInformation GenerateRandomChar(string name,PlayerDifficulty difficulty = PlayerDifficulty.Easy)
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
                pantsColor: pantsColor, shoesColor: shoesColor,difficulty:difficulty);
            return newChar;
        }
	}
}

