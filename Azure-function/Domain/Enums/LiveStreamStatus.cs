namespace Domain.Enums
{
    /// <summary>
    /// Represents the possible lifecycle states of a live stream session.
    /// </summary>
    public enum LiveStreamStatus
    {
        /// <summary>Stream has been created but not yet started.</summary>
        Scheduled = 0,

        /// <summary>Stream is currently broadcasting.</summary>
        Live = 1,

        /// <summary>Stream ended normally.</summary>
        Ended = 2,

        /// <summary>Stream was cancelled before it started.</summary>
        Cancelled = 3
    }
}
