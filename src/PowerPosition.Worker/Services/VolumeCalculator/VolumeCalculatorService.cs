using Axpo;
using PowerPosition.Worker.Constants;

namespace PowerPosition.Worker.Services.VolumeCalculator;
public class VolumeCalculatorService : IVolumeCalculatorService
{
    public double[] AggregateVolumes(IEnumerable<PowerTrade> trades)
    {
        var volumes = new double[PowerPositionConstants.HoursInDay];
        foreach (var trade in trades)
        {
            foreach (var period in trade.Periods)
            {
                int index = period.Period - PowerPositionConstants.FirstPeriod;
                if (IsValidIndex(index))
                {
                    volumes[index] += period.Volume;
                }
            }
        }
        return volumes;
    }

    private static bool IsValidIndex(int index)
    {
        return index is >= PowerPositionConstants.MinArrayIndex and <= PowerPositionConstants.MaxArrayIndex;
    }
}

