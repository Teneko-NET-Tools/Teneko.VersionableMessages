﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Vernuntii.Git;
using Vernuntii.MessagesProviders;

namespace Vernuntii.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IGitServicesScope"/>.
    /// </summary>
    public static class GitServicesScopeExtensions
    {
        /// <summary>
        /// Adds an instance of <see cref="Repository"/> as <see cref="IRepository"/> if <see cref="IRepository"/> has not been added before.
        /// Then <see cref="ICommitsAccessor"/>, <see cref="ICommitTagsAccessor"/> and <see cref="ICommitVersionsAccessor"/> are associated
        /// wit this <see cref="IRepository"/> instance, in case any of these interfaces have not been added before.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="configureOptions"></param>
        public static IGitServicesScope AddRepository(this IGitServicesScope scope, Action<RepositoryOptions>? configureOptions = null)
        {
            var services = scope.Services;

            if (configureOptions != null) {
                services.AddOptions<RepositoryOptions>().Configure(configureOptions);
            }

            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<RepositoryOptions>>().Value);
            services.TryAddSingleton<IRepository, Repository>();
            services.TryAddSingleton<ICommitsAccessor>(sp => sp.GetRequiredService<IRepository>());
            services.TryAddSingleton<ICommitTagsAccessor>(sp => sp.GetRequiredService<IRepository>());
            services.TryAddSingleton<ICommitVersionsAccessor>(sp => sp.GetRequiredService<IRepository>());
            services.TryAddSingleton<IBranchesAccessor>(sp => sp.GetRequiredService<IRepository>());
            return scope;
        }

        /// <summary>
        /// Configures the since-commit.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="configure"></param>
        public static IGitServicesScope Configure(this IGitServicesScope scope, Action<IGitConfigurer> configure)
        {
            var services = scope.Services;
            GitConfigurer? configurer = null;

            Func<IServiceProvider, GitConfigurer> configurerProvider = sp => {
                if (configurer != null) {
                    return configurer;
                }

                configurer = new GitConfigurer(sp);
                configure(configurer);
                return configurer;
            };

            services.AddSingleton<IConfigureOptions<FoundCommitVersionOptions>>(sp => configurerProvider(sp));
            services.AddSingleton<IConfigureOptions<GitCommitMessagesProviderOptions>>(sp => configurerProvider(sp));
            services.AddScoped<IConfigureOptions<VersionIncrementationOptions>>(sp => configurerProvider(sp));
            return scope;
        }

        /// <summary>
        /// Uses <see cref="GitCommitMessagesProvider"/> as <see cref="IMessagesProvider"/> and
        /// tries to register <see cref="GitCommitMessagesProviderOptions"/> from <see cref="IOptions{TOptions}"/>
        /// where T is <see cref="GitCommitMessagesProviderOptions"/>.
        /// </summary>
        /// <param name="scope"></param>
        public static IGitServicesScope UseCommitMessagesProvider(this IGitServicesScope scope)
        {
            scope.Services.TryAddScoped(sp => sp.GetRequiredService<IOptionsSnapshot<GitCommitMessagesProviderOptions>>().Value);

            scope.Services.ScopeToVernuntii(features => features
                .AddVersionIncrementation(features => features
                    .UseMessagesProvider(sp => {
                        // TODO: Try to remove this
                        var repository = sp.GetRequiredService<IRepository>();
                        return ActivatorUtilities.CreateInstance<GitCommitMessagesProvider>(sp, repository);
                    })));

            return scope;
        }

        /// <summary>
        /// Sets <see cref="VersionIncrementationOptions.StartVersion"/>
        /// to <see cref="FoundCommitVersion.CommitVersion"/> if the
        /// latest version has been found and is present in
        /// <see cref="FoundCommitVersion"/>.
        /// </summary>
        /// <param name="scope"></param>
        public static IGitServicesScope UseLatestCommitVersion(this IGitServicesScope scope)
        {
            var services = scope.Services;
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<CommitVersionFinderOptions>>().Value);
            services.TryAddSingleton<ICommitVersionFinder, LatestCommitVersionFinder>();
            services.TryAddScoped<FoundCommitVersion>();

            services.AddOptions<GitCommitMessagesProviderOptions>()
                .PostConfigure<FoundCommitVersion, ICommitsAccessor>((options, foundCommitVersion, commitsAccessor) => {
                    if (foundCommitVersion.CommitVersion != null) {
                        // We want to start reading messages after found commit version
                        options.SinceCommit = foundCommitVersion.CommitVersion.CommitSha;
                    }
                });

            services.AddOptions<VersionIncrementationOptions>()
                .PostConfigure<FoundCommitVersion>((options, foundCommitVersion) => {
                    if (foundCommitVersion.CommitVersion != null) {
                        options.StartVersion = foundCommitVersion.CommitVersion;
                        options.IsStartVersionCoreAlreadyReleased = foundCommitVersion.IsCommitVersionCoreAlreadyReleased;
                    }
                });

            return scope;
        }
    }
}
