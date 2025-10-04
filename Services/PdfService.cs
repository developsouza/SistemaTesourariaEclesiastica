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
            html.AppendLine("<title>Recibo de Fechamento</title>");
            html.AppendLine("<style>");
            html.AppendLine(@"
        @page { 
            size: A4; 
            margin: 15mm 10mm 10mm 10mm; 
        }
        body { 
            font-family: Arial, sans-serif; 
            margin: 0;
            padding: 0;
            font-size: 10px;
            line-height: 1.2;
        }
        .header { 
            text-align: center; 
            margin-bottom: 15px; 
            border-bottom: 1px solid #333;
            padding-bottom: 8px;
        }
        .title { 
            font-size: 16px; 
            font-weight: bold; 
            color: #333;
            margin: 0;
        }
        .subtitle { 
            font-size: 12px; 
            color: #666;
            margin: 2px 0;
        }
        .info-section-full { 
            margin: 8px 0; 
            padding: 8px;
            background-color: #f9f9f9;
            border-radius: 3px;
            width: 100%;
        }
        .info-title {
            font-size: 11px;
            font-weight: bold;
            color: #333;
            margin-bottom: 5px;
            border-bottom: 1px solid #ddd;
            padding-bottom: 2px;
        }
        .info-grid {
            display: grid;
            grid-template-columns: 1fr 1fr 1fr 1fr;
            gap: 10px;
            margin-top: 5px;
        }
        .info-item {
            display: flex;
            flex-direction: column;
        }
        .info-label { 
            font-weight: bold;
            color: #555;
            font-size: 9px;
            margin-bottom: 2px;
        }
        .info-value { 
            color: #333;
            font-size: 9px;
        }
        .table { 
            width: 100%; 
            border-collapse: collapse; 
            margin: 5px 0;
            font-size: 9px;
        }
        .table th, .table td { 
            border: 1px solid #ddd; 
            padding: 4px; 
            text-align: left;
        }
        .table th { 
            background-color: #333; 
            color: white;
            font-weight: bold;
            font-size: 8px;
        }
        .table-striped tbody tr:nth-child(even) {
            background-color: #f9f9f9;
        }
        .currency { 
            text-align: right;
        }
        .summary {
            background-color: #e8f4f8;
            padding: 8px;
            border-radius: 3px;
            margin: 8px 0;
        }
        .summary-title {
            font-size: 11px;
            font-weight: bold;
            color: #0066cc;
            margin-bottom: 5px;
        }
        .summary-row {
            display: flex;
            justify-content: space-between;
            margin: 3px 0;
            padding: 2px 0;
            font-size: 10px;
        }
        .summary-label {
            font-weight: bold;
            color: #333;
        }
        .summary-value {
            font-weight: bold;
            color: #0066cc;
            font-size: 11px;
        }
        .summary-value-highlight {
            font-weight: bold;
            font-size: 12px;
            padding: 2px 4px;
            border-radius: 3px;
        }
        .highlight-box {
            background-color: #fff3cd;
            border-left: 3px solid #ffc107;
            padding: 8px;
            margin: 8px 0;
        }
        .info-box {
            background-color: #d1ecf1;
            border-left: 3px solid #17a2b8;
            padding: 8px;
            margin: 8px 0;
        }
        .footer { 
            margin-top: 15px; 
            text-align: center; 
            font-size: 8px; 
            color: #666;
            border-top: 1px solid #ddd;
            padding-top: 8px;
        }
        .signature-center { 
            margin-top: 30px; 
            text-align: center;
        }
        .signature-line { 
            border-top: 1px solid #333; 
            margin: 25px auto 0 auto; 
            padding-top: 3px;
            width: 300px;
            font-size: 9px;
        }
        .text-danger { color: #dc3545; }
        .text-success { color: #28a745; }
        .text-warning { color: #ffc107; }
        .text-info { color: #17a2b8; }
        .fw-bold { font-weight: bold; }
        .fs-small { font-size: 10px; }
        .compact-table {
            margin: 3px 0;
        }
        .compact-table td {
            padding: 2px 4px;
        }
        .note {
            margin: 3px 0 0 0; 
            font-size: 8px;
        }
        .balance-section {
            display: flex;
            gap: 15px;
            margin: 10px 0;
        }
        .balance-column {
            flex: 1;
        }
        .info-section { 
            margin: 8px 0; 
            padding: 8px;
            background-color: #f9f9f9;
            border-radius: 3px;
        }
    ");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // Header
            html.AppendLine("<div class='header'>");
            html.AppendLine("<div class='title'>RECIBO DE FECHAMENTO DE PERÍODO</div>");
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
            html.AppendLine("</div>");

            // Informações Gerais - ocupando toda a largura
            html.AppendLine("<div class='info-section-full'>");
            html.AppendLine("<div class='info-title'>INFORMAÇÕES GERAIS</div>");
            html.AppendLine("<div class='info-grid'>");

            html.AppendLine("<div class='info-item'>");
            html.AppendLine("<span class='info-label'>Centro de Custo:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.CentroCusto.Nome}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-item'>");
            html.AppendLine("<span class='info-label'>Tipo:</span>");
            html.AppendLine($"<span class='info-value'>{(fechamento.TipoFechamento == TipoFechamento.Diario ? "Diário" : fechamento.TipoFechamento == TipoFechamento.Semanal ? "Semanal" : "Mensal")}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-item'>");
            html.AppendLine("<span class='info-label'>Período:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.DataInicio:dd/MM/yyyy} - {fechamento.DataFim:dd/MM/yyyy}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-item'>");
            html.AppendLine("<span class='info-label'>Status:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.Status}</span>");
            html.AppendLine("</div>");

            html.AppendLine("</div>"); // Fim info-grid
            html.AppendLine("</div>"); // Fim info-section-full

            // Seção de Balanços lado a lado
            html.AppendLine("<div class='balance-section'>");

            // Balanço Físico
            html.AppendLine("<div class='balance-column'>");
            html.AppendLine("<div class='highlight-box'>");
            html.AppendLine("<div class='info-title'>BALANÇO FÍSICO (DINHEIRO)</div>");
            html.AppendLine("<table class='table compact-table'>");
            html.AppendLine("<tr>");
            html.AppendLine("<td><strong>Entradas:</strong></td>");
            html.AppendLine($"<td class='currency text-success fw-bold'>{fechamento.TotalEntradasFisicas:C}</td>");
            html.AppendLine("</tr>");
            html.AppendLine("<tr>");
            html.AppendLine("<td><strong>Saídas:</strong></td>");
            html.AppendLine($"<td class='currency text-danger fw-bold'>{fechamento.TotalSaidasFisicas:C}</td>");
            html.AppendLine("</tr>");
            html.AppendLine("<tr style='background-color: #fff3cd;'>");
            html.AppendLine("<td><strong>= TOTAL:</strong></td>");
            html.AppendLine($"<td class='currency fw-bold fs-small text-warning'>{fechamento.BalancoFisico:C}</td>");
            html.AppendLine("</tr>");
            html.AppendLine("</table>");
            html.AppendLine("<p class='note'>Valor a repassar fisicamente.</p>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            // Balanço Digital - SIMPLIFICADO (sem rateios e valor final)
            html.AppendLine("<div class='balance-column'>");
            html.AppendLine("<div class='info-box'>");
            html.AppendLine("<div class='info-title'>BALANÇO DIGITAL</div>");
            html.AppendLine("<table class='table compact-table'>");
            html.AppendLine("<tr>");
            html.AppendLine("<td><strong>Entradas:</strong></td>");
            html.AppendLine($"<td class='currency text-success fw-bold'>{fechamento.TotalEntradasDigitais:C}</td>");
            html.AppendLine("</tr>");
            html.AppendLine("<tr>");
            html.AppendLine("<td><strong>Saídas:</strong></td>");
            html.AppendLine($"<td class='currency text-danger fw-bold'>{fechamento.TotalSaidasDigitais:C}</td>");
            html.AppendLine("</tr>");
            html.AppendLine("<tr style='background-color: #d1ecf1;'>");
            html.AppendLine("<td><strong>Subtotal:</strong></td>");
            html.AppendLine($"<td class='currency fw-bold fs-small text-info'>{fechamento.BalancoDigital:C}</td>");
            html.AppendLine("</tr>");
            html.AppendLine("</table>");
            html.AppendLine("<p class='note'>Já depositado eletronicamente.</p>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            html.AppendLine("</div>"); // Fim balance-section

            // Rateios (se houver) - versão compacta
            if (fechamento.ItensRateio.Any())
            {
                html.AppendLine("<div class='info-section'>");
                html.AppendLine("<div class='info-title'>RATEIOS APLICADOS</div>");
                html.AppendLine("<table class='table table-striped compact-table'>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th>Regra</th>");
                html.AppendLine("<th>Destino</th>");
                html.AppendLine("<th>%</th>");
                html.AppendLine("<th>Valor</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");

                foreach (var rateio in fechamento.ItensRateio.OrderBy(r => r.RegraRateio.Nome))
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{rateio.RegraRateio.Nome}</td>");
                    html.AppendLine($"<td>{rateio.RegraRateio.CentroCustoDestino.Nome}</td>");
                    html.AppendLine($"<td class='currency'>{rateio.Percentual:F1}%</td>");
                    html.AppendLine($"<td class='currency fw-bold'>{rateio.ValorRateio:C}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</tbody>");
                html.AppendLine("</table>");
                html.AppendLine("</div>");
            }

            // Resumo Final - usando o mesmo layout em grid das Informações Gerais
            html.AppendLine("<div class='summary'>");
            html.AppendLine("<div class='summary-title'>RESUMO FINAL</div>");

            // Determinar quantas colunas usar baseado na presença de rateios
            var gridColumns = fechamento.TotalRateios > 0 ? "1fr 1fr 1fr 1fr 1fr" : "1fr 1fr 1fr 1fr";
            html.AppendLine($"<div class='info-grid' style='grid-template-columns: {gridColumns};'>");

            html.AppendLine("<div class='info-item'>");
            html.AppendLine("<span class='info-label'>Total Entradas:</span>");
            html.AppendLine($"<span class='summary-value-highlight text-success'>{fechamento.TotalEntradas:C}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-item'>");
            html.AppendLine("<span class='info-label'>Total Saídas:</span>");
            html.AppendLine($"<span class='summary-value-highlight text-danger'>{fechamento.TotalSaidas:C}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='info-item'>");
            html.AppendLine("<span class='info-label'>Físico (Repassar):</span>");
            html.AppendLine($"<span class='summary-value-highlight text-warning'>{fechamento.BalancoFisico:C}</span>");
            html.AppendLine("</div>");

            // Se houver rateios, adicionar como quarto item
            if (fechamento.TotalRateios > 0)
            {
                html.AppendLine("<div class='info-item'>");
                html.AppendLine("<span class='info-label'>(-) Rateios Aplicados:</span>");
                html.AppendLine($"<span class='summary-value-highlight text-danger'>- {fechamento.TotalRateios:C}</span>");
                html.AppendLine("</div>");
            }

            html.AppendLine("<div class='info-item'>");
            html.AppendLine("<span class='info-label'>Valor do repasse:</span>");
            html.AppendLine($"<span class='summary-value-highlight text-info'>{(fechamento.TotalRateios > 0 ? fechamento.SaldoFinal : fechamento.BalancoDigital):C}</span>");
            html.AppendLine("</div>");

            html.AppendLine("</div>"); // Fim info-grid
            html.AppendLine("</div>"); // Fim summary

            // Observações (se houver) - compactas
            if (!string.IsNullOrEmpty(fechamento.Observacoes))
            {
                html.AppendLine("<div class='info-section'>");
                html.AppendLine("<div class='info-title'>OBSERVAÇÕES</div>");
                html.AppendLine($"<p style='font-size: 8px; margin: 0;'>{fechamento.Observacoes}</p>");
                html.AppendLine("</div>");
            }

            // Assinatura centralizada - apenas tesoureiro
            html.AppendLine("<div class='signature-center'>");
            html.AppendLine("<div class='signature-line'>");
            html.AppendLine($"{fechamento.UsuarioSubmissao.NomeCompleto}");
            html.AppendLine("<br>Tesoureiro Responsável");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            // Footer compacto
            html.AppendLine("<div class='footer'>");
            html.AppendLine($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm} - Sistema de Tesouraria Eclesiástica");
            html.AppendLine("</div>");

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }
    }
}
