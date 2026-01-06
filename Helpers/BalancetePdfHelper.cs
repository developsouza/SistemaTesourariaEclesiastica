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
            // Usando os mesmos estilos base do PdfService
            html.AppendLine(@"
                @page { 
                    size: A4 portrait; 
                    margin: 3mm 10mm 10mm 10mm; 
                }
                body {
                    font-family: Arial, sans-serif;
                    margin: 0;
                    padding: 0;
                    font-size: 10px;
                    line-height: 1.3;
                    color: #000;
                    background: white;
                }
                
                /* CABEÇALHO COM GRID IGUAL AO PDFSERVICE */
                .header { 
                    margin-bottom: 4px; 
                    border: 2px solid #000;
                    padding: 6px;
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
                    width: 100%;
                }
                .header-title h1 {
                    font-size: 13px;
                    font-weight: bold;
                    margin: 1px 0;
                    text-transform: uppercase;
                    line-height: 1.2;
                }
                .header-title p {
                    font-size: 9px;
                    margin: 2px 0;
                }
                .header-title .subtitle {
                    font-size: 10px;
                    font-weight: bold;
                    margin: 4px 0 2px 0;
                }
                
                .periodo-info-container {
                    display: table;
                    width: 100%;
                    border-left: 2px solid #000;
                    border-right: 2px solid #000;
                    border-bottom: 2px solid #000;
                    margin-bottom: 4px;
                }
                .periodo-info-row {
                    display: table-row;
                }
                .periodo-box {
                    display: table-cell;
                    border: 1px solid #000;
                    padding: 3px 6px;
                    font-size: 10px;
                    text-align: center;
                    font-weight: bold;
                    background: white;
                }
                .periodo-box.center {
                    border-left: none;
                    border-right: none;
                }
                .secao-titulo {
                    background: transparent;
                    color: black;
                    padding: 3px 6px;
                    font-weight: bold;
                    font-size: 10px;
                    text-transform: uppercase;
                    margin-top: 4px;
                    margin-bottom: 0;
                    border: 2px solid #000;
                    border-bottom: none;
                    width: 55%;
                    display: inline-block;
                    text-align: center;
                    box-sizing: border-box;
                }
                table {
                    width: 100%;
                    border-collapse: collapse;
                    margin-bottom: 0;
                    margin-top: 0;
                    font-size: 9px;
                    background: white;
                    border-left: 2px solid #000;
                    border-right: 2px solid #000;
                    border-top: 2px solid #000;
                }
                table td {
                    padding: 2px 5px;
                    border-bottom: 1px solid #000;
                    line-height: 1.3;
                }
                .item-descricao {
                    width: 55%;
                    text-align: left;
                    border-right: 1px solid #000;
                }
                .item-label {
                    width: 5%;
                    text-align: center;
                    border-right: 1px solid #000;
                    font-weight: normal;
                }
                .item-valor {
                    width: 20%;
                    text-align: right;
                    padding-right: 8px;
                    border-right: 1px solid #000;
                }
                .item-total {
                    width: 20%;
                    text-align: right;
                    padding-right: 8px;
                }
                .total-row td {
                    font-weight: bold;
                    padding: 3px 5px;
                    border-bottom: 2px solid #000;
                }
                .last-section {
                    border-bottom: 2px solid #000;
                }
                
                /* ASSINATURAS E RODAPÉ */
                .assinaturas {
                    display: table;
                    width: 100%;
                    margin-top: 40px;
                    padding-top: 15px;
                }
                .assinaturas-row {
                    display: table-row;
                }
                .assinatura {
                    display: table-cell;
                    text-align: center;
                    width: 50%;
                    vertical-align: top;
                }
                .assinatura-linha {
                    border-top: 1px solid #000;
                    width: 220px;
                    margin: 0 auto 4px;
                    padding-top: 4px;
                    font-size: 9px;
                }
                .assinatura-cargo {
                    font-size: 9px;
                    margin-top: 2px;
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
            ");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // CABEÇALHO PADRONIZADO COM GRID
            html.AppendLine("<div class='header'>");
            
            // Logo à esquerda
            html.AppendLine("<div class='header-logo'>");
            html.AppendLine("<img src='wwwroot/images/logoadpb.png' alt='Logo ADPB' />");
            html.AppendLine("</div>");
            
            // Conteúdo do cabeçalho
            html.AppendLine("<div class='header-content'>");
            html.AppendLine("<div class='header-title'>");
            html.AppendLine("<h1>CONVENÇÃO DE MINISTROS DAS ASSEMBLÉIAS DE DEUS</h1>");
            html.AppendLine("<h1>NO ESTADO DA PARAÍBA - COMADEP</h1>");
            html.AppendLine("<p>CNPJ 04.362.336/0001-55 - Rua 1º de Maio, 239,Jaguaribe - João Pessoa-PB</p>");
            html.AppendLine("<p class='subtitle'>Balancete de Verificação das Igrejas Representadas e Congregações</p>");
            html.AppendLine("<p style='margin-bottom: 0;'>Realizado em</p>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            
            html.AppendLine("</div>"); // Fim header

            // Informações do Período
            html.AppendLine("<div class='periodo-info-container'>");
            html.AppendLine("<div class='periodo-info-row'>");
            html.AppendLine($"<div class='periodo-box left'>{model.CentroCustoNome}</div>");
            html.AppendLine("<div class='periodo-box center'>PB</div>");
            html.AppendLine($"<div class='periodo-box right'>{model.DataInicio.Day} / {mes} / {ano}</div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            // RECEITAS OPERACIONAIS
            html.AppendLine("<div class='secao-titulo'>RECEITAS OPERACIONAIS</div>");
            html.AppendLine("<table>");

            html.AppendLine("<tr>");
            html.AppendLine("<td class='item-descricao'>Saldo do Mês Anterior</td>");
            html.AppendLine("<td class='item-label'>R$</td>");
            html.AppendLine("<td class='item-valor'></td>");
            html.AppendLine("<td class='item-total'>R$</td>");
            html.AppendLine("</tr>");

            var dizimos = model.ReceitasOperacionais.FirstOrDefault(r => r.Descricao.Contains("Dízimo"));
            html.AppendLine("<tr>");
            html.AppendLine("<td class='item-descricao'>Dízimos e ofertas do Tempo Centra</td>");
            html.AppendLine("<td class='item-label'>R$</td>");
            html.AppendLine($"<td class='item-valor'>{(dizimos != null && dizimos.Valor > 0 ? dizimos.Valor.ToString("N2") : "")}</td>");
            html.AppendLine("<td class='item-total'></td>");
            html.AppendLine("</tr>");

            var circulo = model.ReceitasOperacionais.FirstOrDefault(r => r.Descricao.Contains("Círculo"));
            html.AppendLine("<tr>");
            html.AppendLine("<td class='item-descricao'>Ofertas do Círculo de Oração (Adultos e Mocidades</td>");
            html.AppendLine("<td class='item-label'>R$</td>");
            html.AppendLine($"<td class='item-valor'>{(circulo != null && circulo.Valor > 0 ? circulo.Valor.ToString("N2") : "")}</td>");
            html.AppendLine("<td class='item-total'></td>");
            html.AppendLine("</tr>");

            var ofertas = model.ReceitasOperacionais.FirstOrDefault(r => r.Descricao == "Ofertas");
            html.AppendLine("<tr>");
            html.AppendLine("<td class='item-descricao'>Ofertas</td>");
            html.AppendLine("<td class='item-label'>R$</td>");
            html.AppendLine($"<td class='item-valor'>{(ofertas != null && ofertas.Valor > 0 ? ofertas.Valor.ToString("N2") : "")}</td>");
            html.AppendLine("<td class='item-total'></td>");
            html.AppendLine("</tr>");

            html.AppendLine("<tr class='total-row'>");
            html.AppendLine("<td class='item-descricao'>Total do crédito</td>");
            html.AppendLine("<td class='item-label'></td>");
            html.AppendLine("<td class='item-valor'></td>");
            html.AppendLine($"<td class='item-total'>R$ {model.TotalCredito:N2}</td>");
            html.AppendLine("</tr>");
            html.AppendLine("</table>");

            // IMOBILIZADOS
            html.AppendLine("<div class='secao-titulo'>IMOBILIZADOS</div>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><td class='item-descricao'>Imóveis</td><td class='item-label'>R$</td><td class='item-valor'></td><td class='item-total'></td></tr>");
            html.AppendLine("<tr><td class='item-descricao'>Veículo</td><td class='item-label'>R$</td><td class='item-valor'></td><td class='item-total'></td></tr>");
            html.AppendLine("<tr><td class='item-descricao'>Móveis e Utensílio</td><td class='item-label'>R$</td><td class='item-valor'></td><td class='item-total'></td></tr>");
            html.AppendLine("<tr><td class='item-descricao'>Instalações</td><td class='item-label'>R$</td><td class='item-valor'></td><td class='item-total'></td></tr>");
            html.AppendLine("<tr><td class='item-descricao'>Máquinas e Equipamentos</td><td class='item-label'>R$</td><td class='item-valor'></td><td class='item-total'></td></tr>");
            html.AppendLine("<tr class='total-row'>");
            html.AppendLine("<td class='item-descricao'>Total do Crédito</td>");
            html.AppendLine("<td class='item-label'></td>");
            html.AppendLine("<td class='item-valor'></td>");
            html.AppendLine($"<td class='item-total'>R$ {model.TotalCreditoComImobilizados:N2}</td>");
            html.AppendLine("</tr>");
            html.AppendLine("</table>");

            // DESPESAS ADMINISTRATIVAS
            html.AppendLine("<div class='secao-titulo'>DESPESAS ADIMINISTRATIVAS</div>");
            html.AppendLine("<table>");
            var lastAdm = model.DespesasAdministrativas.LastOrDefault();
            foreach (var item in model.DespesasAdministrativas)
            {
                var descricaoHtml = item.Descricao;
                if (item.Descricao.Contains("Manutenção Pastoral") || item.Descricao.Contains("Ajuda Pastoral"))
                {
                    descricaoHtml = $"<strong>{item.Descricao}</strong>";
                }

                html.AppendLine("<tr>");
                html.AppendLine($"<td class='item-descricao'>{descricaoHtml}</td>");
                html.AppendLine("<td class='item-label'>R$</td>");
                html.AppendLine($"<td class='item-valor'>{(item.Valor > 0 ? item.Valor.ToString("N2") : "")}</td>");
                html.AppendLine($"<td class='item-total'>{(item == lastAdm ? "R$ 0,00" : "")}</td>");
                html.AppendLine("</tr>");
            }
            html.AppendLine("</table>");

            // DESPESAS TRIBUTÁRIAS
            html.AppendLine("<div class='secao-titulo'>Despesas Tributárias</div>");
            html.AppendLine("<table>");
            var lastTrib = model.DespesasTributarias.LastOrDefault();
            foreach (var item in model.DespesasTributarias)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td class='item-descricao'>{item.Descricao}</td>");
                html.AppendLine("<td class='item-label'>R$</td>");
                html.AppendLine($"<td class='item-valor'>{(item.Valor > 0 ? item.Valor.ToString("N2") : "")}</td>");
                html.AppendLine($"<td class='item-total'>{(item == lastTrib ? "R$ 0,00" : "")}</td>");
                html.AppendLine("</tr>");
            }
            html.AppendLine("</table>");

            // DESPESAS FINANCEIRAS
            html.AppendLine("<div class='secao-titulo'>DESPESAS FINANCEIRAS</div>");
            html.AppendLine("<table>");
            foreach (var item in model.DespesasFinanceiras)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td class='item-descricao'>{item.Descricao}</td>");
                html.AppendLine("<td class='item-label'>R$</td>");
                html.AppendLine($"<td class='item-valor'>{(item.Valor > 0 ? item.Valor.ToString("N2") : "")}</td>");
                html.AppendLine("<td class='item-total'></td>");
                html.AppendLine("</tr>");
            }
            html.AppendLine("<tr class='total-row last-section'>");
            html.AppendLine("<td class='item-descricao'>Total do Débito</td>");
            html.AppendLine("<td class='item-label'></td>");
            html.AppendLine("<td class='item-valor'></td>");
            html.AppendLine($"<td class='item-total'>R$ {model.TotalDebito:N2}</td>");
            html.AppendLine("</tr>");
            html.AppendLine("</table>");

            // Assinaturas
            html.AppendLine("<div class='assinaturas'>");
            html.AppendLine("<div class='assinaturas-row'>");
            html.AppendLine("<div class='assinatura'>");
            html.AppendLine("<div class='assinatura-linha'>");
            html.AppendLine(!string.IsNullOrEmpty(model.TesoureriroResponsavel) ? model.TesoureriroResponsavel : "");
            html.AppendLine("</div>");
            html.AppendLine("<div class='assinatura-cargo'>Tesoureiro</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='assinatura'>");
            html.AppendLine("<div class='assinatura-linha'>");
            html.AppendLine(!string.IsNullOrEmpty(model.VistoDoPastor) ? model.VistoDoPastor : "");
            html.AppendLine("</div>");
            html.AppendLine("<div class='assinatura-cargo'>Visto do Pastor</div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            // Rodapé padronizado
            html.AppendLine("<div class='footer'>");
            html.AppendLine($"Documento gerado em {DateTime.Now:dd/MM/yyyy HH:mm} - Sistema de Gestão de Tesouraria Eclesiástica");
            html.AppendLine("</div>");

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }
    }
}
