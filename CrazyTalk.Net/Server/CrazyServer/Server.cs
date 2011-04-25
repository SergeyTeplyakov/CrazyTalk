//-----------------------------------------------------------------------------------------------//
// Server class that handles requests from clients
//
// Author:    Sergey Teplyakov
// Date:      April 22, 2011
//-----------------------------------------------------------------------------------------------//


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using CrazyTalk.Core.Communication;
using CrazyTalk.Core.Messages;
using CrazyTalk.Core.Messages.Commands;
using CrazyTalk.Core.Utils;
using CrazyTalk.CrazyServer.Messages;

namespace CrazyTalk.CrazyServer
{
    /// <summary>
    /// For this time this is simple and even naive implementation for CrazyTalkServer
    /// </summary>
    public class Server
    {

        private readonly TcpServer tcpServer;
        private readonly ReaderWriterLockSlim usersSync = new ReaderWriterLockSlim();
        private readonly List<FullUserInfo> users = new List<FullUserInfo>();
        private long messageId;
        private readonly IMessageConverter messageConverter = new DynamicXmlMessageConverter();

        class FullUserInfo
        {
            public FullUserInfo(UserInfo userInfo)
            {
                Contract.Requires(userInfo != null);
                UserInfo = userInfo;

                UserState = "Disconnected";
            }

            public UserInfo UserInfo { get; private set; }
            public string UserState { get; private set; }
            public IPEndPoint EndPoint { get; private set; }
            
            public void ChangeUserState(string userState)
            {
                UserState = userState;
            }

            public void OnConnect(IPEndPoint endPoint)
            {
                Contract.Requires(endPoint != null);
                EndPoint = endPoint;
                UserState = "Connected";
            }

            public void OnDisconnect()
            {
                EndPoint = null;
                UserState = "Disconnected";
            }

        }

        public Server(int port)
        {
            Contract.Requires(0 < port && port < 65536);

            tcpServer = new TcpServer(port);
            tcpServer.SocketConnected += SocketConnected;
            tcpServer.DataReceived += DataReceived;
            tcpServer.SocketDisconnected += SocketDisconnected;

        }

        void SocketConnected(object sender, TcpServer.SocketEndPointEventArgs e)
        {
            Console.WriteLine("Socket connected from: {0}", e.IPEndPoint);
        }

        void SocketDisconnected(object sender, TcpServer.SocketEndPointEventArgs e)
        {
            Console.WriteLine("Socket disconnected from: " + e.IPEndPoint);
            using (usersSync.UseUpgradeableLock())
            {
                // Finding appropriate user
                var userInfo = users.SingleOrDefault(ui => ui.EndPoint == e.IPEndPoint);
                if (userInfo == null)
                {
                    Console.WriteLine("Unknown user disconnected!!");
                    return;
                }

                // Changing it state
                using (usersSync.UseWriteLock())
                {
                    userInfo.OnDisconnect();
                }
                
                // And sending this information to al users
                SendUpdatedUserState(userInfo);
            }
        }

        private void SendUpdatedUserState(FullUserInfo userInfo)
        {
            Contract.Requires(userInfo != null);
            // Creating message to send
            var userStateCommand = new UserStateCommand(userInfo.UserInfo, userInfo.UserState);
            var message = new Message(1, messageId, userStateCommand);
            string xml = messageConverter.ConvertToXml(message).ToString();
            
            // Finding all connected users (without this particular user)
            var collectionToSend = users.Where(ui => ui.EndPoint != null && ui != userInfo);

            // And sending user information to them
            foreach (var ui in collectionToSend)
            {
                tcpServer.SendString(ui.EndPoint, xml);
            }
        }

        void DataReceived(object sender, TcpServer.DataReceivedEventArgs e)
        {
            Console.WriteLine("Data received!");
            try
            {
                // Converting received data to xml
                var xml = ASCIIEncoding.UTF8.GetString(e.Data, 0, e.Data.Length);
                Console.WriteLine("Data received from {0}: {1}", e.IPEndPoint, xml);
                XElement xmlElement = XElement.Parse(xml);
                Console.WriteLine("Xml data: " + xmlElement);

                // TODO: we're not dealing with splitted message. fix this later

                // Creating message from xml string
                Message message = messageConverter.ConvertFromXml(xmlElement);
                long currentMessageId = message.Id;
                Command command = message.Command;
                switch(command.CommandType)
                {
                    case CommandType.Ack:
                        // Server can't get this command
                        break;
                    case CommandType.Login:
                        HandleLoginMessage(currentMessageId, (LoginCommand)command, e.IPEndPoint);
                        break;
                    case CommandType.UserState:
                        HandleUserStateCommand(currentMessageId, (UserStateCommand)command);
                        break;
                    case CommandType.TextMessage:
                        HandleTextMessageCommand(currentMessageId, (TextMessageCommand)command);
                        break;
                    default:
                        Debug.Assert(false, "Unknown command type: " + command.CommandType);
                        break;
                }
                //tcpServer.SendData(e.Data, e.IPEndPoint);

                //Это может быть регистрационное сообщение
                //var userName = doc.Descendants("userName").SingleOrDefault();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing data. {0}", ex);
            }
            
        }

        private void HandleLoginMessage(long incomingMessageId, LoginCommand command, IPEndPoint endPoint)
        {
            using (usersSync.UseWriteLock())
            {
                // Finding existing user or creating new one
                var currentUser = users.SingleOrDefault(ui => ui.UserInfo.Name == command.UserInfo.Name);
                if (currentUser == null)
                {
                    currentUser = new FullUserInfo(command.UserInfo);
                    users.Add(currentUser);
                }

                // Changing user state
                currentUser.OnConnect(endPoint);

                // Sending Ack message
                var ackCommand = new AckCommand(incomingMessageId);
                var message = CreateMessage(ackCommand);
                SendMessage(endPoint, message);

                // Sending updated state
                SendUpdatedUserState(currentUser);
            }
        }

        private const long ProtocolVersion = 1;



        // Helper method that creates messag with specified command
        private Message CreateMessage(Command command)
        {
            Contract.Requires(command != null);
            Contract.Ensures(Contract.Result<Message>() != null);
            
            var message = new Message(ProtocolVersion, messageId, command);
            Interlocked.Increment(ref messageId);
            return message;
        }

        private void SendMessage(IPEndPoint endPoint, Message message)
        {
            string xml = messageConverter.ConvertToXml(message).ToString();
            tcpServer.SendString(endPoint, xml); 
        }

        private void HandleUserStateCommand(long incomingMessageId, UserStateCommand command)
        {
            using (usersSync.UseReadLock())
            {
                var userInfo = users.SingleOrDefault(ui => ui.UserInfo.Name == command.UserInfo.Name);
                if (userInfo == null)
                {
                    // Nothing to handle. This user unknown
                    return;
                }

                userInfo.ChangeUserState(command.UserState);
                SendUpdatedUserState(userInfo);
            }
        }

        private void HandleTextMessageCommand(long incomiMessageId, TextMessageCommand command)
        {
            using (usersSync.UseReadLock())
            {
                var fromUserInfo = users.SingleOrDefault(ui => ui.UserInfo.Name == command.From.Name);
                var toUserInfo = users.SingleOrDefault(ui => ui.UserInfo.Name == command.To.Name);
                if (fromUserInfo == null || toUserInfo == null ||
                    toUserInfo.EndPoint == null)
                {
                    // Can't forward message
                    return;
                }

                var message = CreateMessage(command);
                SendMessage(toUserInfo.EndPoint, message);
            }
        }
    }

}