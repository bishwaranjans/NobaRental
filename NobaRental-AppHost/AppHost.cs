var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var backendApi = builder.AddProject<Projects.NobaRental_Backend_WebApi>("nobarental-backendapi")
    .WithHttpHealthCheck("/health")
    .WithReference(redis)
    .WaitFor(redis);

builder.AddProject<Projects.NobaRental_Frontend_Server>("nobarental-frontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(backendApi)
    .WaitFor(backendApi);

await builder.Build().RunAsync();
