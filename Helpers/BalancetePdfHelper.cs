using iText.Html2pdf;
using SistemaTesourariaEclesiastica.ViewModels;
using System.Text;

namespace SistemaTesourariaEclesiastica.Helpers
{
    public static class BalancetePdfHelper
    {
        public static byte[] GerarPdfBalanceteMensal(BalanceteMensalViewModel model)
        {
            var html = GerarHtmlBalanceteMensal(model);

            using var memoryStream = new MemoryStream();

            HtmlConverter.ConvertToPdf(html, memoryStream);

            return memoryStream.ToArray();
        }

        private static string GerarHtmlBalanceteMensal(BalanceteMensalViewModel model)
        {
            var mes = model.DataInicio.ToString("MMMM", new System.Globalization.CultureInfo("pt-BR")).ToUpper();
            var ano = model.DataInicio.Year;

            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>Balancete Mensal</title>");
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
                    font-size: 9px;
                    line-height: 1.3;
                    color: #333;
                }
                .balancete-header {
                    text-align: center;
                    border: 2px solid #000;
                    padding: 12px;
                    margin-bottom: 15px;
                }
                .balancete-header h1 {
                    font-size: 11px;
                    font-weight: bold;
                    margin: 0 0 3px 0;
                    text-transform: uppercase;
                }
                .balancete-header p {
                    font-size: 9px;
                    margin: 2px 0;
                }
                .periodo-info {
                    display: flex;
                    justify-content: space-between;
                    border: 1px solid #000;
                    padding: 8px;
                    margin-bottom: 15px;
                    font-size: 9px;
                }
                .periodo-info .local {
                    font-weight: bold;
                }
                .secao-titulo {
                    background: #333;
                    color: white;
                    padding: 6px 8px;
                    font-weight: bold;
                    font-size: 9px;
                    text-transform: uppercase;
                    margin-top: 12px;
                    margin-bottom: 4px;
                }
                table {
                    width: 100%;
                    border-collapse: collapse;
                    margin-bottom: 8px;
                    font-size: 8.5px;
                }
                table td {
                    padding: 3px 6px;
                    border: 1px solid #ccc;
                }
                .item-descricao {
                    width: 70%;
                }
                .item-valor {
                    width: 30%;
                    text-align: right;
                    font-family: 'Courier New', monospace;
                }
                .total-row {
                    background: #f0f0f0;
                    font-weight: bold;
                }
                .subtotal-row {
                    background: #fafafa;
                    font-weight: bold;
                }
                .valor-positivo {
                    color: #006400;
                }
                .valor-negativo {
                    color: #8B0000;
                }
                .saldo-final {
                    text-align: center;
                    margin: 15px 0;
                    padding: 10px;
                    background: #f8f9fa;
                    border: 2px solid #333;
                }
                .saldo-final h4 {
                    margin: 0;
                    font-size: 12px;
                }
                .assinaturas {
                    display: flex;
                    justify-content: space-around;
                    margin-top: 40px;
                    padding-top: 15px;
                }
                .assinatura {
                    text-align: center;
                    flex: 1;
                }
                .assinatura-linha {
                    border-top: 1px solid #000;
                    width: 200px;
                    margin: 0 auto 4px;
                    padding-top: 4px;
                    font-size: 8px;
                }
                .assinatura-cargo {
                    font-size: 7.5px;
                    color: #666;
                }
                .rodape {
                    text-align: center;
                    margin-top: 15px;
                    font-size: 7px;
                    color: #666;
                }
            ");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // Cabeçalho
            html.AppendLine("<div class='balancete-header'>");
            html.AppendLine("<h1>CONVENÇÃO DE MINISTROS DAS ASSEMBLEIAS DE DEUS</h1>");
            html.AppendLine("<h1>NO ESTADO DA PARAÍBA - COMADEP</h1>");
            html.AppendLine("<p>PBJ 04.362.336/0001-58 - Rua 1º de Maio, 239, Jaguaribe - João Pessoa</p>");
            html.AppendLine("<h1 style='margin-top: 8px;'>Balancete de Verificação de Igrejas Representadas e Congregações</h1>");
            html.AppendLine("<p>Realizado em</p>");
            html.AppendLine("</div>");

            // Informações do Período
            html.AppendLine("<div class='periodo-info'>");
            html.AppendLine($"<div class='local'>{model.CentroCustoNome}</div>");
            html.AppendLine("<div class='cidade'>PB</div>");
            html.AppendLine($"<div class='data'>{model.DataInicio.Day} / {mes} / {ano}</div>");
            html.AppendLine("</div>");

            // Saldo Mês Anterior
            if (model.SaldoMesAnterior != 0)
            {
                html.AppendLine("<table>");
                html.AppendLine("<tr>");
                html.AppendLine("<td class='item-descricao'>Saldo do Mês Anterior</td>");
                html.AppendLine($"<td class='item-valor'>R$ {model.SaldoMesAnterior:N2}</td>");
                html.AppendLine("</tr>");
                html.AppendLine("</table>");
            }

            // Receitas Operacionais
            html.AppendLine("<div class='secao-titulo'>RECEITAS OPERACIONAIS</div>");
            html.AppendLine("<table>");
            foreach (var item in model.ReceitasOperacionais)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td class='item-descricao'>{item.Descricao}</td>");
                html.AppendLine($"<td class='item-valor'>R$ {item.Valor:N2}</td>");
                html.AppendLine("</tr>");
            }
            html.AppendLine("</table>");

            html.AppendLine("<table>");
            html.AppendLine("<tr>");
            html.AppendLine("<td class='item-descricao'><strong>Oferta</strong></td>");
            html.AppendLine("<td class='item-valor'><strong>R$</strong></td>");
            html.AppendLine("</tr>");
            html.AppendLine("<tr class='total-row'>");
            html.AppendLine("<td class='item-descricao'><strong>Total do crédito</strong></td>");
            html.AppendLine($"<td class='item-valor valor-positivo'><strong>R$ {model.TotalCredito:N2}</strong></td>");
            html.AppendLine("</tr>");
            html.AppendLine("</table>");

            // Imobilizados
            if (model.Imobilizados.Any())
            {
                html.AppendLine("<div class='secao-titulo'>IMOBILIZADOS</div>");
                html.AppendLine("<table>");
                foreach (var item in model.Imobilizados)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td class='item-descricao'>{item.Descricao}</td>");
                    html.AppendLine($"<td class='item-valor'>R$ {item.Valor:N2}</td>");
                    html.AppendLine("</tr>");
                }
                html.AppendLine("<tr class='total-row'>");
                html.AppendLine("<td class='item-descricao'><strong>Total do Crédito</strong></td>");
                html.AppendLine($"<td class='item-valor valor-positivo'><strong>R$ {model.TotalCreditoComImobilizados:N2}</strong></td>");
                html.AppendLine("</tr>");
                html.AppendLine("</table>");
            }

            // Despesas Administrativas
            html.AppendLine("<div class='secao-titulo'>DESPESAS ADMINISTRATIVAS</div>");
            html.AppendLine("<table>");
            foreach (var item in model.DespesasAdministrativas)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td class='item-descricao'>{item.Descricao}</td>");
                html.AppendLine($"<td class='item-valor'>R$ {item.Valor:N2}</td>");
                html.AppendLine("</tr>");
            }
            html.AppendLine("</table>");

            // Despesas Tributárias
            if (model.DespesasTributarias.Any())
            {
                html.AppendLine("<div class='secao-titulo'>DESPESAS TRIBUTÁRIAS</div>");
                html.AppendLine("<table>");
                foreach (var item in model.DespesasTributarias)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td class='item-descricao'>{item.Descricao}</td>");
                    html.AppendLine($"<td class='item-valor'>R$ {item.Valor:N2}</td>");
                    html.AppendLine("</tr>");
                }
                html.AppendLine("<tr class='subtotal-row'>");
                html.AppendLine("<td class='item-descricao'><strong>Subtotal Tributárias</strong></td>");
                html.AppendLine($"<td class='item-valor'><strong>R$ {model.SubtotalDespesasTributarias:N2}</strong></td>");
                html.AppendLine("</tr>");
                html.AppendLine("</table>");
            }

            // Despesas Financeiras
            if (model.DespesasFinanceiras.Any())
            {
                html.AppendLine("<div class='secao-titulo'>DESPESAS FINANCEIRAS</div>");
                html.AppendLine("<table>");
                foreach (var item in model.DespesasFinanceiras)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td class='item-descricao'>{item.Descricao}</td>");
                    html.AppendLine($"<td class='item-valor'>R$ {item.Valor:N2}</td>");
                    html.AppendLine("</tr>");
                }
                html.AppendLine("<tr class='subtotal-row'>");
                html.AppendLine("<td class='item-descricao'><strong>Subtotal Financeiras</strong></td>");
                html.AppendLine($"<td class='item-valor'><strong>R$ {model.SubtotalDespesasFinanceiras:N2}</strong></td>");
                html.AppendLine("</tr>");
                html.AppendLine("</table>");
            }

            // Recolhimentos
            if (model.Recolhimentos.Any())
            {
                html.AppendLine("<table>");
                foreach (var recolhimento in model.Recolhimentos)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td class='item-descricao'>{recolhimento.Destino} ({recolhimento.Percentual:N2}%)</td>");
                    html.AppendLine($"<td class='item-valor'>R$ {recolhimento.Valor:N2}</td>");
                    html.AppendLine("</tr>");
                }
                html.AppendLine("</table>");
            }

            // Totais Finais
            html.AppendLine("<table>");
            html.AppendLine("<tr class='total-row'>");
            html.AppendLine("<td class='item-descricao'><strong>Total do Débito</strong></td>");
            html.AppendLine($"<td class='item-valor valor-negativo'><strong>R$ {model.TotalDebito:N2}</strong></td>");
            html.AppendLine("</tr>");
            html.AppendLine("</table>");

            // Saldo Final
            var saldoClass = model.Saldo >= 0 ? "valor-positivo" : "valor-negativo";
            html.AppendLine("<div class='saldo-final'>");
            html.AppendLine($"<h4>SALDO: <span class='{saldoClass}'><strong>R$ {model.Saldo:N2}</strong></span></h4>");
            html.AppendLine("</div>");

            // Assinaturas
            html.AppendLine("<div class='assinaturas'>");
            html.AppendLine("<div class='assinatura'>");
            html.AppendLine("<div class='assinatura-linha'>");
            html.AppendLine(model.TesoureriroResponsavel ?? "_____________________________");
            html.AppendLine("</div>");
            html.AppendLine("<div class='assinatura-cargo'>Tesoureiro</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='assinatura'>");
            html.AppendLine("<div class='assinatura-linha'>");
            html.AppendLine(model.VistoDoPastor ?? "_____________________________");
            html.AppendLine("</div>");
            html.AppendLine("<div class='assinatura-cargo'>Visto do Pastor</div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            // Rodapé
            html.AppendLine("<div class='rodape'>");
            html.AppendLine($"<p>Relatório gerado em {model.DataGeracao:dd/MM/yyyy HH:mm}</p>");
            html.AppendLine("<p>Sistema Integrado de Gestão de Tesouraria Eclesiástica</p>");
            html.AppendLine("</div>");

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }
    }
}
