using Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

// To use this, set a secret in the project for the connection string named appin-distributed-calculator
var insights = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureApplicationInsights("appin-distributed-calculator")
    : builder.AddConnectionString("appin-distributed-calculator", "APPLICATIONINSIGHTS_CONNECTION_STRING");

// Configure Adder in Go
var add = builder.AddGolangApp("addapp", "../../go-adder")
    .WithHttpEndpoint(env: "APP_PORT")
    .PublishAsDockerFile();

#pragma warning disable ASPIREHOSTINGPYTHON001
// Configure Multiplier in Python
var multiply = builder.AddPythonApp("multiplyapp", "../../python-multiplier", "app.py")
    .WithHttpEndpoint(targetPort: 5001, env: "APP_PORT", name: "http")
    .WithEnvironment("OTEL_SERVICE_NAME", "multiplyapp")
    .PublishAsDockerFile();

// Configure Divider in NodeJS
var divide = builder.AddNodeApp(name: "divideapp", scriptPath: "app.js", workingDirectory: "../../node-divider")
    .WithHttpEndpoint(targetPort: 4000, env: "APP_PORT", name: "http")
    .WithEnvironment("OTEL_SERVICE_NAME", "divideapp")
    .PublishAsDockerFile();

// Configure Subtractor in .NET
var subtract = builder.AddProject<Projects.dotnet_subtractor>("subtractapp")
    .WithReference(insights)
    .WithEnvironment("OTEL_SERVICE_NAME", "subtractapp");

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
    .WithReference(add)
    .WithReference(multiply)
    .WithReference(divide)
    .WithReference(subtract)
    .WithReference(stateStore)
    .WithReference(insights)
    .WithHttpEndpoint(targetPort: 3000, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("OTEL_SERVICE_NAME", "calculator-front-end")
    .PublishAsDockerFile();

builder.Build().Run();
