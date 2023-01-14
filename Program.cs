using Amazon.SQS;
using AWS_SQSHeartBeat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("Starting SQS Heartbeat Demo");

await Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        var queueURL = "https://sqs.eu-west-3.amazonaws.com/285159751624/heartbeat-test-queue";

        services.AddSingleton<IAmazonSQS>(new AmazonSQSClient(
            awsAccessKeyId: "AKIAUEZGYP7EO7V5OX4U",
            awsSecretAccessKey: "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"));

        services.AddHostedService<MessageSender>(service => 
            new MessageSender(
                service.GetRequiredService<IAmazonSQS>(),
                queueURL
            ));

        services.AddHostedService<MessageReceiver>(service =>
            new MessageReceiver(
                service.GetRequiredService<IAmazonSQS>(),
                queueURL
            ));
    })
    .Build()
    .RunAsync();