﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace BasicSendReceiveUsingTopicSubscriptionClient
{
    using Microsoft.Azure.ServiceBus;
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        // Connection String for the namespace can be obtained from the Azure portal under the 
        // 'Shared Access policies' section.
        const string ServiceBusConnectionString = "{ServiceBus connection string}";
        const string TopicName = "{Topic Name}";
        const string FirstSubscriptionName = "{First Subscription Name}";
        const string SecondSubscriptionName = "{Second Subscription Name}";
        static ITopicClient topicClient;
        static ISubscriptionClient firstSubscriptionClient;
        static ISubscriptionClient secondSubscriptionClient;


        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            const int numberOfMessages = 10;
            topicClient = new TopicClient(ServiceBusConnectionString, TopicName);
            firstSubscriptionClient = new SubscriptionClient(ServiceBusConnectionString, TopicName, FirstSubscriptionName);
            secondSubscriptionClient = new SubscriptionClient(ServiceBusConnectionString, TopicName, SecondSubscriptionName);

            Console.WriteLine("======================================================");
            Console.WriteLine("Press any key to exit after receiving all the messages.");
            Console.WriteLine("======================================================");

            // Register Subscription's MessageHandler and receive messages in a loop
            RegisterOnMessageHandlerAndReceiveMessages();

            // Send Messages
            await SendMessagesAsync(numberOfMessages);

            Console.ReadKey();

            await firstSubscriptionClient.CloseAsync();
            await secondSubscriptionClient.CloseAsync();
            await topicClient.CloseAsync();
        }

        static void RegisterOnMessageHandlerAndReceiveMessages()
        {
            // Configure the MessageHandler Options in terms of exception handling, number of concurrent messages to deliver etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of Concurrent calls to the callback `ProcessMessagesAsync`, set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
                AutoComplete = false
            };

            // Register the function that will process messages
            firstSubscriptionClient.RegisterMessageHandler(ProcessMessagesFirstSubscriptionAsync, messageHandlerOptions);
            secondSubscriptionClient.RegisterMessageHandler(ProcessMessagesSecondSubscriptionAsync, messageHandlerOptions);
        }

        static async Task ProcessMessagesFirstSubscriptionAsync(Message message, CancellationToken token)
        {
            // Process the message
            Console.WriteLine($"First Sub received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

            // Complete the message so that it is not received again.
            // This can be done only if the subscriptionClient is created in ReceiveMode.PeekLock mode (which is default).
            await firstSubscriptionClient.CompleteAsync(message.SystemProperties.LockToken);

            // Note: Use the cancellationToken passed as necessary to determine if the subscriptionClient has already been closed.
            // If subscriptionClient has already been Closed, you may chose to not call CompleteAsync() or AbandonAsync() etc. calls 
            // to avoid unnecessary exceptions.
        }

        static async Task ProcessMessagesSecondSubscriptionAsync(Message message, CancellationToken token)
        {
            // Process the message
            Console.WriteLine($"Second Sub received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

            // Complete the message so that it is not received again.
            // This can be done only if the subscriptionClient is created in ReceiveMode.PeekLock mode (which is default).
            await secondSubscriptionClient.CompleteAsync(message.SystemProperties.LockToken);


            // Note: Use the cancellationToken passed as necessary to determine if the subscriptionClient has already been closed.
            // If subscriptionClient has already been Closed, you may chose to not call CompleteAsync() or AbandonAsync() etc. calls 
            // to avoid unnecessary exceptions.
        }

        // Use this Handler to look at the exceptions received on the MessagePump
        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            Console.WriteLine("Exception context for troubleshooting:");
            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");
            return Task.CompletedTask;
        }

        static async Task SendMessagesAsync(int numberOfMessagesToSend)
        {
            try
            {
                for (var i = 0; i < numberOfMessagesToSend; i++)
                {
                    // Create a new message to send to the topic
                    string messageBody = $"Message {i}";
                    var message = new Message(Encoding.UTF8.GetBytes(messageBody));

                    // Write the body of the message to the console
                    Console.WriteLine($"Sending message: {messageBody}");

                    // Send the message to the topic
                    await topicClient.SendAsync(message);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }
        }
    }
}