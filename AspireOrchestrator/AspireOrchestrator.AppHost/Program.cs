using Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

// Configure Adder in Go
var add = builder.AddContainer("addapp", "acr3stty56cqa3yc.azurecr.io/addapp")
    .WithHttpEndpoint(targetPort: 6000, env: "APP_PORT", name: "http")
    .PublishAsContainer();
var addEnpoint = add.GetEndpoint("http");

// Configure Multiplier in Python
var multiply = builder.AddContainer("multiplyapp", "acr3stty56cqa3yc.azurecr.io/multiplyapp")
    .WithHttpEndpoint(targetPort: 5001, env: "APP_PORT", name: "http")
    .PublishAsContainer();
var multiplyEnpoint = multiply.GetEndpoint("http");

// Configure Divider in NodeJS
var divide = builder.AddContainer("divideapp", "acr3stty56cqa3yc.azurecr.io/divideapp")
    .WithHttpEndpoint(targetPort: 4000, env: "APP_PORT", name: "http")
    .PublishAsContainer();
var divideEnpoint = divide.GetEndpoint("http");

// Configure Subtractor in .NET
var subtract = builder.AddProject<Projects.dotnet_subtractor>("subtractapp");

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
    .WithHttpEndpoint(targetPort: 3000, env: "PORT")
    .PublishAsDockerFile();

builder.Build().Run();
