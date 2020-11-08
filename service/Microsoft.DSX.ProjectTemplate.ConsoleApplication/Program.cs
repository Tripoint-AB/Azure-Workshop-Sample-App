using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.InteropExtensions;
using Microsoft.DSX.ProjectTemplate.API;
using Microsoft.DSX.ProjectTemplate.Command;
using Microsoft.DSX.ProjectTemplate.Data.DTOs;
using Microsoft.DSX.ProjectTemplate.Data.Models;
using Microsoft.DSX.ProjectTemplate.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Microsoft.DSX.ProjectTemplate.ConsoleApplication
{
    class Program
    {
        private static string SqlConnectionString = "";
        private static string ServiceBusConnectionString = "";
        private static string QueueName = "";
        static IQueueClient queueClient;


        public static async Task Main(string[] args)
        {    
                
            ServicePointManager.DefaultConnectionLimit = 24;
            
            var host = new HostBuilder()
                .ConfigureServices((context, collection) =>
                {
                    var services = collection;
                    var options = services.LoadOptions(context);
                    services
                        .AddDbConnections(options)
                        .AddAutoMapperProfiles()
                        .AddServices()
                        .AddMediatR(typeof(HandlerBase))
                        .AddOptions();
                    SqlConnectionString = options.AppSettings.ConnectionStrings.Database;
                    ServiceBusConnectionString = options.AppSettings.Dependencies.ServiceBus;
                    QueueName = options.AppSettings.Dependencies.Topic;
                }).Build();
            await RunAsync(host);

        }

        static async Task RunAsync(IHost host)
        {
            queueClient = new QueueClient(ServiceBusConnectionString, QueueName);

            Console.WriteLine("======================================================");
            Console.WriteLine("Press ENTER key to exit after receiving all the messages.");
            Console.WriteLine("======================================================");

            // Register QueueClient's MessageHandler and receive messages in a loop
            RegisterOnMessageHandlerAndReceiveMessages();

            Console.ReadKey();

            await queueClient.CloseAsync();
            await host.StopAsync();
        }
        
        static void RegisterOnMessageHandlerAndReceiveMessages()
        {
            // Configure the message handler options in terms of exception handling, number of concurrent messages to deliver, etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether the message pump should automatically complete the messages after returning from user callback.
                // False below indicates the complete operation is handled by the user callback as in ProcessMessagesAsync().
                AutoComplete = false
            };

            // Register the function that processes messages.
            queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }
        
        static async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            // Process the message.
            Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");
            var dto = JsonConvert.DeserializeObject<GroupDto>(message.GetBody<string>());

            using (var context = new DbCtx(SqlConnectionString))
            {
                Console.WriteLine("Saving new data to Database");

                var model = new Group()
                {
                    Name = dto.Name,
                    IsActive = dto.IsActive
                };
                await context.AddAsync(model, token);
                await context.SaveChangesAsync(token);
                Console.WriteLine("Added new data to Database");
            }

            // Complete the message so that it is not received again.
            // This can be done only if the queue Client is created in ReceiveMode.PeekLock mode (which is the default).
            await queueClient.CompleteAsync(message.SystemProperties.LockToken);

            // Note: Use the cancellationToken passed as necessary to determine if the queueClient has already been closed.
            // If queueClient has already been closed, you can choose to not call CompleteAsync() or AbandonAsync() etc.
            // to avoid unnecessary exceptions.
        }
        
        // Use this handler to examine the exceptions received on the message pump.
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
    }

    public class DbCtx : DbContext
    {
        public DbCtx( string connString)
        {
            SqlConnectionString = connString;
        }

        private string SqlConnectionString { get; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlConnectionString);
        }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Library> Libraries { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<User> Users { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureIndexes(modelBuilder);

            ConfigureRelationships(modelBuilder);

            ConfigurePropertyConversion(modelBuilder);

            ConfigureSeedData(modelBuilder);
        }


        private void ConfigureRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Library>()
                .OwnsOne(lib => lib.Address);
        }

        private void ConfigurePropertyConversion(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .Property(b => b.Metadata)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<Dictionary<string, string>>(v));
        }

        private void ConfigureSeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Group>()
                .HasData(
                    new Group()
                    {
                        Id = 1,
                        Name = "Surface",
                        IsActive = true
                    },
                    new Group()
                    {
                        Id = 2,
                        Name = "HoloLens",
                        IsActive = true
                    },
                    new Group()
                    {
                        Id = 3,
                        Name = "Xbox",
                        IsActive = true
                    }
                );
        }

        private void ConfigureIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Group>()
                .HasIndex(x => x.Name)
                .IsUnique();

            modelBuilder.Entity<Project>()
                .HasIndex(x => x.Name)
                .IsUnique();

            modelBuilder.Entity<Library>()
                .HasIndex(x => x.Name)
                .IsUnique();
        }
    }

}