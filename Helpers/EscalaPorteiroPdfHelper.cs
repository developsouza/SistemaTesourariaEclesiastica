using iText.Html2pdf;
using SistemaTesourariaEclesiastica.Extensions;
using SistemaTesourariaEclesiastica.Models;
using System.Text;

namespace SistemaTesourariaEclesiastica.Helpers
{
    public static class EscalaPorteiroPdfHelper
    {
        public static byte[] GerarPdfEscala(
            List<EscalaPorteiro> escalas,
            List<Porteiro> todosPorteiros,
            ResponsavelPorteiro? responsavel,
            DateTime dataInicio,
            DateTime dataFim)
        {
            using var memoryStream = new MemoryStream();

            // Criar o HTML da escala
            var html = GerarHtmlEscala(escalas, todosPorteiros, responsavel, dataInicio, dataFim);

            // Converter HTML para PDF
            var converterProperties = new ConverterProperties();
            HtmlConverter.ConvertToPdf(html, memoryStream, converterProperties);

            return memoryStream.ToArray();
        }

        private static string ObterLogoBase64()
        {
            try
            {
                var logoPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logoadjacuma.png");

                if (File.Exists(logoPath))
                {
                    var imageBytes = File.ReadAllBytes(logoPath);
                    var base64String = Convert.ToBase64String(imageBytes);
                    return $"data:image/png;base64,{base64String}";
                }
            }
            catch
            {
                // Se não conseguir carregar o logo, continua sem ele
            }

            return string.Empty;
        }

        private static string GerarHtmlEscala(
            List<EscalaPorteiro> escalas,
            List<Porteiro> todosPorteiros,
            ResponsavelPorteiro? responsavel,
            DateTime dataInicio,
            DateTime dataFim)
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <style>
                    @page {
                        size: A4;
                        margin: 15mm;
                    }
        
                    * {
                        margin: 0;
                        padding: 0;
                        box-sizing: border-box;
                    }
        
                    body {
                        font-family: 'Arial', 'Helvetica', sans-serif;
                        font-size: 9pt;
                        line-height: 1.3;
                        color: #1a1a1a;
                    }
        
                    .container {
                        width: 100%;
                        max-width: 100%;
                    }
        
                    /* Cabeçalho Premium com Logo */
                    .header {
                        background: linear-gradient(135deg, #2563eb 0%, #1e40af 100%);
                        color: white;
                        padding: 12px 15px;
                        border-radius: 6px;
                        margin-bottom: 12px;
                        box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                        display: flex;
                        align-items: center;
                        gap: 15px;
                    }
        
                    .header-logo {
                        flex-shrink: 0;
                    }
        
                    .header-logo img {
                        width: auto;
                        height: 70px;
                        max-width: 280px;
                        object-fit: contain;
                        background: transparent;
                        border-radius: 0;
                        padding: 0;
                    }
        
                    .header-content {
                        flex-grow: 1;
                        text-align: center;
                    }
        
                    .header h1 {
                        font-size: 18pt;
                        font-weight: 700;
                        margin-bottom: 3px;
                        letter-spacing: 0.5px;
                    }
        
                    .header .subtitle {
                        font-size: 9pt;
                        opacity: 0.95;
                        font-weight: 500;
                    }
        
                    /* Tabela da Escala */
                    .table-container {
                        margin-bottom: 12px;
                        margin-left: auto;
                        margin-right: auto;
                        max-width: 100%;
                    }
        
                    table {
                        width: 100%;
                        border-collapse: collapse;
                        background: white;
                        box-shadow: 0 2px 6px rgba(0,0,0,0.12);
                        border-radius: 6px;
                        overflow: hidden;
                        margin: 0 auto;
                        border: 1px solid #d1d5db;
                    }
        
                    thead {
                        background: linear-gradient(135deg, #e5e7eb 0%, #d1d5db 100%);
                    }
        
                    th {
                        padding: 8px 10px;
                        text-align: center;
                        font-weight: 700;
                        font-size: 8.5pt;
                        color: #1f2937;
                        border-bottom: 2px solid #9ca3af;
                        text-transform: uppercase;
                        letter-spacing: 0.3px;
                        border-right: 1px solid #d1d5db;
                    }
        
                    th:last-child {
                        border-right: none;
                    }
        
                    td {
                        padding: 7px 10px;
                        border-bottom: 1px solid #e5e7eb;
                        border-right: 1px solid #e5e7eb;
                        font-size: 8.5pt;
                        vertical-align: middle;
                    }
        
                    td:last-child {
                        border-right: none;
                    }
        
                    /* Alinhamento das colunas */
                    th:first-child, td:first-child {
                        text-align: center;
                    }
        
                    td:nth-child(2) {
                        text-align: left;
                    }
        
                    td:nth-child(3) {
                        text-align: center;
                    }
        
                    td:nth-child(4) {
                        text-align: left;
                    }
        
                    /* Efeito zebra mais visível */
                    tbody tr:nth-child(odd) {
                        background-color: #ffffff;
                    }
        
                    tbody tr:nth-child(even) {
                        background-color: #f3f4f6;
                    }
        
                    tbody tr:hover {
                        background-color: #e5e7eb;
                    }
        
                    tbody tr:last-child td {
                        border-bottom: none;
                    }
        
                    .date-cell {
                        font-weight: 700;
                        color: #2563eb;
                    }
        
                    .badge {
                        display: inline-block;
                        padding: 3px 8px;
                        border-radius: 12px;
                        font-size: 7.5pt;
                        font-weight: 600;
                        background: #dbeafe;
                        color: #1e40af;
                        white-space: nowrap;
                    }
        
                    /* Seção de Contatos */
                    .contacts-section {
                        margin-top: 12px;
                        padding-top: 12px;
                        border-top: 2px solid #e5e7eb;
                    }
        
                    .section-title {
                        font-size: 10pt;
                        font-weight: 700;
                        color: #374151;
                        margin-bottom: 8px;
                        padding-left: 3px;
                        border-left: 3px solid #2563eb;
                        padding-left: 8px;
                    }
        
                    /* Responsável - Destaque */
                    .responsible-box {
                        background: linear-gradient(135deg, #dcfce7 0%, #bbf7d0 100%);
                        border: 2px solid #22c55e;
                        border-radius: 6px;
                        padding: 10px;
                        margin-bottom: 10px;
                        box-shadow: 0 1px 3px rgba(0,0,0,0.08);
                    }
        
                    .responsible-box .title {
                        font-size: 9pt;
                        font-weight: 700;
                        color: #15803d;
                        margin-bottom: 4px;
                    }
        
                    .responsible-box .title .check {
                        display: inline-block;
                        margin-right: 6px;
                        font-weight: 700;
                        font-size: 11pt;
                    }
        
                    .responsible-box .info {
                        font-size: 8.5pt;
                        color: #166534;
                        line-height: 1.5;
                    }
        
                    .responsible-box .info strong {
                        font-weight: 700;
                    }
        
                    /* Porteiros Grid */
                    .porters-grid {
                        display: grid;
                        grid-template-columns: repeat(3, 1fr);
                        gap: 6px;
                        margin-top: 8px;
                    }
        
                    .porter-card {
                        background: #f9fafb;
                        border: 1px solid #e5e7eb;
                        border-radius: 4px;
                        padding: 6px 8px;
                        font-size: 8pt;
                    }
        
                    .porter-card .name {
                        font-weight: 700;
                        color: #1f2937;
                        margin-bottom: 2px;
                        font-size: 8.5pt;
                    }
        
                    .porter-card .phone {
                        color: #6b7280;
                        font-size: 7.5pt;
                    }
        
                    .porter-card .phone .icon {
                        margin-right: 3px;
                    }
        
                    /* Footer */
                    .footer {
                        margin-top: 12px;
                        padding-top: 8px;
                        border-top: 1px solid #e5e7eb;
                        font-size: 7pt;
                        color: #6b7280;
                        text-align: center;
                    }
        
                    .footer-info {
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        margin-bottom: 4px;
                        font-size: 7.5pt;
                        flex-wrap: wrap;
                        gap: 10px;
                    }
        
                    .footer-note {
                        font-style: italic;
                        margin-top: 4px;
                        text-align: center;
                    }
        
                    .footer-credit {
                        text-align: center;
                        white-space: normal;
                    }
                </style>
            </head>
<body>
    <div class='container'>");

            // Cabeçalho com Logo
            var logoBase64 = ObterLogoBase64();
            var logoHtml = string.Empty;

            if (!string.IsNullOrEmpty(logoBase64))
            {
                logoHtml = $@"
            <div class='header-logo'>
                <img src='{logoBase64}' alt='Logo AD Jacuma' />
            </div>";
            }

            sb.AppendLine($@"
            <div class='header'>
                {logoHtml}
                <div class='header-content'>
                    <h1>Escala de Porteiros</h1>
                    <div class='subtitle'>
                        Periodo: {LocalizacaoHelper.FormatarData(dataInicio)} a {LocalizacaoHelper.FormatarData(dataFim)}
                    </div>
                </div>
            </div>");

            // Tabela da Escala
            if (escalas.Any())
            {
                sb.AppendLine(@"
                <div class='table-container'>
                    <table>
                        <thead>
                            <tr>
                                <th style='width: 12%'>Data</th>
                                <th style='width: 15%'>Dia da Semana</th>
                                <th style='width: 10%'>Horário</th>
                                <th style='width: 20%'>Tipo de Culto</th>
                                <th style='width: 43%'>Porteiro(s)</th>
                            </tr>
                        </thead>
                        <tbody>");

                foreach (var escala in escalas.OrderBy(e => e.DataCulto).ThenBy(e => e.Horario))
                {
                    var diaSemana = LocalizacaoHelper.ObterNomeDiaSemana(escala.DataCulto.DayOfWeek);
                    var tipoCulto = escala.TipoCulto.GetDisplayName();
                    var dataFormatada = LocalizacaoHelper.FormatarData(escala.DataCulto);

                    // ? CORREÇÃO: Formato 24h usando formatação manual
                    var horarioFormatado = escala.Horario.HasValue
                        ? $"{escala.Horario.Value.Hours:D2}:{escala.Horario.Value.Minutes:D2}"
                        : "-";

                    var porteiros = escala.Porteiro?.Nome ?? "";
                    if (escala.Porteiro2 != null)
                    {
                        porteiros += " e " + escala.Porteiro2.Nome;
                    }

                    sb.AppendLine($@"
                            <tr>
                                <td class='date-cell'>{dataFormatada}</td>
                                <td>{diaSemana}</td>
                                <td style='text-align: center;'>{horarioFormatado}</td>
                                <td><span class='badge'>{tipoCulto}</span></td>
                                <td><strong>{porteiros}</strong></td>
                            </tr>");
                }

                sb.AppendLine(@"
                        </tbody>
                    </table>
                </div>");
            }

            // Seção de Contatos
            sb.AppendLine(@"
                <div class='contacts-section'>
                    <div class='section-title'>Informações de Contato</div>");

            // Responsável (removido emoji, usando símbolo texto)
            if (responsavel != null)
            {
                sb.AppendLine($@"
                    <div class='responsible-box'>
                        <div class='title'>
                            <span class='check'>&#10003;</span>Responsável pela Escala
                        </div>
                        <div class='info'>
                            <strong>{responsavel.Nome}</strong><br>
                            Telefone: {LocalizacaoHelper.FormatarTelefone(responsavel.Telefone)}
                        </div>
                    </div>");
            }

            // Porteiros (sem emoji de telefone)
            if (todosPorteiros.Any())
            {
                sb.AppendLine(@"
                    <div style='margin-top: 10px'>
                        <div class='section-title'>Porteiros</div>
                        <div class='porters-grid'>");

                foreach (var porteiro in todosPorteiros.OrderBy(p => p.Nome))
                {
                    sb.AppendLine($@"
                            <div class='porter-card'>
                                <div class='name'>{porteiro.Nome}</div>
                                <div class='phone'>
                                    <span class='icon'>Tel:</span>{LocalizacaoHelper.FormatarTelefone(porteiro.Telefone)}
                                </div>
                            </div>");
                }

                sb.AppendLine(@"
                        </div>
                    </div>");
            }

            sb.AppendLine(@"
                </div>");

            // Footer
            sb.AppendLine($@"
                <div class='footer'>
                    <div class='footer-info'>
                        <span>Gerado em: {LocalizacaoHelper.FormatarDataHora(DateTime.Now)}</span>
                    </div>
                    <div class='footer-credit'>
                        <span>Sistema de Tesouraria - AD Jacumã - Desenvolvido por | Gilney Souza</span>
                    </div>
                    <div class='footer-note'>
                        Em caso de imprevistos, entrar em contato com o(a) responsavel.
                    </div>
                </div>");

            sb.AppendLine(@"
    </div>
</body>
</html>");

            return sb.ToString();
        }
    }
}
