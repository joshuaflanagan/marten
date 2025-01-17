

using System;
using Baseline;
using ImTools;
using Marten.Events;
using Marten.Exceptions;
using Marten.Internal.CodeGeneration;
using Marten.Internal.Storage;

#nullable enable
namespace Marten.Internal.Sessions
{

    public partial class QuerySession
    {
        private readonly IProviderGraph _providers;

                protected internal virtual IDocumentStorage<T> selectStorage<T>(DocumentProvider<T> provider) where T : notnull
        {
            return provider.QueryOnly;
        }

        public IDocumentStorage StorageFor(Type documentType)
        {
            if (_byType.TryFind(documentType, out var storage))
            {
                return storage;
            }

            storage = typeof(StorageFinder<>).CloseAndBuildAs<IStorageFinder>(documentType).Find(this);
            _byType = _byType.AddOrUpdate(documentType, storage);

            return storage;
        }

        private ImHashMap<Type, IDocumentStorage> _byType = ImHashMap<Type, IDocumentStorage>.Empty;

        private interface IStorageFinder
        {
            IDocumentStorage Find(QuerySession session);
        }

        private class StorageFinder<T>: IStorageFinder where T : notnull
        {
            public IDocumentStorage Find(QuerySession session)
            {
                return session.StorageFor<T>();
            }
        }

        internal IDocumentStorage<T, TId> StorageFor<T, TId>() where T : notnull where TId : notnull
        {
            var storage = StorageFor<T>();
            if (storage is IDocumentStorage<T, TId> s) return s;


            throw new DocumentIdTypeMismatchException(storage, typeof(TId));
        }

        /// <summary>
        /// This returns the query-only version of the document storage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <returns></returns>
        /// <exception cref="DocumentIdTypeMismatchException"></exception>
        internal IDocumentStorage<T, TId> QueryStorageFor<T, TId>() where T : notnull where TId : notnull
        {
            var storage = _providers.StorageFor<T>().QueryOnly;
            if (storage is IDocumentStorage<T, TId> s) return s;


            throw new DocumentIdTypeMismatchException(storage, typeof(TId));
        }

        public IDocumentStorage<T> StorageFor<T>() where T : notnull
        {
            return selectStorage(_providers.StorageFor<T>());
        }

        public IEventStorage EventStorage()
        {
            return (IEventStorage) selectStorage(_providers.StorageFor<IEvent>());
        }

    }
}
