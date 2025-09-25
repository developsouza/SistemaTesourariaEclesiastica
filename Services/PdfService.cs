using iText.Html2pdf;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using SistemaTesourariaEclesiastica.Models;
using System.Text;

namespace SistemaTesourariaEclesiastica.Services
{
    public class PdfService
    {
        public byte[] GerarReciboFechamento(FechamentoPeriodo fechamento)
        {
            var html = GerarHtmlReciboFechamento(fechamento);
            
            using var memoryStream = new MemoryStream();
            var properties = new ConverterProperties();
            
            HtmlConverter.ConvertToPdf(html, memoryStream, properties);
            
            return memoryStream.ToArray();
        }

        private string GerarHtmlReciboFechamento(FechamentoPeriodo fechamento)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='UTF-8'>");
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
                    padding-bottom: 20px;
                }
                .title { 
                    font-size: 18px; 
                    font-weight: bold; 
                    margin-bottom: 10px;
                }
                .subtitle { 
                    font-size: 14px; 
                    color: #666;
                }
                .info-section { 
                    margin: 20px 0; 
                    padding: 15px;
                    background-color: #f9f9f9;
                    border: 1px solid #ddd;
                }
                .info-title { 
                    font-weight: bold; 
                    font-size: 14px;
                    margin-bottom: 10px;
                    color: #333;
                }
                .info-row { 
                    display: flex; 
                    justify-content: space-between; 
                    margin: 5px 0;
                    padding: 3px 0;
                }
                .info-label { 
                    font-weight: bold; 
                    width: 40%;
                }
                .info-value { 
                    width: 60%;
                    text-align: right;
                }
                .table { 
                    width: 100%; 
                    border-collapse: collapse; 
                    margin: 20px 0;
                }
                .table th, .table td { 
                    border: 1px solid #ddd; 
                    padding: 8px; 
                    text-align: left;
                }
                .table th { 
                    background-color: #f2f2f2; 
                    font-weight: bold;
                }
                .table .currency { 
                    text-align: right;
                }
                .summary { 
                    margin-top: 30px; 
                    padding: 20px;
                    background-color: #e8f4f8;
                    border: 2px solid #2196F3;
                }
                .summary-title { 
                    font-size: 16px; 
                    font-weight: bold; 
                    margin-bottom: 15px;
                    text-align: center;
                    color: #1976D2;
                }
                .summary-row { 
                    display: flex; 
                    justify-content: space-between; 
                    margin: 8px 0;
                    padding: 5px 0;
                    border-bottom: 1px dotted #ccc;
                }
                .summary-label { 
                    font-weight: bold;
                }
                .summary-value { 
                    font-weight: bold;
                    color: #1976D2;
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
            ");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            
            // Header
            html.AppendLine("<div class='header'>");
            html.AppendLine("<div class='title'>RECIBO DE FECHAMENTO DE PERÍODO</div>");
            html.AppendLine($"<div class='subtitle'>{fechamento.CentroCusto.Nome}</div>");
            html.AppendLine($"<div class='subtitle'>Período: {fechamento.Mes:00}/{fechamento.Ano}</div>");
            html.AppendLine("</div>");
            
            // Informações Gerais
            html.AppendLine("<div class='info-section'>");
            html.AppendLine("<div class='info-title'>INFORMAÇÕES GERAIS</div>");
            html.AppendLine("<div class='info-row'>");
            html.AppendLine("<span class='info-label'>Centro de Custo:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.CentroCusto.Nome}</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='info-row'>");
            html.AppendLine("<span class='info-label'>Período:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.Mes:00}/{fechamento.Ano}</span>");
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
            html.AppendLine("<span class='info-label'>Data de Fechamento:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.DataSubmissao:dd/MM/yyyy HH:mm}</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='info-row'>");
            html.AppendLine("<span class='info-label'>Responsável:</span>");
            html.AppendLine($"<span class='info-value'>{fechamento.UsuarioSubmissao.NomeCompleto}</span>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            
            // Detalhes de Entradas
            if (fechamento.DetalhesFechamento.Any(d => d.TipoMovimento == "Entrada"))
            {
                html.AppendLine("<div class='info-section'>");
                html.AppendLine("<div class='info-title'>ENTRADAS DO PERÍODO</div>");
                html.AppendLine("<table class='table'>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th>Data</th>");
                html.AppendLine("<th>Descrição</th>");
                html.AppendLine("<th>Plano de Contas</th>");
                html.AppendLine("<th>Membro</th>");
                html.AppendLine("<th>Valor</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");
                
                foreach (var entrada in fechamento.DetalhesFechamento.Where(d => d.TipoMovimento == "Entrada").OrderBy(d => d.Data))
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{entrada.Data:dd/MM/yyyy}</td>");
                    html.AppendLine($"<td>{entrada.Descricao}</td>");
                    html.AppendLine($"<td>{entrada.PlanoContas ?? "-"}</td>");
                    html.AppendLine($"<td>{entrada.Membro ?? "-"}</td>");
                    html.AppendLine($"<td class='currency'>{entrada.Valor:C}</td>");
                    html.AppendLine("</tr>");
                }
                
                html.AppendLine("</tbody>");
                html.AppendLine("</table>");
                html.AppendLine("</div>");
            }
            
            // Detalhes de Saídas
            if (fechamento.DetalhesFechamento.Any(d => d.TipoMovimento == "Saida"))
            {
                html.AppendLine("<div class='info-section'>");
                html.AppendLine("<div class='info-title'>SAÍDAS DO PERÍODO</div>");
                html.AppendLine("<table class='table'>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th>Data</th>");
                html.AppendLine("<th>Descrição</th>");
                html.AppendLine("<th>Plano de Contas</th>");
                html.AppendLine("<th>Fornecedor</th>");
                html.AppendLine("<th>Valor</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");
                
                foreach (var saida in fechamento.DetalhesFechamento.Where(d => d.TipoMovimento == "Saida").OrderBy(d => d.Data))
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{saida.Data:dd/MM/yyyy}</td>");
                    html.AppendLine($"<td>{saida.Descricao}</td>");
                    html.AppendLine($"<td>{saida.PlanoContas ?? "-"}</td>");
                    html.AppendLine($"<td>{saida.Fornecedor ?? "-"}</td>");
                    html.AppendLine($"<td class='currency'>{saida.Valor:C}</td>");
                    html.AppendLine("</tr>");
                }
                
                html.AppendLine("</tbody>");
                html.AppendLine("</table>");
                html.AppendLine("</div>");
            }
            
            // Rateios
            if (fechamento.ItensRateio.Any())
            {
                html.AppendLine("<div class='info-section'>");
                html.AppendLine("<div class='info-title'>RATEIOS APLICADOS</div>");
                html.AppendLine("<table class='table'>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th>Regra</th>");
                html.AppendLine("<th>Centro de Custo Destino</th>");
                html.AppendLine("<th>Percentual</th>");
                html.AppendLine("<th>Valor Base</th>");
                html.AppendLine("<th>Valor do Rateio</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");
                
                foreach (var rateio in fechamento.ItensRateio.OrderBy(r => r.RegraRateio.Nome))
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{rateio.RegraRateio.Nome}</td>");
                    html.AppendLine($"<td>{rateio.RegraRateio.CentroCustoDestino.Nome}</td>");
                    html.AppendLine($"<td>{rateio.Percentual:F2}%</td>");
                    html.AppendLine($"<td class='currency'>{rateio.ValorBase:C}</td>");
                    html.AppendLine($"<td class='currency'>{rateio.ValorRateio:C}</td>");
                    html.AppendLine("</tr>");
                }
                
                html.AppendLine("</tbody>");
                html.AppendLine("</table>");
                html.AppendLine("</div>");
            }
            
            // Resumo Financeiro
            html.AppendLine("<div class='summary'>");
            html.AppendLine("<div class='summary-title'>RESUMO FINANCEIRO</div>");
            html.AppendLine("<div class='summary-row'>");
            html.AppendLine("<span class='summary-label'>Total de Entradas:</span>");
            html.AppendLine($"<span class='summary-value'>{fechamento.TotalEntradas:C}</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='summary-row'>");
            html.AppendLine("<span class='summary-label'>Total de Saídas:</span>");
            html.AppendLine($"<span class='summary-value'>{fechamento.TotalSaidas:C}</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='summary-row'>");
            html.AppendLine("<span class='summary-label'>Total de Rateios:</span>");
            html.AppendLine($"<span class='summary-value'>{fechamento.TotalRateios:C}</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='summary-row'>");
            html.AppendLine("<span class='summary-label'>Balanço Digital:</span>");
            html.AppendLine($"<span class='summary-value'>{fechamento.BalancoDigital:C}</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='summary-row'>");
            html.AppendLine("<span class='summary-label'>Balanço Físico:</span>");
            html.AppendLine($"<span class='summary-value'>{fechamento.BalancoFisico:C}</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='summary-row'>");
            html.AppendLine("<span class='summary-label'>Diferença:</span>");
            var diferenca = fechamento.BalancoFisico - fechamento.BalancoDigital;
            html.AppendLine($"<span class='summary-value'>{diferenca:C}</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='summary-row'>");
            html.AppendLine("<span class='summary-label'>Saldo Final:</span>");
            html.AppendLine($"<span class='summary-value'>{fechamento.SaldoFinal:C}</span>");
            html.AppendLine("</div>");
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
    }
}
