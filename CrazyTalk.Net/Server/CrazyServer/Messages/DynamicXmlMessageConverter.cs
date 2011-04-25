using System;
using System.Diagnostics.Contracts;
using System.Xml.Linq;
using CrazyTalk.Core.Messages;
using CrazyTalk.Core.Messages.Commands;
using DynamicXml;

namespace CrazyTalk.CrazyServer.Messages
{
    public class DynamicXmlMessageConverter : IMessageConverter
    {
        public Message ConvertFromXml(XElement element)
        {
            // using DynamicXElement wraper for parsing xml
            dynamic dynamicElement = element.AsDynamic();

            long version = dynamicElement.version;

            // Extracting message Id
            long messageId = dynamicElement.messageId;

            // Extracting command type
            string stringCommandType = dynamicElement.command["type"];
            
            CommandType commandType;
            if (!Enum.TryParse(value: stringCommandType, ignoreCase: true, result: out commandType))
                throw new InvalidOperationException("Unknown command type: " + stringCommandType);

            Command command = ReadCommand(commandType, dynamicElement);
            return new Message(version, messageId, command);
        }

        public XElement ConvertToXml(Message message)
        {
            XElement element = new XElement("message");
            
            // We should use AsDynamicWriter instead AsDynamic
            // because we're changing exiting element not reading one
            dynamic dynamicElement = element.AsDynamicWriter();
            dynamicElement.version = message.Version;
            dynamicElement.messageId = message.Id;
            
            dynamicElement.command["type"] = message.Command.CommandType.ToString();
            
            // Writing command to dynamicElement
            WriteCommand(dynamicElement, message.Command);
            
            return element;
        }

        /// <summary>
        /// Static helper method that reads command from dynamic xml wrapper.
        /// </summary>
        private static Command ReadCommand(CommandType commandType, dynamic dynamicElement)
        {
            switch (commandType)
            {
                case CommandType.Login:
                    {
                        string name = dynamicElement.command.userInfo.name;
                        var userInfo = new UserInfo(name);
                        return new LoginCommand(userInfo); ;
                    }

                case CommandType.Ack:
                    long messageId = dynamicElement.command.messageId;
                    return new AckCommand(messageId);

                case CommandType.UserState:
                    {
                        string userName = dynamicElement.command.userInfo.name;
                        var userInfo = new UserInfo(userName);
                        string userState = dynamicElement.command.userState;
                        return new UserStateCommand(userInfo, userState);
                    }

                case CommandType.TextMessage:
                    {
                        string from = dynamicElement.command.from.userInfo.name;
                        string to = dynamicElement.command.to.userInfo.name;
                        string message = dynamicElement.command.textMessage;
                        return new TextMessageCommand(new UserInfo(from),
                                                      new UserInfo(to), message);
                    }
                    break;
                default:
                    throw new InvalidOperationException("Unsupported command type: " + commandType);
            }
        }

        private static void WriteCommand(dynamic dynamicElement, Command command)
        {
            switch(command.CommandType)
            {
                case CommandType.Login:
                    LoginCommand loginCommand = (LoginCommand) command;
                    dynamicElement.command.userInfo.name = loginCommand.UserInfo.Name;
                    break;
                case CommandType.Ack:
                    AckCommand ackCommand = (AckCommand) command;
                    dynamicElement.command.messageId = ackCommand.MessageId;
                    break;
                case CommandType.UserState:
                    UserStateCommand userStateCommand = (UserStateCommand) command;
                    dynamicElement.command.userInfo.name = userStateCommand.UserInfo.Name;
                    dynamicElement.command.userState = userStateCommand.UserState;
                    break;
                case CommandType.TextMessage:
                    TextMessageCommand textMessageCommand = (TextMessageCommand) command;
                    dynamicElement.command.from.userInfo.name = textMessageCommand.From.Name;
                    dynamicElement.command.to.userInfo.name = textMessageCommand.To.Name;
                    dynamicElement.command.textMessage = textMessageCommand.Message;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported command type: " + command.CommandType);
            }
        }
    }
}