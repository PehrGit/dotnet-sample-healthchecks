using Bookings.Application.Queries;
using Bookings.Domain.Bookings;
using Eventuous;
using Eventuous.Projections.MongoDB.Tools;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Bookings.HttpApi.Bookings;

[Route("/bookings")]
public class QueryApi : ControllerBase {
    public IMongoDatabase Database { get; }
    readonly IAggregateStore _store;
        
    public QueryApi(IAggregateStore store, IMongoDatabase database)
    {
        Database = database;
        _store = store;
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<BookingState> GetBooking(string id, CancellationToken cancellationToken) {
        var booking = await _store.Load<Booking>(StreamName.For<Booking>(id), cancellationToken);
        return booking.State;
    }
    
    [HttpGet]
    [Route("projection/{id}")]
    public async Task<BookingDocument> GetBookingProjection(string id, CancellationToken cancellationToken)
    {
        var result = Database.GetDocumentCollection<BookingDocument>().Find(document => document.Id == id)
            .SingleOrDefault();
        return result;
    }
}