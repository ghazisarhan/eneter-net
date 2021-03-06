﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.Nodes.Broker;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using System.Threading;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;

namespace Eneter.MessagingUnitTests.Nodes.Broker
{
    [TestFixture]
    public class Test_Broker
    {
        [Test]
        public void NotifySubscribers()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            // Create channels
            IMessagingSystemFactory aMessagingSystem = new SynchronousMessagingSystemFactory();
            
            IDuplexInputChannel aBrokerInputChannel = aMessagingSystem.CreateDuplexInputChannel("BrokerChannel");
            IDuplexOutputChannel aSubscriber1ClientOutputChannel = aMessagingSystem.CreateDuplexOutputChannel("BrokerChannel");
            IDuplexOutputChannel aSubscriber2ClientOutputChannel = aMessagingSystem.CreateDuplexOutputChannel("BrokerChannel");
            IDuplexOutputChannel aSubscriber3ClientOutputChannel = aMessagingSystem.CreateDuplexOutputChannel("BrokerChannel");

            IDuplexBrokerFactory aBrokerFactory = new DuplexBrokerFactory();

            IDuplexBroker aBroker = aBrokerFactory.CreateBroker();
            BrokerMessageReceivedEventArgs aBrokerReceivedMessage = null;
            aBroker.BrokerMessageReceived += (x, y) =>
                {
                    aBrokerReceivedMessage = y;
                };
            aBroker.AttachDuplexInputChannel(aBrokerInputChannel);
            
            IDuplexBrokerClient aBrokerClient1 = aBrokerFactory.CreateBrokerClient();
            BrokerMessageReceivedEventArgs aClient1ReceivedMessage = null;
            aBrokerClient1.BrokerMessageReceived += (x, y) =>
                {
                    aClient1ReceivedMessage = y;
                };
            aBrokerClient1.AttachDuplexOutputChannel(aSubscriber1ClientOutputChannel);

            IDuplexBrokerClient aBrokerClient2 = aBrokerFactory.CreateBrokerClient();
            BrokerMessageReceivedEventArgs aClient2ReceivedMessage = null;
            aBrokerClient2.BrokerMessageReceived += (x, y) =>
                {
                    aClient2ReceivedMessage = y;
                };
            aBrokerClient2.AttachDuplexOutputChannel(aSubscriber2ClientOutputChannel);

            IDuplexBrokerClient aBrokerClient3 = aBrokerFactory.CreateBrokerClient();
            BrokerMessageReceivedEventArgs aClient3ReceivedMessage = null;
            aBrokerClient3.BrokerMessageReceived += (x, y) =>
            {
                aClient3ReceivedMessage = y;
            };
            aBrokerClient3.AttachDuplexOutputChannel(aSubscriber3ClientOutputChannel);


            string[] aSubscription1 = {"TypeA", "TypeB"};
            aBrokerClient1.Subscribe(aSubscription1);

            string[] aSubscription2 = { "TypeA" };
            aBrokerClient2.Subscribe(aSubscription2);

            string[] aSubscription3 = { "MTypeC" };
            aBrokerClient3.Subscribe(aSubscription3);


            aBroker.Subscribe("TypeA");

            aBrokerClient3.SendMessage("TypeA", "Message A");
            Assert.AreEqual("TypeA", aClient1ReceivedMessage.MessageTypeId);
            Assert.AreEqual("Message A", (string)aClient1ReceivedMessage.Message);
            Assert.AreEqual(null, aClient1ReceivedMessage.ReceivingError);
            
            Assert.AreEqual("TypeA", aClient2ReceivedMessage.MessageTypeId);
            Assert.AreEqual("Message A", (string)aClient2ReceivedMessage.Message);
            Assert.AreEqual(null, aClient2ReceivedMessage.ReceivingError);

            Assert.AreEqual(null, aClient3ReceivedMessage);

            Assert.AreEqual("TypeA", aBrokerReceivedMessage.MessageTypeId);
            Assert.AreEqual("Message A", (string)aBrokerReceivedMessage.Message);
            Assert.AreEqual(null, aBrokerReceivedMessage.ReceivingError);


            aClient1ReceivedMessage = null;
            aClient2ReceivedMessage = null;
            aClient3ReceivedMessage = null;
            aBrokerReceivedMessage = null;

            aBrokerClient2.Unsubscribe();
            
            aBrokerClient3.SendMessage("MTypeC", "Message MTC");

            Assert.AreEqual(null, aClient1ReceivedMessage);

            Assert.AreEqual(null, aClient2ReceivedMessage);

            Assert.AreEqual("MTypeC", aClient3ReceivedMessage.MessageTypeId);
            Assert.AreEqual("Message MTC", (string)aClient3ReceivedMessage.Message);
            Assert.AreEqual(null, aClient3ReceivedMessage.ReceivingError);

            Assert.AreEqual(null, aBrokerReceivedMessage);

            
            aClient1ReceivedMessage = null;
            aClient2ReceivedMessage = null;
            aClient3ReceivedMessage = null;
            aBrokerReceivedMessage = null;

            aBroker.SendMessage("TypeA", "Message A");
            Assert.AreEqual("TypeA", aClient1ReceivedMessage.MessageTypeId);
            Assert.AreEqual("Message A", (string)aClient1ReceivedMessage.Message);
            Assert.AreEqual(null, aClient1ReceivedMessage.ReceivingError);

            Assert.AreEqual(null, aClient2ReceivedMessage);

            Assert.AreEqual(null, aClient3ReceivedMessage);


            Assert.AreEqual("TypeA", aBrokerReceivedMessage.MessageTypeId);
            Assert.AreEqual("Message A", (string)aBrokerReceivedMessage.Message);
            Assert.AreEqual(null, aBrokerReceivedMessage.ReceivingError);


            aClient1ReceivedMessage = null;
            aClient2ReceivedMessage = null;
            aClient3ReceivedMessage = null;
            aBrokerReceivedMessage = null;


            aBroker.Unsubscribe("TypeA");

            string[] aNewMessageType = { "TypeA" };
            aBrokerClient3.Subscribe(aNewMessageType);
            
            aBrokerClient1.DetachDuplexOutputChannel();

            aBrokerClient3.SendMessage("TypeA", "Message A");
            Assert.AreEqual(null, aClient1ReceivedMessage);

            Assert.AreEqual(null, aClient2ReceivedMessage);

            Assert.AreEqual("TypeA", aClient3ReceivedMessage.MessageTypeId);
            Assert.AreEqual("Message A", (string)aClient3ReceivedMessage.Message);
            Assert.AreEqual(null, aClient3ReceivedMessage.ReceivingError);

            Assert.AreEqual(null, aBrokerReceivedMessage);
        }

        [Test]
        public void SubscribeSameMessageTwice()
        {
            // Create channels
            IMessagingSystemFactory aMessagingSystem = new SynchronousMessagingSystemFactory();
        
            IDuplexInputChannel aBrokerInputChannel = aMessagingSystem.CreateDuplexInputChannel("BrokerChannel");
            IDuplexOutputChannel aSubscriberClientOutputChannel = aMessagingSystem.CreateDuplexOutputChannel("BrokerChannel");
            IDuplexOutputChannel aPublisherClientOutputChannel = aMessagingSystem.CreateDuplexOutputChannel("BrokerChannel");

            IDuplexBrokerFactory aBrokerFactory = new DuplexBrokerFactory();

            IDuplexBroker aBroker = aBrokerFactory.CreateBroker();
            aBroker.AttachDuplexInputChannel(aBrokerInputChannel);
        
            IDuplexBrokerClient aSubscriber = aBrokerFactory.CreateBrokerClient();
            List<BrokerMessageReceivedEventArgs> aClient1ReceivedMessage = new List<BrokerMessageReceivedEventArgs>();
            aSubscriber.BrokerMessageReceived += (x, y) =>
                {
                    aClient1ReceivedMessage.Add(y);
                };
            aSubscriber.AttachDuplexOutputChannel(aSubscriberClientOutputChannel);

            IDuplexBrokerClient aPublisher = aBrokerFactory.CreateBrokerClient();
            aPublisher.AttachDuplexOutputChannel(aPublisherClientOutputChannel);

            // Subscribe the 1st time.
            aSubscriber.Subscribe("TypeA");
        
            // Subscribe the 2nd time.
            aSubscriber.Subscribe("TypeA");
        
            // Notify the message.
            aPublisher.SendMessage("TypeA", "Message A");
        
            // Although the client is subscribed twice, the message shall be notified once.
            Assert.AreEqual(1, aClient1ReceivedMessage.Count);
            Assert.AreEqual("TypeA", aClient1ReceivedMessage[0].MessageTypeId);
            Assert.AreEqual("Message A", (String)aClient1ReceivedMessage[0].Message);
        }

        [Test]
        public void DoNotNotifyPublisher()
        {
            // Create channels
            IMessagingSystemFactory aMessagingSystem = new SynchronousMessagingSystemFactory();

            IDuplexInputChannel aBrokerInputChannel = aMessagingSystem.CreateDuplexInputChannel("BrokerChannel");
            IDuplexOutputChannel aClient1OutputChannel = aMessagingSystem.CreateDuplexOutputChannel("BrokerChannel");
            IDuplexOutputChannel aClient2OutputChannel = aMessagingSystem.CreateDuplexOutputChannel("BrokerChannel");

            // Specify in the factory that the publisher shall not be notified from its own published events.
            IDuplexBrokerFactory aBrokerFactory = new DuplexBrokerFactory()
            {
                IsPublisherNotified = false
            };

            IDuplexBroker aBroker = aBrokerFactory.CreateBroker();
            aBroker.AttachDuplexInputChannel(aBrokerInputChannel);

            IDuplexBrokerClient aClient1 = aBrokerFactory.CreateBrokerClient();
            List<BrokerMessageReceivedEventArgs> aClient1ReceivedMessage = new List<BrokerMessageReceivedEventArgs>();
            aClient1.BrokerMessageReceived += (x, y) =>
            {
                aClient1ReceivedMessage.Add(y);
            };
            aClient1.AttachDuplexOutputChannel(aClient1OutputChannel);

            IDuplexBrokerClient aClient2 = aBrokerFactory.CreateBrokerClient();
            List<BrokerMessageReceivedEventArgs> aClient2ReceivedMessage = new List<BrokerMessageReceivedEventArgs>();
            aClient2.BrokerMessageReceived += (x, y) =>
            {
                aClient2ReceivedMessage.Add(y);
            };
            aClient2.AttachDuplexOutputChannel(aClient2OutputChannel);


            aClient1.Subscribe("TypeA");
            aClient2.Subscribe("TypeA");


            // Notify the message.
            aClient2.SendMessage("TypeA", "Message A");

            // Client 2 should not get the notification.
            Assert.AreEqual(1, aClient1ReceivedMessage.Count);
            Assert.AreEqual(0, aClient2ReceivedMessage.Count);
            Assert.AreEqual("TypeA", aClient1ReceivedMessage[0].MessageTypeId);
            Assert.AreEqual("Message A", (String)aClient1ReceivedMessage[0].Message);
        }

        [Test]
        public void Notify_50000()
        {
            // Create channels
            IMessagingSystemFactory aMessagingSystem = new SynchronousMessagingSystemFactory();
            string aBrokerAddress = "BrokerChannel";
            ISerializer aSerializer = new XmlStringSerializer();
            Notify(50000, aSerializer, aMessagingSystem, aBrokerAddress);
        }

        [Test]
        public void Notify_50000_TCP()
        {
            Random aRnd = new Random();
            int aPort = aRnd.Next(8000, 9000); 

            IMessagingSystemFactory aMessagingSystem = new TcpMessagingSystemFactory();
            string aBrokerAddress = "tcp://127.0.0.1:" + aPort + "/";

            ISerializer aSerializer = new BinarySerializer();

            Notify(50000, aSerializer, aMessagingSystem, aBrokerAddress);
        }

        [Test]
        public void Notify_50000_TCP_Interop_BrokerSerializer()
        {
            Random aRnd = new Random();
            int aPort = aRnd.Next(8000, 9000);

            IProtocolFormatter aProtocolFormatter = new EasyProtocolFormatter();
            IMessagingSystemFactory aMessagingSystem = new TcpMessagingSystemFactory(aProtocolFormatter);
            string aBrokerAddress = "tcp://127.0.0.1:" + aPort + "/";

            ISerializer aSerializer = new BrokerCustomSerializer();

            Notify(50000, aSerializer, aMessagingSystem, aBrokerAddress);
        }


        private void Notify(int numberOfTimes, ISerializer serializer, IMessagingSystemFactory messaging, string aBrokerAddress)
        {
            IDuplexInputChannel aBrokerInputChannel = messaging.CreateDuplexInputChannel(aBrokerAddress);
            IDuplexOutputChannel aClient1OutputChannel = messaging.CreateDuplexOutputChannel(aBrokerAddress);
            IDuplexOutputChannel aClient2OutputChannel = messaging.CreateDuplexOutputChannel(aBrokerAddress);

            // Specify in the factory that the publisher shall not be notified from its own published events.
            IDuplexBrokerFactory aBrokerFactory = new DuplexBrokerFactory(serializer)
            {
                IsPublisherNotified = false
            };

            IDuplexBroker aBroker = aBrokerFactory.CreateBroker();
            aBroker.AttachDuplexInputChannel(aBrokerInputChannel);

            IDuplexBrokerClient aClient1 = aBrokerFactory.CreateBrokerClient();
            int aCount = 0;
            AutoResetEvent aCompletedEvent = new AutoResetEvent(false);
            aClient1.BrokerMessageReceived += (x, y) =>
            {
                ++aCount;
                if (aCount == numberOfTimes)
                {
                    aCompletedEvent.Set();
                }
            };
            aClient1.AttachDuplexOutputChannel(aClient1OutputChannel);

            IDuplexBrokerClient aClient2 = aBrokerFactory.CreateBrokerClient();
            aClient2.AttachDuplexOutputChannel(aClient2OutputChannel);

            try
            {
                var aTimer = new PerformanceTimer();
                aTimer.Start();

                aClient1.Subscribe("TypeA");

                for (int i = 0; i < numberOfTimes; ++i)
                {
                    // Notify the message.
                    aClient2.SendMessage("TypeA", "Message A");
                }

                aCompletedEvent.WaitOne();

                aTimer.Stop();

                // Client 2 should not get the notification.
                Assert.AreEqual(numberOfTimes, aCount);
            }
            finally
            {
                aClient1.DetachDuplexOutputChannel();
                aClient2.DetachDuplexOutputChannel();
                aBroker.DetachDuplexInputChannel();
            }
        }
    }
}
