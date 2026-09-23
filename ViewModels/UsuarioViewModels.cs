using System.ComponentModel.DataAnnotations;

namespace HelpDesk.ViewModels
{
    public class PerfilViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    public class UsuarioListItemViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NomeEmpresa { get; set; } = string.Empty;
        public string Perfil { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public DateTime DataCriacao { get; set; }
    }

    public class UsuarioIndexViewModel
    {
        public List<UsuarioListItemViewModel> Usuarios { get; set; } = new();
    }

    public class UsuarioCreateViewModel
    {
        [Required(ErrorMessage = "Informe o nome completo.")]
        [StringLength(150, ErrorMessage = "O nome deve ter até {1} caracteres.")]
        [Display(Name = "Nome completo")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(150)]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe uma senha.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter ao menos {2} caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a senha.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar senha")]
        [Compare(nameof(Senha), ErrorMessage = "As senhas não coincidem.")]
        public string ConfirmarSenha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione o perfil de acesso.")]
        [Display(Name = "Perfil de acesso")]
        public int PerfilId { get; set; }

        [Display(Name = "Usuário ativo")]
        public bool Ativo { get; set; } = true;

        public List<PerfilViewModel> Perfis { get; set; } = new();
    }
}
