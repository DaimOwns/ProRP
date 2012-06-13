using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Net;

using Reality.Communication.Incoming;
using Reality.Config;
using Reality.Network;
using Reality.Storage;
//using Reality.Plugins;
using Reality.Util;

using Reality.Game;
using Reality.Game.Sessions;
using Reality.Game.Misc;
using Reality.Game.Handlers;
using Reality.Game.Moderation;
using Reality.Game.Messenger;
using Reality.Game.Characters;
using Reality.Game.Catalog;
using Reality.Game.Items;
using Reality.Game.Navigation;
using Reality.Game.Rooms;
using Reality.Game.Advertisements;
using Reality.Game.Rights;
using Reality.Game.Bots;
using Reality.Game.Infobus;
using Reality.Game.Achievements;
using Reality.Game.Recycler;
using Reality.Game.Pets;
using Reality.Game.Music;
using Reality.Game.Rooms.Trading;
using Reality.Game.Misc.Chat;

namespace Reality
{
    public static class Program
    {
        private static bool mAlive;
        private static SnowTcpListener mServer;

        /// <summary>
        /// Should be used by all non-worker threads to check if they should remain alive, allowing for safe termination.
        /// </summary>
        public static bool Alive
        {
            get
            {
                return (!Environment.HasShutdownStarted && mAlive);
            }
        }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        public static void Main(string[] args)
        {
            mAlive = true;
            DateTime InitStart = DateTime.Now;

            // Set up basic output, configuration, etc
            Output.InitializeStream(true, OutputLevel.DebugInformation);
            Output.WriteLine("Initializing Reality...");

            ConfigManager.Initialize(Constants.DataFileDirectory + "\\server-main.cfg");
            Output.SetVerbosityLevel((OutputLevel)ConfigManager.GetValue("output.verbositylevel"));

            // Process args
            foreach (string arg in args)
            {
                Output.WriteLine("Command line argument: " + arg);
                Input.ProcessInput(arg.Split(' '));
            }

            try
            {
                // Initialize and test database
                Output.WriteLine("Initializing MySQL manager...");
                SqlDatabaseManager.Initialize();

                using (SqlDatabaseClient MySqlClient = SqlDatabaseManager.GetClient())
                {
                    Output.WriteLine("Resetting database counters and statistics...");
                    PerformDatabaseCleanup(MySqlClient);

                    Output.WriteLine("Initializing game components and workers...");

                    // Core
                    DataRouter.Initialize();

                    // Sessions, characters
                    Handshake.Initialize();
                    GlobalHandler.Initialize();
                    SessionManager.Initialize();
                    CharacterInfoLoader.Initialize();
                    RightsManager.Initialize(MySqlClient);
                    SingleSignOnAuthenticator.Initialize();

                    // Room management and navigator
                    RoomManager.Initialize(MySqlClient);
                    RoomInfoLoader.Initialize();
                    RoomHandler.Initialize();
                    RoomItemHandler.Initialize();
                    Navigator.Initialize(MySqlClient);

                    // Help and moderation
                    HelpTool.Initialize(MySqlClient);
                    ModerationPresets.Initialize(MySqlClient);
                    ModerationTicketManager.Initialize(MySqlClient);
                    ModerationHandler.Initialize();
                    ModerationBanManager.Initialize(MySqlClient);

                    // Catalog, pets and items
                    ItemDefinitionManager.Initialize(MySqlClient);
                    CatalogManager.Initialize(MySqlClient);
                    CatalogPurchaseHandler.Initialize();
                    Inventory.Initialize();
                    ItemEventDispatcher.Initialize();
                    PetDataManager.Initialize(MySqlClient);

                    // Messenger
                    MessengerHandler.Initialize();

                    // Achievements and quests
                    AchievementManager.Initialize(MySqlClient);
                    QuestManager.Initialize(MySqlClient);

                    // Misc/extras
                    CrossdomainPolicy.Initialize("Data\\crossdomain.xml");
                    InfobusManager.Initialize();
                    ActivityPointsWorker.Initialize();
                    BotManager.Initialize(MySqlClient);
                    InterstitialManager.Initialize(MySqlClient);
                    ChatEmotions.Initialize();
                    EffectsCacheWorker.Initialize();
                    JobCacheWorker.Initialize();
                    RecyclerManager.Initialize(MySqlClient);
                    DrinkSetManager.Initialize(MySqlClient);
                    SongManager.Initialize();
                    TradeHandler.Initialize();

                    // Polish/misc
                    WarningSurpressors.Initialize();
                    RandomGenerator.Initialize();
                    Output.WriteLine("Initializing wordfilter");
                    Wordfilter.Initialize(MySqlClient);

                    // Initialize network components
                    Output.WriteLine("Setting up server listener on port " + (int)ConfigManager.GetValue("net.bind.port") + "...");
                    mServer = new SnowTcpListener(new IPEndPoint(IPAddress.Any, (int)ConfigManager.GetValue("net.bind.port")),
                        (int)ConfigManager.GetValue("net.backlog"), new Reality.Network.OnNewConnectionCallback(
                            SessionManager.HandleIncomingConnection));
                }
            }
            catch (Exception e)
            {
                HandleFatalError("Could not initialize Reality: " + e.Message);
                string text = System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\error-log.txt");
                System.IO.StreamWriter file = new System.IO.StreamWriter(Environment.CurrentDirectory + "\\error-log.txt");
                file.WriteLine(text + "Could not initialize Reality: " + e.Message + "\n\n" + e.StackTrace,
                    OutputLevel.CriticalError + "\n\n");

                file.Close();
                return;
            }

            // Init complete
            TimeSpan TimeSpent = DateTime.Now - InitStart;

            Output.WriteLine("The server has initialized successfully (" + Math.Round(TimeSpent.TotalSeconds, 2) + " seconds). Ready for connections.", OutputLevel.Notification);
            Output.WriteLine("Press the ENTER key for command input. Shut down server with 'STOP' command.", OutputLevel.Notification);

            Console.Beep();
            Input.Listen(); // This will make the main thread process console while Program.Alive.
        }

        private static void PerformDatabaseCleanup(SqlDatabaseClient MySqlClient)
        {
            MySqlClient.ExecuteNonQuery("UPDATE rooms SET current_users = 0");
            MySqlClient.SetParameter("timestamp", UnixTimestamp.GetCurrent());
            MySqlClient.ExecuteNonQuery("UPDATE room_visits SET timestamp_left = @timestamp WHERE timestamp_left = 0");
        }

        public static void HandleFatalError(string Message)
        {
            Output.WriteLine(Message, OutputLevel.CriticalError);
            Output.WriteLine("Cannot proceed; press any key to stop the server.", OutputLevel.CriticalError);

            Console.ReadKey(true);

            Stop();
        }

        public static void Stop()
        {
            Output.WriteLine("Stopping Reality...");

            mAlive = false; // Will destroy any threads looping for Program.Alive.

            SqlDatabaseManager.Uninitialize();

            mServer.Dispose();
            mServer = null;

            Output.WriteLine("Bye!");

            Environment.Exit(0);
        }
    }
}
