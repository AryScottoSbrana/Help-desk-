using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HelpDesk.ViewModels
{
    /// <summary>Situação de SLA usada apenas para exibição (cores/badges).</summary>
    public enum SlaSituacao
    {
        DentroDoPrazo,
        ProximoDoLimite,
        Estourado,
        Encerrado
    }

    /// <summary>Linha da listagem de tickets (Index).</summary>
    public class TicketListItemViewModel
    {
        public int Id { get; set; }
        public string NumeroTicket { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Prioridade { get; set; } = string.Empty;
        public string? PrioridadeCorHex { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool StatusFinal { get; set; }
        public string Solicitante { get; set; } = string.Empty;
        public string? Atendente { get; set; }
        public DateTime DataAbertura { get; set; }
        public DateTime DataLimiteSla { get; set; }
        public SlaSituacao Sla { get; set; }
        public int QuantidadeAnexos { get; set; }
    }

    /// <summary>Filtros da tela de listagem.</summary>
    public class TicketFiltroViewModel
    {
        public string? Busca { get; set; }
        public int? EmpresaId { get; set; }
        public int? ModuloId { get; set; }
        public int? CategoriaId { get; set; }
        public int? PrioridadeId { get; set; }
        public int? StatusId { get; set; }
        public bool ApenasComSlaEstourado { get; set; }
        public bool ApenasMeusTickets { get; set; }

        public int PaginaAtual { get; set; } = 1;
        public int TamanhoPagina { get; set; } = 20;
    }

    /// <summary>Modelo completo exibido na tela Index (filtros + resultado + combos).</summary>
    public class TicketIndexViewModel
    {
        public TicketFiltroViewModel Filtro { get; set; } = new();
        public List<TicketListItemViewModel> Tickets { get; set; } = new();

        public int TotalRegistros { get; set; }
        public int TotalPaginas { get; set; }

        public List<Empresa2ViewModel> Empresas { get; set; } = new();
        public bool MostrarFiltroEmpresa { get; set; }
        public List<Modulo2ViewModel> Modulos { get; set; } = new();
        public List<Categoria2ViewModel> Categorias { get; set; } = new();
        public List<Prioridade2ViewModel> Prioridades { get; set; } = new();
        public List<Status2ViewModel> StatusList { get; set; } = new();

        // Indicadores resumidos para o topo da página
        public int TotalAbertos { get; set; }
        public int TotalEmAtendimento { get; set; }
        public int TotalSlaEstourado { get; set; }
        public int TotalResolvidosNoMes { get; set; }
    }

    public class Modulo2ViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    public class Categoria2ViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    public class Prioridade2ViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int TempoSlaHoras { get; set; }
    }

    public class Status2ViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    /// <summary>Usado para popular o combo de atendentes disponíveis para atribuição.</summary>
    public class UsuarioOpcaoViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Empresa { get; set; }
    }

    /// <summary>Formulário de abertura de um novo ticket.</summary>
    public class TicketCreateViewModel
    {
        [Required(ErrorMessage = "Informe um título para o chamado.")]
        [StringLength(200, ErrorMessage = "O título deve ter até {1} caracteres.")]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Descreva a sua necessidade.")]
        [StringLength(4000, ErrorMessage = "A descrição deve ter até {1} caracteres.")]
        [Display(Name = "Descrição da necessidade")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione o módulo do sistema.")]
        [Display(Name = "Módulo")]
        public int ModuloId { get; set; }

        [Required(ErrorMessage = "Selecione uma categoria.")]
        [Display(Name = "Categoria")]
        public int CategoriaId { get; set; }

        [Required(ErrorMessage = "Selecione a prioridade.")]
        [Display(Name = "Prioridade")]
        public int PrioridadeId { get; set; }

        [Display(Name = "Anexos (imagens de comprovação)")]
        public List<IFormFile>? Anexos { get; set; }

        public List<Modulo2ViewModel> Modulos { get; set; } = new();
        public List<Categoria2ViewModel> Categorias { get; set; } = new();
        public List<Prioridade2ViewModel> Prioridades { get; set; } = new();
    }

    /// <summary>Item exibido na timeline/histórico do ticket.</summary>
    public class TicketHistoricoItemViewModel
    {
        public DateTime DataHora { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string TipoEvento { get; set; } = string.Empty;
        public string? StatusAnterior { get; set; }
        public string? StatusNovo { get; set; }
        public string? Comentario { get; set; }
    }

    public class TicketAnexoViewModel
    {
        public int Id { get; set; }
        public string NomeArquivoOriginal { get; set; } = string.Empty;
        public string CaminhoArquivo { get; set; } = string.Empty;
        public string TipoConteudo { get; set; } = string.Empty;
        public long TamanhoBytes { get; set; }
        public DateTime DataUpload { get; set; }
    }
}