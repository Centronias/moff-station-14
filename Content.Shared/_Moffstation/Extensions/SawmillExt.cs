using System.Runtime.CompilerServices;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Extensions;

public static class SawmillExt
{
    extension(ISawmill sawmill)
    {
        /// Throws a debug assert and logs the given <paramref name="message"/> to the system's error logger.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssertOrLogError(string message)
        {
            DebugTools.Assert(message);
            sawmill.Error(message);
        }

        /// <see cref="AssertOrLogError"/>, but returns <paramref name="ret"/> instead of void.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T AssertOrLogError<T>(string message, T ret)
        {
            sawmill.AssertOrLogError(message);
            return ret;
        }
    }
}
