using System;
using System.IO;
using Apache.NMS;
using Apache.NMS.ActiveMQ;

namespace ActiveMq
{
    public class ActiveMqService : IActiveMqService
    {
        private readonly string brokerUri = "tcp://localhost:61616";
        private readonly string user = "admin";
        private readonly string pass = "admin";
        private readonly string queueName = "classementQueue";
        private readonly string logPath = Path.Combine(Path.GetTempPath(), "activemq_send.log");


        public void sendMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                message = "Notification: top du classement changé";

            SendActiveMqMessage(message);
        }

        private void SendActiveMqMessage(string text)
        {
            try
            {
                // Utiliser l'interface et la classe concrète du provider ActiveMQ explicitement
                IConnectionFactory factory = new Apache.NMS.ActiveMQ.ConnectionFactory(brokerUri);

                using (IConnection connection = factory.CreateConnection(user, pass))
                {
                    connection.Start();

                    using (ISession session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge))
                    {
                        IDestination destination = session.GetQueue(queueName);

                        using (IMessageProducer producer = session.CreateProducer(destination))
                        {
                            producer.DeliveryMode = MsgDeliveryMode.Persistent;
                            ITextMessage msg = session.CreateTextMessage(text);
                            producer.Send(msg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"{DateTime.UtcNow:O} - ERROR: {ex}{Environment.NewLine}");
            }
        }
    }
}