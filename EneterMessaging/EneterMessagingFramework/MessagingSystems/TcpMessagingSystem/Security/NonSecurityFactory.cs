﻿

using System.IO;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security
{
    internal class NonSecurityFactory : ISecurityFactory
    {
        public Stream CreateSecurityStreamAndAuthenticate(Stream source)
        {
            // Return source stream without wrapping it into any other stream.
            return source;
        }
    }
}
