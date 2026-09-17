using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HelpDesk.Models
{
    public class Perfil
    {
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string Nome { get; set; } = string.Empty;

        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }

    public class Usuario
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        public byte[] SenhaHash { get; set; } = Array.Empty<byte>();
        public byte[] SenhaSalt { get; set; } = Array.Empty<byte>();

        public int PerfilId { get; set; }
        public Perfil? Perfil { get; set; }

        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public ICollection<Ticket> TicketsAbertos { get; set; } = new List<Ticket>();
        public ICollection<Ticket> TicketsAtendidos { get; set; } = new List<Ticket>();
    }

    public class Categoria
    {
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Descricao { get; set; }

        public bool Ativo { get; set; } = true;
    }

    public class Modulo
    {
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string Nome { get; set; } = string.Empty;
    }

    public class Prioridade
    {
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string Nome { get; set; } = string.Empty;

        /// <summary>Prazo de atendimento (SLA), em horas corridas.</summary>
        public int TempoSlaHoras { get; set; }

        [MaxLength(7)]
        public string? CorHex { get; set; }

        public bool Ativo { get; set; } = true;
    }

    public class StatusTicket
    {
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string Nome { get; set; } = string.Empty;

        public int Ordem { get; set; }

        /// <summary>Indica se este status encerra o ticket (Fechado/Cancelado).</summary>
        public bool Final { get; set; }
    }

    public class Ticket
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string NumeroTicket { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        public string Descricao { get; set; } = string.Empty;

        public int CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }

        public int ModuloId { get; set; }
        public Modulo? Modulo { get; set; }

        public int PrioridadeId { get; set; }
        public Prioridade? Prioridade { get; set; }

        public int StatusId { get; set; }
        public StatusTicket? Status { get; set; }

        public int SolicitanteId { get; set; }
        public Usuario? Solicitante { get; set; }

        public int? AtendenteId { get; set; }
        public Usuario? Atendente { get; set; }

        public DateTime DataAbertura { get; set; } = DateTime.UtcNow;
        public DateTime DataLimiteSla { get; set; }
        public DateTime? DataPrimeiraResposta { get; set; }
        public DateTime? DataResolucao { get; set; }
        public DateTime? DataFechamento { get; set; }

        public string? Solucao { get; set; }
        public bool SlaViolado { get; set; }

        public ICollection<TicketAnexo> Anexos { get; set; } = new List<TicketAnexo>();
        public ICollection<TicketHistorico> Historico { get; set; } = new List<TicketHistorico>();

        [NotMapped]
        public bool SlaEstourado => Status is { Final: false } && DateTime.UtcNow > DataLimiteSla;
    }

    public class TicketAnexo
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        [Required, MaxLength(255)]
        public string NomeArquivoOriginal { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string NomeArquivoArmazenado { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string CaminhoArquivo { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string TipoConteudo { get; set; } = string.Empty;

        public long TamanhoBytes { get; set; }
        public DateTime DataUpload { get; set; } = DateTime.UtcNow;

        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
    }

    public static class TipoEvento
    {
        public const string Criacao = "Criacao";
        public const string Comentario = "Comentario";
        public const string MudancaStatus = "MudancaStatus";
        public const string Atribuicao = "Atribuicao";
        public const string Anexo = "Anexo";
        public const string Fechamento = "Fechamento";
    }

    public class TicketHistorico
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public DateTime DataHora { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(30)]
        public string TipoEvento { get; set; } = string.Empty;

        public int? StatusAnteriorId { get; set; }
        public StatusTicket? StatusAnterior { get; set; }

        public int? StatusNovoId { get; set; }
        public StatusTicket? StatusNovo { get; set; }

        public string? Comentario { get; set; }
    }
}
