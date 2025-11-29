using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using SistemaTesourariaEclesiastica.Models;

namespace SistemaTesourariaEclesiastica.Helpers
{
    public static class TransparenciaPdfHelper
    {
        // Cores do tema (verde transparência)
        private static readonly DeviceRgb VERDE_PRIMARY = new DeviceRgb(16, 185, 129);
        private static readonly DeviceRgb VERDE_DARK = new DeviceRgb(5, 150, 105);
        private static readonly DeviceRgb VERDE_LIGHT = new DeviceRgb(209, 250, 229);
        private static readonly DeviceRgb CINZA_ESCURO = new DeviceRgb(55, 65, 81);
        private static readonly DeviceRgb CINZA_MEDIO = new DeviceRgb(107, 114, 128);
        private static readonly DeviceRgb CINZA_CLARO = new DeviceRgb(243, 244, 246);
        private static readonly DeviceRgb BRANCO = new DeviceRgb(255, 255, 255);

        public static byte[] GerarPdfTransparencia(
            string membroNome,
            string? membroApelido,
            string? centroCustoNome,
            DateTime dataCadastro,
            decimal totalContribuido,
            int quantidadeContribuicoes,
            DateTime? ultimaContribuicao,
            List<Entrada> contribuicoes)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            // Fontes
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // ==========================================
            // CABEÇALHO PREMIUM
            // ==========================================
            AdicionarCabecalho(document, fontBold, fontRegular);

            // ==========================================
            // TÍTULO DO DOCUMENTO
            // ==========================================
            var titulo = new Paragraph("HISTÓRICO DE CONTRIBUIÇÕES")
                .SetFont(fontBold)
                .SetFontSize(24)
                .SetFontColor(VERDE_PRIMARY)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(10)
                .SetMarginBottom(5);
            document.Add(titulo);

            var subtitulo = new Paragraph("Portal de Transparência")
                .SetFont(fontRegular)
                .SetFontSize(12)
                .SetFontColor(CINZA_MEDIO)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20);
            document.Add(subtitulo);

            // ==========================================
            // INFORMAÇÕES DO MEMBRO
            // ==========================================
            AdicionarInformacoesMembro(document, fontBold, fontRegular, membroNome, membroApelido, centroCustoNome, dataCadastro);

            // ==========================================
            // CARDS DE RESUMO
            // ==========================================
            AdicionarCardsResumo(document, fontBold, fontRegular, totalContribuido, quantidadeContribuicoes, ultimaContribuicao);

            // ==========================================
            // TABELA DE CONTRIBUIÇÕES
            // ==========================================
            if (contribuicoes != null && contribuicoes.Any())
            {
                AdicionarTabelaContribuicoes(document, fontBold, fontRegular, contribuicoes, totalContribuido);
            }
            else
            {
                var mensagem = new Paragraph("Nenhuma contribuição aprovada encontrada.")
                    .SetFont(fontRegular)
                    .SetFontSize(12)
                    .SetFontColor(CINZA_MEDIO)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(20);
                document.Add(mensagem);
            }

            // ==========================================
            // RODAPÉ
            // ==========================================
            AdicionarRodape(document, fontRegular, pdf.GetNumberOfPages());

            document.Close();
            return memoryStream.ToArray();
        }

        private static void AdicionarCabecalho(Document document, PdfFont fontBold, PdfFont fontRegular)
        {
            // Container do cabeçalho com fundo verde
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 1 }))
                .UseAllAvailableWidth()
                .SetBackgroundColor(VERDE_PRIMARY)
                .SetMarginBottom(15);

            var headerCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPadding(15);

            // Ícone e título
            var tituloHeader = new Paragraph("🔍 SISTEMA DE TESOURARIA ECLESIÁSTICA")
                .SetFont(fontBold)
                .SetFontSize(16)
                .SetFontColor(BRANCO)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(3);

            var subtituloHeader = new Paragraph("Portal de Transparência para Membros")
                .SetFont(fontRegular)
                .SetFontSize(10)
                .SetFontColor(BRANCO)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(0);

            headerCell.Add(tituloHeader);
            headerCell.Add(subtituloHeader);
            headerTable.AddCell(headerCell);

            document.Add(headerTable);
        }

        private static void AdicionarInformacoesMembro(
            Document document,
            PdfFont fontBold,
            PdfFont fontRegular,
            string membroNome,
            string? membroApelido,
            string? centroCustoNome,
            DateTime dataCadastro)
        {
            var tituloSecao = new Paragraph("Informações do Membro")
                .SetFont(fontBold)
                .SetFontSize(14)
                .SetFontColor(CINZA_ESCURO)
                .SetMarginBottom(10);
            document.Add(tituloSecao);

            // Tabela de informações
            var infoTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 3 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(20)
                .SetBackgroundColor(CINZA_CLARO)
                .SetBorder(new SolidBorder(VERDE_PRIMARY, 2));

            // Nome
            infoTable.AddCell(CriarCelulaLabel("Nome:", fontBold));
            infoTable.AddCell(CriarCelulaValor(membroNome + (!string.IsNullOrEmpty(membroApelido) ? $" ({membroApelido})" : ""), fontRegular));

            // Centro de Custo
            infoTable.AddCell(CriarCelulaLabel("Centro de Custo:", fontBold));
            infoTable.AddCell(CriarCelulaValor(centroCustoNome ?? "N/A", fontRegular));

            // Data de Cadastro
            infoTable.AddCell(CriarCelulaLabel("Membro desde:", fontBold));
            infoTable.AddCell(CriarCelulaValor(dataCadastro.ToString("dd/MM/yyyy"), fontRegular));

            document.Add(infoTable);
        }

        private static void AdicionarCardsResumo(
            Document document,
            PdfFont fontBold,
            PdfFont fontRegular,
            decimal totalContribuido,
            int quantidadeContribuicoes,
            DateTime? ultimaContribuicao)
        {
            var tituloSecao = new Paragraph("Resumo Financeiro")
                .SetFont(fontBold)
                .SetFontSize(14)
                .SetFontColor(CINZA_ESCURO)
                .SetMarginBottom(10);
            document.Add(tituloSecao);

            // Tabela de 3 colunas para os cards
            var cardsTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1, 1 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(20);

            // Card 1: Total Contribuído
            var card1 = new Cell()
                .SetBorder(new SolidBorder(VERDE_PRIMARY, 2))
                .SetBackgroundColor(VERDE_LIGHT)
                .SetPadding(10)
                .SetTextAlignment(TextAlignment.CENTER);

            card1.Add(new Paragraph("Total Contribuído")
                .SetFont(fontRegular)
                .SetFontSize(10)
                .SetFontColor(CINZA_MEDIO)
                .SetMarginBottom(5));

            card1.Add(new Paragraph(totalContribuido.ToString("C2"))
                .SetFont(fontBold)
                .SetFontSize(16)
                .SetFontColor(VERDE_DARK)
                .SetMarginBottom(0));

            cardsTable.AddCell(card1);

            // Card 2: Quantidade de Registros
            var card2 = new Cell()
                .SetBorder(new SolidBorder(VERDE_PRIMARY, 2))
                .SetBackgroundColor(CINZA_CLARO)
                .SetPadding(10)
                .SetTextAlignment(TextAlignment.CENTER);

            card2.Add(new Paragraph("Total de Registros")
                .SetFont(fontRegular)
                .SetFontSize(10)
                .SetFontColor(CINZA_MEDIO)
                .SetMarginBottom(5));

            card2.Add(new Paragraph(quantidadeContribuicoes.ToString())
                .SetFont(fontBold)
                .SetFontSize(16)
                .SetFontColor(CINZA_ESCURO)
                .SetMarginBottom(0));

            cardsTable.AddCell(card2);

            // Card 3: Última Contribuição
            var card3 = new Cell()
                .SetBorder(new SolidBorder(VERDE_PRIMARY, 2))
                .SetBackgroundColor(VERDE_LIGHT)
                .SetPadding(10)
                .SetTextAlignment(TextAlignment.CENTER);

            card3.Add(new Paragraph("Última Contribuição")
                .SetFont(fontRegular)
                .SetFontSize(10)
                .SetFontColor(CINZA_MEDIO)
                .SetMarginBottom(5));

            card3.Add(new Paragraph(ultimaContribuicao?.ToString("dd/MM/yyyy") ?? "N/A")
                .SetFont(fontBold)
                .SetFontSize(16)
                .SetFontColor(VERDE_DARK)
                .SetMarginBottom(0));

            cardsTable.AddCell(card3);

            document.Add(cardsTable);
        }

        private static void AdicionarTabelaContribuicoes(
            Document document,
            PdfFont fontBold,
            PdfFont fontRegular,
            List<Entrada> contribuicoes,
            decimal totalContribuido)
        {
            var tituloSecao = new Paragraph("Detalhamento das Contribuições")
                .SetFont(fontBold)
                .SetFontSize(14)
                .SetFontColor(CINZA_ESCURO)
                .SetMarginBottom(10);
            document.Add(tituloSecao);

            // Tabela principal
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 1.5f, 2f, 2f, 1.5f, 1.5f }))
                .UseAllAvailableWidth()
                .SetMarginBottom(10);

            // Cabeçalho da tabela
            table.AddHeaderCell(CriarCelulaCabecalho("Data", fontBold));
            table.AddHeaderCell(CriarCelulaCabecalho("Descrição", fontBold));
            table.AddHeaderCell(CriarCelulaCabecalho("Tipo", fontBold));
            table.AddHeaderCell(CriarCelulaCabecalho("Meio Pgto", fontBold));
            table.AddHeaderCell(CriarCelulaCabecalho("Valor", fontBold));

            // Linhas de dados
            bool alternate = false;
            foreach (var contribuicao in contribuicoes.OrderByDescending(c => c.Data))
            {
                var bgColor = alternate ? CINZA_CLARO : BRANCO;

                table.AddCell(CriarCelulaDado(contribuicao.Data.ToString("dd/MM/yyyy"), fontRegular, bgColor));
                table.AddCell(CriarCelulaDado(contribuicao.Descricao ?? "-", fontRegular, bgColor));
                table.AddCell(CriarCelulaDado(contribuicao.PlanoDeContas?.Nome ?? "-", fontRegular, bgColor));
                table.AddCell(CriarCelulaDado(contribuicao.MeioDePagamento?.Nome ?? "-", fontRegular, bgColor));
                table.AddCell(CriarCelulaDado(contribuicao.Valor.ToString("C2"), fontBold, bgColor, VERDE_DARK, TextAlignment.RIGHT));

                alternate = !alternate;
            }

            // Linha de total
            var cellTotal1 = new Cell(1, 4)
                .SetBackgroundColor(VERDE_PRIMARY)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(10)
                .Add(new Paragraph("TOTAL GERAL")
                    .SetFont(fontBold)
                    .SetFontSize(12)
                    .SetFontColor(BRANCO)
                    .SetTextAlignment(TextAlignment.RIGHT));

            var cellTotal2 = new Cell()
                .SetBackgroundColor(VERDE_PRIMARY)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(10)
                .Add(new Paragraph(totalContribuido.ToString("C2"))
                    .SetFont(fontBold)
                    .SetFontSize(12)
                    .SetFontColor(BRANCO)
                    .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(cellTotal1);
            table.AddCell(cellTotal2);

            document.Add(table);
        }

        private static void AdicionarRodape(Document document, PdfFont fontRegular, int totalPaginas)
        {
            // Linha separadora
            var linha = new Paragraph("_".PadRight(100, '_'))
                .SetFontSize(8)
                .SetFontColor(CINZA_CLARO)
                .SetMarginTop(20)
                .SetMarginBottom(10)
                .SetTextAlignment(TextAlignment.CENTER);
            document.Add(linha);

            // Mensagem de gratidão - Versículo completo
            var mensagemVersiculo = new Paragraph("\"Cada um dê conforme determinou em seu coração, não com pesar ou por obrigação, pois Deus ama quem dá com alegria.\"")
                .SetFont(fontRegular)
                .SetFontSize(9)
                .SetFontColor(CINZA_MEDIO)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(5);
            document.Add(mensagemVersiculo);

            // Referência bíblica centralizada na linha seguinte
            var referenciaBiblica = new Paragraph("2 Coríntios 9:7")
                .SetFont(fontRegular)
                .SetFontSize(8)
                .SetFontColor(CINZA_MEDIO)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10);
            document.Add(referenciaBiblica);

            // Informações do rodapé
            var rodape = new Paragraph($"Documento gerado em {DateTime.Now:dd/MM/yyyy HH:mm} | Página {totalPaginas} | Sistema de Tesouraria Eclesiástica © {DateTime.Now.Year}")
                .SetFont(fontRegular)
                .SetFontSize(8)
                .SetFontColor(CINZA_MEDIO)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(5);
            document.Add(rodape);

            var avisoSeguranca = new Paragraph("🔒 Este documento contém informações confidenciais e é de uso exclusivo do membro identificado.")
                .SetFont(fontRegular)
                .SetFontSize(7)
                .SetFontColor(CINZA_MEDIO)
                .SetTextAlignment(TextAlignment.CENTER);
            document.Add(avisoSeguranca);
        }

        // ==========================================
        // MÉTODOS AUXILIARES
        // ==========================================

        private static Cell CriarCelulaLabel(string texto, PdfFont font)
        {
            return new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPadding(8)
                .SetBackgroundColor(VERDE_LIGHT)
                .Add(new Paragraph(texto)
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetFontColor(CINZA_ESCURO));
        }

        private static Cell CriarCelulaValor(string texto, PdfFont font)
        {
            return new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPadding(8)
                .Add(new Paragraph(texto)
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetFontColor(CINZA_ESCURO));
        }

        private static Cell CriarCelulaCabecalho(string texto, PdfFont font)
        {
            return new Cell()
                .SetBackgroundColor(VERDE_PRIMARY)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(8)
                .Add(new Paragraph(texto)
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetFontColor(BRANCO)
                    .SetTextAlignment(TextAlignment.CENTER));
        }

        private static Cell CriarCelulaDado(
            string texto,
            PdfFont font,
            DeviceRgb bgColor,
            DeviceRgb? textColor = null,
            TextAlignment? alignment = null)
        {
            var paragraph = new Paragraph(texto)
                .SetFont(font)
                .SetFontSize(9)
                .SetFontColor(textColor ?? CINZA_ESCURO);

            if (alignment.HasValue)
            {
                paragraph.SetTextAlignment(alignment.Value);
            }

            return new Cell()
                .SetBackgroundColor(bgColor)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(6)
                .Add(paragraph);
        }
    }
}
