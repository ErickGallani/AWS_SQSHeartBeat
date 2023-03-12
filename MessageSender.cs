using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Bogus;
using Microsoft.Extensions.Hosting;

namespace AWS_SQSHeartBeat
{
    public class MessageSender : BackgroundService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly string _queueURL;
        private CancellationToken _cancellationToken;

        public MessageSender(IAmazonSQS sqsClient, string queueURL)
        {
            _sqsClient = sqsClient;
            _queueURL = queueURL;
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken) =>
            Task.Run(async () =>
            {
                _cancellationToken = cancellationToken;

                Console.WriteLine("Starting Sender worker");

                var faker = new Faker();

                for (int i = 0; i < 10; i++)
                {
                    var json = new
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = faker.Name.FullName(),
                        Date = faker.Date.Future()
                    };

                    var senderResponse = await SendMessageAsync(JsonSerializer.Serialize(json));

                    Console.WriteLine($"Message sent {senderResponse.MessageId}");

                    await Task.Delay(1000);
                }
            });

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;


            Console.WriteLine("Stopped Sender worker");

            return Task.CompletedTask;
        }

        private Task<SendMessageResponse> SendMessageAsync(string messageBody) =>
            _sqsClient.SendMessageAsync(_queueURL, messageBody, _cancellationToken);
    }
}
