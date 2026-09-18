using HelpDesk.Data;
using HelpDesk.Models;
using HelpDesk.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace HelpDesk.Controllers;

public class HomeController : ControllerAutenticado
{
    private readonly HelpDeskContext _context;
    private readonly IWebHostEnvironment _env;

    // Regras de upload de anexos (imagens de comprovação)
    private static readonly string[] ExtensoesPermitidas = { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
    private const long TamanhoMaximoBytes = 5 * 1024 * 1024; // 5 MB por arquivo

    public HomeController(HelpDeskContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<IActionResult> Index(TicketFiltroViewModel filtro)
    {
        var query = _context.Tickets
                        .Include(t => t.Categoria)
                        .Include(t => t.Modulo)
                        .Include(t => t.Prioridade)
                        .Include(t => t.Status)
                        .Include(t => t.Solicitante)
                        .Include(t => t.Atendente)
                        .Include(t => t.Anexos)
                        .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var termo = filtro.Busca.Trim();
            query = query.Where(t => t.NumeroTicket.Contains(termo) || t.Titulo.Contains(termo));
        }

        if (filtro.CategoriaId.HasValue)
            query = query.Where(t => t.CategoriaId == filtro.CategoriaId);

        if (filtro.ModuloId.HasValue)
            query = query.Where(t => t.ModuloId == filtro.ModuloId);

        if (filtro.PrioridadeId.HasValue)
            query = query.Where(t => t.PrioridadeId == filtro.PrioridadeId);

        if (filtro.StatusId.HasValue)
            query = query.Where(t => t.StatusId == filtro.StatusId);

        if (filtro.ApenasComSlaEstourado)
            query = query.Where(t => !t.Status!.Final && DateTime.UtcNow > t.DataLimiteSla);

        if (filtro.ApenasMeusTickets)
            query = query.Where(t => t.SolicitanteId == UsuarioLogadoId || t.AtendenteId == UsuarioLogadoId);

        var totalRegistros = await query.CountAsync();

        var tickets = await query
            .OrderByDescending(t => t.DataAbertura)
            .Skip((filtro.PaginaAtual - 1) * filtro.TamanhoPagina)
            .Take(filtro.TamanhoPagina)
            .Select(t => new TicketListItemViewModel
            {
                Id = t.Id,
                NumeroTicket = t.NumeroTicket,
                Titulo = t.Titulo,
                Categoria = t.Categoria!.Nome,
                Modulo = t.Modulo != null ? t.Modulo.Nome : null,
                Prioridade = t.Prioridade!.Nome,
                PrioridadeCorHex = t.Prioridade.CorHex,
                Status = t.Status!.Nome,
                StatusFinal = t.Status.Final,
                Solicitante = t.Solicitante!.Nome,
                Atendente = t.Atendente != null ? t.Atendente.Nome : null,
                DataAbertura = t.DataAbertura,
                DataLimiteSla = t.DataLimiteSla,
                QuantidadeAnexos = t.Anexos.Count
            })
            .ToListAsync();

        foreach (var t in tickets)
            t.Sla = CalcularSituacaoSla(t.DataLimiteSla, t.StatusFinal);

        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var viewModel = new TicketIndexViewModel
        {
            Filtro = filtro,
            Tickets = tickets,
            TotalRegistros = totalRegistros,
            TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)filtro.TamanhoPagina),

            Categorias = await _context.Categorias.Where(c => c.Ativo)
                .Select(c => new Categoria2ViewModel { Id = c.Id, Nome = c.Nome }).ToListAsync(),

            Modulos = await _context.Modulos.OrderBy(m => m.Sequencia)
                .Select(m => new Modulo2ViewModel { Id = m.Id, Nome = m.Nome }).ToListAsync(),

            Prioridades = await _context.Prioridades.Where(p => p.Ativo)
                .Select(p => new Prioridade2ViewModel { Id = p.Id, Nome = p.Nome, TempoSlaHoras = p.TempoSlaHoras }).ToListAsync(),

            StatusList = await _context.StatusTickets.OrderBy(s => s.Ordem)
                .Select(s => new Status2ViewModel { Id = s.Id, Nome = s.Nome }).ToListAsync(),

            TotalAbertos = await _context.Tickets.CountAsync(t => t.Status!.Nome == "Aberto"),
            TotalEmAtendimento = await _context.Tickets.CountAsync(t => t.Status!.Nome == "Em Atendimento"),
            TotalSlaEstourado = await _context.Tickets.CountAsync(t => !t.Status!.Final && DateTime.UtcNow > t.DataLimiteSla),
            TotalResolvidosNoMes = await _context.Tickets.CountAsync(t => t.DataFechamento != null && t.DataFechamento >= inicioMes)
        };

        return View(viewModel);
    }

    private static SlaSituacao CalcularSituacaoSla(DateTime dataLimiteSla, bool statusFinal)
    {
        if (statusFinal) return SlaSituacao.Encerrado;

        var restante = dataLimiteSla - DateTime.UtcNow;

        if (restante.TotalMinutes < 0) return SlaSituacao.Estourado;
        if (restante.TotalHours <= 2) return SlaSituacao.ProximoDoLimite;
        return SlaSituacao.DentroDoPrazo;
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
