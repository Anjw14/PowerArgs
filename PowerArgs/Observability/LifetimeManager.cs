﻿using System;
using System.Collections.Generic;

namespace PowerArgs
{
    /// <summary>
    /// An interface that defined the contract for associating cleanup
    /// code with a lifetime
    /// </summary>
    public interface ILifetimeManager
    {
        /// <summary>
        /// Registers the given cleanup code to run when the lifetime being
        /// managed by this manager ends
        /// </summary>
        /// <param name="cleanupCode">the code to run</param>
        /// <returns>a promise that resolves after the cleanup code runs</returns>
        Promise OnDisposed(Action cleanupCode);

        /// <summary>
        /// Registers the given disposable to dispose when the lifetime being
        /// managed by this manager ends
        /// </summary>
        /// <param name="obj">the object to dispose</param>
        /// <returns>a promise that resolves after the object is disposed</returns>
        Promise OnDisposed(IDisposable obj);

        /// <summary>
        /// returns true if expired
        /// </summary>
        bool IsExpired { get;  }
    }

    /// <summary>
    /// An implementation of ILifetimeManager
    /// </summary>
    public class LifetimeManager : ILifetimeManager
    {
        private List<IDisposable> _managedItems;

        /// <summary>
        /// returns true if expired
        /// </summary>
        public bool IsExpired { get; private set; }

        internal IReadOnlyCollection<IDisposable> ManagedItems
        {
            get
            {
                return _managedItems.AsReadOnly();
            }
        }

        /// <summary>
        /// Creates the lifetime manager
        /// </summary>
        public LifetimeManager()
        {
            _managedItems = new List<IDisposable>();
        }

        /// <summary>
        /// Registers the given disposable to dispose when the lifetime being
        /// managed by this manager ends
        /// </summary>
        /// <param name="obj">the object to dispose</param>
        /// <returns>a promise that resolves after the object is disposed</returns>
        public Promise OnDisposed(IDisposable obj) => OnDisposed(() => obj.Dispose());

        /// <summary>
        /// Registers the given cleanup code to run when the lifetime being
        /// managed by this manager ends
        /// </summary>
        /// <param name="cleanupCode">the code to run</param>
        /// <returns>a promise that resolves after the cleanup code runs</returns>
        public Promise OnDisposed(Action cleanupCode)
        {
            var d = Deferred.Create();
            _managedItems.Add(new Subscription(()=>
            {
                cleanupCode();
                d.Resolve();
            }));
            IsExpired = true;
            return d.Promise;
        }
    }
}
