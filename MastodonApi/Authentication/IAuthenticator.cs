using System.Threading.Tasks;

namespace MastodonApi.Authentication
{
    public interface IAuthenticator
    {
        Task<string> Connect(string serverName);
        void Disconnect(string serverName);
    }

    internal interface _IAuthenticator : IAuthenticator
    {
    }
}
