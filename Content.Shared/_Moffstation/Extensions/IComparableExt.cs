using System.Runtime.CompilerServices;

namespace Content.Shared._Moffstation.Extensions;

public static class IComparableExt
{
    extension<T>(T value) where T : IComparable<T>
    {
        /// <seealso cref="MathHelper.Clamp{T}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Clamp(T min, T max) => MathHelper.Clamp(value, min, max);
    }
}
