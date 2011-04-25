using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrazyTalk.CrazyServer;

namespace CrazyServerLauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Server server = new Server(12345);
                Console.WriteLine("Server created successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error!!! {0}", e);
            }
            Console.ReadLine();
        }
    }
}
