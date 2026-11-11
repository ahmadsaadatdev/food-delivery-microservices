var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var catalogsDb = postgres.AddDatabase("catalogsdb");
var customersDb = postgres.AddDatabase("customersdb");
var identityDb = postgres.AddDatabase("identitydb");
var ordersDb = postgres.AddDatabase("ordersdb");

var mongo = builder.AddMongoDB("mongodb")
    .WithDataVolume();

var rabbit = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

var redis = builder.AddRedis("redis");

var catalogs = builder.AddProject<Projects.FoodDelivery_Services_Catalogs_Api>("catalogs-api")
    .WithReference(catalogsDb).WaitFor(catalogsDb)
    .WithReference(mongo).WaitFor(mongo)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithReference(redis)
    .WithExternalHttpEndpoints();

var customers = builder.AddProject<Projects.FoodDelivery_Services_Customers_Api>("customers-api")
    .WithReference(customersDb).WaitFor(customersDb)
    .WithReference(mongo).WaitFor(mongo)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithExternalHttpEndpoints();

var identity = builder.AddProject<Projects.FoodDelivery_Services_Identity_Api>("identity-api")
    .WithReference(identityDb).WaitFor(identityDb)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithExternalHttpEndpoints();

var orders = builder.AddProject<Projects.FoodDelivery_Services_Orders_Api>("orders-api")
    .WithReference(ordersDb).WaitFor(ordersDb)
    .WithReference(mongo).WaitFor(mongo)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.FoodDelivery_ApiGateway>("api-gateway")
    .WithReference(catalogs)
    .WithReference(customers)
    .WithReference(identity)
    .WithReference(orders)
    .WaitFor(catalogs)
    .WaitFor(customers)
    .WaitFor(identity)
    .WaitFor(orders)
    .WithExternalHttpEndpoints()
    .WithEnvironment("services__catalogs-api__https__0", catalogs.GetEndpoint("https"))
    .WithEnvironment("services__customers-api__https__0", customers.GetEndpoint("https"))
    .WithEnvironment("services__orders-api__https__0", orders.GetEndpoint("https"))
    .WithEnvironment("services__identity-api__https__0", identity.GetEndpoint("https"));

builder.Build().Run();
