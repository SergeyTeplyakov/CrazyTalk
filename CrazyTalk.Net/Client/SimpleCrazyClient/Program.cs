using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace SimpleCrazyClient
{
    class Program
    {
        static Tuple<string, string, int> ValidateEventArgs(string[] args)
        {
            // We should have at least 3 additional command line arguments
            // --host "1231312" --port 1231 --user "MyUser
            if (!args.Contains("--host") || !args.Contains("--port") || !args.Contains("--user"))
            {
                Console.WriteLine("Please speicify --host, --port and --user!");
                return null;
            }

            string host = null;
            string userName = null;
            int port = 0;

            for(int i = 0; i < args.Length; i++)
            {
                if (i == args.Length - 1)
                    break; // we should have at least one element

                if (args[i] == "--host")
                {
                    host = args[i + 1];
                    i++;
                }
                else if (args[i] == "--port")
                {
                    if (!int.TryParse(args[i + 1], out port))
                    {
                        Console.WriteLine("Port should be an integer!");
                        return null;
                    }
                    i++;
                }
                else if (args[i] == "--user")
                {
                    userName = args[i + 1];
                    i++;
                }
            }

            return new Tuple<string, string, int>(host, userName, port);
        }

        static void Main(string[] args)
        {
            Tuple<string, string, int> parsedArgs = ValidateEventArgs(args);
            if (parsedArgs == null)
                return;
            
            string host = parsedArgs.Item1;
            string userName = parsedArgs.Item2;
            int port = parsedArgs.Item3;
            
            try
            {

                Console.WriteLine("Type user name and than text message. Press \"q\" for exit...");
                
                SimpleClient client = new SimpleClient(host, port);
                client.Login(userName);

                string remoteUserName = null;
                string message = null;
                while (true)
                {
                    string tmp = Console.ReadLine();
                    if (tmp == "q")
                        break;
                    if (remoteUserName == null)
                        remoteUserName = tmp;
                    else
                    {
                        message = tmp;
                        client.SendTextMessage(remoteUserName, message);

                        remoteUserName = null;
                        message = null;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error! {0}", e);
            }
            Console.ReadLine();
        }
    }
}
