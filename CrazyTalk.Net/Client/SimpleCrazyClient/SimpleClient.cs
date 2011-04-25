//-----------------------------------------------------------------------------------------------//
// Simple client implementation for CrazyTalk server
//
// Author:    Sergey Teplyakov
// Date:      April 24, 2011
//-----------------------------------------------------------------------------------------------//


using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using CrazyTalk.Core.Communication;
using CrazyTalk.Core.Messages;
using CrazyTalk.Core.Messages.Commands;
using CrazyTalk.CrazyServer.Messages;

namespace SimpleCrazyClient
{
    /// <summary>
    /// Simple client implementation for CrazyTalk server
    /// </summary>
    public class SimpleClient
    {
        private readonly TcpClientConnection connection;
        private readonly IMessageConverter messageConverter = new DynamicXmlMessageConverter();
        private string currentUserName;
        private long messageId = 1;

        public SimpleClient(string host, int port)
        {
            Contract.Requires(!string.IsNullOrEmpty(host));
            Contract.Requires(0 < port && port < 65536);

            var address = host == "localhost" ? IPAddress.Loopback : IPAddress.Parse(host);
            connection = new TcpClientConnection(new IPEndPoint(address, port));
            connection.SocketDisconnected += SocketDisconnected;
            connection.DataReceived += DataReceived;
        }

        public void Login(string userName)
        {
            Contract.Requires(!string.IsNullOrEmpty(userName));

            currentUserName = userName;

            var loginCommand = new LoginCommand(new UserInfo(userName));
            var message = CreateMessage(loginCommand);
            SendMessage(message);
        }
        
        public void SendTextMessage(string userName, string textMessage)
        {
            Contract.Requires(!string.IsNullOrEmpty(userName));
            Contract.Requires(!string.IsNullOrEmpty(textMessage));
            var textMessageCommand = new TextMessageCommand(new UserInfo(currentUserName),
                                                            new UserInfo(userName), textMessage);
            var message = CreateMessage(textMessageCommand);
            SendMessage(message);

        }

        private Message CreateMessage(Command command)
        {
            Contract.Requires(command != null);
            Contract.Ensures(Contract.Result<Message>() != null);

            var message = new Message(1, messageId, command);
            Interlocked.Increment(ref messageId);
            return message;
        }

        private void SendMessage(Message message)
        {
            Contract.Requires(message != null);

            string xml = messageConverter.ConvertToXml(message).ToString();
            connection.SendString(xml);
        }

        private void SocketDisconnected(object sender, TcpClientConnection.SocketDisconnectedEventArgs e)
        {
            Console.WriteLine("Connection to server failed!");
            if (e.Error != null)
            {
                Console.WriteLine("Error: {0}", e.Error);
            }
        }

        private void DataReceived(object sender, TcpClientConnection.DataReceivedEventArgs e)
        {
            try
            {
                Console.WriteLine("Data received: {0}", e.Data);
                var element = XElement.Parse(e.Data);
                Message message = messageConverter.ConvertFromXml(element);
                Console.WriteLine("Message received: {0}", message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing data: {0}", ex);
            }
        }
    }
}