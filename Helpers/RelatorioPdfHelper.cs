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
            html.AppendLine(GerarCabecalhoHtml("Relatório de Fluxo de Caixa", centroCustoNome));

            html.AppendLine("<div class='periodo'>");
            html.AppendLine($"<p><strong>Período:</strong> {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}</p>");
            html.AppendLine($"<p><strong>Data de Geração:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
            html.AppendLine("</div>");

            // Totais
            var totalEntradas = dados.Sum(d => d.Entradas);
            var totalSaidas = dados.Sum(d => d.Saidas);
            var saldoFinal = dados.LastOrDefault()?.SaldoAcumulado ?? 0;

            html.AppendLine("<div class='resumo'>");
            html.AppendLine("<table class='resumo-table'>");
            html.AppendLine("<tr>");
            html.AppendLine($"<th>Total Entradas</th><td class='valor-positivo'>{totalEntradas:C2}</td>");
            html.AppendLine($"<th>Total Saídas</th><td class='valor-negativo'>{totalSaidas:C2}</td>");
            html.AppendLine($"<th>Saldo Final</th><td class='valor-destaque'>{saldoFinal:C2}</td>");
            html.AppendLine("</tr></table></div>");

            // Detalhamento
            html.AppendLine("<table>");
            html.AppendLine("<thead><tr>");
            html.AppendLine("<th>Data</th><th>Entradas</th><th>Saídas</th><th>Saldo Dia</th><th>Saldo Acumulado</th>");
            html.AppendLine("</tr></thead><tbody>");

            foreach (var item in dados)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{item.Data:dd/MM/yyyy}</td>");
                html.AppendLine($"<td class='valor-positivo'>{item.Entradas:C2}</td>");
                html.AppendLine($"<td class='valor-negativo'>{item.Saidas:C2}</td>");
                html.AppendLine($"<td>{item.SaldoDia:C2}</td>");
                html.AppendLine($"<td class='valor-destaque'>{item.SaldoAcumulado:C2}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</tbody></table>");
            html.AppendLine(GerarRodapeHtml());

            return html.ToString();
        }

        private static string GerarHtmlEntradas(List<Entrada> entradas, DateTime dataInicio, DateTime dataFim, decimal total, string? centroCustoNome)
        {
            var html = new StringBuilder();
            html.AppendLine(GerarCabecalhoHtml("Relatório de Entradas por Período", centroCustoNome));

            html.AppendLine("<div class='periodo'>");
            html.AppendLine($"<p><strong>Período:</strong> {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}</p>");
            html.AppendLine($"<p><strong>Total de Registros:</strong> {entradas.Count}</p>");
            html.AppendLine($"<p><strong>Total Geral:</strong> <span class='valor-destaque'>{total:C2}</span></p>");
            html.AppendLine("</div>");

            // Agrupamento por Centro de Custo
            var porCentroCusto = entradas.GroupBy(e => e.CentroCusto?.Nome ?? "Sem Centro de Custo");

            foreach (var grupo in porCentroCusto)
            {
                html.AppendLine($"<h3 class='secao-titulo'>{grupo.Key}</h3>");
                html.AppendLine("<table>");
                html.AppendLine("<thead><tr>");
                html.AppendLine("<th>Data</th><th>Descrição</th><th>Membro</th><th>Plano Contas</th><th>Valor</th>");
                html.AppendLine("</tr></thead><tbody>");

                foreach (var entrada in grupo.OrderBy(e => e.Data))
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{entrada.Data:dd/MM/yyyy}</td>");
                    html.AppendLine($"<td>{entrada.Descricao}</td>");
                    html.AppendLine($"<td>{entrada.Membro?.NomeCompleto ?? "-"}</td>");
                    html.AppendLine($"<td>{entrada.PlanoDeContas?.Nome ?? "-"}</td>");
                    html.AppendLine($"<td class='valor-positivo'>{entrada.Valor:C2}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("<tr class='subtotal'>");
                html.AppendLine($"<td colspan='4'><strong>Subtotal {grupo.Key}</strong></td>");
                html.AppendLine($"<td class='valor-destaque'><strong>{grupo.Sum(e => e.Valor):C2}</strong></td>");
                html.AppendLine("</tr>");
                html.AppendLine("</tbody></table>");
            }

            html.AppendLine(GerarRodapeHtml());
            return html.ToString();
        }

        private static string GerarHtmlSaidas(List<Saida> saidas, DateTime dataInicio, DateTime dataFim, decimal total, string? centroCustoNome)
        {
            var html = new StringBuilder();
            html.AppendLine(GerarCabecalhoHtml("Relatório de Saídas por Período", centroCustoNome));

            html.AppendLine("<div class='periodo'>");
            html.AppendLine($"<p><strong>Período:</strong> {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}</p>");
            html.AppendLine($"<p><strong>Total de Registros:</strong> {saidas.Count}</p>");
            html.AppendLine($"<p><strong>Total Geral:</strong> <span class='valor-destaque'>{total:C2}</span></p>");
            html.AppendLine("</div>");

            // Agrupamento por Centro de Custo
            var porCentroCusto = saidas.GroupBy(s => s.CentroCusto?.Nome ?? "Sem Centro de Custo");

            foreach (var grupo in porCentroCusto)
            {
                html.AppendLine($"<h3 class='secao-titulo'>{grupo.Key}</h3>");
                html.AppendLine("<table>");
                html.AppendLine("<thead><tr>");
                html.AppendLine("<th>Data</th><th>Descrição</th><th>Fornecedor</th><th>Categoria</th><th>Valor</th>");
                html.AppendLine("</tr></thead><tbody>");

                foreach (var saida in grupo.OrderBy(s => s.Data))
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{saida.Data:dd/MM/yyyy}</td>");
                    html.AppendLine($"<td>{saida.Descricao}</td>");
                    html.AppendLine($"<td>{saida.Fornecedor?.Nome ?? "-"}</td>");
                    html.AppendLine($"<td>{saida.PlanoDeContas?.Nome ?? "-"}</td>");
                    html.AppendLine($"<td class='valor-negativo'>{saida.Valor:C2}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("<tr class='subtotal'>");
                html.AppendLine($"<td colspan='4'><strong>Subtotal {grupo.Key}</strong></td>");
                html.AppendLine($"<td class='valor-destaque'><strong>{grupo.Sum(s => s.Valor):C2}</strong></td>");
                html.AppendLine("</tr>");
                html.AppendLine("</tbody></table>");
            }

            html.AppendLine(GerarRodapeHtml());
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
            html.AppendLine(GerarCabecalhoHtml("Relatório de Contribuições por Membro", null));

            html.AppendLine("<div class='periodo'>");
            html.AppendLine($"<p><strong>Período:</strong> {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}</p>");
            html.AppendLine("</div>");

            // Resumo por Centro de Custo
            if (resumoPorCentroCusto != null)
            {
                html.AppendLine("<h3 class='secao-titulo'>Resumo por Centro de Custo</h3>");
                html.AppendLine("<table class='resumo-table'>");
                html.AppendLine("<thead><tr>");
                html.AppendLine("<th>Centro de Custo</th><th>Total</th><th>Membros</th><th>Lançamentos</th>");
                html.AppendLine("</tr></thead><tbody>");

                foreach (var item in resumoPorCentroCusto)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td><strong>{item.CentroCustoNome}</strong></td>");
                    html.AppendLine($"<td class='valor-positivo'><strong>{item.TotalContribuicoes:C2}</strong></td>");
                    html.AppendLine($"<td class='text-center'>{item.QuantidadeMembros}</td>");
                    html.AppendLine($"<td class='text-center'>{item.QuantidadeContribuicoes}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</tbody></table>");
            }

            html.AppendLine(GerarRodapeHtml());
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
            html.AppendLine(GerarCabecalhoHtml("Relatório de Despesas por Centro de Custo", null));

            html.AppendLine("<div class='periodo'>");
            html.AppendLine($"<p><strong>Período:</strong> {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}</p>");
            html.AppendLine("</div>");

            // Resumo por Categoria
            if (resumoPorCategoria != null)
            {
                html.AppendLine("<h3 class='secao-titulo'>Despesas por Centro de Custo e Categoria</h3>");
                html.AppendLine("<table>");
                html.AppendLine("<thead><tr>");
                html.AppendLine("<th>Centro de Custo</th><th>Categoria</th><th>Total</th><th>Quantidade</th>");
                html.AppendLine("</tr></thead><tbody>");

                foreach (var item in resumoPorCategoria)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{item.CentroCustoNome}</td>");
                    html.AppendLine($"<td>{item.CategoriaNome}</td>");
                    html.AppendLine($"<td class='valor-negativo'>{item.TotalDespesas:C2}</td>");
                    html.AppendLine($"<td class='text-center'>{item.QuantidadeDespesas}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</tbody></table>");
            }

            html.AppendLine(GerarRodapeHtml());
            return html.ToString();
        }

        private static string GerarCabecalhoHtml(string titulo, string? centroCustoNome)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head>");
            html.AppendLine("<meta charset='utf-8'/>");
            html.AppendLine("<style>");
            html.AppendLine(ObterEstilosCss());
            html.AppendLine("</style>");
            html.AppendLine("</head><body>");
            html.AppendLine("<div class='header'>");
            html.AppendLine("<h1>CONVENÇÃO DE MINISTROS DAS ASSEMBLEIAS DE DEUS NO ESTADO DA PARAÍBA - COMADEP</h1>");
            html.AppendLine("<p>CNPJ: 04.362.336/0001-58 | Rua 1º de Maio, 239, Jaguaribe - João Pessoa/PB</p>");
            html.AppendLine("</div>");
            html.AppendLine($"<h2 class='titulo-relatorio'>{titulo}</h2>");

            if (!string.IsNullOrEmpty(centroCustoNome))
            {
                html.AppendLine($"<p class='centro-custo'><strong>Centro de Custo:</strong> {centroCustoNome}</p>");
            }

            return html.ToString();
        }

        private static string GerarRodapeHtml()
        {
            return @"
                <div class='rodape'>
            <p>Relatório gerado em " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + @"</p>
            <p>Sistema Integrado de Gestão de Tesouraria Eclesiástica</p>
            </div>
             </body></html>";
        }

        private static string ObterEstilosCss()
        {
            return @"
              body { font-family: Arial, sans-serif; font-size: 10pt; margin: 20px; }
             .header { text-align: center; border: 2px solid #000; padding: 10px; margin-bottom: 20px; }
               .header h1 { font-size: 12pt; margin: 5px 0; }
               .header p { font-size: 9pt; margin: 3px 0; }
                         .titulo-relatorio { text-align: center; color: #2563eb; border-bottom: 2px solid #2563eb; padding-bottom: 10px; }
                      .centro-custo { text-align: center; font-size: 11pt; margin: 10px 0; }
                      .periodo { background: #f3f4f6; padding: 10px; margin: 15px 0; border-left: 4px solid #2563eb; }
                   .resumo { margin: 15px 0; }
              .resumo-table { width: 100%; border-collapse: collapse; margin: 10px 0; }
            .resumo-table th, .resumo-table td { padding: 8px; border: 1px solid #ddd; }
                   .resumo-table th { background: #2563eb; color: white; font-weight: bold; }
                    table { width: 100%; border-collapse: collapse; margin: 15px 0; font-size: 9pt; }
                   th { background: #e5e7eb; padding: 8px; text-align: left; border: 1px solid #9ca3af; font-weight: bold; }
                   td { padding: 6px; border: 1px solid #d1d5db; }
                        tr:nth-child(even) { background: #f9fafb; }
                   .valor-positivo { color: #059669; font-weight: bold; text-align: right; }
                 .valor-negativo { color: #dc2626; font-weight: bold; text-align: right; }
                          .valor-destaque { color: #2563eb; font-weight: bold; text-align: right; }
                        .text-center { text-align: center; }
               .subtotal { background: #e5e7eb; font-weight: bold; }
             .secao-titulo { background: #374151; color: white; padding: 8px; margin-top: 20px; }
                  .rodape { margin-top: 30px; text-align: center; font-size: 8pt; color: #6b7280; border-top: 1px solid #d1d5db; padding-top: 10px; }
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
