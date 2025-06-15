namespace PowerPosition.Worker.Constants
{
    public class PowerPositionConstants
    {
        #region GeneralSettings
        public const string settingsInterval = "PowerPositionSettings:IntervalInMinutes";
        public const int IntervalInMinutes = 1;
        public const string settingsOutputFolder = "PowerPositionSettings:OutputFolder";
        public const string OutputFolder = "../../reports";
        public const string settingsLocalTimeZone = "PowerPositionSettings:LocalTimeZone";
        public const string localTimeZone = "Europe/London";
        public const string settingsInitialRetry = "PowerPositionSettings:InitialRetryDelayMilliseconds";
        public const int InitialRetryDelayMilliseconds = 100;
        public const string settingsMaxRetryAttempts = "PowerPositionSettings:MaxRetryAttempts";
        public const int MaxRetryAttempts = 5;
        #endregion


        #region VolumeCalculator
        public const int HoursInDay = 24;
        public const int FirstPeriod = 1;
        public const int LastPeriod = 24;
        public const int MinArrayIndex = 0;
        public const int MaxArrayIndex = HoursInDay - 1;
        #endregion
    }
}
