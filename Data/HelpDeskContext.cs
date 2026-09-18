using HelpDesk.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Data
{
    public class HelpDeskContext : DbContext
    {
        public HelpDeskContext(DbContextOptions<HelpDeskContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Perfil> Perfis => Set<Perfil>();
        public DbSet<Empresa> Empresas => Set<Empresa>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Modulo> Modulos => Set<Modulo>();
        public DbSet<Prioridade> Prioridades => Set<Prioridade>();
        public DbSet<StatusTicket> StatusTickets => Set<StatusTicket>();
        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<TicketAnexo> TicketAnexos => Set<TicketAnexo>();
        public DbSet<TicketHistorico> TicketHistoricos => Set<TicketHistorico>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // O script SQL criou esta tabela no singular (TicketHistorico), mas a
            // convenção do EF Core mapearia para o nome do DbSet (plural,
            // TicketHistoricos). Mapeamento explícito evita o erro "invalid object
            // name" sem precisar renomear a tabela já existente no banco.
            modelBuilder.Entity<TicketHistorico>().ToTable("TicketHistorico");

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Ticket>()
                .HasIndex(t => t.NumeroTicket)
                .IsUnique();

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Solicitante)
                .WithMany(u => u.TicketsAbertos)
                .HasForeignKey(t => t.SolicitanteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Atendente)
                .WithMany(u => u.TicketsAtendidos)
                .HasForeignKey(t => t.AtendenteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistorico>()
                .HasOne(h => h.StatusAnterior)
                .WithMany()
                .HasForeignKey(h => h.StatusAnteriorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistorico>()
                .HasOne(h => h.StatusNovo)
                .WithMany()
                .HasForeignKey(h => h.StatusNovoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasMany(t => t.Anexos)
                .WithOne(a => a.Ticket)
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Ticket>()
                .HasMany(t => t.Historico)
                .WithOne(h => h.Ticket)
                .HasForeignKey(h => h.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}