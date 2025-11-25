using System;
using System.IO;
using System.Linq;
using System.Text;
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

        private void LogDiagnostics()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"=== DIAGNOSTICS {DateTime.UtcNow:O} ===");
                sb.AppendLine($"Configured brokerUri: {brokerUri}");
                var t = typeof(Apache.NMS.ActiveMQ.ConnectionFactory);
                sb.AppendLine($"Type: {t.FullName}");
                sb.AppendLine($"Assembly: {t.Assembly.FullName}");
                sb.AppendLine($"IsAbstract: {t.IsAbstract}");
                var nmsAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.FullName.IndexOf("Apache.NMS", StringComparison.OrdinalIgnoreCase) >= 0);
                foreach (var a in nmsAssemblies)
                {
                    try { sb.AppendLine($"Loaded assembly: {a.FullName}, Location: {a.Location}"); } catch { sb.AppendLine($"Loaded assembly: {a.FullName}, Location: <no access>"); }
                }
                sb.AppendLine("====================================");
                File.AppendAllText(logPath, sb.ToString());
            }
            catch (Exception ex)
            {
                try { File.AppendAllText(logPath, $"Diagnostics logging failed: {ex}{Environment.NewLine}"); } catch { }
            }
        }

        private void SendActiveMqMessage(string text)
        {
            try
            {
                LogDiagnostics();

                // essayer deux formes d'URI — certains providers attendent le préfixe "activemq:"
                string[] candidateUris = { "activemq:tcp://localhost:61616", brokerUri };
                Exception lastEx = null;

                foreach (var uri in candidateUris)
                {
                    try
                    {
                        File.AppendAllText(logPath, $"{DateTime.UtcNow:O} - Trying ConnectionFactory with uri: {uri}{Environment.NewLine}");

                        IConnectionFactory factory = new Apache.NMS.ActiveMQ.ConnectionFactory(uri);

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

                        File.AppendAllText(logPath, $"{DateTime.UtcNow:O} - Send succeeded with uri: {uri}{Environment.NewLine}");
                        return; // succès — on quitte
                    }
                    catch (Exception ex)
                    {
                        lastEx = ex;
                        File.AppendAllText(logPath, $"{DateTime.UtcNow:O} - Attempt failed for {uri}: {ex}{Environment.NewLine}");
                    }
                }

                // si on arrive ici, toutes les tentatives ont échoué ; relancer la dernière exception
                if (lastEx != null) throw lastEx;
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"{DateTime.UtcNow:O} - ERROR: {ex}{Environment.NewLine}");
            }
        }
    }
}