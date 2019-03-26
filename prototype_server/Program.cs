using System;
using System.Threading;

#if DEBUG
    using prototype_server.Libs.LiteNetLib;
#else
    using LiteNetLib;
#endif

namespace prototype_server
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var routes = new Routes();
            var server = new NetManager(routes);
            
            routes.ServerInstance = server;
            
            Run(server, routes);
            
            Console.ReadKey();
        }

        private static void Run(NetManager server, RoutesBase routes)
        {
            if (server.Start(15000))
            {
                Console.WriteLine("Server started listening on port 15000");
            }
            else
            {
                Console.Error.WriteLine("Server could not start!");
                return;
            }

            while (server.IsRunning)
            {
                server.PollEvents();
                routes.SyncWithConnectedClients();

                Thread.Sleep(15);
            }
        }
    }
}
