using System;

namespace prototype_server
{
    public class Program
    {   
        public static void Main(string[] args)
        {
            var app = new App();

            app.Run();

            Console.ReadKey();
        }
    }
}
