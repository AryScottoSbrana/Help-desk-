using System.Security.Cryptography;
using HelpDesk.Data;
using HelpDesk.Models;
using HelpDesk.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly HelpDeskContext _context;

        // Parâmetros do PBKDF2 usado para armazenar a senha com segurança
        private const int TamanhoSaltBytes = 16;
        private const int TamanhoHashBytes = 32;
        private const int Iteracoes = 100_000;

        public UsuariosController(HelpDeskContext context)
        {
            _context = context;
        }

        // GET: /Usuarios
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.Perfil)
                .OrderBy(u => u.Nome)
                .Select(u => new UsuarioListItemViewModel
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Email = u.Email,
                    Perfil = u.Perfil!.Nome,
                    Ativo = u.Ativo,
                    DataCriacao = u.DataCriacao
                })
                .ToListAsync();

            return View(new UsuarioIndexViewModel { Usuarios = usuarios });
        }

        // GET: /Usuarios/Create
        public async Task<IActionResult> Create()
        {
            var model = new UsuarioCreateViewModel
            {
                Perfis = await CarregarPerfisAsync()
            };

            return View(model);
        }

        // POST: /Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioCreateViewModel model)
        {
            var emailJaExiste = await _context.Usuarios
                .AnyAsync(u => u.Email.ToLower() == model.Email.Trim().ToLower());

            if (emailJaExiste)
                ModelState.AddModelError(nameof(model.Email), "Já existe um usuário cadastrado com este e-mail.");

            if (!ModelState.IsValid)
            {
                model.Perfis = await CarregarPerfisAsync();
                return View(model);
            }

            var salt = RandomNumberGenerator.GetBytes(TamanhoSaltBytes);
            var hash = Rfc2898DeriveBytes.Pbkdf2(model.Senha, salt, Iteracoes, HashAlgorithmName.SHA256, TamanhoHashBytes);

            var usuario = new Usuario
            {
                Nome = model.Nome.Trim(),
                Email = model.Email.Trim().ToLowerInvariant(),
                SenhaHash = hash,
                SenhaSalt = salt,
                PerfilId = model.PerfilId,
                Ativo = model.Ativo,
                DataCriacao = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            TempData["Mensagem"] = $"Usuário \"{usuario.Nome}\" cadastrado com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<PerfilViewModel>> CarregarPerfisAsync()
        {
            return await _context.Perfis
                .OrderBy(p => p.Id)
                .Select(p => new PerfilViewModel { Id = p.Id, Nome = p.Nome })
                .ToListAsync();
        }
    }
}
