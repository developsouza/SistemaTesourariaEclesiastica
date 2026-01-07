using iText.Html2pdf;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.ViewModels;
using System.Text;

namespace SistemaTesourariaEclesiastica.Helpers
{
    /// <summary>
    /// Helper para geração de PDFs de relatórios
    /// </summary>
    public static class RelatorioPdfHelper
    {
        /// <summary>
        /// Gera PDF do relatório de Fluxo de Caixa
        /// </summary>
        public static byte[] GerarPdfFluxoCaixa(List<FluxoDeCaixaItem> dados, DateTime dataInicio, DateTime dataFim, string? centroCustoNome = null)
        {
            var html = GerarHtmlFluxoCaixa(dados, dataInicio, dataFim, centroCustoNome);
            return ConverterHtmlParaPdf(html);
        }

        /// <summary>
        /// Gera PDF do relatório de Entradas por Período
        /// </summary>
        public static byte[] GerarPdfEntradas(List<Entrada> entradas, DateTime dataInicio, DateTime dataFim, decimal total, string? centroCustoNome = null)
        {
            var html = GerarHtmlEntradas(entradas, dataInicio, dataFim, total, centroCustoNome);
            return ConverterHtmlParaPdf(html);
        }

        /// <summary>
        /// Gera PDF do relatório de Saídas por Período
        /// </summary>
        public static byte[] GerarPdfSaidas(List<Saida> saidas, DateTime dataInicio, DateTime dataFim, decimal total, string? centroCustoNome = null)
        {
            var html = GerarHtmlSaidas(saidas, dataInicio, dataFim, total, centroCustoNome);
            return ConverterHtmlParaPdf(html);
        }

        /// <summary>
        /// Gera PDF do relatório de Contribuições por Membro com detalhamento por Centro de Custo
        /// </summary>
        public static byte[] GerarPdfContribuicoes(
            List<Entrada> contribuicoes,
            dynamic resumoPorCentroCusto,
            dynamic resumoPorMembro,
            DateTime dataInicio,
            DateTime dataFim)
        {
            var html = GerarHtmlContribuicoes(contribuicoes, resumoPorCentroCusto, resumoPorMembro, dataInicio, dataFim);
            return ConverterHtmlParaPdf(html);
        }

        /// <summary>
        /// Gera PDF do relatório de Despesas por Centro de Custo
        /// </summary>
        public static byte[] GerarPdfDespesasPorCentroCusto(
            List<Saida> despesas,
            dynamic resumoPorCentroCusto,
            dynamic resumoPorCategoria,
            DateTime dataInicio,
            DateTime dataFim)
        {
            var html = GerarHtmlDespesasPorCentroCusto(despesas, resumoPorCentroCusto, resumoPorCategoria, dataInicio, dataFim);
            return ConverterHtmlParaPdf(html);
        }

        #region Métodos Privados de Geração de HTML

        private static string GerarHtmlFluxoCaixa(List<FluxoDeCaixaItem> dados, DateTime dataInicio, DateTime dataFim, string? centroCustoNome)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>Relatório de Fluxo de Caixa</title>");
            html.AppendLine("<style>");
            html.AppendLine(ObterEstilosCssPadronizado());
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // Cabeçalho padrão com logo
            GerarCabecalhoPadronizado(html, "RELATÓRIO DE FLUXO DE CAIXA", centroCustoNome, dataInicio, dataFim);

            // Totais resumo executivo
            var totalEntradas = dados.Sum(d => d.Entradas);
            var totalSaidas = dados.Sum(d => d.Saidas);
            var saldoFinal = dados.LastOrDefault()?.SaldoAcumulado ?? 0;

            html.AppendLine("<div class='summary-box'>");
            html.AppendLine("<div style='font-size: 10px; font-weight: bold; margin-bottom: 6px; color: #2c3e50; text-align: center;'>RESUMO EXECUTIVO</div>");
            html.AppendLine("<div class='summary-grid'>");

            html.AppendLine("<div class='summary-item'>");
            html.AppendLine("<span class='summary-label'>Total de Entradas</span>");
            html.AppendLine($"<span class='summary-value text-success'>{totalEntradas:C}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='summary-item'>");
            html.AppendLine("<span class='summary-label'>Total de Saídas</span>");
            html.AppendLine($"<span class='summary-value text-danger'>{totalSaidas:C}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='summary-item'>");
            html.AppendLine("<span class='summary-label'>Saldo Final</span>");
            html.AppendLine($"<span class='summary-value text-info'>{saldoFinal:C}</span>");
            html.AppendLine("</div>");

            html.AppendLine("</div>");
            html.AppendLine("</div>");

            // Detalhamento
            html.AppendLine("<h3 style='background-color: #34495e; color: white; padding: 8px; margin: 8px 0 5px 0; font-size: 11px;'>DETALHAMENTO DIÁRIO</h3>");
            html.AppendLine("<table>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr>");
            html.AppendLine("<th>Data</th><th>Entradas</th><th>Saídas</th><th>Saldo Dia</th><th>Saldo Acumulado</th>");
            html.AppendLine("</tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");

            foreach (var item in dados)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td class='center'>{item.Data:dd/MM/yyyy}</td>");
                html.AppendLine($"<td class='currency text-success'>{item.Entradas:C}</td>");
                html.AppendLine($"<td class='currency text-danger'>{item.Saidas:C}</td>");
                html.AppendLine($"<td class='currency'>{item.SaldoDia:C}</td>");
                html.AppendLine($"<td class='currency text-info'>{item.SaldoAcumulado:C}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");

            // Rodapé padrão
            GerarRodapePadronizado(html);

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private static string GerarHtmlEntradas(List<Entrada> entradas, DateTime dataInicio, DateTime dataFim, decimal total, string? centroCustoNome)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>Relatório de Entradas</title>");
            html.AppendLine("<style>");
            html.AppendLine(ObterEstilosCssPadronizado());
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // Cabeçalho padrão
            GerarCabecalhoPadronizado(html, "RELATÓRIO DE ENTRADAS (RECEITAS)", centroCustoNome, dataInicio, dataFim);

            // Resumo executivo
            html.AppendLine("<div class='summary-box'>");
            html.AppendLine("<div style='font-size: 10px; font-weight: bold; margin-bottom: 6px; color: #2c3e50; text-align: center;'>RESUMO EXECUTIVO</div>");
            html.AppendLine("<div class='summary-grid'>");

            html.AppendLine("<div class='summary-item'>");
            html.AppendLine("<span class='summary-label'>Total de Registros</span>");
            html.AppendLine($"<span class='summary-value'>{entradas.Count}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='summary-item'>");
            html.AppendLine("<span class='summary-label'>Total Geral</span>");
            html.AppendLine($"<span class='summary-value text-success'>{total:C}</span>");
            html.AppendLine("</div>");

            html.AppendLine("</div>");
            html.AppendLine("</div>");

            // Agrupamento por Centro de Custo
            var porCentroCusto = entradas.GroupBy(e => e.CentroCusto?.Nome ?? "Sem Centro de Custo");

            foreach (var grupo in porCentroCusto)
            {
                html.AppendLine($"<div class='sede-separator'>{grupo.Key}</div>");
                html.AppendLine("<table class='table-striped'>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th style='width: 80px;'>Data</th>");
                html.AppendLine("<th>Descrição</th>");
                html.AppendLine("<th>Membro</th>");
                html.AppendLine("<th>Categoria</th>");
                html.AppendLine("<th class='currency' style='width: 80px;'>Valor</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");

                foreach (var entrada in grupo.OrderBy(e => e.Data))
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{entrada.Data:dd/MM/yyyy}</td>");
                    html.AppendLine($"<td>{entrada.Descricao}</td>");
                    html.AppendLine($"<td>{entrada.Membro?.NomeCompleto ?? "-"}</td>");
                    html.AppendLine($"<td>{entrada.PlanoDeContas?.Nome ?? "-"}</td>");
                    html.AppendLine($"<td class='currency text-success'>{entrada.Valor:C}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("<tr class='totals-row'>");
                html.AppendLine($"<td colspan='4'>Subtotal:</td>");
                html.AppendLine($"<td class='currency text-success'>{grupo.Sum(e => e.Valor):C}</td>");
                html.AppendLine("</tr>");

                html.AppendLine("</tbody>");
                html.AppendLine("</table>");
            }

            // Rodapé padrão
            GerarRodapePadronizado(html);

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private static string GerarHtmlSaidas(List<Saida> saidas, DateTime dataInicio, DateTime dataFim, decimal total, string? centroCustoNome)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>Relatório de Saídas</title>");
            html.AppendLine("<style>");
            html.AppendLine(ObterEstilosCssPadronizado());
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // Cabeçalho padrão
            GerarCabecalhoPadronizado(html, "RELATÓRIO DE SAÍDAS (DESPESAS)", centroCustoNome, dataInicio, dataFim);

            // Resumo executivo
            html.AppendLine("<div class='summary-box'>");
            html.AppendLine("<div style='font-size: 10px; font-weight: bold; margin-bottom: 6px; color: #2c3e50; text-align: center;'>RESUMO EXECUTIVO</div>");
            html.AppendLine("<div class='summary-grid'>");

            html.AppendLine("<div class='summary-item'>");
            html.AppendLine("<span class='summary-label'>Total de Registros</span>");
            html.AppendLine($"<span class='summary-value'>{saidas.Count}</span>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='summary-item'>");
            html.AppendLine("<span class='summary-label'>Total Geral</span>");
            html.AppendLine($"<span class='summary-value text-danger'>{total:C}</span>");
            html.AppendLine("</div>");

            html.AppendLine("</div>");
            html.AppendLine("</div>");

            // Agrupamento por Centro de Custo
            var porCentroCusto = saidas.GroupBy(s => s.CentroCusto?.Nome ?? "Sem Centro de Custo");

            foreach (var grupo in porCentroCusto)
            {
                html.AppendLine($"<div class='sede-separator'>{grupo.Key}</div>");
                html.AppendLine("<table class='table-striped'>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th style='width: 80px;'>Data</th>");
                html.AppendLine("<th>Descrição</th>");
                html.AppendLine("<th>Fornecedor</th>");
                html.AppendLine("<th>Categoria</th>");
                html.AppendLine("<th class='currency' style='width: 80px;'>Valor</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");

                foreach (var saida in grupo.OrderBy(s => s.Data))
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{saida.Data:dd/MM/yyyy}</td>");
                    html.AppendLine($"<td>{saida.Descricao}</td>");
                    html.AppendLine($"<td>{saida.Fornecedor?.Nome ?? "-"}</td>");
                    html.AppendLine($"<td>{saida.PlanoDeContas?.Nome ?? "-"}</td>");
                    html.AppendLine($"<td class='currency text-danger'>{saida.Valor:C}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("<tr class='totals-row'>");
                html.AppendLine($"<td colspan='4'>Subtotal:</td>");
                html.AppendLine($"<td class='currency text-danger'>{grupo.Sum(s => s.Valor):C}</td>");
                html.AppendLine("</tr>");

                html.AppendLine("</tbody>");
                html.AppendLine("</table>");
            }

            // Rodapé padrão
            GerarRodapePadronizado(html);

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private static string GerarHtmlContribuicoes(
            List<Entrada> contribuicoes,
            dynamic resumoPorCentroCusto,
            dynamic resumoPorMembro,
            DateTime dataInicio,
            DateTime dataFim)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>Relatório de Contribuições por Membro</title>");
            html.AppendLine("<style>");
            html.AppendLine(ObterEstilosCssPadronizado());
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // Cabeçalho padrão
            GerarCabecalhoPadronizado(html, "RELATÓRIO DE CONTRIBUIÇÕES POR MEMBRO", null, dataInicio, dataFim);

            // Resumo por Centro de Custo
            if (resumoPorCentroCusto != null)
            {
                html.AppendLine("<h3 style='background-color: #34495e; color: white; padding: 8px; margin: 8px 0 5px 0; font-size: 11px;'>RESUMO POR CENTRO DE CUSTO</h3>");
                html.AppendLine("<table>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th>Centro de Custo</th><th>Total</th><th>Membros</th><th>Lançamentos</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");

                foreach (var item in resumoPorCentroCusto)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td><strong>{item.CentroCustoNome}</strong></td>");
                    html.AppendLine($"<td class='currency text-success'><strong>{item.TotalContribuicoes:C}</strong></td>");
                    html.AppendLine($"<td class='center'>{item.QuantidadeMembros}</td>");
                    html.AppendLine($"<td class='center'>{item.QuantidadeContribuicoes}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</tbody>");
                html.AppendLine("</table>");
            }

            // Rodapé padrão
            GerarRodapePadronizado(html);

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private static string GerarHtmlDespesasPorCentroCusto(
            List<Saida> despesas,
            dynamic resumoPorCentroCusto,
            dynamic resumoPorCategoria,
            DateTime dataInicio,
            DateTime dataFim)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>Relatório de Despesas por Centro de Custo</title>");
            html.AppendLine("<style>");
            html.AppendLine(ObterEstilosCssPadronizado());
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // Cabeçalho padrão
            GerarCabecalhoPadronizado(html, "RELATÓRIO DE DESPESAS POR CENTRO DE CUSTO", null, dataInicio, dataFim);

            // Resumo por Categoria
            if (resumoPorCategoria != null)
            {
                html.AppendLine("<h3 style='background-color: #34495e; color: white; padding: 8px; margin: 8px 0 5px 0; font-size: 11px;'>DESPESAS POR CENTRO DE CUSTO E CATEGORIA</h3>");
                html.AppendLine("<table>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th>Centro de Custo</th><th>Categoria</th><th>Total</th><th>Quantidade</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");

                foreach (var item in resumoPorCategoria)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{item.CentroCustoNome}</td>");
                    html.AppendLine($"<td>{item.CategoriaNome}</td>");
                    html.AppendLine($"<td class='currency text-danger'>{item.TotalDespesas:C}</td>");
                    html.AppendLine($"<td class='center'>{item.QuantidadeDespesas}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</tbody>");
                html.AppendLine("</table>");
            }

            // Rodapé padrão
            GerarRodapePadronizado(html);

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private static void GerarCabecalhoPadronizado(StringBuilder html, string titulo, string? centroCustoNome, DateTime dataInicio, DateTime dataFim)
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
            html.AppendLine($"<div class='title'>{titulo}</div>");

            html.AppendLine("<div class='subtitle'>");
            if (!string.IsNullOrEmpty(centroCustoNome))
            {
                html.AppendLine($"<strong>{centroCustoNome}</strong> <strong>|</strong> ");
            }
            html.AppendLine($"Período: {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            html.AppendLine("</div>"); // Fim header-content
            html.AppendLine("</div>"); // Fim header
        }

        private static void GerarRodapePadronizado(StringBuilder html)
        {
            html.AppendLine("<div class='footer'>");
            html.AppendLine($"Documento gerado em {DateTime.Now:dd/MM/yyyy HH:mm} - Sistema de Gestão de Tesouraria Eclesiástica");
            html.AppendLine("</div>");
        }

        private static string ObterEstilosCssPadronizado()
        {
            return @"
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
        
        .summary-box {
            background-color: #ecf0f1;
            border: 2px solid #3498db;
            border-radius: 4px;
            padding: 8px;
            margin: 8px 0;
        }
        .summary-grid {
            display: grid;
            grid-template-columns: 1fr 1fr 1fr;
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
        
        .totals-row {
            font-weight: bold;
            background-color: #ecf0f1 !important;
            border-top: 2px solid #2c3e50;
            font-size: 10px;
        }
        .totals-row td {
            white-space: nowrap;
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
        
        .sede-separator {
            background-color: #e3f2fd;
            padding: 4px 6px;
            margin: 6px 0 4px 0;
            border-left: 3px solid #2196f3;
            font-weight: bold;
            font-size: 9.5px;
        }
        .congregacao-separator {
            background-color: #e8f5e9;
            padding: 4px 6px;
            margin: 6px 0 4px 0;
            border-left: 3px solid #4caf50;
            font-weight: bold;
            font-size: 9.5px;
        }
            ";
        }

        private static byte[] ConverterHtmlParaPdf(string html)
        {
            using var memoryStream = new MemoryStream();
            var converterProperties = new ConverterProperties();

            HtmlConverter.ConvertToPdf(html, memoryStream, converterProperties);

            return memoryStream.ToArray();
        }

        #endregion
    }
}
