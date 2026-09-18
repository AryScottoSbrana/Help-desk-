using HelpDesk.Data;
using HelpDesk.Models;
using HelpDesk.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Controllers
{
    /// <summary>
    /// Cadastro de empresas (clientes do Help Desk). Fica aberto, sem exigir login,
    /// porque é usado no fluxo de auto-cadastro: uma empresa nova precisa existir
    /// antes que o primeiro usuário dela possa se cadastrar.
    ///
    /// ATENÇÃO: numa implantação real, considere restringir Index/Create a um perfil
    /// de "super administrador" da plataforma, para que clientes não vejam uns aos
    /// outros nem consigam recadastrar a mesma empresa por engano.
    /// </summary>
    public class EmpresasController : Controller
    {
        private readonly HelpDeskContext _context;

        public EmpresasController(HelpDeskContext context)
        {
            _context = context;
        }

        // GET: /Empresas
        public async Task<IActionResult> Index()
        {
            var empresas = await _context.Empresas
                .OrderBy(e => e.Nome)
                .Select(e => new EmpresaListItemViewModel
                {
                    Id = e.Id,
                    Nome = e.Nome,
                    Documento = e.Documento,
                    Ativo = e.Ativo,
                    QuantidadeUsuarios = e.Usuarios.Count,
                    DataCriacao = e.DataCriacao
                })
                .ToListAsync();

            return View(new EmpresaIndexViewModel { Empresas = empresas });
        }

        // GET: /Empresas/Create
        public IActionResult Create() => View(new EmpresaCreateViewModel());

        // POST: /Empresas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmpresaCreateViewModel model)
        {
            var nomeJaExiste = await _context.Empresas
                .AnyAsync(e => e.Nome.ToLower() == model.Nome.Trim().ToLower());

            if (nomeJaExiste)
                ModelState.AddModelError(nameof(model.Nome), "Já existe uma empresa cadastrada com esse nome.");

            if (!ModelState.IsValid) return View(model);

            var empresa = new Empresa
            {
                Nome = model.Nome.Trim(),
                Documento = string.IsNullOrWhiteSpace(model.Documento) ? null : model.Documento.Trim(),
                Ativo = true,
                DataCriacao = DateTime.UtcNow
            };

            _context.Empresas.Add(empresa);
            await _context.SaveChangesAsync();

            TempData["Mensagem"] = $"Empresa \"{empresa.Nome}\" cadastrada. Agora é só cadastrar os usuários dela.";
            return RedirectToAction("Create", "Usuarios", new { empresaId = empresa.Id });
        }
    }
}
