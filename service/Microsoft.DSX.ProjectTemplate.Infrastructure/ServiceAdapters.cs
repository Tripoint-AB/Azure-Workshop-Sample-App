using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Azure.ServiceBus;
using Microsoft.DSX.ProjectTemplate.Data.DTOs;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Microsoft.DSX.ProjectTemplate.Infrastructure
{
    public interface IMessageService
    {
        Task SendAsync(GroupDto evt);
    }

    public class AzureServiceBusEventService : AzureTopicAdapter, IMessageService
    {

        public AzureServiceBusEventService(IOptions<Options> options) : base(options)
        {
            
        }

        public async Task SendAsync(GroupDto evt) {
            var msg = evt.ToMessage();
            msg.UserProperties.Add("MessageType", evt.GetType().Name);
            await Client.SendAsync(msg);
        }
    }
    
    public static class MessageExtensions
    {
        public static Message ToMessage(this object obj) {
            var json = JsonConvert.SerializeObject(obj);
            return json.AsMessage();
        }

        public static Message AsMessage(this string json) {
            using (var ms = new MemoryStream()) {
                WriteObject(ms, json);
                ms.Seek(0, 0);

                return new Message(ms.ToArray()) {
                    MessageId = Guid.NewGuid().ToString(),
                    ContentType = "string"
                };
            }
        }

        private static void WriteObject(Stream stream, object graph) {
            var dataContractSerializer = new DataContractSerializer(typeof(string));
            var binaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream, null, null, false);
            dataContractSerializer.WriteObject(binaryWriter, graph);
            binaryWriter.Flush();
        }

        public static T ParseMessage<T>(this Message message) {
            var stream = new MemoryStream(message.Body);
            var dataContractSerializer = new DataContractSerializer(typeof(string));
            var reader = XmlDictionaryReader.CreateBinaryReader(stream, null, XmlDictionaryReaderQuotas.Max, null);
            var json = (string)dataContractSerializer.ReadObject(reader);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
    
    public abstract class AzureTopicAdapter
    {
        private readonly Lazy<IQueueClient> _client;

        protected IQueueClient Client => _client.Value;



        protected AzureTopicAdapter(IOptions<Options> options)
        {
            ConnectionString = options.Value.AppSettings.Dependencies.ServiceBus;
            TopicName = options.Value.AppSettings.Dependencies.Topic;
            _client = new Lazy<IQueueClient>(GetClient);
        }

        private string TopicName { get; set; }

        private string ConnectionString { get; set; }

        private IQueueClient GetClient() {
            return new QueueClient(ConnectionString, TopicName);
        }
    }
}