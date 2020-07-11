using Kafka.Public;
using Microsoft.Extensions.Logging;

namespace Extensions
{
    public static class KafkaConsumerFactory
    {
        public static KafkaConsumer<TKey, TValue> Create<TKey, TValue>(
            ConsumerConfig config,
            ILoggerFactory loggerFactory) 
            where TKey : class 
            where TValue : class
        {
            SerializationConfig serializationConfig = CreateSerializationConfig<TKey, TValue>(config);
            
            IClusterClient clusterClient = ClusterClientFactory.Create(
                config,
                serializationConfig,
                loggerFactory);

            var consumer = new KafkaConsumer<TKey, TValue>(
                config.Topic,
                clusterClient);
            
            consumer.Subscribe(config.GroupId, config.Topics, new ConsumerGroupConfiguration());

            return consumer;
        }
        
        private static SerializationConfig CreateSerializationConfig<TKey, TValue>(BaseKafkaConfig config)
        {
            var serializationConfig = new SerializationConfig();

            serializationConfig.SetDefaultDeserializers(
                KafkaDeserializerFactory.CreateDeserializer<TKey>(config.KeySerializationType),
                KafkaDeserializerFactory.CreateDeserializer<TValue>(config.ValueSerializationType));
            
            return serializationConfig;
        }
    }
}