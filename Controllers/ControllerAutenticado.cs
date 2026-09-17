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

        /// <summary>Verdadeiro quando o usuário logado é Atendente ou Administrador —
        /// perfis com permissão para gerenciar o atendimento dos chamados.</summary>
        protected bool UsuarioPodeGerenciarAtendimento =>
            UsuarioLogadoPerfil is "Atendente" or "Administrador";

        /// <summary>Id do usuário logado. Só é seguro usar dentro de uma action, pois
        /// OnActionExecuting já garante que existe sessão antes da action rodar.</summary>
        protected int UsuarioLogadoId =>
            UsuarioLogadoIdOuNulo ?? throw new InvalidOperationException("Usuário não autenticado.");

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (UsuarioLogadoIdOuNulo == null)
            {
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = returnUrl.ToString() });
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
