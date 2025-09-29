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
        body { 
            font-family: Arial, sans-serif; 
            margin: 20px;
            font-size: 12px;
        }
        .header { 
            text-align: center; 
            margin-bottom: 30px; 
            border-bottom: 2px solid #333;
            padding-bottom: 10px;
        }
        .title { 
            font-size: 24px; 
            font-weight: bold; 
            color: #333;
        }
        .subtitle { 
            font-size: 16px; 
            color: #666;
            margin-top: 5px;
        }
        .info-section { 
            margin: 20px 0; 
            padding: 15px;
            background-color: #f9f9f9;
            border-radius: 5px;
        }
        .info-title {
            font-size: 14px;
            font-weight: bold;
            color: #333;
            margin-bottom: 10px;
            border-bottom: 1px solid #ddd;
            padding-bottom: 5px;
        }
        .info-row { 
            margin: 8px 0;
            display: flex;
            justify-content: space-between;
        }
        .info-label { 
            font-weight: bold;
            color: #555;
        }
        .info-value { 
            color: #333;
        }
        .table { 
            width: 100%; 
            border-collapse: collapse; 
            margin: 15px 0;
        }
        .table th, .table td { 
            border: 1px solid #ddd; 
            padding: 8px; 
            text-align: left;
        }
        .table th { 
            background-color: #333; 
            color: white;
            font-weight: bold;
        }
        .table-striped tbody tr:nth-child(even) {
            background-color: #f9f9f9;
        }
        .currency { 
            text-align: right;
        }
        .summary {
            background-color: #e8f4f8;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
        }
        .summary-title {
            font-size: 16px;
            font-weight: bold;
            color: #0066cc;
            margin-bottom: 10px;
        }
        .summary-row {
            display: flex;
            justify-content: space-between;
            margin: 8px 0;
            padding: 5px 0;
        }
        .summary-label {
            font-weight: bold;
            color: #333;
        }
        .summary-value {
            font-weight: bold;
            color: #0066cc;
        }
        .highlight-box {
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin: 15px 0;
        }
        .success-box {
            background-color: #d4edda;
            border-left: 4px solid #28a745;
            padding: 15px;
            margin: 15px 0;
        }
        .info-box {
            background-color: #d1ecf1;
            border-left: 4px solid #17a2b8;
            padding: 15px;
            margin: 15px 0;
        }
        .footer { 
            margin-top: 40px; 
            text-align: center; 
            font-size: 10px; 
            color: #666;
            border-top: 1px solid #ddd;
            padding-top: 20px;
        }
        .signatures { 
            margin-top: 50px; 
            display: flex; 
            justify-content: space-between;
        }
        .signature { 
            text-align: center; 
            width: 45%;
        }
        .signature-line { 
            border-top: 1px solid #333; 
            margin-top: 50px; 
            padding-top: 5px;
        }
        .text-danger { color: #dc3545; }
        .text-success { color: #28a745; }
        .text-warning { color: #ffc107; }
        .text-info { color: #17a2b8; }
        .fw-bold { font-weight: bold; }
        .fs-5 { font-size: 1.25rem; }
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

            // Informações Gerais
            html.AppendLine("<div class='info-section'>");
            html.AppendLine("<div class='info-title'>INFORMAÇÕES GERAIS</div>");
            html.AppendLine("<div class='info-row'>");
            html.AppendLine("<span class='info-label'>Centro de Custo:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.CentroCusto.Nome}</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='info-row'>");
            html.AppendLine("<span class='info-label'>Tipo de Fechamento:</span>");
            html.AppendLine($"<span class='info-value'>{(fechamento.TipoFechamento == TipoFechamento.Diario ? "Fechamento Diário" : fechamento.TipoFechamento == TipoFechamento.Semanal ? "Fechamento Semanal" : "Fechamento Mensal")}</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='info-row'>");
            html.AppendLine("<span class='info-label'>Data de Início:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.DataInicio:dd/MM/yyyy}</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='info-row'>");
            html.AppendLine("<span class='info-label'>Data de Fim:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.DataFim:dd/MM/yyyy}</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='info-row'>");
            html.AppendLine("<span class='info-label'>Status:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.Status}</span>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            // ========================================
            // DISCRIMINAÇÃO DO BALANÇO FÍSICO
            // ========================================
            html.AppendLine("<div class='highlight-box'>");
            html.AppendLine("<div class='info-title'>📦 DISCRIMINAÇÃO DO BALANÇO FÍSICO (DINHEIRO EM ESPÉCIE)</div>");
            html.AppendLine("<table class='table'>");
            html.AppendLine("<tr>");
            html.AppendLine("<td><strong>Entradas Físicas (Dinheiro):</strong></td>");
            html.AppendLine($"<td class='currency text-success fw-bold'>+ {fechamento.TotalEntradasFisicas:C}</td>");
            html.AppendLine("</tr>");
            html.AppendLine("<tr>");
            html.AppendLine("<td><strong>Saídas Físicas (Dinheiro):</strong></td>");
            html.AppendLine($"<td class='currency text-danger fw-bold'>- {fechamento.TotalSaidasFisicas:C}</td>");
            html.AppendLine("</tr>");
            html.AppendLine("<tr style='background-color: #fff3cd;'>");
            html.AppendLine("<td><strong>= BALANÇO FÍSICO TOTAL:</strong></td>");
            html.AppendLine($"<td class='currency fw-bold fs-5 text-warning'>{fechamento.BalancoFisico:C}</td>");
            html.AppendLine("</tr>");
            html.AppendLine("</table>");
            html.AppendLine("<p style='margin: 10px 0 0 0; font-size: 11px;'><strong>⚠️ ATENÇÃO:</strong> Este é o valor em dinheiro vivo que precisa ser fisicamente repassado para a Sede.</p>");
            html.AppendLine("</div>");

            // ========================================
            // DISCRIMINAÇÃO DO BALANÇO DIGITAL
            // ========================================
            html.AppendLine("<div class='info-box'>");
            html.AppendLine("<div class='info-title'>💳 DISCRIMINAÇÃO DO BALANÇO DIGITAL (PIX, TRANSFERÊNCIAS, CARTÕES)</div>");
            html.AppendLine("<table class='table'>");
            html.AppendLine("<tr>");
            html.AppendLine("<td><strong>Entradas Digitais (PIX/Cartões/Transferências):</strong></td>");
            html.AppendLine($"<td class='currency text-success fw-bold'>+ {fechamento.TotalEntradasDigitais:C}</td>");
            html.AppendLine("</tr>");
            html.AppendLine("<tr>");
            html.AppendLine("<td><strong>Saídas Digitais (PIX/Cartões/Transferências):</strong></td>");
            html.AppendLine($"<td class='currency text-danger fw-bold'>- {fechamento.TotalSaidasDigitais:C}</td>");
            html.AppendLine("</tr>");
            html.AppendLine("<tr style='background-color: #e7f3ff;'>");
            html.AppendLine("<td><strong>= SUBTOTAL DIGITAL:</strong></td>");
            html.AppendLine($"<td class='currency fw-bold'>{fechamento.BalancoDigital:C}</td>");
            html.AppendLine("</tr>");

            if (fechamento.TotalRateios > 0)
            {
                html.AppendLine("<tr>");
                html.AppendLine("<td><strong>(-) Rateios Aplicados:</strong></td>");
                html.AppendLine($"<td class='currency text-danger fw-bold'>- {fechamento.TotalRateios:C}</td>");
                html.AppendLine("</tr>");
                html.AppendLine("<tr style='background-color: #d1ecf1;'>");
                html.AppendLine("<td><strong>= SALDO FINAL (após rateios):</strong></td>");
                html.AppendLine($"<td class='currency fw-bold fs-5 text-info'>{fechamento.SaldoFinal:C}</td>");
                html.AppendLine("</tr>");
            }
            else
            {
                html.AppendLine("<tr style='background-color: #d1ecf1;'>");
                html.AppendLine("<td><strong>= BALANÇO DIGITAL TOTAL:</strong></td>");
                html.AppendLine($"<td class='currency fw-bold fs-5 text-info'>{fechamento.BalancoDigital:C}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</table>");
            html.AppendLine("<p style='margin: 10px 0 0 0; font-size: 11px;'><strong>ℹ️ INFORMATIVO:</strong> Estes valores já foram depositados eletronicamente e não requerem repasse físico.</p>");
            html.AppendLine("</div>");

            // ========================================
            // RATEIOS APLICADOS (SE HOUVER)
            // ========================================
            if (fechamento.ItensRateio.Any())
            {
                html.AppendLine("<div class='info-section'>");
                html.AppendLine("<div class='info-title'>📊 RATEIOS APLICADOS SOBRE O BALANÇO DIGITAL</div>");
                html.AppendLine($"<p style='margin-bottom: 15px;'><strong>Base de Cálculo:</strong> {fechamento.BalancoDigital:C} (Balanço Digital)</p>");
                html.AppendLine("<table class='table table-striped'>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th>Regra de Rateio</th>");
                html.AppendLine("<th>Centro de Custo Destino</th>");
                html.AppendLine("<th style='text-align: right;'>Percentual</th>");
                html.AppendLine("<th style='text-align: right;'>Valor Base</th>");
                html.AppendLine("<th style='text-align: right;'>Valor Rateado</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");

                foreach (var rateio in fechamento.ItensRateio.OrderBy(r => r.RegraRateio.Nome))
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{rateio.RegraRateio.Nome}</td>");
                    html.AppendLine($"<td>{rateio.RegraRateio.CentroCustoDestino.Nome}</td>");
                    html.AppendLine($"<td class='currency'>{rateio.Percentual:F2}%</td>");
                    html.AppendLine($"<td class='currency'>{rateio.ValorBase:C}</td>");
                    html.AppendLine($"<td class='currency fw-bold'>{rateio.ValorRateio:C}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</tbody>");
                html.AppendLine("<tfoot style='background-color: #f0f0f0;'>");
                html.AppendLine("<tr>");
                html.AppendLine("<td colspan='4' style='text-align: right;'><strong>TOTAL DE RATEIOS:</strong></td>");
                html.AppendLine($"<td class='currency fw-bold fs-5'>{fechamento.TotalRateios:C}</td>");
                html.AppendLine("</tr>");
                html.AppendLine("</tfoot>");
                html.AppendLine("</table>");
                html.AppendLine("</div>");
            }

            // ========================================
            // RESUMO FINANCEIRO FINAL
            // ========================================
            html.AppendLine("<div class='summary'>");
            html.AppendLine("<div class='summary-title'>💰 RESUMO FINANCEIRO FINAL</div>");
            html.AppendLine("<div class='summary-row'>");
            html.AppendLine("<span class='summary-label'>Total de Entradas (Físico + Digital):</span>");
            html.AppendLine($"<span class='summary-value'>{fechamento.TotalEntradas:C}</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='summary-row'>");
            html.AppendLine("<span class='summary-label'>Total de Saídas (Físico + Digital):</span>");
            html.AppendLine($"<span class='summary-value'>{fechamento.TotalSaidas:C}</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='summary-row'>");
            html.AppendLine("<span class='summary-label'>Balanço Físico (A repassar):</span>");
            html.AppendLine($"<span class='summary-value text-warning'>{fechamento.BalancoFisico:C}</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='summary-row'>");
            html.AppendLine("<span class='summary-label'>Balanço Digital (Informativo):</span>");
            html.AppendLine($"<span class='summary-value text-info'>{fechamento.BalancoDigital:C}</span>");
            html.AppendLine("</div>");
            if (fechamento.TotalRateios > 0)
            {
                html.AppendLine("<div class='summary-row'>");
                html.AppendLine("<span class='summary-label'>Total de Rateios:</span>");
                html.AppendLine($"<span class='summary-value text-danger'>{fechamento.TotalRateios:C}</span>");
                html.AppendLine("</div>");
                html.AppendLine("<div class='summary-row'>");
                html.AppendLine("<span class='summary-label'>Saldo Final (pós-rateio):</span>");
                html.AppendLine($"<span class='summary-value'>{fechamento.SaldoFinal:C}</span>");
                html.AppendLine("</div>");
            }
            html.AppendLine("</div>");

            // Observações
            if (!string.IsNullOrEmpty(fechamento.Observacoes))
            {
                html.AppendLine("<div class='info-section'>");
                html.AppendLine("<div class='info-title'>OBSERVAÇÕES</div>");
                html.AppendLine($"<p>{fechamento.Observacoes}</p>");
                html.AppendLine("</div>");
            }

            // Assinaturas
            html.AppendLine("<div class='signatures'>");
            html.AppendLine("<div class='signature'>");
            html.AppendLine("<div class='signature-line'>");
            html.AppendLine($"{fechamento.UsuarioSubmissao.NomeCompleto}");
            html.AppendLine("<br>Tesoureiro Responsável");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            if (fechamento.UsuarioAprovacao != null)
            {
                html.AppendLine("<div class='signature'>");
                html.AppendLine("<div class='signature-line'>");
                html.AppendLine($"{fechamento.UsuarioAprovacao.NomeCompleto}");
                html.AppendLine("<br>Aprovado por");
                html.AppendLine("</div>");
                html.AppendLine("</div>");
            }
            html.AppendLine("</div>");

            // Footer
            html.AppendLine("<div class='footer'>");
            html.AppendLine($"Documento gerado em {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            html.AppendLine("<br>Sistema de Tesouraria Eclesiástica");
            html.AppendLine("</div>");

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        //private string GerarHtmlReciboFechamento(FechamentoPeriodo fechamento)
        //{
        //    var html = new StringBuilder();

        //    html.AppendLine("<!DOCTYPE html>");
        //    html.AppendLine("<html>");
        //    html.AppendLine("<head>");
        //    html.AppendLine("<meta charset='UTF-8'>");
        //    html.AppendLine("<title>Recibo de Fechamento</title>");
        //    html.AppendLine("<style>");
        //    html.AppendLine(@"
        //        body { 
        //            font-family: Arial, sans-serif; 
        //            margin: 20px; 
        //            font-size: 12px;
        //        }
        //        .header { 
        //            text-align: center; 
        //            margin-bottom: 30px; 
        //            border-bottom: 2px solid #333;
        //            padding-bottom: 20px;
        //        }
        //        .title { 
        //            font-size: 18px; 
        //            font-weight: bold; 
        //            margin-bottom: 10px;
        //        }
        //        .subtitle { 
        //            font-size: 14px; 
        //            color: #666;
        //        }
        //        .info-section { 
        //            margin: 20px 0; 
        //            padding: 15px;
        //            background-color: #f9f9f9;
        //            border: 1px solid #ddd;
        //        }
        //        .info-title { 
        //            font-weight: bold; 
        //            font-size: 14px;
        //            margin-bottom: 10px;
        //            color: #333;
        //        }
        //        .info-row { 
        //            display: flex; 
        //            justify-content: space-between; 
        //            margin: 5px 0;
        //            padding: 3px 0;
        //        }
        //        .info-label { 
        //            font-weight: bold; 
        //            width: 40%;
        //        }
        //        .info-value { 
        //            width: 60%;
        //            text-align: right;
        //        }
        //        .table { 
        //            width: 100%; 
        //            border-collapse: collapse; 
        //            margin: 20px 0;
        //        }
        //        .table th, .table td { 
        //            border: 1px solid #ddd; 
        //            padding: 8px; 
        //            text-align: left;
        //        }
        //        .table th { 
        //            background-color: #f2f2f2; 
        //            font-weight: bold;
        //        }
        //        .table .currency { 
        //            text-align: right;
        //        }
        //        .summary { 
        //            margin-top: 30px; 
        //            padding: 20px;
        //            background-color: #e8f4f8;
        //            border: 2px solid #2196F3;
        //        }
        //        .summary-title { 
        //            font-size: 16px; 
        //            font-weight: bold; 
        //            margin-bottom: 15px;
        //            text-align: center;
        //            color: #1976D2;
        //        }
        //        .summary-row { 
        //            display: flex; 
        //            justify-content: space-between; 
        //            margin: 8px 0;
        //            padding: 5px 0;
        //            border-bottom: 1px dotted #ccc;
        //        }
        //        .summary-label { 
        //            font-weight: bold;
        //        }
        //        .summary-value { 
        //            font-weight: bold;
        //            color: #1976D2;
        //        }
        //        .footer { 
        //            margin-top: 40px; 
        //            text-align: center; 
        //            font-size: 10px; 
        //            color: #666;
        //            border-top: 1px solid #ddd;
        //            padding-top: 20px;
        //        }
        //        .signatures { 
        //            margin-top: 50px; 
        //            display: flex; 
        //            justify-content: space-between;
        //        }
        //        .signature { 
        //            text-align: center; 
        //            width: 45%;
        //        }
        //        .signature-line { 
        //            border-top: 1px solid #333; 
        //            margin-top: 50px; 
        //            padding-top: 5px;
        //        }
        //    ");
        //    html.AppendLine("</style>");
        //    html.AppendLine("</head>");
        //    html.AppendLine("<body>");

        //    // Header
        //    html.AppendLine("<div class='header'>");
        //    html.AppendLine("<div class='title'>RECIBO DE FECHAMENTO DE PERÍODO</div>");
        //    html.AppendLine($"<div class='subtitle'>{fechamento.CentroCusto.Nome}</div>");
        //    html.AppendLine($"<div class='subtitle'>Período: {fechamento.Mes:00}/{fechamento.Ano}</div>");
        //    html.AppendLine("</div>");

        //    // Informações Gerais
        //    html.AppendLine("<div class='info-section'>");
        //    html.AppendLine("<div class='info-title'>INFORMAÇÕES GERAIS</div>");
        //    html.AppendLine("<div class='info-row'>");
        //    html.AppendLine("<span class='info-label'>Centro de Custo:</span>");
        //    html.AppendLine($"<span class='info-value'>{fechamento.CentroCusto.Nome}</span>");
        //    html.AppendLine("</div>");
        //    html.AppendLine("<div class='info-row'>");
        //    html.AppendLine("<span class='info-label'>Período:</span>");
        //    html.AppendLine($"<span class='info-value'>{fechamento.Mes:00}/{fechamento.Ano}</span>");
        //    html.AppendLine("</div>");
        //    html.AppendLine("<div class='info-row'>");
        //    html.AppendLine("<span class='info-label'>Data de Início:</span>");
        //    html.AppendLine($"<span class='info-value'>{fechamento.DataInicio:dd/MM/yyyy}</span>");
        //    html.AppendLine("</div>");
        //    html.AppendLine("<div class='info-row'>");
        //    html.AppendLine("<span class='info-label'>Data de Fim:</span>");
        //    html.AppendLine($"<span class='info-value'>{fechamento.DataFim:dd/MM/yyyy}</span>");
        //    html.AppendLine("</div>");
        //    html.AppendLine("<div class='info-row'>");
        //    html.AppendLine("<span class='info-label'>Data de Fechamento:</span>");
        //    html.AppendLine($"<span class='info-value'>{fechamento.DataSubmissao:dd/MM/yyyy HH:mm}</span>");
        //    html.AppendLine("</div>");
        //    html.AppendLine("<div class='info-row'>");
        //    html.AppendLine("<span class='info-label'>Responsável:</span>");
        //    html.AppendLine($"<span class='info-value'>{fechamento.UsuarioSubmissao.NomeCompleto}</span>");
        //    html.AppendLine("</div>");
        //    html.AppendLine("</div>");

        //    // Detalhes de Entradas
        //    if (fechamento.DetalhesFechamento.Any(d => d.TipoMovimento == "Entrada"))
        //    {
        //        html.AppendLine("<div class='info-section'>");
        //        html.AppendLine("<div class='info-title'>ENTRADAS DO PERÍODO</div>");
        //        html.AppendLine("<table class='table'>");
        //        html.AppendLine("<thead>");
        //        html.AppendLine("<tr>");
        //        html.AppendLine("<th>Data</th>");
        //        html.AppendLine("<th>Descrição</th>");
        //        html.AppendLine("<th>Plano de Contas</th>");
        //        html.AppendLine("<th>Membro</th>");
        //        html.AppendLine("<th>Valor</th>");
        //        html.AppendLine("</tr>");
        //        html.AppendLine("</thead>");
        //        html.AppendLine("<tbody>");

        //        foreach (var entrada in fechamento.DetalhesFechamento.Where(d => d.TipoMovimento == "Entrada").OrderBy(d => d.Data))
        //        {
        //            html.AppendLine("<tr>");
        //            html.AppendLine($"<td>{entrada.Data:dd/MM/yyyy}</td>");
        //            html.AppendLine($"<td>{entrada.Descricao}</td>");
        //            html.AppendLine($"<td>{entrada.PlanoContas ?? "-"}</td>");
        //            html.AppendLine($"<td>{entrada.Membro ?? "-"}</td>");
        //            html.AppendLine($"<td class='currency'>{entrada.Valor:C}</td>");
        //            html.AppendLine("</tr>");
        //        }

        //        html.AppendLine("</tbody>");
        //        html.AppendLine("</table>");
        //        html.AppendLine("</div>");
        //    }

        //    // Detalhes de Saídas
        //    if (fechamento.DetalhesFechamento.Any(d => d.TipoMovimento == "Saida"))
        //    {
        //        html.AppendLine("<div class='info-section'>");
        //        html.AppendLine("<div class='info-title'>SAÍDAS DO PERÍODO</div>");
        //        html.AppendLine("<table class='table'>");
        //        html.AppendLine("<thead>");
        //        html.AppendLine("<tr>");
        //        html.AppendLine("<th>Data</th>");
        //        html.AppendLine("<th>Descrição</th>");
        //        html.AppendLine("<th>Plano de Contas</th>");
        //        html.AppendLine("<th>Fornecedor</th>");
        //        html.AppendLine("<th>Valor</th>");
        //        html.AppendLine("</tr>");
        //        html.AppendLine("</thead>");
        //        html.AppendLine("<tbody>");

        //        foreach (var saida in fechamento.DetalhesFechamento.Where(d => d.TipoMovimento == "Saida").OrderBy(d => d.Data))
        //        {
        //            html.AppendLine("<tr>");
        //            html.AppendLine($"<td>{saida.Data:dd/MM/yyyy}</td>");
        //            html.AppendLine($"<td>{saida.Descricao}</td>");
        //            html.AppendLine($"<td>{saida.PlanoContas ?? "-"}</td>");
        //            html.AppendLine($"<td>{saida.Fornecedor ?? "-"}</td>");
        //            html.AppendLine($"<td class='currency'>{saida.Valor:C}</td>");
        //            html.AppendLine("</tr>");
        //        }

        //        html.AppendLine("</tbody>");
        //        html.AppendLine("</table>");
        //        html.AppendLine("</div>");
        //    }

        //    // Rateios
        //    if (fechamento.ItensRateio.Any())
        //    {
        //        html.AppendLine("<div class='info-section'>");
        //        html.AppendLine("<div class='info-title'>RATEIOS APLICADOS</div>");
        //        html.AppendLine("<table class='table'>");
        //        html.AppendLine("<thead>");
        //        html.AppendLine("<tr>");
        //        html.AppendLine("<th>Regra</th>");
        //        html.AppendLine("<th>Centro de Custo Destino</th>");
        //        html.AppendLine("<th>Percentual</th>");
        //        html.AppendLine("<th>Valor Base</th>");
        //        html.AppendLine("<th>Valor do Rateio</th>");
        //        html.AppendLine("</tr>");
        //        html.AppendLine("</thead>");
        //        html.AppendLine("<tbody>");

        //        foreach (var rateio in fechamento.ItensRateio.OrderBy(r => r.RegraRateio.Nome))
        //        {
        //            html.AppendLine("<tr>");
        //            html.AppendLine($"<td>{rateio.RegraRateio.Nome}</td>");
        //            html.AppendLine($"<td>{rateio.RegraRateio.CentroCustoDestino.Nome}</td>");
        //            html.AppendLine($"<td>{rateio.Percentual:F2}%</td>");
        //            html.AppendLine($"<td class='currency'>{rateio.ValorBase:C}</td>");
        //            html.AppendLine($"<td class='currency'>{rateio.ValorRateio:C}</td>");
        //            html.AppendLine("</tr>");
        //        }

        //        html.AppendLine("</tbody>");
        //        html.AppendLine("</table>");
        //        html.AppendLine("</div>");
        //    }

        //    // Cálculo do impacto dos rateios
        //    html.AppendLine("<div class='info-section'>");
        //    html.AppendLine("<div class='info-title'>IMPACTO DOS RATEIOS NO SALDO</div>");
        //    html.AppendLine("<table class='table'>");
        //    html.AppendLine("<tr>");
        //    html.AppendLine($"<td><strong>Balanço Digital Bruto:</strong></td>");
        //    html.AppendLine($"<td class='currency'>{(fechamento.BalancoDigital + fechamento.TotalRateios):C}</td>");
        //    html.AppendLine("</tr>");
        //    html.AppendLine("<tr>");
        //    html.AppendLine($"<td><strong>(-) Total de Rateios Aplicados:</strong></td>");
        //    html.AppendLine($"<td class='currency text-danger'>({fechamento.TotalRateios:C})</td>");
        //    html.AppendLine("</tr>");
        //    html.AppendLine("<tr class='table-primary'>");
        //    html.AppendLine($"<td><strong>= Saldo Final (pós-rateio):</strong></td>");
        //    html.AppendLine($"<td class='currency'><strong>{fechamento.SaldoFinal:C}</strong></td>");
        //    html.AppendLine("</tr>");
        //    html.AppendLine("<tr>");
        //    html.AppendLine($"<td><strong>Balanço Físico a Repassar:</strong></td>");
        //    html.AppendLine($"<td class='currency'><strong>{fechamento.BalancoFisico:C}</strong></td>");
        //    html.AppendLine("</tr>");
        //    html.AppendLine("</table>");
        //    html.AppendLine("</div>");

        //    // Resumo Financeiro
        //    html.AppendLine("<div class='summary'>");
        //    html.AppendLine("<div class='summary-title'>RESUMO FINANCEIRO</div>");
        //    html.AppendLine("<div class='summary-row'>");
        //    html.AppendLine("<span class='summary-label'>Total de Entradas:</span>");
        //    html.AppendLine($"<span class='summary-value'>{fechamento.TotalEntradas:C}</span>");
        //    html.AppendLine("</div>");
        //    html.AppendLine("<div class='summary-row'>");
        //    html.AppendLine("<span class='summary-label'>Total de Saídas:</span>");
        //    html.AppendLine($"<span class='summary-value'>{fechamento.TotalSaidas:C}</span>");
        //    html.AppendLine("</div>");
        //    html.AppendLine("<div class='summary-row'>");
        //    html.AppendLine("<span class='summary-label'>Total de Rateios:</span>");
        //    html.AppendLine($"<span class='summary-value'>{fechamento.TotalRateios:C}</span>");
        //    html.AppendLine("</div>");
        //    html.AppendLine("<div class='summary-row'>");
        //    html.AppendLine("<span class='summary-label'>Balanço Digital:</span>");
        //    html.AppendLine($"<span class='summary-value'>{fechamento.BalancoDigital:C}</span>");
        //    html.AppendLine("</div>");
        //    html.AppendLine("<div class='summary-row'>");
        //    html.AppendLine("<span class='summary-label'>Balanço Físico:</span>");
        //    html.AppendLine($"<span class='summary-value'>{fechamento.BalancoFisico:C}</span>");
        //    html.AppendLine("</div>");
        //    html.AppendLine("<div class='summary-row'>");
        //    html.AppendLine("<span class='summary-label'>Diferença:</span>");
        //    var diferenca = fechamento.BalancoFisico - fechamento.BalancoDigital;
        //    html.AppendLine($"<span class='summary-value'>{diferenca:C}</span>");
        //    html.AppendLine("</div>");
        //    html.AppendLine("<div class='summary-row'>");
        //    html.AppendLine("<span class='summary-label'>Saldo Final:</span>");
        //    html.AppendLine($"<span class='summary-value'>{fechamento.SaldoFinal:C}</span>");
        //    html.AppendLine("</div>");
        //    html.AppendLine("</div>");

        //    // Observações
        //    if (!string.IsNullOrEmpty(fechamento.Observacoes))
        //    {
        //        html.AppendLine("<div class='info-section'>");
        //        html.AppendLine("<div class='info-title'>OBSERVAÇÕES</div>");
        //        html.AppendLine($"<p>{fechamento.Observacoes}</p>");
        //        html.AppendLine("</div>");
        //    }

        //    // Assinaturas
        //    html.AppendLine("<div class='signatures'>");
        //    html.AppendLine("<div class='signature'>");
        //    html.AppendLine("<div class='signature-line'>");
        //    html.AppendLine($"{fechamento.UsuarioSubmissao.NomeCompleto}");
        //    html.AppendLine("<br>Tesoureiro Responsável");
        //    html.AppendLine("</div>");
        //    html.AppendLine("</div>");

        //    if (fechamento.UsuarioAprovacao != null)
        //    {
        //        html.AppendLine("<div class='signature'>");
        //        html.AppendLine("<div class='signature-line'>");
        //        html.AppendLine($"{fechamento.UsuarioAprovacao.NomeCompleto}");
        //        html.AppendLine("<br>Aprovado por");
        //        html.AppendLine("</div>");
        //        html.AppendLine("</div>");
        //    }
        //    html.AppendLine("</div>");

        //    // Footer
        //    html.AppendLine("<div class='footer'>");
        //    html.AppendLine($"Documento gerado em {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        //    html.AppendLine("<br>Sistema de Tesouraria Eclesiástica");
        //    html.AppendLine("</div>");

        //    html.AppendLine("</body>");
        //    html.AppendLine("</html>");

        //    return html.ToString();
        //}
    }
}
