using HelpDesk.Data;
using HelpDesk.Models;
using HelpDesk.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Controllers
{
    public class TicketsController : ControllerAutenticado
    {
        private readonly HelpDeskContext _context;
        private readonly IWebHostEnvironment _env;

        // Regras de upload de anexos (imagens de comprovação)
        private static readonly string[] ExtensoesPermitidas = { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
        private const long TamanhoMaximoBytes = 5 * 1024 * 1024; // 5 MB por arquivo

        public TicketsController(HelpDeskContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Tickets
        public async Task<IActionResult> Index(TicketFiltroViewModel filtro)
        {
            // Solicitante só vê chamados da própria empresa; Atendente/Administrador
            // veem chamados de todas as empresas (e podem filtrar por uma específica).
            var query = _context.Tickets.AsQueryable();

            if (!UsuarioPodeGerenciarAtendimento)
                query = query.Where(t => t.EmpresaId == UsuarioLogadoEmpresaId);
            else if (filtro.EmpresaId.HasValue)
                query = query.Where(t => t.EmpresaId == filtro.EmpresaId);

            query = query
                .Include(t => t.Empresa)
                .Include(t => t.Modulo)
                .Include(t => t.Categoria)
                .Include(t => t.Prioridade)
                .Include(t => t.Status)
                .Include(t => t.Solicitante)
                .Include(t => t.Atendente)
                .Include(t => t.Anexos);

            if (!string.IsNullOrWhiteSpace(filtro.Busca))
            {
                var termo = filtro.Busca.Trim();
                query = query.Where(t => t.NumeroTicket.Contains(termo) || t.Titulo.Contains(termo));
            }

            if (filtro.ModuloId.HasValue)
                query = query.Where(t => t.ModuloId == filtro.ModuloId);

            if (filtro.CategoriaId.HasValue)
                query = query.Where(t => t.CategoriaId == filtro.CategoriaId);

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
                    Empresa = t.Empresa!.Nome,
                    Modulo = t.Modulo!.Nome,
                    Categoria = t.Categoria!.Nome,
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

            // Mesma regra de visibilidade aplicada aos indicadores (KPIs) do topo.
            var kpiQuery = _context.Tickets.AsQueryable();
            if (!UsuarioPodeGerenciarAtendimento)
                kpiQuery = kpiQuery.Where(t => t.EmpresaId == UsuarioLogadoEmpresaId);
            else if (filtro.EmpresaId.HasValue)
                kpiQuery = kpiQuery.Where(t => t.EmpresaId == filtro.EmpresaId);

            var viewModel = new TicketIndexViewModel
            {
                Filtro = filtro,
                Tickets = tickets,
                TotalRegistros = totalRegistros,
                TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)filtro.TamanhoPagina),

                MostrarFiltroEmpresa = UsuarioPodeGerenciarAtendimento,
                Empresas = UsuarioPodeGerenciarAtendimento
                    ? await _context.Empresas.Where(e => e.Ativo).OrderBy(e => e.Nome)
                        .Select(e => new Empresa2ViewModel { Id = e.Id, Nome = e.Nome }).ToListAsync()
                    : new List<Empresa2ViewModel>(),

                Modulos = await _context.Modulos.Where(m => m.Ativo)
                    .Select(m => new Modulo2ViewModel { Id = m.Id, Nome = m.Nome }).ToListAsync(),

                Categorias = await _context.Categorias.Where(c => c.Ativo)
                    .Select(c => new Categoria2ViewModel { Id = c.Id, Nome = c.Nome }).ToListAsync(),

                Prioridades = await _context.Prioridades.Where(p => p.Ativo)
                    .Select(p => new Prioridade2ViewModel { Id = p.Id, Nome = p.Nome, TempoSlaHoras = p.TempoSlaHoras }).ToListAsync(),

                StatusList = await _context.StatusTickets.OrderBy(s => s.Ordem)
                    .Select(s => new Status2ViewModel { Id = s.Id, Nome = s.Nome }).ToListAsync(),

                TotalAbertos = await kpiQuery.CountAsync(t => t.Status!.Nome == "Aberto"),
                TotalEmAtendimento = await kpiQuery.CountAsync(t => t.Status!.Nome == "Em Atendimento"),
                TotalSlaEstourado = await kpiQuery.CountAsync(t => !t.Status!.Final && DateTime.UtcNow > t.DataLimiteSla),
                TotalResolvidosNoMes = await kpiQuery.CountAsync(t => t.DataFechamento != null && t.DataFechamento >= inicioMes)
            };

            return View(viewModel);
        }

        // GET: /Tickets/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new TicketCreateViewModel
            {
                Modulos = await _context.Modulos.Where(m => m.Ativo)
                    .Select(m => new Modulo2ViewModel { Id = m.Id, Nome = m.Nome }).ToListAsync(),
                Categorias = await _context.Categorias.Where(c => c.Ativo)
                    .Select(c => new Categoria2ViewModel { Id = c.Id, Nome = c.Nome }).ToListAsync(),
                Prioridades = await _context.Prioridades.Where(p => p.Ativo)
                    .Select(p => new Prioridade2ViewModel { Id = p.Id, Nome = p.Nome, TempoSlaHoras = p.TempoSlaHoras }).ToListAsync()
            };

            return View(viewModel);
        }

        // POST: /Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await RecarregarCombos(model);
                return View(model);
            }

            var prioridade = await _context.Prioridades.FindAsync(model.PrioridadeId);
            if (prioridade == null)
            {
                ModelState.AddModelError(nameof(model.PrioridadeId), "Prioridade inválida.");
                await RecarregarCombos(model);
                return View(model);
            }

            var statusAberto = await _context.StatusTickets.FirstAsync(s => s.Nome == "Aberto");
            var agora = DateTime.UtcNow;

            var ticket = new Ticket
            {
                NumeroTicket = await GerarNumeroTicketAsync(),
                Titulo = model.Titulo.Trim(),
                Descricao = model.Descricao.Trim(),
                EmpresaId = UsuarioLogadoEmpresaId,
                ModuloId = model.ModuloId,
                CategoriaId = model.CategoriaId,
                PrioridadeId = model.PrioridadeId,
                StatusId = statusAberto.Id,
                SolicitanteId = UsuarioLogadoId,
                DataAbertura = agora,
                DataLimiteSla = agora.AddHours(prioridade.TempoSlaHoras)
            };

            ticket.Historico.Add(new TicketHistorico
            {
                UsuarioId = UsuarioLogadoId,
                TipoEvento = TipoEvento.Criacao,
                StatusNovoId = statusAberto.Id,
                Comentario = "Ticket aberto pelo solicitante."
            });

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            if (model.Anexos is { Count: > 0 })
                await SalvarAnexosAsync(ticket.Id, model.Anexos);

            TempData["Mensagem"] = $"Ticket {ticket.NumeroTicket} aberto com sucesso.";
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        // GET: /Tickets/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Empresa)
                .Include(t => t.Modulo)
                .Include(t => t.Categoria)
                .Include(t => t.Prioridade)
                .Include(t => t.Status)
                .Include(t => t.Solicitante)
                .Include(t => t.Atendente)
                .Include(t => t.Anexos)
                .Include(t => t.Historico).ThenInclude(h => h.Usuario)
                .Include(t => t.Historico).ThenInclude(h => h.StatusAnterior)
                .Include(t => t.Historico).ThenInclude(h => h.StatusNovo)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound();

            // Solicitante só acessa chamados da própria empresa; Atendente/Admin acessam qualquer um.
            if (!UsuarioPodeGerenciarAtendimento && ticket.EmpresaId != UsuarioLogadoEmpresaId) return NotFound();

            var viewModel = new TicketDetailsViewModel
            {
                Id = ticket.Id,
                NumeroTicket = ticket.NumeroTicket,
                Titulo = ticket.Titulo,
                Descricao = ticket.Descricao,
                Categoria = ticket.Categoria!.Nome,
                Modulo = ticket.Modulo!.Nome,
                Empresa = ticket.Empresa!.Nome,
                Prioridade = ticket.Prioridade!.Nome,
                PrioridadeCorHex = ticket.Prioridade.CorHex,
                StatusId = ticket.StatusId,
                Status = ticket.Status!.Nome,
                StatusFinal = ticket.Status.Final,
                Solicitante = ticket.Solicitante!.Nome,
                AtendenteId = ticket.AtendenteId,
                Atendente = ticket.Atendente?.Nome,
                DataAbertura = ticket.DataAbertura,
                DataLimiteSla = ticket.DataLimiteSla,
                DataResolucao = ticket.DataResolucao,
                DataFechamento = ticket.DataFechamento,
                Solucao = ticket.Solucao,
                Sla = CalcularSituacaoSla(ticket.DataLimiteSla, ticket.Status.Final),

                Anexos = ticket.Anexos.Select(a => new TicketAnexoViewModel
                {
                    Id = a.Id,
                    NomeArquivoOriginal = a.NomeArquivoOriginal,
                    CaminhoArquivo = a.CaminhoArquivo,
                    TipoConteudo = a.TipoConteudo,
                    TamanhoBytes = a.TamanhoBytes,
                    DataUpload = a.DataUpload
                }).ToList(),

                Historico = ticket.Historico.OrderBy(h => h.DataHora).Select(h => new TicketHistoricoItemViewModel
                {
                    DataHora = h.DataHora,
                    Usuario = h.Usuario!.Nome,
                    TipoEvento = h.TipoEvento,
                    StatusAnterior = h.StatusAnterior?.Nome,
                    StatusNovo = h.StatusNovo?.Nome,
                    Comentario = h.Comentario
                }).ToList(),

                StatusDisponiveis = await _context.StatusTickets.OrderBy(s => s.Ordem)
                    .Select(s => new Status2ViewModel { Id = s.Id, Nome = s.Nome }).ToListAsync(),

                AtendentesDisponiveis = await _context.Usuarios
                    .Include(u => u.Perfil)
                    .Include(u => u.Empresa)
                    .Where(u => u.Ativo && (u.Perfil!.Nome == "Atendente" || u.Perfil.Nome == "Administrador"))
                    .OrderBy(u => u.Nome)
                    .Select(u => new UsuarioOpcaoViewModel { Id = u.Id, Nome = u.Nome, Empresa = u.Empresa!.Nome })
                    .ToListAsync(),

                PodeGerenciarAtendimento = UsuarioPodeGerenciarAtendimento
            };

            return View(viewModel);
        }

        // POST: /Tickets/AtribuirAtendente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtribuirAtendente(int ticketId, int? atendenteId)
        {
            if (!UsuarioPodeGerenciarAtendimento)
            {
                TempData["Erro"] = "Apenas atendentes ou administradores podem alterar o atendente do chamado.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null) return NotFound();

            if (atendenteId.HasValue)
            {
                // Atendente/Admin gerenciam chamados de qualquer empresa, então o atendente
                // atribuído também pode ser de uma empresa diferente da do chamado.
                var atendenteValido = await _context.Usuarios
                    .AnyAsync(u => u.Id == atendenteId && u.Ativo);
                if (!atendenteValido)
                {
                    TempData["Erro"] = "Atendente selecionado é inválido ou está inativo.";
                    return RedirectToAction(nameof(Details), new { id = ticketId });
                }
            }

            var atendenteAnteriorId = ticket.AtendenteId;
            ticket.AtendenteId = atendenteId;

            string comentarioHistorico;
            if (atendenteId == null)
            {
                comentarioHistorico = "Atendente removido do chamado.";
            }
            else
            {
                var nomeNovoAtendente = await _context.Usuarios
                    .Where(u => u.Id == atendenteId)
                    .Select(u => u.Nome)
                    .FirstAsync();

                comentarioHistorico = atendenteAnteriorId == null
                    ? $"Chamado atribuído a {nomeNovoAtendente}."
                    : $"Atendente alterado para {nomeNovoAtendente}.";
            }

            _context.TicketHistoricos.Add(new TicketHistorico
            {
                TicketId = ticketId,
                UsuarioId = UsuarioLogadoId,
                TipoEvento = TipoEvento.Atribuicao,
                Comentario = comentarioHistorico
            });

            await _context.SaveChangesAsync();

            TempData["Mensagem"] = "Atendente atualizado com sucesso.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        // POST: /Tickets/AdicionarComentario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarComentario(int ticketId, string novoComentario, List<IFormFile>? novosAnexos)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return NotFound();

            // Solicitante só comenta em chamados da própria empresa; Atendente/Admin em qualquer um.
            if (!UsuarioPodeGerenciarAtendimento && ticket.EmpresaId != UsuarioLogadoEmpresaId) return NotFound();

            if (!string.IsNullOrWhiteSpace(novoComentario))
            {
                _context.TicketHistoricos.Add(new TicketHistorico
                {
                    TicketId = ticketId,
                    UsuarioId = UsuarioLogadoId,
                    TipoEvento = TipoEvento.Comentario,
                    Comentario = novoComentario.Trim()
                });

                if (ticket.DataPrimeiraResposta == null)
                    ticket.DataPrimeiraResposta = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }

            if (novosAnexos is { Count: > 0 })
                await SalvarAnexosAsync(ticketId, novosAnexos);

            TempData["Mensagem"] = "Comentário registrado.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        // POST: /Tickets/AlterarStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarStatus(int ticketId, int novoStatusId, string? comentario)
        {
            if (!UsuarioPodeGerenciarAtendimento)
            {
                TempData["Erro"] = "Apenas atendentes ou administradores podem alterar o status do chamado.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var ticket = await _context.Tickets.Include(t => t.Status).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null) return NotFound();
            // Sem checagem de empresa aqui: quem chega até este ponto já é Atendente/Admin (checado acima),
            // e esses perfis gerenciam chamados de todas as empresas.

            var novoStatus = await _context.StatusTickets.FindAsync(novoStatusId);
            if (novoStatus == null) return NotFound();

            var statusAnteriorId = ticket.StatusId;

            // Assume o atendimento automaticamente se ainda não houver atendente
            if (ticket.AtendenteId == null && novoStatus.Nome != "Aberto")
                ticket.AtendenteId = UsuarioLogadoId;

            ticket.StatusId = novoStatusId;

            if (novoStatus.Nome == "Resolvido" && ticket.DataResolucao == null)
                ticket.DataResolucao = DateTime.UtcNow;

            if (novoStatus.Final)
            {
                ticket.DataFechamento = DateTime.UtcNow;
                ticket.SlaViolado = ticket.DataFechamento > ticket.DataLimiteSla;
            }

            _context.TicketHistoricos.Add(new TicketHistorico
            {
                TicketId = ticketId,
                UsuarioId = UsuarioLogadoId,
                TipoEvento = novoStatus.Final ? TipoEvento.Fechamento : TipoEvento.MudancaStatus,
                StatusAnteriorId = statusAnteriorId,
                StatusNovoId = novoStatusId,
                Comentario = comentario
            });

            await _context.SaveChangesAsync();

            TempData["Mensagem"] = $"Status atualizado para \"{novoStatus.Nome}\".";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        // POST: /Tickets/Fechar  (encerra o ticket já com a descrição da solução)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fechar(int ticketId, string solucaoInput)
        {
            if (!UsuarioPodeGerenciarAtendimento)
            {
                TempData["Erro"] = "Apenas atendentes ou administradores podem fechar o chamado.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            if (string.IsNullOrWhiteSpace(solucaoInput))
            {
                TempData["Erro"] = "Descreva a solução aplicada antes de fechar o ticket.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null) return NotFound();
            // Sem checagem de empresa: só Atendente/Admin chegam aqui, e gerenciam todas as empresas.

            var statusFechado = await _context.StatusTickets.FirstAsync(s => s.Nome == "Fechado");
            var statusAnteriorId = ticket.StatusId;
            var agora = DateTime.UtcNow;

            ticket.Solucao = solucaoInput.Trim();
            ticket.StatusId = statusFechado.Id;
            ticket.DataResolucao ??= agora;
            ticket.DataFechamento = agora;
            ticket.SlaViolado = agora > ticket.DataLimiteSla;

            _context.TicketHistoricos.Add(new TicketHistorico
            {
                TicketId = ticketId,
                UsuarioId = UsuarioLogadoId,
                TipoEvento = TipoEvento.Fechamento,
                StatusAnteriorId = statusAnteriorId,
                StatusNovoId = statusFechado.Id,
                Comentario = "Solução: " + ticket.Solucao
            });

            await _context.SaveChangesAsync();

            TempData["Mensagem"] = $"Ticket {ticket.NumeroTicket} fechado e arquivado no histórico.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        // ---------------------------------------------------------------
        // Métodos auxiliares
        // ---------------------------------------------------------------

        private async Task RecarregarCombos(TicketCreateViewModel model)
        {
            model.Modulos = await _context.Modulos.Where(m => m.Ativo)
                .Select(m => new Modulo2ViewModel { Id = m.Id, Nome = m.Nome }).ToListAsync();
            model.Categorias = await _context.Categorias.Where(c => c.Ativo)
                .Select(c => new Categoria2ViewModel { Id = c.Id, Nome = c.Nome }).ToListAsync();
            model.Prioridades = await _context.Prioridades.Where(p => p.Ativo)
                .Select(p => new Prioridade2ViewModel { Id = p.Id, Nome = p.Nome, TempoSlaHoras = p.TempoSlaHoras }).ToListAsync();
        }

        private async Task<string> GerarNumeroTicketAsync()
        {
            var ano = DateTime.UtcNow.Year;
            var quantidadeNoAno = await _context.Tickets.CountAsync(t => t.DataAbertura.Year == ano);
            return $"HD-{ano}-{(quantidadeNoAno + 1):D6}";
        }

        private async Task SalvarAnexosAsync(int ticketId, List<IFormFile> arquivos)
        {
            var pastaDestino = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "tickets", ticketId.ToString());
            Directory.CreateDirectory(pastaDestino);

            foreach (var arquivo in arquivos)
            {
                if (arquivo.Length == 0) continue;

                var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();

                if (!ExtensoesPermitidas.Contains(extensao))
                {
                    TempData["Erro"] = $"Arquivo \"{arquivo.FileName}\" ignorado: formato não permitido.";
                    continue;
                }

                if (arquivo.Length > TamanhoMaximoBytes)
                {
                    TempData["Erro"] = $"Arquivo \"{arquivo.FileName}\" ignorado: excede o limite de 5 MB.";
                    continue;
                }

                var nomeArmazenado = $"{Guid.NewGuid()}{extensao}";
                var caminhoFisico = Path.Combine(pastaDestino, nomeArmazenado);

                await using (var stream = new FileStream(caminhoFisico, FileMode.Create))
                {
                    await arquivo.CopyToAsync(stream);
                }

                var caminhoRelativo = $"/uploads/tickets/{ticketId}/{nomeArmazenado}";

                _context.TicketAnexos.Add(new TicketAnexo
                {
                    TicketId = ticketId,
                    NomeArquivoOriginal = arquivo.FileName,
                    NomeArquivoArmazenado = nomeArmazenado,
                    CaminhoArquivo = caminhoRelativo,
                    TipoConteudo = arquivo.ContentType,
                    TamanhoBytes = arquivo.Length,
                    UsuarioId = UsuarioLogadoId
                });

                _context.TicketHistoricos.Add(new TicketHistorico
                {
                    TicketId = ticketId,
                    UsuarioId = UsuarioLogadoId,
                    TipoEvento = TipoEvento.Anexo,
                    Comentario = $"Anexo \"{arquivo.FileName}\" adicionado."
                });
            }

            await _context.SaveChangesAsync();
        }

        private static SlaSituacao CalcularSituacaoSla(DateTime dataLimiteSla, bool statusFinal)
        {
            if (statusFinal) return SlaSituacao.Encerrado;

            var restante = dataLimiteSla - DateTime.UtcNow;

            if (restante.TotalMinutes < 0) return SlaSituacao.Estourado;
            if (restante.TotalHours <= 2) return SlaSituacao.ProximoDoLimite;
            return SlaSituacao.DentroDoPrazo;
        }
    }
}