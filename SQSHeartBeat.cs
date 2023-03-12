using System.Net;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace AWS_SQSHeartBeat
{
    public class SQSHeartBeat
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly string _queueURL;
        private readonly Message _message;

        public SQSHeartBeat(Message message, IAmazonSQS sqsClient, string queueURL)
        {
            _sqsClient = sqsClient;
            _queueURL = queueURL;
            _message = message;
        }

        public async Task ProcessMessageAsync(Func<Message, CancellationToken, Task> messageProcessingFunction, CancellationToken cancellationToken)
        {
            using (var heartBeatLinkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var processingFunctionTokens = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var heartBeat = StartBeatAsync(_message.ReceiptHandle, _message.MessageId, processingFunctionTokens, heartBeatLinkedToken.Token);

                try
                {
                    await messageProcessingFunction(_message, processingFunctionTokens.Token);
                }
                finally
                {
                    heartBeatLinkedToken.Cancel();
                    await heartBeat;
                }
            }
        }

        private async Task StartBeatAsync(string receiptHandle, string messageId, CancellationTokenSource processingHandler, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"Resetting Visibility Timeout {messageId}");

                    var response = await _sqsClient.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
                    {
                        QueueUrl = _queueURL,
                        ReceiptHandle = receiptHandle,
                        VisibilityTimeout = 30
                    }, cancellationToken);

                    if (response.HttpStatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception("ChangeMessageVisibilityAsync returned non successful status code");
                    }

                    await Task.Delay(15_000);
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Task cancelled");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to ChangeMessageVisibility giving up processing current message: {ex.Message}");
                    processingHandler.Cancel(); // cancel the handler to avoid issues due to the problem changing the visibility timeout
                }
            }
        }
    }
}
