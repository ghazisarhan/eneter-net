﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.MessagingSystems.Composites.MessageBus;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.MessageBusMessaging
{
    [TestFixture]
    public class Test_MessageBusMessaging_Tcp_Interop : BaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            // Generate random number for the port.
            int aPort1 = RandomPortGenerator.GenerateInt();
            int aPort2 = aPort1 + 10;

            IMessagingSystemFactory anUnderlyingMessaging = new TcpMessagingSystemFactory(new EasyProtocolFormatter());

            IDuplexInputChannel aMessageBusServiceInputChannel = anUnderlyingMessaging.CreateDuplexInputChannel("tcp://[::1]:" + aPort1 + "/");
            IDuplexInputChannel aMessageBusClientInputChannel = anUnderlyingMessaging.CreateDuplexInputChannel("tcp://[::1]:" + aPort2 + "/");
            myMessageBus = new MessageBusFactory().CreateMessageBus();
            myMessageBus.AttachDuplexInputChannels(aMessageBusServiceInputChannel, aMessageBusClientInputChannel);

            MessagingSystemFactory = new MessageBusMessagingFactory("tcp://[::1]:" + aPort1 + "/", "tcp://[::1]:" + aPort2 + "/", anUnderlyingMessaging)
            {
                ConnectTimeout = TimeSpan.FromMilliseconds(3000)
            };

            // Address of the service in the message bus.
            ChannelId = "Service1_Address";

            CompareResponseReceiverId = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (myMessageBus != null)
            {
                myMessageBus.DetachDuplexInputChannels();
                myMessageBus = null;
            }
        }

        private IMessageBus myMessageBus;
    }
}
