﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.Composites.BusMessaging;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.MessageBusMessaging
{
    [TestFixture]
    public class Test_MessageBusMessaging_Tcp : BaseTester
    {
        [SetUp]
        public void Setup()
        {
            EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            // Generate random number for the port.
            int aPort1 = RandomPortGenerator.GenerateInt();
            int aPort2 = aPort1 + 1;

            IMessagingSystemFactory anUnderlyingMessaging = new TcpMessagingSystemFactory();

            IDuplexInputChannel aMessageBusServiceInputChannel = anUnderlyingMessaging.CreateDuplexInputChannel("tcp://[::1]:" + aPort1 + "/");
            IDuplexInputChannel aMessageBusClientInputChannel = anUnderlyingMessaging.CreateDuplexInputChannel("tcp://[::1]:" + aPort2 + "/");
            myMessageBus = new MessageBusFactory().CreateMessageBus();
            myMessageBus.AttachDuplexInputChannels(aMessageBusServiceInputChannel, aMessageBusClientInputChannel);

            MessagingSystemFactory = new MessageBusMessagingFactory("tcp://[::1]:" + aPort1 + "/", "tcp://[::1]:" + aPort1 + "/", anUnderlyingMessaging);

            // Address of the service in the message bus.
            ChannelId = "Service1_Address";
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