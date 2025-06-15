using Axpo;

namespace PowerPosition.Worker.Services.TradeFetcher;

public interface ITradeService
{
    Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime date, CancellationToken cancellationToken);
}