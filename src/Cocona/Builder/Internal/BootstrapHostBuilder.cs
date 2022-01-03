using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Cocona.Builder.Internal
{
    internal class BootstrapHostBuilder : IHostBuilder
    {
        private readonly IServiceCollection _services;

        private readonly List<Action<IConfigurationBuilder>> _configureHostConfigs = new();
        private readonly List<Action<HostBuilderContext, IServiceCollection>> _configureServices = new();
        private readonly List<Action<HostBuilderContext, IConfigurationBuilder>> _configureAppConfigs = new();
        private readonly List<Action<IHostBuilder>> _remainingOperations = new();

        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

        public BootstrapHostBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public IHost Build()
        {
            throw new InvalidOperationException();
        }

        public (HostBuilderContext HostBuilderContext, ConfigurationManager HostConfiguration) Apply(ConfigurationManager configuration, HostBuilder hostBuilder)
        {
            // Use default services/configurations for HostBuilder
            this.ConfigureDefaults(Array.Empty<string>());

            var hostConfiguration = new ConfigurationManager();

            foreach (var action in _configureHostConfigs)
            {
                action(hostConfiguration);
            }

            var hostBuilderContext = new HostBuilderContext(new Dictionary<object, object>())
            {
                Configuration = hostConfiguration,
                HostingEnvironment = new HostEnvironment()
                {
                    ApplicationName = hostConfiguration[HostDefaults.ApplicationKey],
                    EnvironmentName = hostConfiguration[HostDefaults.EnvironmentKey] ?? Environments.Production,
                    ContentRootPath = hostConfiguration[HostDefaults.ContentRootKey],
                    ContentRootFileProvider = new PhysicalFileProvider(hostConfiguration[HostDefaults.ContentRootKey]),
                },
            };

            configuration.SetBasePath(hostBuilderContext.HostingEnvironment.ContentRootPath);
            configuration.AddConfiguration(hostConfiguration, true);
            foreach (var action in _configureAppConfigs)
            {
                action(hostBuilderContext, configuration);
            }
            hostBuilderContext.Configuration = configuration;

            foreach (var action in _configureServices)
            {
                action(hostBuilderContext, _services);
            }

            foreach (var action in _remainingOperations)
            {
                action(hostBuilder);
            }

            return (hostBuilderContext, hostConfiguration);
        }

        class HostEnvironment : IHostEnvironment
        {
            public string? ApplicationName { get; set; }
            public IFileProvider? ContentRootFileProvider { get; set; }
            public string? ContentRootPath { get; set; }
            public string? EnvironmentName { get; set; }
        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _configureAppConfigs.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            _remainingOperations.Add(builder => builder.ConfigureContainer<TContainerBuilder>(configureDelegate));
            return this;
        }

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            _configureHostConfigs.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            _configureServices.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
            where TContainerBuilder : notnull
        {
            _remainingOperations.Add(builder => builder.UseServiceProviderFactory<TContainerBuilder>(factory));
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
            where TContainerBuilder : notnull
        {
            _remainingOperations.Add(builder => builder.UseServiceProviderFactory<TContainerBuilder>(factory));
            return this;
        }
    }
}
