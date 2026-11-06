using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Content.Shared._Moffstation.Extensions;

public static class MoffUtils
{
    /// Throws an exception. For typechecking, returns <typeparamref name="T"/> for use in expressions, though it definitely never returns.
    [MethodImpl(MethodImplOptions.AggressiveInlining), DoesNotReturn]
    public static T Unreachable<T>(string msg) => throw new Exception(msg);

    /// Throws an exception.
    [MethodImpl(MethodImplOptions.AggressiveInlining), DoesNotReturn]
    public static void Unreachable(string msg) => throw new Exception(msg);
}
