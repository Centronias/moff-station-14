using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Content.Shared._Moffstation.Extensions;

public static class EnumExt
{
    extension<T>(T variant) where T : Enum
    {
        /// <see cref="MoffUtils.Unreachable{T}"/>, specialized for throwing on unknown enum variants.
        [MethodImpl(MethodImplOptions.AggressiveInlining), DoesNotReturn]
        public TRet UnknownEnumVariant<TRet>() =>
            MoffUtils.Unreachable<TRet>($"Unknown variant of enum {nameof(T)}: {variant}");
    }
}
