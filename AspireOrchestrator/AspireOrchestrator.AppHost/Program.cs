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

// Configure Multiplier in Python
#pragma warning disable ASPIREHOSTINGPYTHON001
var multiply = builder.AddPythonApp("multiplyapp", "../../python-multiplier", "app.py")
    .WithHttpEndpoint(env: "APP_PORT")
    .PublishAsDockerFile();

// Configure Divider in NodeJS
var divide = builder.AddNodeApp(name: "divideapp", scriptPath: "app.js", workingDirectory: "../../node-divider")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(env: "APP_PORT")
    .PublishAsDockerFile();

// Configure Subtractor in .NET
var subtract = builder.AddProject<Projects.dotnet_subtractor>("subtractapp")
    .WithReference(insights);

// Configure Dapr State Store
var stateStore = builder.AddDaprStateStore("statestore");

// Configure Frontend in React
builder.AddViteApp(name: "calculator-front-end", workingDirectory: "../../react-calculator", packageManager: "yarn")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppPort = 3000,
        AppProtocol = "http",
        DaprHttpPort = 3500
    })
    .WithYarnPackageInstallation()
    .WithEnvironment("DAPR_HTTP_PORT", "3500")
    .WithReference(add)
    .WithReference(multiply)
    .WithReference(divide)
    .WithReference(subtract)
    .WithReference(stateStore)
    .WithReference(insights)
    .PublishAsDockerFile();

builder.Build().Run();
