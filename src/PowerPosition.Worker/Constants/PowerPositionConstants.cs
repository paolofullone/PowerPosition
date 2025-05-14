namespace PowerPosition.Worker.Constants
{
    public class PowerPositionConstants
    {
        public const string settingsInterval = "PowerPosition:IntervalInMinutes";
        public const int defaultInterval = 60;
        public const string settingsOutputFolder = "PowerPosition:OutputFolder";
        public const int retryDelayMiliseconds = 100;
        public const int maxGetTradesRetries = 5;
    }
}
