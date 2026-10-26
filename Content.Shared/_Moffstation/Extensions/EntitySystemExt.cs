using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Content.Shared._Moffstation.Extensions;

public static class EntitySystemExt
{
    extension(EntitySystem entSys)
    {
        /// Throws a debug assert and logs the given <paramref name="message"/> to the system's error logger.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Migrate usages to ISawmill. Just inline this.")]
        public void AssertOrLogError(string message) => entSys.Log.AssertOrLogError(message);

        /// <see cref="AssertOrLogError"/>, but returns <paramref name="ret"/> instead of void.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Migrate usages to ISawmill. Just inline this.")]
        public T AssertOrLogError<T>(string message, T ret) => entSys.Log.AssertOrLogError(message, ret);

        /// Throws an exception. For typechecking, returns <typeparamref name="T"/> for use in expressions, though it definitely never returns.
        [MethodImpl(MethodImplOptions.AggressiveInlining), DoesNotReturn]
        public T Unreachable<T>(string msg) => throw new Exception(msg);

        /// Throws an exception.
        [MethodImpl(MethodImplOptions.AggressiveInlining), DoesNotReturn]
        public void Unreachable(string msg) => throw new Exception(msg);

        /// <see cref="Unreachable{T}"/>, specialized for throwing on unknown enum variants.
        public TRet UnknownEnumVariant<T, TRet>(T variant) where T : Enum =>
            entSys.Unreachable<TRet>($"Unknown variant of enum {nameof(T)}: {variant}");
    }
}
