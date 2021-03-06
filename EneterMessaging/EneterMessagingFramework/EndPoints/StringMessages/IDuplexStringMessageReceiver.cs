﻿

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// Receiver of text messages.
    /// </summary>
    public interface IDuplexStringMessageReceiver : IAttachableDuplexInputChannel
    {
        /// <summary>
        /// The event is raised when a text message is received.
        /// </summary>
        event EventHandler<StringRequestReceivedEventArgs> RequestReceived;

        /// <summary>
        /// The event is raised when a duplex string message sender opened the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is raised when a duplex string message sender closed the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// Sends the response message back to the string message sender.
        /// </summary>
        /// <param name="responseReceiverId">identifies the string message sender that shall receive the response</param>
        /// <param name="responseMessage">response text message</param>
        void SendResponseMessage(string responseReceiverId, string responseMessage);
    }
}
