using Amazon.SQS;
using Amazon.SQS.Model;

namespace AWS_SQSHeartBeat
{
    public class SQSHeartBeat
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly string _queueURL;
        private readonly Message _message;


        private CancellationToken _internalToken;
        private CancellationTokenSource _internalTokenSource;

        public SQSHeartBeat(Message message, IAmazonSQS sqsClient, string queueURL)
        {
            _sqsClient = sqsClient;
            _queueURL = queueURL;
            _message = message;

            _internalTokenSource = new CancellationTokenSource();
            _internalToken = _internalTokenSource.Token;
        }

        public async Task ProcessMessageAsync(Func<Message, CancellationTokenSource, CancellationToken, Task> messageProcessingFunction, CancellationToken cancellationToken)
        {
            using (CancellationTokenSource linkedTokens = 
                CancellationTokenSource.CreateLinkedTokenSource(_internalToken, cancellationToken))
            {
                await Task.WhenAll(
                    messageProcessingFunction(_message, _internalTokenSource, cancellationToken),
                    StartBeatAsync(_message.ReceiptHandle, _message.MessageId, linkedTokens.Token));
            }
        }            

        private async Task StartBeatAsync(string receiptHandle, string messageId, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"Resetting Visibility Timeout {messageId}");

                    await _sqsClient.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
                    {
                        QueueUrl = _queueURL,
                        ReceiptHandle = receiptHandle,
                        VisibilityTimeout = 30
                    }, cancellationToken);

                    await Task.Delay(15_000);
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Task cancelled");
                }
            }
        }
    }
}
