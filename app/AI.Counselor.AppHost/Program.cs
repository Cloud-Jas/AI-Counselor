var builder = DistributedApplication.CreateBuilder(args);

var appInsightsConnectionString =
    builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

var notificationService = builder.AddProject<Projects.AI_Counselor_NotificationService>("notificationservice").WithEnvironment(
        "APPLICATIONINSIGHTS_CONNECTION_STRING",
        appInsightsConnectionString);

var counselorService = builder.AddProject<Projects.AI_Counselor_CounselorService>("counselorservice").WithEnvironment(
        "APPLICATIONINSIGHTS_CONNECTION_STRING",
        appInsightsConnectionString)
    .WithReference(notificationService);

builder.AddProject<Projects.AICounsellorFrontend>("aicounsellorfrontend").WithEnvironment(
        "APPLICATIONINSIGHTS_CONNECTION_STRING",
        appInsightsConnectionString)
    .WithReference(counselorService.GetEndpoint("http"));

builder.AddProject<Projects.AI_Counselor_IngestionService>("ingestionservice").WithEnvironment(
        "APPLICATIONINSIGHTS_CONNECTION_STRING",
        appInsightsConnectionString)
    .WithReference(counselorService.GetEndpoint("http"))
    .WithReference(notificationService.GetEndpoint("http"));

builder.Build().Run();
