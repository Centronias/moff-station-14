using System.Runtime.CompilerServices;

namespace Content.Shared._Moffstation.Extensions;

public static class TimeSpanExt
{
    extension(TimeSpan timespan)
    {
        /// Returns a string representation of the given <paramref name="timespan"/> in the form <c>mm:ss</c>, where
        /// <c>mm</c> can be <c>00</c> or greater (eg. days and greater are converted to hours) and <c>ss</c> can be
        /// from <c>00</c> to <c>59</c>. Fractional seconds are ignored.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once InconsistentNaming
        public string ToMMSSString() => $"{(int)timespan.TotalMinutes:D2}:{timespan.Seconds:D2}";
    }
}
