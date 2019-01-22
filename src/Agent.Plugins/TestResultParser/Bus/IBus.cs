﻿using System;

namespace Agent.Plugins.TestResultParser.Plugin
{
    public interface IBus<out TMessage>
    {
        /* Subscribe to Message Bus to receive messages via Pub-Sub model */
        Guid Subscribe(Action<TMessage> handlerAction);

        /* Unsubscribe to Message Bus so that subscriber no longer receives messages */
        void Unsubscribe(Guid subscriptionId);
    }
}