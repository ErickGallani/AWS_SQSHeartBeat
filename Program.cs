using Amazon.SQS;
using AWS_SQSHeartBeat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("Starting SQS Heartbeat Demo");

await Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {

        var queueUrl = context.Configuration.GetSection("AwsConfig:QueueUrl").Value;
        var awsAccessKeyId = context.Configuration.GetSection("AwsConfig:AwsAccessKeyId").Value;
        var awsSecretAccessKey = context.Configuration.GetSection("AwsConfig:AwsSecretAccessKey").Value;

        services.AddSingleton<IAmazonSQS>(new AmazonSQSClient(awsAccessKeyId, awsSecretAccessKey));

        services.AddHostedService<MessageSender>(service =>
            new MessageSender(
                service.GetRequiredService<IAmazonSQS>(),
                queueUrl
            ));

        services.AddHostedService<MessageReceiver>(service =>
            new MessageReceiver(
                service.GetRequiredService<IAmazonSQS>(),
                queueUrl
            ));
    })
    .Build()
    .RunAsync();