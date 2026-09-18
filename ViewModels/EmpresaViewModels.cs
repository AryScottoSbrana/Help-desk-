using System.ComponentModel.DataAnnotations;

namespace HelpDesk.ViewModels
{
    /// <summary>Usado para popular combos de empresa (ex.: cadastro de usuário).</summary>
    public class Empresa2ViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    public class EmpresaListItemViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Documento { get; set; }
        public bool Ativo { get; set; }
        public int QuantidadeUsuarios { get; set; }
        public DateTime DataCriacao { get; set; }
    }

    public class EmpresaIndexViewModel
    {
        public List<EmpresaListItemViewModel> Empresas { get; set; } = new();
    }

    public class EmpresaCreateViewModel
    {
        [Required(ErrorMessage = "Informe o nome da empresa.")]
        [StringLength(150, ErrorMessage = "O nome deve ter até {1} caracteres.")]
        [Display(Name = "Nome da empresa")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "O documento deve ter até {1} caracteres.")]
        [Display(Name = "CNPJ (opcional)")]
        public string? Documento { get; set; }
    }
}
