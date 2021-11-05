using Bookings.Application.Bookings;
using Bookings.Domain;
using Bookings.Domain.Bookings;
using Bookings.Infrastructure;
using Bookings.Integration;
using Eventuous;
using Eventuous.Diagnostics.OpenTelemetry;
using Eventuous.Diagnostics.OpenTelemetry.Subscriptions;
using Eventuous.Diagnostics.Registrations;
using Eventuous.EventStore;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Projections.MongoDB;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Bookings;

public static class Registrations {
    public static void AddEventuous(this IServiceCollection services) {
        services.AddEventStoreClient("esdb://localhost:2113?tls=false");
        services.AddEventStore<EsdbEventStore>();
        services.AddApplicationService<BookingsCommandService, BookingState, BookingId>();
        services.AddSingleton<IAggregateStore, AggregateStore>();

        services.AddSingleton<Services.IsRoomAvailable>((id,   period) => new ValueTask<bool>(true));
        services.AddSingleton<Services.ConvertCurrency>((from, currency) => new Money(from.Amount * 2, currency));

        services.AddSingleton(Mongo.ConfigureMongo());
        services.AddCheckpointStore<MongoCheckpointStore>();
        services.AddSubscription<AllStreamSubscription, AllStreamSubscriptionOptions>(
            "BookingsProjections",
            builder => builder
                .AddEventHandler<BookingStateProjection>()
        );

        services.AddSubscription<StreamSubscription, StreamSubscriptionOptions>(
            "PaymentIntegration",
            builder => builder
                .Configure(x => x.StreamName = PaymentsIntegrationHandler.Stream)
                .AddEventHandler<PaymentsIntegrationHandler>()
        );
    }

    public static void AddOpenTelemetry(this IServiceCollection services) {
        services.AddOpenTelemetryMetrics(
            builder => builder
                .AddAspNetCoreInstrumentation()
                .AddEventuousSubscriptions()
                .AddPrometheusExporter()
        );

        services.AddOpenTelemetryTracing(
            builder => builder
                .AddAspNetCoreInstrumentation()
                .AddGrpcClientInstrumentation()
                .AddEventuousTracing()
                .AddMongoDBInstrumentation()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("bookings"))
                .SetSampler(new AlwaysOnSampler())
                .AddZipkinExporter()
        );
    }
}