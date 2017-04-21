using System;
using System.Threading;

using MastodonApi.Authentication;

namespace MastodonApi
{
    public class MastodonSession : IDisposable
    {
        public MastodonSession(string serverName)
        {
            if (string.IsNullOrEmpty(serverName)) { throw new ArgumentNullException("serverName"); }
            ServerName = serverName;
        }

        ~MastodonSession()
        {
            Dispose(false);
        }
        
        /// <summary>True if the session is authenticated.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Get the name of the server this session is initiated from.</summary>
        public string ServerName { get; private set; }

        private void AssertNotDisposed()
        {
            lock (this) { AssertNotDisposedLocked(); }
        }

        /// <remarks>This method is intended to be invoked while holding a lock on the object.</remarks>
        private void AssertNotDisposedLocked()
        {
            if (!_disposed) { return; }
            throw new ObjectDisposedException("MastodonSession");
        }

        public void Authenticate(IAuthenticator authenticator)
        {
            if (null == authenticator) { throw new ArgumentNullException("authenticator"); }
            lock (this) {
                AssertNotDisposedLocked();
                if (IsActive) { throw new InvalidOperationException("Already authenticated."); }
                authenticator.Connect(ServerName);
                ConnectionCompletedEvent.WaitOne();
                // On success, preserve a reference to the authenticator and the server name for
                // later disconnection.
                _authenticator = authenticator;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            _disposed = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) { GC.SuppressFinalize(this); }
            if (IsActive) {
                _authenticator.Disconnect(ServerName);
            }
        }

        private IAuthenticator _authenticator;
        private bool _disposed;
        /// <summary>For test purpose only.</summary>
        internal static AutoResetEvent ConnectionCompletedEvent = new AutoResetEvent(false);
    }
}
