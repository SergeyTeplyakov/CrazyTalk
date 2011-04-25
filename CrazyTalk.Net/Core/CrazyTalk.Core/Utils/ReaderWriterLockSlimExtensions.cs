//-----------------------------------------------------------------------------------------------//
// Static helper class with usefull extensions methods for ReaderWriterLockSlim.
//
// Author:    Sergey Teplyakov
// Date:      April 24, 2011
//-----------------------------------------------------------------------------------------------//


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics.Contracts;

namespace CrazyTalk.Core.Utils
{
    // TODO: Add unit tests for this class

    /// <summary>
    /// Static helper class with usefull extensions methods for ReaderWriterLockSlim.
    /// This class introduces RAII idiom (with using statement) for reading and writing locking
    /// </summary>
    public static class ReaderWriterLockSlimExtensions
    {
        /// <summary>
        /// Helper class that return IDisposable object for reading operations.
        /// Calling Dispose on return value will call ExitReadLock.
        /// </summary>
        public static IDisposable UseReadLock(this ReaderWriterLockSlim readerWriterLock)
        {
            Contract.Requires(readerWriterLock != null);
            Contract.Ensures(Contract.Result<IDisposable>() != null);

            readerWriterLock.EnterReadLock();
            return new DisposeActionWrapper(
                () => readerWriterLock.ExitReadLock());
        }

        /// <summary>
        /// Helper class that return IDisposable object for writing operations.
        /// Calling Dispose on return value will call ExitWriteLock.
        /// </summary>
        public static IDisposable UseWriteLock(this ReaderWriterLockSlim readerWriterLock)
        {
            Contract.Requires(readerWriterLock != null);
            Contract.Ensures(Contract.Result<IDisposable>() != null);
            
            readerWriterLock.EnterWriteLock();
            return new DisposeActionWrapper(
                () => readerWriterLock.ExitWriteLock());
        }

        /// <summary>
        /// Helper class that return IDisposable object for upgradeable operations.
        /// Calling Dispose on return value will call ExitUpgradeableLock.
        /// </summary>
        public static IDisposable UseUpgradeableLock(this ReaderWriterLockSlim readerWriterLock)
        {
            Contract.Requires(readerWriterLock != null);
            Contract.Ensures(Contract.Result<IDisposable>() != null);

            readerWriterLock.EnterUpgradeableReadLock();
            return new DisposeActionWrapper(
                () => readerWriterLock.ExitUpgradeableReadLock());
        }

        /// <summary>
        /// Internal class that helps implement RAII behavior
        /// </summary>
        internal class DisposeActionWrapper : IDisposable
        {
            private readonly Action action;

            public DisposeActionWrapper(Action action)
            {
                Contract.Requires(action != null);

                this.action = action;
            }

            public void Dispose()
            {
                action();
            }

        }
    

        /// <summary>
        /// Internal helper class that simplifies implementation for UseReadLock and other methods.
        /// </summary>
        [Obsolete("Use DisposeActionWrapper instead")]
        internal class ReaderWriteLockSlimDisposability : IDisposable
        {
            public enum LockType { Read, Write, Upgradeable }

            private readonly ReaderWriterLockSlim readerWriterLock;
            private readonly LockType lockType;

            public ReaderWriteLockSlimDisposability(ReaderWriterLockSlim readerWriterLock, LockType lockType)
            {
                switch(lockType)
                {
                    case LockType.Read:
                        readerWriterLock.EnterReadLock();
                        break;
                    case LockType.Write:
                        break;
                    case LockType.Upgradeable:
                        break;
                }
                this.readerWriterLock = readerWriterLock;
                this.lockType = lockType;
            }

            public void Dispose()
            {
                switch (lockType)
                {
                    case LockType.Read:
                        readerWriterLock.ExitReadLock();
                        break;
                    case LockType.Write:
                        readerWriterLock.ExitWriteLock();
                        break;
                    case LockType.Upgradeable:
                        readerWriterLock.ExitUpgradeableReadLock();
                        break;
                }
            }
        }
    }
}
