﻿using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

class Publishing : BridgeAcceptanceTest
{
    [Test]
    public async Task Subscriber_should_get_the_event()
    {
        var context = await Scenario.Define<Context>()
            .WithBridge(bridgeConfiguration =>
            {
                var bridgeTransport = new TestableBridgeTransport(TransportBeingTested);

                bridgeTransport.AddTestEndpoint<Publisher>();
                bridgeConfiguration.AddTransport(bridgeTransport);

                var subscriberEndpoint = new BridgeEndpoint(Conventions.EndpointNamingConvention(typeof(Subscriber)));

                subscriberEndpoint.RegisterPublisher<MyEvent>(Conventions.EndpointNamingConvention(typeof(Publisher)));
                bridgeConfiguration.AddTestTransportEndpoint(subscriberEndpoint);
            })
            .WithEndpoint<Publisher>(b => b
                .When(c => TransportBeingTested.SupportsPublishSubscribe || c.SubscriberSubscribed, (session, _) =>
                    session.Publish(new MyEvent())))
            .WithEndpoint<Subscriber>()
            .Done(c => c.SubscriberGotEvent)
            .Run();

        Assert.That(context.SubscriberGotEvent, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool SubscriberSubscribed { get; set; }
        public bool SubscriberGotEvent { get; set; }
    }

    class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() =>
            EndpointSetup<DefaultPublisher>(c =>
            {
                c.OnEndpointSubscribed<Context>((_, ctx) =>
                {
                    ctx.SubscriberSubscribed = true;
                });
            }, metadata => metadata.RegisterSelfAsPublisherFor<MyEvent>(this));
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() => EndpointSetup<DefaultTestServer>(_ => { }, metadata => metadata.RegisterPublisherFor<MyEvent, Publisher>());

        public class MessageHandler(Context context) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext handlerContext)
            {
                context.SubscriberGotEvent = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyEvent : IEvent;
}