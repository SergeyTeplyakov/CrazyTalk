using System.Xml.Linq;
using CrazyTalk.Core.Messages;
using CrazyTalk.Core.Messages.Commands;
using CrazyTalk.CrazyServer.Messages;
using DynamicXml;
using NUnit.Framework;

namespace CrazyTalk.CrazyServer.UnitTests.Messages
{
    [TestFixture]
    public class DynamicXmlMessageConverterTest
    {
        #region ConvertFromXml method tests
        
        [TestCase]
        public void LoginCommandTest()
        {
string stringMessage =
@"<message>
  <version>1</version>
  <messageId>123</messageId>
  <command type=""Login"">
    <userInfo>
      <name>User name</name>
    </userInfo>
  </command>
 </message>";
            
            XElement xml = XElement.Parse(stringMessage);
            DynamicXmlMessageConverter converter = new DynamicXmlMessageConverter();
            Message message = converter.ConvertFromXml(xml);
            
            // Checking message header
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Id, Is.EqualTo(123));

            // Checking command type
            Command command = message.Command;
            Assert.That(command.CommandType, Is.EqualTo(CommandType.Login));
            Assert.That(command, Is.TypeOf(typeof(LoginCommand)));

            // Checking LoginCommand data
            LoginCommand loginCommand = (LoginCommand) command;
            UserInfo userInfo = loginCommand.UserInfo;
            Assert.That(userInfo.Name, Is.EqualTo("User name"));


        }

        [TestCase]
        public void AckCommandTest()
        {
            string stringMessage =
@"<message>
  <version>1</version>
  <messageId>123</messageId>
  <command type=""Ack"">
    <messageId>321</messageId>
  </command>
 </message>";

            XElement xml = XElement.Parse(stringMessage);
            DynamicXmlMessageConverter converter = new DynamicXmlMessageConverter();
            Message message = converter.ConvertFromXml(xml);

            // Checking command
            Command command = message.Command;
            AckCommand ackCommand = (AckCommand)command;
            Assert.That(ackCommand.CommandType, Is.EqualTo(CommandType.Ack));
            Assert.That(ackCommand.MessageId, Is.EqualTo(321));
        }

        [TestCase]
        public void UserStateCommandTest()
        {
            string stringMessage =
@"<message>
  <version>1</version>
  <messageId>123</messageId>
  <command type=""UserState"">
    <userInfo>
      <name>Another user name</name>
    </userInfo>
    <userState>Some user state</userState>
  </command>
 </message>";

            XElement xml = XElement.Parse(stringMessage);
            DynamicXmlMessageConverter converter = new DynamicXmlMessageConverter();
            Message message = converter.ConvertFromXml(xml);

            // Checking command
            Command command = message.Command;
            Assert.That(command.CommandType, Is.EqualTo(CommandType.UserState));

            UserStateCommand userStateCommand = (UserStateCommand)command;
            UserInfo userInfo = userStateCommand.UserInfo;
            Assert.That(userInfo.Name, Is.EqualTo("Another user name"));

            string userState = userStateCommand.UserState;
            Assert.That(userState, Is.EqualTo("Some user state"));
        }

        [TestCase]
        public void TextMessageCommandTest()
        {
            string stringMessage =
@"<message>
  <version>1</version>
  <messageId>123</messageId>
  <command type=""TextMessage"">
    <from>
      <userInfo>
        <name>Sender</name>
      </userInfo>
    </from>
    <to>
      <userInfo>
        <name>Receiver</name>
      </userInfo>
    </to>
    <textMessage>Some silly text message</textMessage>
  </command>
 </message>";

            XElement xml = XElement.Parse(stringMessage);
            DynamicXmlMessageConverter converter = new DynamicXmlMessageConverter();
            Message message = converter.ConvertFromXml(xml);

            // Checking command
            Command command = message.Command;
            Assert.That(command.CommandType, Is.EqualTo(CommandType.TextMessage));

            TextMessageCommand textMessageCommand = (TextMessageCommand)command;
            Assert.That(textMessageCommand.From.Name, Is.EqualTo("Sender"));

            Assert.That(textMessageCommand.To.Name, Is.EqualTo("Receiver"));

            Assert.That(textMessageCommand.Message, Is.EqualTo("Some silly text message"));
        }

        #endregion ConvertFromXml method tests

        #region ConvertToXml method test
        
        [TestCase]
        public void ConvertLoginCommandTest()
        {
            long version = 1;
            long messageId = 12312;
            string userName = "Test user";
            LoginCommand command = new LoginCommand(new UserInfo(userName));
            Message message = new Message(version, messageId, command);

            // Converting to xml
            DynamicXmlMessageConverter converter = new DynamicXmlMessageConverter();
            XElement xml = converter.ConvertToXml(message);
            
            // And back again
            Message message2 = converter.ConvertFromXml(xml);

            // Check message equivalence
            Assert.That(message2.Version, Is.EqualTo(message.Version));
            Assert.That(message2.Id, Is.EqualTo(message.Id));
            LoginCommand command2 = (LoginCommand) message2.Command;
            Assert.That(command2.UserInfo.Name, Is.EqualTo(userName));
        }

        [TestCase]
        public void ConvertAckCommandTest()
        {
            long version = 1;
            long messageId = 123123;
            long ackMessageId = 3242;

            AckCommand command = new AckCommand(ackMessageId);
            Message message = new Message(version, messageId, command);

            //Converting to xml
            DynamicXmlMessageConverter converter = new DynamicXmlMessageConverter();
            XElement xml = converter.ConvertToXml(message);

            // And back again
            Message message2 = converter.ConvertFromXml(xml);
            AckCommand command2 = (AckCommand) message2.Command;
            Assert.That(command2.MessageId, Is.EqualTo(command.MessageId));
        }

        [TestCase]
        public void ConvertUserInfoCommandTest()
        {
            long version = 1;
            long messageId = 123123;

            UserStateCommand command = new UserStateCommand(new UserInfo("userName"), "userState");
            Message message = new Message(version, messageId, command);

            //Converting to xml
            DynamicXmlMessageConverter converter = new DynamicXmlMessageConverter();
            XElement xml = converter.ConvertToXml(message);

            // And back again
            Message message2 = converter.ConvertFromXml(xml);
            UserStateCommand command2 = (UserStateCommand)message2.Command;
            Assert.That(command2.UserInfo.Name, Is.EqualTo(command.UserInfo.Name));
            Assert.That(command2.UserState, Is.EqualTo(command.UserState));
        }

        [TestCase]
        public void ConvertTextMessageCommandTest()
        {
            long version = 1;
            long messageId = 123123;

            TextMessageCommand command = new TextMessageCommand(
                new UserInfo("from user"), new UserInfo("to user"), "text message");
            Message message = new Message(version, messageId, command);

            //Converting to xml
            DynamicXmlMessageConverter converter = new DynamicXmlMessageConverter();
            XElement xml = converter.ConvertToXml(message);

            // And back again
            Message message2 = converter.ConvertFromXml(xml);
            TextMessageCommand command2 = (TextMessageCommand)message2.Command;
            Assert.That(command2.From.Name, Is.EqualTo(command.From.Name));
            Assert.That(command2.To.Name, Is.EqualTo(command.To.Name));
            Assert.That(command2.Message, Is.EqualTo(command.Message));
        }
        #endregion ConvertToXml method test

    }
}
