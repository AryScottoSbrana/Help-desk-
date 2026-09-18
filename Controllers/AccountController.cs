using System.Security.Cryptography;
using HelpDesk.Data;
using HelpDesk.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Controllers
{
    public class AccountController : Controller
    {
        private readonly HelpDeskContext _context;

        public AccountController(HelpDeskContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        public IActionResult Login(string? returnUrl = null)
        {
            // Já logado? segue direto.
            if (HttpContext.Session.GetInt32("UsuarioId") != null)
                return RedirectToLocal(returnUrl);

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var usuario = await _context.Usuarios
                .Include(u => u.Perfil)
                .Include(u => u.Empresa)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.Trim().ToLower());

            var senhaOk = usuario != null && SenhaConfere(model.Senha, usuario.SenhaHash, usuario.SenhaSalt);

            if (usuario == null || !usuario.Ativo || !senhaOk)
            {
                ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos, ou usuário inativo.");
                return View(model);
            }

            HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
            HttpContext.Session.SetString("UsuarioNome", usuario.Nome);
            HttpContext.Session.SetString("UsuarioPerfil", usuario.Perfil!.Nome);
            HttpContext.Session.SetInt32("EmpresaId", usuario.EmpresaId);
            HttpContext.Session.SetString("EmpresaNome", usuario.Empresa!.Nome);

            return RedirectToLocal(model.ReturnUrl);
        }

        // GET/POST: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Tickets");
        }

        private static bool SenhaConfere(string senhaDigitada, byte[] hashArmazenado, byte[] salt)
        {
            // Mesmos parâmetros usados em UsuariosController ao cadastrar (PBKDF2/SHA256/100k iterações).
            var hashCalculado = Rfc2898DeriveBytes.Pbkdf2(senhaDigitada, salt, 100_000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(hashCalculado, hashArmazenado);
        }
    }
}
