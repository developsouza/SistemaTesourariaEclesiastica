// Services/PdfService.cs

using iText.Html2pdf;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using System.Text;

namespace SistemaTesourariaEclesiastica.Services
{
    public class PdfService
    {
        public byte[] GerarReciboFechamento(FechamentoPeriodo fechamento)
        {
            var html = GerarHtmlFechamento(fechamento);

            using var memoryStream = new MemoryStream();
            var properties = new ConverterProperties();

            HtmlConverter.ConvertToPdf(html, memoryStream, properties);

            return memoryStream.ToArray();
        }

        private string GerarHtmlFechamento(FechamentoPeriodo fechamento)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>Prestação de Contas - Fechamento de Período</title>");
            html.AppendLine("<style>");
            html.AppendLine(@"
        @page { 
            size: A4; 
            margin: 10mm 8mm 8mm 8mm; 
        }
        body { 
            font-family: Arial, sans-serif; 
            margin: 0;
            padding: 0;
            font-size: 7.5px;
            line-height: 1.2;
            color: #333;
        }
        .header { 
            text-align: center; 
            margin-bottom: 10px; 
            border-bottom: 2px solid #2c3e50;
            padding-bottom: 6px;
        }
        .title { 
            font-size: 14px; 
            font-weight: bold; 
            color: #2c3e50;
            margin: 0;
        }
        .subtitle { 
            font-size: 9px; 
            color: #555;
            margin: 2px 0;
        }
        .section-title {
            background-color: #3498db;
            color: white;
            padding: 4px 8px;
            font-size: 9px;
            font-weight: bold;
            margin-top: 10px;
            margin-bottom: 5px;
            border-radius: 2px;
        }
        .section-title-success {
            background-color: #27ae60;
        }
        .section-title-danger {
            background-color: #e74c3c;
        }
        .section-title-warning {
            background-color: #f39c12;
        }
        .section-title-info {
            background-color: #16a085;
        }
        .info-grid { 
            display: grid; 
            grid-template-columns: 1fr 1fr 1fr 1fr;
            gap: 5px;
            margin-bottom: 8px;
        }
        .info-item {
            background-color: #f8f9fa;
            padding: 4px;
            border-radius: 2px;
            border-left: 2px solid #3498db;
        }
        .info-label { 
            font-size: 6.5px;
            color: #666;
            display: block;
            margin-bottom: 1px;
        }
        .info-value { 
            font-weight: bold;
            font-size: 8px;
            color: #2c3e50;
        }
        .summary-box {
            background-color: #ecf0f1;
            border: 1px solid #bdc3c7;
            border-radius: 3px;
            padding: 6px;
            margin: 6px 0;
        }
        .summary-grid {
            display: grid;
            grid-template-columns: 1fr 1fr 1fr 1fr 1fr;
            gap: 5px;
            margin-top: 5px;
        }
        .summary-item {
            text-align: center;
            padding: 4px;
            background-color: white;
            border-radius: 2px;
        }
        .summary-label {
            font-size: 6.5px;
            color: #666;
            display: block;
            margin-bottom: 2px;
        }
        .summary-value {
            font-size: 9px;
            font-weight: bold;
            color: #2c3e50;
        }
        .text-success { color: #27ae60; }
        .text-danger { color: #e74c3c; }
        .text-warning { color: #f39c12; }
        .text-info { color: #16a085; }
        
        table { 
            width: 100%; 
            border-collapse: collapse; 
            margin: 5px 0;
            font-size: 7px;
        }
        table thead {
            background-color: #34495e;
            color: white;
        }
        table th { 
            padding: 3px 2px;
            text-align: left;
            font-weight: bold;
            font-size: 7px;
        }
        table td { 
            padding: 2px 2px;
            border-bottom: 1px solid #ecf0f1;
        }
        table tbody tr:hover {
            background-color: #f8f9fa;
        }
        .table-striped tbody tr:nth-child(odd) {
            background-color: #f8f9fa;
        }
        .currency {
            text-align: right;
            font-family: 'Courier New', monospace;
        }
        .center {
            text-align: center;
        }
        .badge {
            display: inline-block;
            padding: 1px 4px;
            border-radius: 2px;
            font-size: 6px;
            font-weight: bold;
            color: white;
        }
        .badge-success { background-color: #27ae60; }
        .badge-info { background-color: #3498db; }
        .badge-warning { background-color: #f39c12; }
        .badge-danger { background-color: #e74c3c; }
        
        .totals-row {
            font-weight: bold;
            background-color: #ecf0f1 !important;
            border-top: 1px solid #2c3e50;
        }
        .signature-section {
            margin-top: 15px;
            page-break-inside: avoid;
        }
        .signature-line {
            display: inline-block;
            width: 45%;
            text-align: center;
            border-top: 1px solid #333;
            padding-top: 3px;
            margin: 0 2%;
            font-size: 7px;
        }
        .footer { 
            position: fixed;
            bottom: 3mm;
            width: 100%;
            text-align: center; 
            font-size: 6px; 
            color: #999;
            border-top: 1px solid #ddd;
            padding-top: 2px;
        }
        .page-break {
            page-break-after: always;
        }
        .no-data {
            text-align: center;
            padding: 10px;
            color: #999;
            font-style: italic;
            font-size: 7px;
        }
        .alert-box {
            background-color: #fff3cd;
            border: 1px solid #ffc107;
            border-radius: 3px;
            padding: 5px;
            margin: 6px 0;
            font-size: 6.5px;
        }
        .congregacao-separator {
            background-color: #e8f5e9;
            padding: 4px 6px;
            margin: 8px 0 4px 0;
            border-left: 3px solid #4caf50;
            font-weight: bold;
            font-size: 8px;
        }
        .sede-separator {
            background-color: #e3f2fd;
            padding: 4px 6px;
            margin: 8px 0 4px 0;
            border-left: 3px solid #2196f3;
            font-weight: bold;
            font-size: 8px;
        }
    ");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // ==================== CABEÇALHO ====================
            GerarCabecalho(html, fechamento);

            // ==================== RESUMO EXECUTIVO ====================
            GerarResumoExecutivo(html, fechamento);

            // ==================== INFORMAÇÕES DO FECHAMENTO ====================
            GerarInformacoesFechamento(html, fechamento);

            // ==================== CONGREGAÇÕES INCLUÍDAS (SE HOUVER) ====================
            if (fechamento.FechamentosCongregacoesIncluidos?.Any() == true)
            {
                GerarCongregacoesIncluidas(html, fechamento);
            }

            // ==================== DETALHAMENTO DE ENTRADAS ====================
            GerarDetalhamentoEntradas(html, fechamento);

            // ==================== DETALHAMENTO DE SAÍDAS ====================
            GerarDetalhamentoSaidas(html, fechamento);

            // ==================== RATEIOS APLICADOS ====================
            if (fechamento.ItensRateio?.Any() == true)
            {
                GerarRateiosAplicados(html, fechamento);
            }

            // ==================== OBSERVAÇÕES ====================
            if (!string.IsNullOrEmpty(fechamento.Observacoes))
            {
                GerarObservacoes(html, fechamento);
            }

            // ==================== ASSINATURAS ====================
            GerarAssinaturas(html, fechamento);

            // ==================== RODAPÉ ====================
            html.AppendLine("<div class='footer'>");
            html.AppendLine($"Documento gerado em {DateTime.Now:dd/MM/yyyy HH:mm} - Sistema de Gestão de Tesouraria Eclesiástica");
            html.AppendLine("</div>");

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private void GerarCabecalho(StringBuilder html, FechamentoPeriodo fechamento)
        {
            html.AppendLine("<div class='header'>");
            html.AppendLine("<div class='title'>PRESTAÇÃO DE CONTAS - FECHAMENTO DE PERÍODO</div>");
            html.AppendLine($"<div class='subtitle'>{fechamento.CentroCusto.Nome}</div>");

            if (fechamento.TipoFechamento == TipoFechamento.Diario)
            {
                html.AppendLine($"<div class='subtitle'>Fechamento Diário: {fechamento.DataInicio:dd/MM/yyyy}</div>");
            }
            else if (fechamento.TipoFechamento == TipoFechamento.Semanal)
            {
                var diasSemana = (fechamento.DataFim - fechamento.DataInicio).Days + 1;
                html.AppendLine($"<div class='subtitle'>Fechamento Semanal: {fechamento.DataInicio:dd/MM/yyyy} a {fechamento.DataFim:dd/MM/yyyy} ({diasSemana} dia(s))</div>");
            }
            else
            {
                html.AppendLine($"<div class='subtitle'>Período: {fechamento.Mes:00}/{fechamento.Ano}</div>");
            }

            var statusBadge = fechamento.Status switch
            {
                StatusFechamentoPeriodo.Aprovado => "<span class='badge badge-success'>APROVADO</span>",
                StatusFechamentoPeriodo.Pendente => "<span class='badge badge-warning'>PENDENTE</span>",
                StatusFechamentoPeriodo.Rejeitado => "<span class='badge badge-danger'>REJEITADO</span>",
                _ => "<span class='badge badge-info'>PROCESSADO</span>"
            };

            html.AppendLine($"<div class='subtitle'>{statusBadge}</div>");
            html.AppendLine("</div>");
        }

        private void GerarResumoExecutivo(StringBuilder html, FechamentoPeriodo fechamento)
        {
            html.AppendLine("<div class='summary-box'>");
            html.AppendLine("<div style='font-size: 9px; font-weight: bold; margin-bottom: 5px; color: #2c3e50;'>RESUMO EXECUTIVO</div>");
            html.AppendLine("<div class='summary-grid'>");

            html.AppendLine("<div class='summary-item'>");
            html.AppendLine("<span class='summary-label'>Total de Entradas</span>");
            html.AppendLine($"<span class='summary-value text-success'>{fechamento.TotalEntradas:C}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='summary-item'>");
            html.AppendLine("<span class='summary-label'>Total de Saídas</span>");
            html.AppendLine($"<span class='summary-value text-danger'>{fechamento.TotalSaidas:C}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='summary-item'>");
            html.AppendLine("<span class='summary-label'>Balanço Físico</span>");
            html.AppendLine($"<span class='summary-value text-warning'>{fechamento.BalancoFisico:C}</span>");
            html.AppendLine("</div>");

            if (fechamento.TotalRateios > 0)
            {
                html.AppendLine("<div class='summary-item'>");
                html.AppendLine("<span class='summary-label'>(-) Rateios</span>");
                html.AppendLine($"<span class='summary-value text-danger'>-{fechamento.TotalRateios:C}</span>");
                html.AppendLine("</div>");
            }

            html.AppendLine("<div class='summary-item'>");
            html.AppendLine("<span class='summary-label'>Saldo Final</span>");
            html.AppendLine($"<span class='summary-value text-info'>{fechamento.SaldoFinal:C}</span>");
            html.AppendLine("</div>");

            html.AppendLine("</div>");
            html.AppendLine("</div>");
        }

        private void GerarInformacoesFechamento(StringBuilder html, FechamentoPeriodo fechamento)
        {
            html.AppendLine("<div class='section-title'>INFORMAÇÕES DO FECHAMENTO</div>");
            html.AppendLine("<div class='info-grid'>");

            html.AppendLine("<div class='info-item'>");
            html.AppendLine("<span class='info-label'>Centro de Custo:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.CentroCusto.Nome}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-item'>");
            html.AppendLine("<span class='info-label'>Tipo de Fechamento:</span>");
            var tipoTexto = fechamento.TipoFechamento == TipoFechamento.Diario ? "Diário" :
                            fechamento.TipoFechamento == TipoFechamento.Semanal ? "Semanal" : "Mensal";
            html.AppendLine($"<span class='info-value'>{tipoTexto}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-item'>");
            html.AppendLine("<span class='info-label'>Período:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.DataInicio:dd/MM/yyyy} - {fechamento.DataFim:dd/MM/yyyy}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-item'>");
            html.AppendLine("<span class='info-label'>Status:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.Status}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-item'>");
            html.AppendLine("<span class='info-label'>Submetido por:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.UsuarioSubmissao?.NomeCompleto ?? "N/A"}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-item'>");
            html.AppendLine("<span class='info-label'>Data de Submissão:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.DataSubmissao:dd/MM/yyyy HH:mm}</span>");
            html.AppendLine("</div>");

            if (fechamento.UsuarioAprovacao != null)
            {
                html.AppendLine("<div class='info-item'>");
                html.AppendLine("<span class='info-label'>Aprovado por:</span>");
                html.AppendLine($"<span class='info-value'>{fechamento.UsuarioAprovacao.NomeCompleto}</span>");
                html.AppendLine("</div>");

                html.AppendLine("<div class='info-item'>");
                html.AppendLine("<span class='info-label'>Data de Aprovação:</span>");
                html.AppendLine($"<span class='info-value'>{fechamento.DataAprovacao:dd/MM/yyyy HH:mm}</span>");
                html.AppendLine("</div>");
            }

            html.AppendLine("</div>");
        }

        private void GerarCongregacoesIncluidas(StringBuilder html, FechamentoPeriodo fechamento)
        {
            html.AppendLine("<div class='section-title section-title-info'>PRESTAÇÕES DE CONGREGAÇÕES INCLUÍDAS NESTE FECHAMENTO</div>");

            html.AppendLine("<div class='alert-box'>");
            html.AppendLine($"<strong>Este fechamento da SEDE consolidou {fechamento.FechamentosCongregacoesIncluidos.Count} prestação(ões) de congregação(ões).</strong> ");
            html.AppendLine("Os valores e detalhes abaixo já estão incluídos no resumo executivo e nos totais apresentados.");
            html.AppendLine("</div>");

            html.AppendLine("<table class='table-striped'>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr>");
            html.AppendLine("<th>Congregação</th>");
            html.AppendLine("<th>Período</th>");
            html.AppendLine("<th class='currency'>Entradas</th>");
            html.AppendLine("<th class='currency'>Saídas</th>");
            html.AppendLine("<th class='currency'>Balanço Físico</th>");
            html.AppendLine("<th class='center'>Data Aprovação</th>");
            html.AppendLine("</tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");

            foreach (var congFechamento in fechamento.FechamentosCongregacoesIncluidos.OrderBy(f => f.CentroCusto.Nome))
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td><strong>{congFechamento.CentroCusto.Nome}</strong></td>");
                html.AppendLine($"<td>{congFechamento.DataInicio:dd/MM/yyyy} - {congFechamento.DataFim:dd/MM/yyyy}</td>");
                html.AppendLine($"<td class='currency text-success'>{congFechamento.TotalEntradas:C}</td>");
                html.AppendLine($"<td class='currency text-danger'>{congFechamento.TotalSaidas:C}</td>");
                html.AppendLine($"<td class='currency text-warning'>{congFechamento.BalancoFisico:C}</td>");
                html.AppendLine($"<td class='center'>{congFechamento.DataAprovacao:dd/MM/yyyy}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
        }

        private void GerarDetalhamentoEntradas(StringBuilder html, FechamentoPeriodo fechamento)
        {
            html.AppendLine("<div class='section-title section-title-success'>DETALHAMENTO DAS ENTRADAS (RECEITAS)</div>");

            var entradasPorOrigem = new Dictionary<string, (List<DetalheFechamento> detalhes, bool ehSede)>();

            // ✅ CORREÇÃO: Adicionar entradas da SEDE PRIMEIRO
            var entradasSede = fechamento.DetalhesFechamento
                .Where(d => d.TipoMovimento == "Entrada")
                .OrderBy(d => d.Data)
                .ThenBy(d => d.Descricao)
                .ToList();

            if (entradasSede.Any())
            {
                entradasPorOrigem[fechamento.CentroCusto.Nome] = (entradasSede, true);
            }

            // Adicionar entradas das congregações incluídas
            if (fechamento.FechamentosCongregacoesIncluidos?.Any() == true)
            {
                foreach (var congFechamento in fechamento.FechamentosCongregacoesIncluidos.OrderBy(c => c.CentroCusto.Nome))
                {
                    var entradasCong = congFechamento.DetalhesFechamento
                        .Where(d => d.TipoMovimento == "Entrada")
                        .OrderBy(d => d.Data)
                        .ThenBy(d => d.Descricao)
                        .ToList();

                    if (entradasCong.Any())
                    {
                        entradasPorOrigem[congFechamento.CentroCusto.Nome] = (entradasCong, false);
                    }
                }
            }

            if (!entradasPorOrigem.Any())
            {
                html.AppendLine("<div class='no-data'>Nenhuma entrada registrada no período.</div>");
                return;
            }

            decimal totalGeralEntradas = 0;

            foreach (var origem in entradasPorOrigem)
            {
                var separatorClass = origem.Value.ehSede ? "sede-separator" : "congregacao-separator";
                var icone = origem.Value.ehSede ? "🏛️" : "📍";

                html.AppendLine($"<div class='{separatorClass}'>{icone} {origem.Key}</div>");

                html.AppendLine("<table class='table-striped'>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th style='width: 50px;'>Data</th>");
                html.AppendLine("<th>Fonte de Renda</th>");
                html.AppendLine("<th>Descrição</th>");
                html.AppendLine("<th>Membro</th>");
                html.AppendLine("<th style='width: 55px;'>Meio Pgto</th>");
                html.AppendLine("<th class='currency' style='width: 55px;'>Valor</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");

                decimal subtotal = 0;

                foreach (var entrada in origem.Value.detalhes)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{entrada.Data:dd/MM/yyyy}</td>");
                    html.AppendLine($"<td>{entrada.PlanoContas ?? "N/A"}</td>");
                    html.AppendLine($"<td>{entrada.Descricao ?? "-"}</td>");
                    html.AppendLine($"<td>{entrada.Membro ?? "-"}</td>");
                    html.AppendLine($"<td>{entrada.MeioPagamento ?? "N/A"}</td>");
                    html.AppendLine($"<td class='currency text-success'><strong>{entrada.Valor:C}</strong></td>");
                    html.AppendLine("</tr>");

                    subtotal += entrada.Valor;
                }

                html.AppendLine("<tr class='totals-row'>");
                html.AppendLine($"<td colspan='5' style='text-align: right;'><strong>Subtotal {origem.Key}:</strong></td>");
                html.AppendLine($"<td class='currency text-success'><strong>{subtotal:C}</strong></td>");
                html.AppendLine("</tr>");

                html.AppendLine("</tbody>");
                html.AppendLine("</table>");

                totalGeralEntradas += subtotal;
            }

            if (entradasPorOrigem.Count > 1)
            {
                html.AppendLine("<table>");
                html.AppendLine("<tr class='totals-row'>");
                html.AppendLine($"<td colspan='5' style='text-align: right; font-size: 9px;'><strong>TOTAL GERAL DE ENTRADAS:</strong></td>");
                html.AppendLine($"<td class='currency text-success' style='font-size: 9px;'><strong>{totalGeralEntradas:C}</strong></td>");
                html.AppendLine("</tr>");
                html.AppendLine("</table>");
            }
        }

        private void GerarDetalhamentoSaidas(StringBuilder html, FechamentoPeriodo fechamento)
        {
            html.AppendLine("<div class='section-title section-title-danger'>DETALHAMENTO DAS SAÍDAS (DESPESAS)</div>");

            var saidasPorOrigem = new Dictionary<string, (List<DetalheFechamento> detalhes, bool ehSede)>();

            // ✅ CORREÇÃO: Adicionar saídas da SEDE PRIMEIRO
            var saidasSede = fechamento.DetalhesFechamento
                .Where(d => d.TipoMovimento == "Saida")
                .OrderBy(d => d.Data)
                .ThenBy(d => d.Descricao)
                .ToList();

            if (saidasSede.Any())
            {
                saidasPorOrigem[fechamento.CentroCusto.Nome] = (saidasSede, true);
            }

            // Adicionar saídas das congregações incluídas
            if (fechamento.FechamentosCongregacoesIncluidos?.Any() == true)
            {
                foreach (var congFechamento in fechamento.FechamentosCongregacoesIncluidos.OrderBy(c => c.CentroCusto.Nome))
                {
                    var saidasCong = congFechamento.DetalhesFechamento
                        .Where(d => d.TipoMovimento == "Saida")
                        .OrderBy(d => d.Data)
                        .ThenBy(d => d.Descricao)
                        .ToList();

                    if (saidasCong.Any())
                    {
                        saidasPorOrigem[congFechamento.CentroCusto.Nome] = (saidasCong, false);
                    }
                }
            }

            if (!saidasPorOrigem.Any())
            {
                html.AppendLine("<div class='no-data'>Nenhuma saída registrada no período.</div>");
                return;
            }

            decimal totalGeralSaidas = 0;

            foreach (var origem in saidasPorOrigem)
            {
                var separatorClass = origem.Value.ehSede ? "sede-separator" : "congregacao-separator";
                var corSeparator = origem.Value.ehSede ? "background-color: #ffebee; border-left-color: #1976d2;" : "background-color: #ffebee; border-left-color: #f44336;";
                var icone = origem.Value.ehSede ? "🏛️" : "📍";

                html.AppendLine($"<div class='congregacao-separator' style='{corSeparator}'>{icone} {origem.Key}</div>");

                html.AppendLine("<table class='table-striped'>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th style='width: 50px;'>Data</th>");
                html.AppendLine("<th>Categoria</th>");
                html.AppendLine("<th>Descrição/Finalidade</th>");
                html.AppendLine("<th>Fornecedor</th>");
                html.AppendLine("<th style='width: 55px;'>Meio Pgto</th>");
                html.AppendLine("<th class='currency' style='width: 55px;'>Valor</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");

                decimal subtotal = 0;

                foreach (var saida in origem.Value.detalhes)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{saida.Data:dd/MM/yyyy}</td>");
                    html.AppendLine($"<td>{saida.PlanoContas ?? "N/A"}</td>");
                    html.AppendLine($"<td>{saida.Descricao ?? "-"}</td>");
                    html.AppendLine($"<td>{saida.Fornecedor ?? "-"}</td>");
                    html.AppendLine($"<td>{saida.MeioPagamento ?? "N/A"}</td>");
                    html.AppendLine($"<td class='currency text-danger'><strong>{saida.Valor:C}</strong></td>");
                    html.AppendLine("</tr>");

                    subtotal += saida.Valor;
                }

                html.AppendLine("<tr class='totals-row'>");
                html.AppendLine($"<td colspan='5' style='text-align: right;'><strong>Subtotal {origem.Key}:</strong></td>");
                html.AppendLine($"<td class='currency text-danger'><strong>{subtotal:C}</strong></td>");
                html.AppendLine("</tr>");

                html.AppendLine("</tbody>");
                html.AppendLine("</table>");

                totalGeralSaidas += subtotal;
            }

            if (saidasPorOrigem.Count > 1)
            {
                html.AppendLine("<table>");
                html.AppendLine("<tr class='totals-row'>");
                html.AppendLine($"<td colspan='5' style='text-align: right; font-size: 9px;'><strong>TOTAL GERAL DE SAÍDAS:</strong></td>");
                html.AppendLine($"<td class='currency text-danger' style='font-size: 9px;'><strong>{totalGeralSaidas:C}</strong></td>");
                html.AppendLine("</tr>");
                html.AppendLine("</table>");
            }
        }

        private void GerarRateiosAplicados(StringBuilder html, FechamentoPeriodo fechamento)
        {
            html.AppendLine("<div class='section-title section-title-warning'>RATEIOS APLICADOS</div>");

            html.AppendLine("<table class='table-striped'>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr>");
            html.AppendLine("<th>Regra de Rateio</th>");
            html.AppendLine("<th>Destino</th>");
            html.AppendLine("<th class='center'>Percentual</th>");
            html.AppendLine("<th class='currency'>Valor Base</th>");
            html.AppendLine("<th class='currency'>Valor Rateado</th>");
            html.AppendLine("</tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");

            foreach (var rateio in fechamento.ItensRateio.OrderBy(r => r.RegraRateio.Nome))
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{rateio.RegraRateio.Nome}</td>");
                html.AppendLine($"<td><span class='badge badge-info'>{rateio.RegraRateio.CentroCustoDestino.Nome}</span></td>");
                html.AppendLine($"<td class='center'>{rateio.Percentual:F2}%</td>");
                html.AppendLine($"<td class='currency'>{rateio.ValorBase:C}</td>");
                html.AppendLine($"<td class='currency text-warning'><strong>{rateio.ValorRateio:C}</strong></td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("<tr class='totals-row'>");
            html.AppendLine("<td colspan='4' style='text-align: right;'><strong>Total de Rateios:</strong></td>");
            html.AppendLine($"<td class='currency text-warning'><strong>{fechamento.TotalRateios:C}</strong></td>");
            html.AppendLine("</tr>");

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");

            html.AppendLine("<div class='alert-box'>");
            html.AppendLine("<strong>Importante:</strong> O valor total de rateios foi deduzido do balanço para cálculo do saldo final. ");
            html.AppendLine("Estes valores foram destinados aos fundos conforme as regras de rateio configuradas.");
            html.AppendLine("</div>");
        }

        private void GerarObservacoes(StringBuilder html, FechamentoPeriodo fechamento)
        {
            html.AppendLine("<div class='section-title'>OBSERVAÇÕES</div>");
            html.AppendLine("<div style='background-color: #f8f9fa; padding: 6px; border-radius: 3px; border-left: 3px solid #6c757d;'>");
            html.AppendLine($"<p style='margin: 0; font-size: 7px; line-height: 1.4;'>{fechamento.Observacoes.Replace("\n", "<br>")}</p>");
            html.AppendLine("</div>");
        }

        private void GerarAssinaturas(StringBuilder html, FechamentoPeriodo fechamento)
        {
            html.AppendLine("<div class='signature-section'>");
            html.AppendLine("<div style='text-align: center; margin-top: 20px;'>");

            html.AppendLine("<div class='signature-line'>");
            html.AppendLine($"<div>{fechamento.UsuarioSubmissao?.NomeCompleto ?? "___________________________"}</div>");
            html.AppendLine("<div style='font-size: 6px; color: #666; margin-top: 1px;'>Tesoureiro Responsável</div>");
            html.AppendLine("</div>");

            if (fechamento.UsuarioAprovacao != null)
            {
                html.AppendLine("<div class='signature-line'>");
                html.AppendLine($"<div>{fechamento.UsuarioAprovacao.NomeCompleto}</div>");
                html.AppendLine("<div style='font-size: 6px; color: #666; margin-top: 1px;'>Tesoureiro Geral (Aprovador)</div>");
                html.AppendLine("</div>");
            }

            html.AppendLine("</div>");
            html.AppendLine("</div>");
        }
    }
}