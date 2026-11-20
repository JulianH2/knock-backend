using System.Globalization;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;

namespace Knock.Market.Api.Modules.Requests.Services;

internal sealed class MyQuotesService(ITechnicianDirectory technicians, QuoteStore quotes)
{
    public async Task<MyQuotePage?> ListAsync(string sub, MyQuotesQuery query, CancellationToken cancellationToken)
    {
        if (!await technicians.ExistsAsync(sub, cancellationToken)) return null;
        var limit = query.Limite is null ? 20 : int.Parse(query.Limite, CultureInfo.InvariantCulture);
        var cursor = query.Cursor is null ? ((DateTime, MongoDB.Bson.ObjectId)?)null : RequestCursor.Decode(query.Cursor);
        return await quotes.ListAsync(sub, query.Estado, cursor, limit, cancellationToken);
    }
}
