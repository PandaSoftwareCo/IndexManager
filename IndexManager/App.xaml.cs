using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Amazon;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.Runtime;
using IndexManager.Framework;
using IndexManager.Interfaces;
using IndexManager.Models;
using IndexManager.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Serilog;
using Serilog.Sinks.Amazon.Kinesis.Stream;
using Serilog.Sinks.Amazon.Kinesis.Stream.Sinks;

namespace IndexManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IConfigurationRoot Configuration { get; }
        public IServiceProvider ServiceProvider { get; }

        public App()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<App>();
            Configuration = builder.Build();

            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration);

            string streamName = Configuration["AppSettings:StreamName"];
            const int shardCount = 5;
            var AccessKey = Configuration["AppSettings:AwsAccessKeyId"];
            var SecretKey = Configuration["AppSettings:AwsSecretAccessKey"];
            IAmazonKinesis client = new AmazonKinesisClient(AccessKey, SecretKey, new AmazonKinesisConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(Configuration["AppSettings:Region"]),
                Timeout = TimeSpan.FromSeconds(10),
                // NOTE: The following property is obsolete for
                //       versions of the AWS SDK for .NET that target .NET Core.
                //ReadWriteTimeout = TimeSpan.FromSeconds(10),
                RetryMode = RequestRetryMode.Standard,
                MaxErrorRetry = 3
            });
            var streamOk = KinesisApi.CreateAndWaitForStreamToBecomeAvailable(
                kinesisClient: client,
                streamName: streamName,
                shardCount: shardCount
            );
            if (streamOk)
            {
                loggerConfig.WriteTo.Async(a => a.AmazonKinesis(
                    kinesisClient: client,
                    streamName: streamName,
                    period: TimeSpan.FromSeconds(2),
                    bufferBaseFilename: "./logs/kinesis-buffer"
                ));
            }

            Log.Logger = loggerConfig.CreateLogger();

            services.AddLogging(builder =>
            {
                builder.AddConfiguration(Configuration);
                builder.AddDebug();
                builder.AddSerilog();
            });

            services.AddHttpClient("HttpClient", httpClient =>
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(100);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                })
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(2));

            services.AddSingleton<IConfigurationRoot>(provider => Configuration);
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.AddSingleton<IAppSettings>(sp =>
                sp.GetRequiredService<IOptions<AppSettings>>().Value);

            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();
        }

        private IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<Exception>()
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(10, retryAttempt => TimeSpan.FromSeconds(2));
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            ServiceProvider.GetService<MainWindow>()?.Show();
        }
    }
}
