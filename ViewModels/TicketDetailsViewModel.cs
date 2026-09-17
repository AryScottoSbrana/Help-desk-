using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HelpDesk.ViewModels
{
    /// <summary>Tela de detalhes/atendimento de um ticket.</summary>
    public class TicketDetailsViewModel
    {
        public int Id { get; set; }
        public string NumeroTicket { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;

        public string Categoria { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public string Prioridade { get; set; } = string.Empty;
        public string? PrioridadeCorHex { get; set; }

        public int StatusId { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool StatusFinal { get; set; }

        public string Solicitante { get; set; } = string.Empty;
        public int? AtendenteId { get; set; }
        public string? Atendente { get; set; }

        public DateTime DataAbertura { get; set; }
        public DateTime DataLimiteSla { get; set; }
        public DateTime? DataResolucao { get; set; }
        public DateTime? DataFechamento { get; set; }
        public SlaSituacao Sla { get; set; }

        public string? Solucao { get; set; }

        public List<TicketAnexoViewModel> Anexos { get; set; } = new();
        public List<TicketHistoricoItemViewModel> Historico { get; set; } = new();
        public List<Status2ViewModel> StatusDisponiveis { get; set; } = new();
        public List<UsuarioOpcaoViewModel> AtendentesDisponiveis { get; set; } = new();

        /// <summary>Controla, na view, a exibição das ações de atendimento
        /// (trocar atendente, alterar status, fechar) — só para Atendente/Administrador.</summary>
        public bool PodeGerenciarAtendimento { get; set; }

        // Campos de entrada para as ações da tela
        [Display(Name = "Comentário")]
        public string? NovoComentario { get; set; }

        [Display(Name = "Novo anexo")]
        public List<IFormFile>? NovosAnexos { get; set; }

        [Display(Name = "Descrição da solução")]
        public string? SolucaoInput { get; set; }
    }
}
