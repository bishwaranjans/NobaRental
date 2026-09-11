var builder = DistributedApplication.CreateBuilder(args);

var backendApi = builder.AddProject<Projects.NobaRental_Backend_WebApi>("nobarental-backendapi")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.NobaRental_Frontend_Server>("nobarental-frontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(backendApi)
    .WaitFor(backendApi);

await builder.Build().RunAsync();
