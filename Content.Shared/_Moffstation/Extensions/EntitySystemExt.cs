using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

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
        [Obsolete("Migrate usages to MoffUtils. Just inline this.")]
        public T Unreachable<T>(string msg) => MoffUtils.Unreachable<T>(msg);

        /// Throws an exception.
        [MethodImpl(MethodImplOptions.AggressiveInlining), DoesNotReturn]
        [Obsolete("Migrate usages to MoffUtils. Just inline this.")]
        public void Unreachable(string msg) => MoffUtils.Unreachable(msg);

        /// <see cref="Unreachable{T}"/>, specialized for throwing on unknown enum variants.
        [MethodImpl(MethodImplOptions.AggressiveInlining), DoesNotReturn]
        [Obsolete("Migrate usages to EnumExt. Just inline this.")]
        public TRet UnknownEnumVariant<T, TRet>(T variant) where T : Enum => variant.UnknownEnumVariant<T, TRet>();
    }
}

public static class ContainerSystemExt
{
    extension(SharedContainerSystem sys)
    {
        /// Tries to get the entity which immediately contains <paramref name="contained"/>.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityUid? GetContainingEntity(Entity<TransformComponent?, MetaDataComponent?> contained) =>
            sys.TryGetContainingContainer(contained, out var container) ? container.Owner : null;

        /// <summary>
        /// <see cref="GetContainingEntity"/>, but also resolves a component on the container.
        /// </summary>
        /// <param name="contained">The entity whose container will be found</param>
        /// <param name="query">The query used to resolve a <typeparamref name="T"/> on the container</param>
        /// <param name="logIfExistsButMissing">
        /// When true and the container exists, this method will log if <typeparamref name="T"/> does NOT exist on the
        /// container. In all other circumstances, this method will not log.
        /// </param>
        /// <typeparam name="T">The type of component to resolve on the container</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity<T>? GetContainingEntity<T>(
            Entity<TransformComponent?, MetaDataComponent?> contained,
            EntityQuery<T> query,
            bool logIfExistsButMissing = true
        ) where T : Component => sys.TryGetContainingContainer(contained, out var container)
            ? query.ResolveOrNull(container.Owner, logIfExistsButMissing)
            : null;
    }
}
