using System.Runtime.CompilerServices;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Extensions;

public static class ContainerSystemExt
{
    extension(SharedContainerSystem sys)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureContainer<T, TContainer>(Entity<T> entity) where T : Component, IManagedContainer<TContainer>
            where TContainer : BaseContainer =>
            entity.Comp.Container = sys.EnsureContainer<TContainer>(entity, entity.Comp.ContainerId);

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

/// Implementing this interface allows the implementing type to be used with <see cref="ContainerSystemExt.EnsureContainer{T,TContainer}"/>.
// ReSharper disable once TypeParameterCanBeVariant // I don't want variance here.
public interface IManagedContainer<TContainer> where TContainer : BaseContainer
{
    string ContainerId { get; }
    TContainer Container { set; }
}
