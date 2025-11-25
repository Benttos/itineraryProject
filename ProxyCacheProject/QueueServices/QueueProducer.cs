using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Newtonsoft.Json;
using System;
using System.Text.Json.Serialization;

namespace QueueService
{
    public class QueueProducer
    {
        private readonly IConnectionFactory _factory;

        public QueueProducer()
            : this("tcp://localhost:61616") // URL par défaut du broker ActiveMQ
        {
        }

        public QueueProducer(string brokerUrl)
        {
            _factory = new ConnectionFactory(brokerUrl);
        }

        private void SendToQueue(string queueName, object payload)
        {
            // Version compatible .NET Framework : using(...) { ... }
            using (var connection = _factory.CreateConnection())
            {
                connection.Start();

                using (var session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge))
                {
                    IDestination destination = session.GetQueue(queueName);

                    using (var producer = session.CreateProducer(destination))
                    {
                        string json = JsonConvert.SerializeObject(payload);
                        ITextMessage msg = session.CreateTextMessage(json);

                        producer.Send(msg);

                        Console.WriteLine($"Message envoyé dans {queueName} : {json}");
                    }
                }
            }
        }

        /// <summary>
        /// Envoie un message sur la queue USER_TOKENS_UPDATED
        /// </summary>
        public void SendTokensUpdated(string userId, int newAmount)
        {
            var message = new
            {
                UserId = userId,
                NewTokenAmount = newAmount
            };

            SendToQueue("USER_TOKENS_UPDATED", message);
        }

        /// <summary>
        /// Envoie un message sur la queue RANKING_FIRST_CHANGED
        /// </summary>
        public void SendRankingChanged(string newLeaderUsername)
        {
            var message = new
            {
                NewFirstPlaceUserId = newLeaderUsername
            };

            SendToQueue("RANKING_FIRST_CHANGED", message);
        }
    }
}
