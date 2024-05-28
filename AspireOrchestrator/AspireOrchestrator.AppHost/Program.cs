using Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

// To use this, set a secret in the project for the connection string named appin-distributed-calculator
var insights = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureApplicationInsights("appin-distributed-calculator")
    : builder.AddConnectionString("appin-distributed-calculator", "APPLICATIONINSIGHTS_CONNECTION_STRING");

// Configure Adder in Go
var add = builder.ExecutionContext.IsPublishMode
    ? builder.AddContainer("addapp", "acrt6xtihl2b3uxe.azurecr.io/addapp")
    : builder.AddContainer("addapp", "addapp")
    .WithHttpEndpoint(targetPort: 6000, env: "APP_PORT", name: "http")
    .PublishAsContainer();
var addEnpoint = add.GetEndpoint("http");

// Configure Multiplier in Python
var multiply = builder.ExecutionContext.IsPublishMode
    ? builder.AddContainer("multiplyapp", "acrt6xtihl2b3uxe.azurecr.io/multiplyapp")
    : builder.AddContainer("multiplyapp", "multiplyapp")
    .WithHttpEndpoint(targetPort: 5001, env: "APP_PORT", name: "http")
    .PublishAsContainer();
var multiplyEnpoint = multiply.GetEndpoint("http");

// Configure Divider in NodeJS
var divide = builder.ExecutionContext.IsPublishMode
    ? builder.AddContainer("divideapp", "acrt6xtihl2b3uxe.azurecr.io/divideapp")
    : builder.AddContainer("divideapp", "divideapp")
    .WithHttpEndpoint(targetPort: 4000, env: "APP_PORT", name: "http")
    .PublishAsContainer();
var divideEnpoint = divide.GetEndpoint("http");

// Configure Subtractor in .NET
var subtract = builder.AddProject<Projects.dotnet_subtractor>("subtractapp")
    .WithReference(insights);

// Configure Dapr State Store
var stateStore = builder.AddDaprStateStore("statestore");

// Configure Frontend in React
builder.AddNpmApp(name: "calculator-front-end", workingDirectory: "../../react-calculator")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppPort = 3000,
        AppProtocol = "http",
        DaprHttpPort = 3500
    })
    .WithEnvironment("DAPR_HTTP_PORT", "3500")
    .WithReference(addEnpoint)
    .WithReference(multiplyEnpoint)
    .WithReference(divideEnpoint)
    .WithReference(subtract)
    .WithReference(stateStore)
    .WithReference(insights)
    .WithHttpEndpoint(targetPort: 3000, env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
