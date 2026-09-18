using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HelpDesk.Controllers
{
    /// <summary>
    /// Controllers que herdam desta classe exigem um usuário logado (sessão).
    /// Caso não haja sessão ativa, o pedido é redirecionado para /Account/Login.
    /// </summary>
    public abstract class ControllerAutenticado : Controller
    {
        protected int? UsuarioLogadoIdOuNulo => HttpContext.Session.GetInt32("UsuarioId");
        protected string UsuarioLogadoNome => HttpContext.Session.GetString("UsuarioNome") ?? string.Empty;
        protected string UsuarioLogadoPerfil => HttpContext.Session.GetString("UsuarioPerfil") ?? string.Empty;
        protected string UsuarioLogadoEmpresaNome => HttpContext.Session.GetString("EmpresaNome") ?? string.Empty;

        /// <summary>Id da empresa do usuário logado. Usado para restringir o Solicitante
        /// à própria empresa; Atendente/Administrador não ficam limitados por ela
        /// (ver <see cref="UsuarioPodeGerenciarAtendimento"/>).</summary>
        protected int UsuarioLogadoEmpresaId =>
            HttpContext.Session.GetInt32("EmpresaId")
                ?? throw new InvalidOperationException("Usuário não autenticado.");

        /// <summary>Verdadeiro quando o usuário logado é Atendente ou Administrador —
        /// perfis com permissão para gerenciar o atendimento dos chamados e que,
        /// por isso, enxergam e atuam em chamados de TODAS as empresas. Um
        /// Solicitante só vê e atua nos chamados da própria empresa.</summary>
        protected bool UsuarioPodeGerenciarAtendimento =>
            UsuarioLogadoPerfil is "Atendente" or "Administrador";

        /// <summary>Id do usuário logado. Só é seguro usar dentro de uma action, pois
        /// OnActionExecuting já garante que existe sessão antes da action rodar.</summary>
        protected int UsuarioLogadoId =>
            UsuarioLogadoIdOuNulo ?? throw new InvalidOperationException("Usuário não autenticado.");

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Exige UsuarioId E EmpresaId na sessão. Isso também protege contra sessões
            // "velhas" (de antes da funcionalidade de Empresa existir) que tenham UsuarioId
            // mas não EmpresaId — nesse caso a sessão é limpa e a pessoa refaz o login.
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            var empresaId = HttpContext.Session.GetInt32("EmpresaId");

            if (usuarioId == null || empresaId == null)
            {
                HttpContext.Session.Clear();

                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = returnUrl.ToString() });
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}