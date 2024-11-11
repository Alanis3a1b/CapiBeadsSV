using CapiBeadsSV.Models;

namespace CapiBeadsSV.Serv
{
    public interface IUserService
    {
        Task<usuarios> GetCurrentUserAsync();
    }
}