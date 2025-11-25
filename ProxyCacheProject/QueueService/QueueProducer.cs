using Apache.NMS;
using Apache.NMS.ActiveMQ;
using System.Text.Json;

namespace QueueService;

public class QueueProducer
{
    private readonly IConnectionFactory factory;

    public QueueProducer(string url = "tcp://localhost:61616")
    {
        factory = new ConnectionFactory(url);
    }

    private void SendToQueue(string queueName, object message)
    {
        using var connection = factory.CreateConnection();
        connection.Start();

        using var session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
        IDestination destination = session.GetQueue(queueName);

        using var producer = session.CreateProducer(destination);

        string json = JsonSerializer.Serialize(message);
        ITextMessage txt = session.CreateTextMessage(json);

        producer.Send(txt);

        Console.WriteLine($"Message envoyé dans {queueName}: {json}");
    }

    // 🔵 Tokens updated
    public void SendTokensUpdated(string userId, int newAmount)
    {
        var message = new
        {
            UserId = userId,
            NewTokenAmount = newAmount
        };

        SendToQueue("USER_TOKENS_UPDATED", message);
    }

    // 🟣 Ranking first changed
    public void SendRankingChanged(string newUsername)
    {
        var message = new
        {
            NewFirstPlaceUserId = newUsername
        };

        SendToQueue("RANKING_FIRST_CHANGED", message);
    }
}
