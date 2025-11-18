#pragma warning disable ASPIRECSHARPAPPS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#:sdk Aspire.AppHost.Sdk@13.0.0
#:package Aspire.Hosting.Azure.AppContainers@13.0.0
#:package Aspire.Hosting.Azure.Redis@13.0.0

var builder = DistributedApplication.CreateBuilder(args);
builder.AddAzureContainerAppEnvironment("cae");

var cache = builder
    .AddAzureRedis("cache")
    .RunAsContainer()
;

var api1 = builder
    .AddCSharpApp("api1", "Api1.cs")
    .WithReference(cache)
    .WaitFor(cache)
    .PublishAsAzureContainerApp((infra, app) => {;})
;

var api2 = builder
    .AddCSharpApp("api2", "Api2.cs")
    .WithReference(cache)
    .WaitFor(cache)
    .PublishAsAzureContainerApp((infra, app) => {;})
;

builder.Build().Run();

#pragma warning restore ASPIRECSHARPAPPS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

