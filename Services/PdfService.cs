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
            margin: 3mm 10mm 10mm 10mm; 
        }
        body { 
            font-family: Arial, sans-serif; 
            margin: 0;
            padding: 0;
            font-size: 10px;
            line-height: 1.3;
            color: #333;
        }
        
        /* CABEÇALHO OTIMIZADO */
        .header { 
            margin-bottom: 4px; 
            border-bottom: 1px solid #2c3e50;
            padding-bottom: 0px;
            display: grid;
            grid-template-columns: 120px 1fr;
            gap: 8px;
            align-items: center;
        }
        .header-logo {
            width: 120px;
            height: 120px;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        .header-logo img {
            width: 100%;
            height: 100%;
            object-fit: contain;
        }
        .header-content {
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
        }
        .header-title {
            text-align: center;
            margin-bottom: 0px;
            width: 100%;
        }
        .title { 
            font-size: 16px; 
            font-weight: bold; 
            color: #2c3e50;
            margin: 0;
        }
        .subtitle { 
            font-size: 11px; 
            color: #555;
            margin: 0;
        }
        
        /* Grid de informações no cabeçalho - mais compacto */
        .header-info-grid { 
            display: grid; 
            grid-template-columns: repeat(4, 1fr);
            gap: 10px;
            justify-content: center;
            margin-top: 4px;
            background-color: #f8f9fa;
            padding: 6px 15px;
            border-radius: 2px;
        }
        .header-info-item {
            padding: 3px 5px;
            text-align: center;
        }
        .header-label { 
            font-size: 8px;
            color: #666;
            display: block;
            margin-bottom: 2px;
        }
        .header-value { 
            font-weight: bold;
            font-size: 9.5px;
            color: #2c3e50;
        }
        
        .summary-box {
            background-color: #ecf0f1;
            border: 2px solid #3498db;
            border-radius: 4px;
            padding: 8px;
            margin: 8px 0;
        }
        .summary-grid {
            display: grid;
            grid-template-columns: 1fr 1fr 1fr 1fr 1fr;
            gap: 6px;
            margin-top: 5px;
        }
        .summary-item {
            text-align: center;
            padding: 5px;
            background-color: white;
            border-radius: 2px;
        }
        .summary-label {
            font-size: 8.5px;
            color: #666;
            display: block;
            margin-bottom: 2px;
        }
        .summary-value {
            font-size: 12px;
            font-weight: bold;
            color: #2c3e50;
        }
        .text-success { color: #27ae60; }
        .text-danger { color: #e74c3c; }
        .text-warning { color: #f39c12; }
        .text-info { color: #16a085; }
        
        /* ✅ NOVO: Seção simples e compacta */
        .info-complementar {
            background-color: #fff9e6;
            border-left: 4px solid #f39c12;
            padding: 6px 8px;
            margin: 8px 0;
            font-size: 8.5px;
            line-height: 1.5;
        }
        .info-complementar-title {
            font-size: 9.5px;
            font-weight: bold;
            color: #856404;
            margin-bottom: 4px;
        }
        .info-line {
            margin: 3px 0;
            color: #333;
        }
        .info-label {
            font-weight: bold;
            color: #856404;
        }
        .info-value {
            color: #333;
        }
        
        /* Layout em 2 colunas para entradas e saídas lado a lado */
        .two-columns {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 10px;
            margin: 8px 0;
        }
        
        .column {
            min-height: 100px;
        }
        
        .column-header {
            font-weight: bold;
            padding: 5px 8px;
            margin-bottom: 5px;
            border-radius: 2px;
            font-size: 11px;
        }
        
        .column-header-success {
            background-color: #27ae60;
            color: white;
        }
        
        .column-header-danger {
            background-color: #e74c3c;
            color: white;
        }
        
        table { 
            width: 100%; 
            border-collapse: collapse; 
            margin: 5px 0;
            font-size: 9px;
        }
        table thead {
            background-color: #34495e;
            color: white;
        }
        table th { 
            padding: 4px 3px;
            text-align: left;
            font-weight: bold;
            font-size: 9px;
        }
        table td { 
            padding: 3px 3px;
            border-bottom: 1px solid #ecf0f1;
            white-space: nowrap;
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
            font-weight: bold;
            white-space: nowrap;
        }
        .center {
            text-align: center;
        }
        .badge {
            display: inline-block;
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 9px;
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
            border-top: 2px solid #2c3e50;
            font-size: 10px;
        }
        .totals-row td {
            white-space: nowrap;
        }
        .signature-section {
            margin-top: 20px;
            page-break-inside: avoid;
        }
        .signature-line {
            display: inline-block;
            width: 45%;
            text-align: center;
            border-top: 1px solid #333;
            padding-top: 4px;
            margin: 0 2%;
            font-size: 9px;
        }
        .footer { 
            position: fixed;
            bottom: 5mm;
            width: 100%;
            text-align: center; 
            font-size: 7.5px; 
            color: #999;
            border-top: 1px solid #ddd;
            padding-top: 3px;
        }
        .page-break {
            page-break-after: always;
        }
        .no-data {
            text-align: center;
            padding: 15px;
            color: #999;
            font-style: italic;
            font-size: 9px;
        }
        .congregacao-separator {
            background-color: #e8f5e9;
            padding: 4px 6px;
            margin: 6px 0 4px 0;
            border-left: 3px solid #4caf50;
            font-weight: bold;
            font-size: 9.5px;
        }
        .sede-separator {
            background-color: #e3f2fd;
            padding: 4px 6px;
            margin: 6px 0 4px 0;
            border-left: 3px solid #2196f3;
            font-weight: bold;
            font-size: 9.5px;
        }
        
        /* Compactar tabelas nas colunas */
        .column table {
            font-size: 8.5px;
        }
        .column table th {
            font-size: 8.5px;
            padding: 3px 2px;
        }
        .column table td {
            padding: 2px 2px;
            white-space: nowrap;
        }
    ");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // ==================== CABEÇALHO OTIMIZADO ====================
            GerarCabecalhoOtimizado(html, fechamento);

            // ==================== RESUMO EXECUTIVO ====================
            GerarResumoExecutivo(html, fechamento);

            // ==================== ✅ NOVO: INFORMAÇÕES COMPLEMENTARES SIMPLIFICADAS ====================
            GerarInformacoesComplementares(html, fechamento);

            // ==================== ENTRADAS E SAÍDAS LADO A LADO ====================
            GerarDetalhamentoLadoALado(html, fechamento);

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

        private void GerarCabecalhoOtimizado(StringBuilder html, FechamentoPeriodo fechamento)
        {
            html.AppendLine("<div class='header'>");

            // Logo à esquerda (coluna 1)
            html.AppendLine("<div class='header-logo'>");
            html.AppendLine("<img src='wwwroot/images/logoadjacumabk.png' alt='Logo' />");
            html.AppendLine("</div>");

            // Conteúdo do cabeçalho (coluna 2)
            html.AppendLine("<div class='header-content'>");

            // Título centralizado
            html.AppendLine("<div class='header-title'>");
            html.AppendLine("<div class='title'>PRESTAÇÃO DE CONTAS - FECHAMENTO DE PERÍODO</div>");

            // ✅ OTIMIZADO: Nome do centro de custo e período na mesma linha com separador
            html.AppendLine("<div class='subtitle'>");
            html.AppendLine($"<strong>{fechamento.CentroCusto.Nome}</strong>");

            // Período
            string periodoTexto;
            if (fechamento.TipoFechamento == TipoFechamento.Diario)
            {
                periodoTexto = $"Fechamento Diário: {fechamento.DataInicio:dd/MM/yyyy}";
            }
            else if (fechamento.TipoFechamento == TipoFechamento.Semanal)
            {
                periodoTexto = $"Fechamento Semanal: {fechamento.DataInicio:dd/MM/yyyy} a {fechamento.DataFim:dd/MM/yyyy}";
            }
            else
            {
                periodoTexto = $"Período: {fechamento.DataInicio:dd/MM/yyyy} a {fechamento.DataFim:dd/MM/yyyy}";
            }

            var statusBadge = fechamento.Status switch
            {
                StatusFechamentoPeriodo.Aprovado => "<span class='badge badge-success'>APROVADO</span>",
                StatusFechamentoPeriodo.Pendente => "<span class='badge badge-warning'>PENDENTE</span>",
                StatusFechamentoPeriodo.Rejeitado => "<span class='badge badge-danger'>REJEITADO</span>",
                _ => "<span class='badge badge-info'>PROCESSADO</span>"
            };

            html.AppendLine($" <strong>|</strong> {periodoTexto} <strong>|</strong> {statusBadge}");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            html.AppendLine("</div>"); // Fim header-content
            html.AppendLine("</div>"); // Fim header
        }

        private void GerarResumoExecutivo(StringBuilder html, FechamentoPeriodo fechamento)
        {
            html.AppendLine("<div class='summary-box'>");
            html.AppendLine("<div style='font-size: 10px; font-weight: bold; margin-bottom: 6px; color: #2c3e50; text-align: center;'>💰 RESUMO EXECUTIVO</div>");
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
            html.AppendLine("<span class='summary-label'>Balanço Total</span>");
            var balanco = fechamento.BalancoFisico + fechamento.BalancoDigital;
            html.AppendLine($"<span class='summary-value text-warning'>{balanco:C}</span>");
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

        // ✅ NOVO: Formato simplificado e direto
        private void GerarInformacoesComplementares(StringBuilder html, FechamentoPeriodo fechamento)
        {
            var temCongregacoes = fechamento.FechamentosCongregacoesIncluidos?.Any() == true;
            var temRateios = fechamento.ItensRateio?.Any() == true;

            if (!temCongregacoes && !temRateios)
                return;

            html.AppendLine("<div class='info-complementar'>");
            html.AppendLine("<div class='info-complementar-title'>INFORMAÇÕES COMPLEMENTARES</div>");

            // ✅ OTIMIZADO: Congregações na mesma linha, apenas nomes
            if (temCongregacoes)
            {
                html.AppendLine("<div class='info-line'>");
                html.AppendLine($"<span class='info-label'>Consolidação:</span> ");
                html.AppendLine($"{fechamento.FechamentosCongregacoesIncluidos.Count} prestação(ões) de congregação(ões): ");

                var nomesConjuntos = string.Join(", ", fechamento.FechamentosCongregacoesIncluidos
                    .OrderBy(f => f.CentroCusto.Nome)
                    .Select(cong => cong.CentroCusto.Nome));

                html.AppendLine(nomesConjuntos);
                html.AppendLine("</div>");
            }

            // ✅ OTIMIZADO: Rateios em uma única linha
            if (temRateios)
            {
                html.AppendLine("<div class='info-line'>");
                html.AppendLine($"<span class='info-label'>Rateios Aplicados:</span> {fechamento.TotalRateios:C} | ");
                html.AppendLine("<span class='info-label'>Detalhamento:</span> ");

                var rateios = fechamento.ItensRateio
                    .OrderBy(r => r.RegraRateio.Nome)
                    .Select(rateio => $"{rateio.RegraRateio.CentroCustoDestino.Nome}: {rateio.Percentual:F2}% = {rateio.ValorRateio:C}");

                html.AppendLine(string.Join("; ", rateios));
                html.AppendLine("</div>");
            }

            html.AppendLine("</div>"); // Fim info-complementar
        }

        private void GerarDetalhamentoLadoALado(StringBuilder html, FechamentoPeriodo fechamento)
        {
            // Agrupar entradas e saídas por origem
            var entradasPorOrigem = new Dictionary<string, (List<DetalheFechamento> detalhes, bool ehSede)>();
            var saidasPorOrigem = new Dictionary<string, (List<DetalheFechamento> detalhes, bool ehSede)>();

            // SEDE
            var entradasSede = fechamento.DetalhesFechamento
                .Where(d => d.TipoMovimento == "Entrada")
                .OrderBy(d => d.Data)
                .ToList();

            var saidasSede = fechamento.DetalhesFechamento
                .Where(d => d.TipoMovimento == "Saida")
                .OrderBy(d => d.Data)
                .ToList();

            if (entradasSede.Any())
                entradasPorOrigem[fechamento.CentroCusto.Nome] = (entradasSede, true);

            if (saidasSede.Any())
                saidasPorOrigem[fechamento.CentroCusto.Nome] = (saidasSede, true);

            // Congregações
            if (fechamento.FechamentosCongregacoesIncluidos?.Any() == true)
            {
                foreach (var congFechamento in fechamento.FechamentosCongregacoesIncluidos.OrderBy(c => c.CentroCusto.Nome))
                {
                    var entradasCong = congFechamento.DetalhesFechamento
                        .Where(d => d.TipoMovimento == "Entrada")
                        .OrderBy(d => d.Data)
                        .ToList();

                    var saidasCong = congFechamento.DetalhesFechamento
                        .Where(d => d.TipoMovimento == "Saida")
                        .OrderBy(d => d.Data)
                        .ToList();

                    if (entradasCong.Any())
                        entradasPorOrigem[congFechamento.CentroCusto.Nome] = (entradasCong, false);

                    if (saidasCong.Any())
                        saidasPorOrigem[congFechamento.CentroCusto.Nome] = (saidasCong, false);
                }
            }

            // Layout em 2 colunas
            html.AppendLine("<div class='two-columns'>");

            // COLUNA ESQUERDA: ENTRADAS
            html.AppendLine("<div class='column'>");
            html.AppendLine("<div class='column-header column-header-success'>📥 ENTRADAS (RECEITAS)</div>");

            if (!entradasPorOrigem.Any())
            {
                html.AppendLine("<div class='no-data'>Nenhuma entrada registrada.</div>");
            }
            else
            {
                decimal totalGeralEntradas = 0;

                foreach (var origem in entradasPorOrigem)
                {
                    var separatorClass = origem.Value.ehSede ? "sede-separator" : "congregacao-separator";
                    var icone = origem.Value.ehSede ? "🏛️" : "📍";

                    html.AppendLine($"<div class='{separatorClass}'>{icone} {origem.Key}</div>");

                    html.AppendLine("<table class='table-striped'>");
                    html.AppendLine("<thead>");
                    html.AppendLine("<tr>");
                    html.AppendLine("<th style='width: 40px;'>Data</th>");
                    html.AppendLine("<th>Fonte/Descrição</th>");
                    html.AppendLine("<th class='currency' style='width: 50px;'>Valor</th>");
                    html.AppendLine("</tr>");
                    html.AppendLine("</thead>");
                    html.AppendLine("<tbody>");

                    decimal subtotal = 0;

                    foreach (var entrada in origem.Value.detalhes)
                    {
                        html.AppendLine("<tr>");
                        html.AppendLine($"<td>{entrada.Data:dd/MM}</td>");
                        var descricaoCompleta = !string.IsNullOrEmpty(entrada.PlanoContas)
                            ? $"{entrada.PlanoContas}: {entrada.Descricao ?? ""}"
                            : entrada.Descricao ?? "N/A";
                        html.AppendLine($"<td>{descricaoCompleta}</td>");
                        html.AppendLine($"<td class='currency text-success'>{entrada.Valor:C}</td>");
                        html.AppendLine("</tr>");

                        subtotal += entrada.Valor;
                    }

                    html.AppendLine("<tr class='totals-row'>");
                    html.AppendLine($"<td colspan='2'>Subtotal:</td>");
                    html.AppendLine($"<td class='currency text-success'>{subtotal:C}</td>");
                    html.AppendLine("</tr>");

                    html.AppendLine("</tbody>");
                    html.AppendLine("</table>");

                    totalGeralEntradas += subtotal;
                }

                if (entradasPorOrigem.Count > 1)
                {
                    html.AppendLine("<table>");
                    html.AppendLine("<tr class='totals-row'>");
                    html.AppendLine($"<td colspan='2' style='text-align: right;'><strong>TOTAL GERAL:</strong></td>");
                    html.AppendLine($"<td class='currency text-success' style='width: 50px;'><strong>{totalGeralEntradas:C}</strong></td>");
                    html.AppendLine("</tr>");
                    html.AppendLine("</table>");
                }
            }

            html.AppendLine("</div>"); // Fim coluna esquerda

            // COLUNA DIREITA: SAÍDAS
            html.AppendLine("<div class='column'>");
            html.AppendLine("<div class='column-header column-header-danger'>📤 SAÍDAS (DESPESAS)</div>");

            if (!saidasPorOrigem.Any())
            {
                html.AppendLine("<div class='no-data'>Nenhuma saída registrada.</div>");
            }
            else
            {
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
                    html.AppendLine("<th style='width: 40px;'>Data</th>");
                    html.AppendLine("<th>Categoria/Descrição</th>");
                    html.AppendLine("<th class='currency' style='width: 50px;'>Valor</th>");
                    html.AppendLine("</tr>");
                    html.AppendLine("</thead>");
                    html.AppendLine("<tbody>");

                    decimal subtotal = 0;

                    foreach (var saida in origem.Value.detalhes)
                    {
                        html.AppendLine("<tr>");
                        html.AppendLine($"<td>{saida.Data:dd/MM}</td>");
                        var descricaoCompleta = !string.IsNullOrEmpty(saida.PlanoContas)
                            ? $"{saida.PlanoContas}: {saida.Descricao ?? ""}"
                            : saida.Descricao ?? "N/A";
                        html.AppendLine($"<td>{descricaoCompleta}</td>");
                        html.AppendLine($"<td class='currency text-danger'>{saida.Valor:C}</td>");
                        html.AppendLine("</tr>");

                        subtotal += saida.Valor;
                    }

                    html.AppendLine("<tr class='totals-row'>");
                    html.AppendLine($"<td colspan='2'>Subtotal:</td>");
                    html.AppendLine($"<td class='currency text-danger'>{subtotal:C}</td>");
                    html.AppendLine("</tr>");

                    html.AppendLine("</tbody>");
                    html.AppendLine("</table>");

                    totalGeralSaidas += subtotal;
                }

                if (saidasPorOrigem.Count > 1)
                {
                    html.AppendLine("<table>");
                    html.AppendLine("<tr class='totals-row'>");
                    html.AppendLine($"<td colspan='2' style='text-align: right;'><strong>TOTAL GERAL:</strong></td>");
                    html.AppendLine($"<td class='currency text-danger' style='width: 50px;'><strong>{totalGeralSaidas:C}</strong></td>");
                    html.AppendLine("</tr>");
                    html.AppendLine("</table>");
                }
            }

            html.AppendLine("</div>"); // Fim coluna direita

            html.AppendLine("</div>"); // Fim two-columns
        }

        private void GerarAssinaturas(StringBuilder html, FechamentoPeriodo fechamento)
        {
            html.AppendLine("<div class='signature-section'>");
            html.AppendLine("<div style='text-align: center; margin-top: 25px;'>");

            // Apenas Tesoureiro Responsável
            html.AppendLine("<div style='display: inline-block; width: 50%; text-align: center; border-top: 1px solid #333; padding-top: 4px; font-size: 8px;'>");
            html.AppendLine($"<div>{fechamento.UsuarioSubmissao?.NomeCompleto ?? "___________________________"}</div>");
            html.AppendLine("<div style='font-size: 7px; color: #666; margin-top: 2px;'>Tesoureiro Responsável</div>");
            html.AppendLine("</div>");

            html.AppendLine("</div>");
            html.AppendLine("</div>");
        }
    }
}