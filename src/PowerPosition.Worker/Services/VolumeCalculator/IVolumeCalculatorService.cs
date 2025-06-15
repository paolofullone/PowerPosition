using Axpo;

namespace PowerPosition.Worker.Services.VolumeCalculator;

public interface IVolumeCalculatorService
{
    double[] AggregateVolumes(IEnumerable<PowerTrade> trades);
}