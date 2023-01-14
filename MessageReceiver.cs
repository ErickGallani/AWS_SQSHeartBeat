using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;

namespace AWS_SQSHeartBeat
{
    public class MessageReceiver : BackgroundService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly string _queueURL;
        private CancellationToken _cancellationToken;
        private Random _random;

        public MessageReceiver(IAmazonSQS sqsClient, string queueURL)
        {
            _sqsClient = sqsClient;
            _queueURL = queueURL;
            _random = new Random();
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken) =>
            Task.Run(async () =>
            {
                _cancellationToken = cancellationToken;

                // Delay to start pulling for messages
                await Task.Delay(2000);

                Console.WriteLine("Starting Receiver worker");

                await ReceiveMessageAsync();
            });

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            Console.WriteLine("Stopped Receiver worker");

            return Task.CompletedTask;
        }

        private async Task ReceiveMessageAsync()
        {
            while(!_cancellationToken.IsCancellationRequested)
            {
                var messageResponse = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueURL,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20
                }, _cancellationToken);

                var processingMessages = messageResponse
                    .Messages
                    .Select(message => new SQSHeartBeat(message, _sqsClient, _queueURL));

                var processingMessagesTasks =
                    processingMessages.Select(processing => processing.ProcessMessageAsync(DoStuff, _cancellationToken));

                await Task.WhenAll(processingMessagesTasks);
            }
        }

        private async Task DoStuff(Message message, CancellationTokenSource heartBeatToken, CancellationToken token)
        {
            try
            {
                var processingTime = 31000 + _random.Next(1000, 60000);

                Console.WriteLine($"\nProcessing Message {message.MessageId} with processing time {processingTime / 1000} seconds");

                // The default Visibility Timeout is 30 seconds, so this delay 
                // will add a random delay in simulation of a long running processing messsage
                // that can take from 31 seconds to 91 seconds
                await Task.Delay(processingTime);

                Console.WriteLine($"\nDeleting message {message.MessageId} from queue...");

                await _sqsClient.DeleteMessageAsync(_queueURL, message.ReceiptHandle, token);

                heartBeatToken.Cancel();
            } 
            catch (TaskCanceledException)
            {
                Console.WriteLine("Task cancelled");
            }
        }
    }
}
