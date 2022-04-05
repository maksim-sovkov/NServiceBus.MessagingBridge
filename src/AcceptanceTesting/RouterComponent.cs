﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;

partial class RouterComponent<TContext> : IComponentBehavior
    where TContext : ScenarioContext
{
    RouterConfiguration routerConfiguration;

    public RouterComponent(RouterConfiguration routerConfiguration) => this.routerConfiguration = routerConfiguration;

#pragma warning disable PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
#pragma warning restore PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
    {
        return Task.FromResult<ComponentRunner>(new Runner(routerConfiguration, new AccptanceTestLoggerFactory(run.ScenarioContext)));
    }

    class Runner : ComponentRunner
    {
        public Runner(RouterConfiguration routerConfiguration, ILoggerFactory loggerFactory)
        {
            this.routerConfiguration = routerConfiguration;
            this.loggerFactory = loggerFactory;
        }

        public override string Name => "Router";

        public override async Task ComponentsStarted(CancellationToken cancellationToken = default)
        {
            router = await routerConfiguration.Start(loggerFactory, null, cancellationToken).ConfigureAwait(false);
        }

        public override Task Stop()
        {
            return router?.Stop();
        }

        RunningRouter router;

        readonly RouterConfiguration routerConfiguration;
        readonly ILoggerFactory loggerFactory;
    }
}