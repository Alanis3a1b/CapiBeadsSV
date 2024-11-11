using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using CapiBeadsSV.Models;
using System.Text.Json;

namespace CapiBeadsSV.Serv
{
    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly capibeadsBDContext _context;

        public UserService(IHttpContextAccessor httpContextAccessor, capibeadsBDContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public async Task<usuarios> GetCurrentUserAsync()
        {
            var userJson = _httpContextAccessor.HttpContext.Session.GetString("user");
            if (string.IsNullOrEmpty(userJson))
            {
                return null;
            }

            var usuarioSesion = JsonSerializer.Deserialize<usuarios>(userJson);
            return await _context.usuarios.FindAsync(usuarioSesion.id_usuario);
        }
    }
}