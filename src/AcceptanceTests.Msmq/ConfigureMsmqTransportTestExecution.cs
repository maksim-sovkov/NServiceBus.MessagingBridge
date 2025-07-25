﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTesting.Support;
using Particular.Msmq;

class ConfigureMsmqTransportTestExecution : IConfigureTransportTestExecution
{
    public BridgeTransportDefinition GetBridgeTransport()
    {
        var transportDefinition = new TestableMsmqTransport();

        return new BridgeTransportDefinition
        {
            TransportDefinition = transportDefinition,
            Cleanup = (ct) => Cleanup(transportDefinition, ct)
        };
    }

    public Func<CancellationToken, Task> ConfigureTransportForEndpoint(string endpointName, EndpointConfiguration endpointConfiguration, PublisherMetadata publisherMetadata)
    {
        var transportDefinition = new TestableMsmqTransport();
        var routingConfig = endpointConfiguration.UseTransport(transportDefinition);
        endpointConfiguration.UsePersistence<AcceptanceTestingPersistence, StorageType.Subscriptions>();

        foreach (var publisher in publisherMetadata.Publishers)
        {
            foreach (var eventType in publisher.Events)
            {
                routingConfig.RegisterPublisher(eventType, publisher.PublisherName);
            }
        }

        endpointConfiguration.EnforcePublisherMetadataRegistration(endpointName, publisherMetadata);

        return (ct) => Cleanup(transportDefinition, ct);
    }

    static Task Cleanup(TestableMsmqTransport msmqTransport, CancellationToken cancellationToken)
    {
        var allQueues = MessageQueue.GetPrivateQueuesByMachine("localhost");
        var queuesToBeDeleted = new List<string>();

        foreach (var messageQueue in allQueues)
        {
            using (messageQueue)
            {
                if (msmqTransport.ReceiveQueues.Any(ra =>
                {
                    var indexOfAt = ra.IndexOf("@", StringComparison.Ordinal);
                    if (indexOfAt >= 0)
                    {
                        ra = ra.Substring(0, indexOfAt);
                    }
                    return messageQueue.QueueName.StartsWith(@"private$\" + ra, StringComparison.OrdinalIgnoreCase);
                }))
                {
                    queuesToBeDeleted.Add(messageQueue.Path);
                }
            }
        }

        foreach (var queuePath in queuesToBeDeleted)
        {
            try
            {
                MessageQueue.Delete(queuePath);
                Console.WriteLine("Deleted '{0}' queue", queuePath);
            }
            catch (Exception)
            {
                Console.WriteLine("Could not delete queue '{0}'", queuePath);
            }
        }

        MessageQueue.ClearConnectionCache();

        return Task.CompletedTask;
    }
}