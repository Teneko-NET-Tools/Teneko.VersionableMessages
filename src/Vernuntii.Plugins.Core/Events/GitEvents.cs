﻿using Microsoft.Extensions.DependencyInjection;

namespace Vernuntii.PluginSystem.Events
{
    /// <summary>
    /// Events for <see cref="IGitPlugin"/>.
    /// </summary>
    public static class GitEvents
    {
        /// <summary>
        /// Event when global service collection is about to be configured.
        /// </summary>
        public readonly static SubjectEvent<IServiceCollection> ConfiguringGlobalServices = new SubjectEvent<IServiceCollection>();

        /// <summary>
        /// Event when global service collection has been configured.
        /// </summary>
        public readonly static SubjectEvent<IServiceCollection> ConfiguredGlobalServices = new SubjectEvent<IServiceCollection>();

        /// <summary>
        /// Event when calculation service collection is about to be configured.
        /// </summary>
        public readonly static SubjectEvent<IServiceCollection> ConfiguringCalculationServices = new SubjectEvent<IServiceCollection>();

        /// <summary>
        /// Event when calculation service collection has been configured.
        /// </summary>
        public readonly static SubjectEvent<IServiceCollection> ConfiguredCalculationServices = new SubjectEvent<IServiceCollection>();
    }
}
