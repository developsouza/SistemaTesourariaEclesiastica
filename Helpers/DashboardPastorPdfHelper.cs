using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using SistemaTesourariaEclesiastica.ViewModels;

namespace SistemaTesourariaEclesiastica.Helpers
{
    public static class DashboardPastorPdfHelper
    {
        // Cores do tema
        private static readonly DeviceRgb PRIMARY_COLOR = new DeviceRgb(0, 123, 255);
        private static readonly DeviceRgb SUCCESS_COLOR = new DeviceRgb(40, 167, 69);
        private static readonly DeviceRgb DANGER_COLOR = new DeviceRgb(220, 53, 69);
        private static readonly DeviceRgb WARNING_COLOR = new DeviceRgb(255, 193, 7);
        private static readonly DeviceRgb INFO_COLOR = new DeviceRgb(23, 162, 184);
        private static readonly DeviceRgb CINZA_ESCURO = new DeviceRgb(52, 58, 64);
        private static readonly DeviceRgb CINZA_MEDIO = new DeviceRgb(108, 117, 125);
        private static readonly DeviceRgb CINZA_CLARO = new DeviceRgb(248, 249, 250);
        private static readonly DeviceRgb BRANCO = new DeviceRgb(255, 255, 255);

        public static byte[] GerarPdfDashboard(DashboardPastorViewModel model)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            // ? OTIMIZADO: Margens reduzidas
            document.SetMargins(15, 20, 15, 20);

            // Fontes
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // CABEÇALHO COMPACTO
            AdicionarCabecalhoCompacto(document, fontBold, fontRegular, model);

            // RESUMO EXECUTIVO COMPACTO
            AdicionarResumoExecutivoCompacto(document, fontBold, fontRegular, model);

            // CARDS DE ESTATÍSTICAS OTIMIZADOS
            AdicionarCardsEstatisticasOtimizados(document, fontBold, fontRegular, model);

            // RANKINGS
            AdicionarRankings(document, fontBold, fontRegular, model);

            // MAIORES DESPESAS
            AdicionarMaioresDespesas(document, fontBold, fontRegular, model);

            // DETALHAMENTO POR CONGREGAÇÃO
            AdicionarDetalhamentoCongregacoes(document, fontBold, fontRegular, model);

            // RODAPÉ
            AdicionarRodape(document, fontRegular, pdf.GetNumberOfPages());

            document.Close();
            return memoryStream.ToArray();
        }

        private static void AdicionarCabecalhoCompacto(Document document, PdfFont fontBold, PdfFont fontRegular, DashboardPastorViewModel model)
        {
            // ? HEADER MAIS COMPACTO
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 1 }))
                .UseAllAvailableWidth()
                .SetBackgroundColor(PRIMARY_COLOR)
                .SetMarginBottom(8); // Reduzido de 15 para 8

            var headerCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPadding(8); // Reduzido de 15 para 8

            var titulo = new Paragraph("DASHBOARD ESTRATÉGICO - VISÃO PASTORAL")
                .SetFont(fontBold)
                .SetFontSize(14) // Reduzido de 18 para 14
                .SetFontColor(BRANCO)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(2); // Reduzido de 5 para 2

            var subtitulo = new Paragraph($"Pastor: {model.NomePastor} | Período: {model.PeriodoReferencia}")
                .SetFont(fontRegular)
                .SetFontSize(9) // Reduzido de 11 para 9
                .SetFontColor(BRANCO)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(0);

            headerCell.Add(titulo);
            headerCell.Add(subtitulo);
            headerTable.AddCell(headerCell);

            document.Add(headerTable);
        }

        private static void AdicionarResumoExecutivoCompacto(Document document, PdfFont fontBold, PdfFont fontRegular, DashboardPastorViewModel model)
        {
            var tituloSecao = new Paragraph("Resumo Executivo do Mês")
                .SetFont(fontBold)
                .SetFontSize(11) // Reduzido de 14 para 11
                .SetFontColor(CINZA_ESCURO)
                .SetMarginBottom(5); // Reduzido de 10 para 5
            document.Add(tituloSecao);

            var resumoTable = new Table(UnitValue.CreatePercentArray(new float[] { 1 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(8) // Reduzido de 15 para 8
                .SetBackgroundColor(new DeviceRgb(236, 240, 241))
                .SetBorder(new SolidBorder(PRIMARY_COLOR, 1.5f)); // Reduzido de 2 para 1.5

            var resumoCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPadding(8); // Reduzido de 12 para 8

            // ? CORREÇÃO: Removida a interrogação antes de Superávit/Déficit
            var resultado = model.SaldoMesAtual >= 0
                ? $"Superávit de {model.SaldoMesAtual:C}"
                : $"Déficit de {Math.Abs(model.SaldoMesAtual):C}";

            var resultadoCor = model.SaldoMesAtual >= 0 ? SUCCESS_COLOR : DANGER_COLOR;
            var resultadoIcone = model.SaldoMesAtual >= 0 ? "?" : "?";

            var textoResultado = new Paragraph()
                .Add(new Text("Resultado: ").SetFont(fontBold).SetFontSize(9)) // Reduzido de 11 para 9
                .Add(new Text($"{resultadoIcone} {resultado}")
                    .SetFont(fontBold)
                    .SetFontSize(10) // Reduzido de 12 para 10
                    .SetFontColor(resultadoCor))
                .SetMarginBottom(3); // Reduzido de 5 para 3

            var margem = model.ReceitasMesAtual > 0
                ? (model.SaldoMesAtual / model.ReceitasMesAtual * 100)
                : 0;

            var textoMargem = new Paragraph($"Margem: {margem:F1}% das receitas")
                .SetFont(fontRegular)
                .SetFontSize(8) // Reduzido de 9 para 8
                .SetFontColor(CINZA_MEDIO);

            resumoCell.Add(textoResultado);
            resumoCell.Add(textoMargem);
            resumoTable.AddCell(resumoCell);

            document.Add(resumoTable);
        }

        private static void AdicionarCardsEstatisticasOtimizados(Document document, PdfFont fontBold, PdfFont fontRegular, DashboardPastorViewModel model)
        {
            var tituloSecao = new Paragraph("Indicadores Consolidados")
                .SetFont(fontBold)
                .SetFontSize(11)
                .SetFontColor(CINZA_ESCURO)
                .SetMarginBottom(5);
            document.Add(tituloSecao);

            // ? CORREÇÃO: 4 cards em linha única com fundo branco e bordas coloridas
            var cardsTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1, 1, 1 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(8);

            // Card 1: Total Receitas
            cardsTable.AddCell(CriarCardEstatisticaVisivel(
                "Total Receitas",
                model.TotalReceitasGeral,
                $"Mês: {model.ReceitasMesAtual:C}",
                SUCCESS_COLOR,
                fontBold,
                fontRegular));

            // Card 2: Total Despesas
            cardsTable.AddCell(CriarCardEstatisticaVisivel(
                "Total Despesas",
                model.TotalDespesasGeral,
                $"Mês: {model.DespesasMesAtual:C}",
                DANGER_COLOR,
                fontBold,
                fontRegular));

            // Card 3: Saldo Consolidado
            cardsTable.AddCell(CriarCardEstatisticaVisivel(
                "Saldo Consolidado",
                model.SaldoGeralAtual,
                $"Mês: {model.SaldoMesAtual:C}",
                PRIMARY_COLOR,
                fontBold,
                fontRegular));

            // Card 4: Congregações (com layout diferenciado)
            var cardCongregacoes = new Cell()
                .SetBorder(new SolidBorder(INFO_COLOR, 2))
                .SetBackgroundColor(BRANCO)
                .SetPadding(8)
                .SetTextAlignment(TextAlignment.CENTER);

            cardCongregacoes.Add(new Paragraph("Congregações")
                .SetFont(fontRegular)
                .SetFontSize(8)
                .SetFontColor(CINZA_ESCURO)
                .SetMarginBottom(2));

            cardCongregacoes.Add(new Paragraph(model.QuantidadeCongregacoes.ToString())
                .SetFont(fontBold)
                .SetFontSize(16)
                .SetFontColor(INFO_COLOR)
                .SetMarginBottom(2));

            cardCongregacoes.Add(new Paragraph($"Rateios: {model.TotalRateiosEnviados:C}")
                .SetFont(fontRegular)
                .SetFontSize(7)
                .SetFontColor(CINZA_MEDIO));

            cardsTable.AddCell(cardCongregacoes);

            document.Add(cardsTable);
        }

        private static Cell CriarCardEstatisticaVisivel(
            string titulo,
            decimal valor,
            string subtitulo,
            DeviceRgb cor,
            PdfFont fontBold,
            PdfFont fontRegular)
        {
            // ? CORREÇÃO: Fundo branco com borda colorida para máximo contraste
            var card = new Cell()
                .SetBorder(new SolidBorder(cor, 2))
                .SetBackgroundColor(BRANCO)
                .SetPadding(8)
                .SetTextAlignment(TextAlignment.CENTER);

            // Título em cinza escuro (visível no fundo branco)
            card.Add(new Paragraph(titulo)
                .SetFont(fontRegular)
                .SetFontSize(8)
                .SetFontColor(CINZA_ESCURO)
                .SetMarginBottom(2));

            // Valor principal na cor da borda (destaque)
            card.Add(new Paragraph(valor.ToString("C"))
                .SetFont(fontBold)
                .SetFontSize(13)
                .SetFontColor(cor)
                .SetMarginBottom(2));

            // Subtítulo em cinza médio
            card.Add(new Paragraph(subtitulo)
                .SetFont(fontRegular)
                .SetFontSize(7)
                .SetFontColor(CINZA_MEDIO));

            return card;
        }

        private static void AdicionarRankings(Document document, PdfFont fontBold, PdfFont fontRegular, DashboardPastorViewModel model)
        {
            var tituloSecao = new Paragraph("Rankings do Mês")
                .SetFont(fontBold)
                .SetFontSize(11)
                .SetFontColor(CINZA_ESCURO)
                .SetMarginBottom(5);
            document.Add(tituloSecao);

            var rankingsTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(8);

            // Coluna Esquerda: Top 5 Receitas
            var colEsquerda = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPadding(3);

            colEsquerda.Add(new Paragraph("Top 5 - Receitas do Mês")
                .SetFont(fontBold)
                .SetFontSize(9)
                .SetFontColor(SUCCESS_COLOR)
                .SetMarginBottom(5));

            if (model.RankingReceitas.Any())
            {
                foreach (var item in model.RankingReceitas.Take(5))
                {
                    var rankingItem = CriarItemRankingAlinhado(
                        item.Posicao,
                        item.NomeCongregacao,
                        item.Valor,
                        SUCCESS_COLOR,
                        fontBold,
                        fontRegular);
                    colEsquerda.Add(rankingItem);
                }
            }
            else
            {
                colEsquerda.Add(new Paragraph("Nenhuma receita registrada")
                    .SetFont(fontRegular)
                    .SetFontSize(8)
                    .SetFontColor(CINZA_MEDIO)
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            rankingsTable.AddCell(colEsquerda);

            // Coluna Direita: Top 5 Despesas
            var colDireita = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPadding(3);

            colDireita.Add(new Paragraph("Top 5 - Despesas do Mês")
                .SetFont(fontBold)
                .SetFontSize(9)
                .SetFontColor(DANGER_COLOR)
                .SetMarginBottom(5));

            if (model.RankingDespesas.Any())
            {
                foreach (var item in model.RankingDespesas.Take(5))
                {
                    var rankingItem = CriarItemRankingAlinhado(
                        item.Posicao,
                        item.NomeCongregacao,
                        item.Valor,
                        DANGER_COLOR,
                        fontBold,
                        fontRegular);
                    colDireita.Add(rankingItem);
                }
            }
            else
            {
                colDireita.Add(new Paragraph("Nenhuma despesa registrada")
                    .SetFont(fontRegular)
                    .SetFontSize(8)
                    .SetFontColor(CINZA_MEDIO)
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            rankingsTable.AddCell(colDireita);

            document.Add(rankingsTable);
        }

        private static Table CriarItemRankingAlinhado(
            int posicao,
            string nome,
            decimal valor,
            DeviceRgb cor,
            PdfFont fontBold,
            PdfFont fontRegular)
        {
            // ? CORREÇÃO: Usar Table ao invés de Div para controle preciso de alinhamento
            var rankingTable = new Table(UnitValue.CreatePercentArray(new float[] { 0.5f, 2f, 1.5f }))
                .UseAllAvailableWidth()
                .SetBackgroundColor(CINZA_CLARO)
                .SetBorder(Border.NO_BORDER)
                .SetMarginBottom(3);

            // Coluna 1: Posição
            var cellPosicao = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPadding(4)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            cellPosicao.Add(new Paragraph($"{posicao}º")
                .SetFont(fontBold)
                .SetFontSize(8)
                .SetFontColor(cor)
                .SetMarginBottom(0));

            rankingTable.AddCell(cellPosicao);

            // Coluna 2: Nome da Congregação
            var cellNome = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPadding(4)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            var nomeTruncado = nome.Length > 22 ? nome.Substring(0, 22) + "..." : nome;
            cellNome.Add(new Paragraph(nomeTruncado)
                .SetFont(fontRegular)
                .SetFontSize(8)
                .SetFontColor(CINZA_ESCURO)
                .SetMarginBottom(0));

            rankingTable.AddCell(cellNome);

            // Coluna 3: Valor (sem quebra de linha)
            var cellValor = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPadding(4)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            cellValor.Add(new Paragraph(valor.ToString("C"))
                .SetFont(fontBold)
                .SetFontSize(9)
                .SetFontColor(cor)
                .SetMarginBottom(0)
                .SetKeepTogether(true)); // ? Evita quebra de linha

            rankingTable.AddCell(cellValor);

            return rankingTable;
        }

        private static void AdicionarMaioresDespesas(Document document, PdfFont fontBold, PdfFont fontRegular, DashboardPastorViewModel model)
        {
            var tituloSecao = new Paragraph("Maiores Categorias de Despesa")
                .SetFont(fontBold)
                .SetFontSize(11) // Reduzido de 14 para 11
                .SetFontColor(CINZA_ESCURO)
                .SetMarginBottom(5); // Reduzido de 10 para 5
            document.Add(tituloSecao);

            if (!model.MaioresDespesas.Any())
            {
                document.Add(new Paragraph("Nenhuma despesa categorizada")
                    .SetFont(fontRegular)
                    .SetFontSize(8)
                    .SetFontColor(CINZA_MEDIO)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(8));
                return;
            }

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1, 2 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(8); // Reduzido de 15 para 8

            // Cabeçalho
            table.AddHeaderCell(CriarCelulaCabecalho("Categoria", fontBold));
            table.AddHeaderCell(CriarCelulaCabecalho("Ocorrências", fontBold));
            table.AddHeaderCell(CriarCelulaCabecalho("Valor Total", fontBold));

            // Dados
            foreach (var despesa in model.MaioresDespesas.Take(5))
            {
                table.AddCell(CriarCelulaDado(despesa.CategoriaDespesa, fontRegular));
                table.AddCell(CriarCelulaDado(despesa.QuantidadeOcorrencias.ToString(), fontRegular, TextAlignment.CENTER));
                table.AddCell(CriarCelulaDado(despesa.ValorTotal.ToString("C"), fontBold, TextAlignment.RIGHT, DANGER_COLOR));
            }

            document.Add(table);
        }

        private static void AdicionarDetalhamentoCongregacoes(Document document, PdfFont fontBold, PdfFont fontRegular, DashboardPastorViewModel model)
        {
            var tituloSecao = new Paragraph($"Indicadores por Congregação ({model.Congregacoes.Count})")
                .SetFont(fontBold)
                .SetFontSize(11) // Reduzido de 14 para 11
                .SetFontColor(CINZA_ESCURO)
                .SetMarginBottom(5); // Reduzido de 10 para 5
            document.Add(tituloSecao);

            if (!model.Congregacoes.Any())
            {
                document.Add(new Paragraph("Nenhuma congregação cadastrada")
                    .SetFont(fontRegular)
                    .SetFontSize(8)
                    .SetFontColor(CINZA_MEDIO)
                    .SetTextAlignment(TextAlignment.CENTER));
                return;
            }

            foreach (var cong in model.Congregacoes)
            {
                // Card de congregação compacto
                var congTable = new Table(UnitValue.CreatePercentArray(new float[] { 1 }))
                    .UseAllAvailableWidth()
                    .SetMarginBottom(6) // Reduzido de 10 para 6
                    .SetBorder(new SolidBorder(PRIMARY_COLOR, 1))
                    .SetBorderRadius(new BorderRadius(4)); // Reduzido de 6 para 4

                var congCell = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetPadding(6) // Reduzido de 10 para 6
                    .SetBackgroundColor(CINZA_CLARO);

                // Nome e Status
                var nomePara = new Paragraph()
                    .Add(new Text(cong.NomeCongregacao).SetFont(fontBold).SetFontSize(9).SetFontColor(PRIMARY_COLOR)) // Reduzido de 11 para 9
                    .Add(new Text($" | {cong.TipoCongregacao}").SetFont(fontRegular).SetFontSize(7).SetFontColor(CINZA_MEDIO)) // Reduzido de 9 para 7
                    .SetMarginBottom(4); // Reduzido de 6 para 4
                congCell.Add(nomePara);

                // Indicadores em tabela compacta
                var indicadoresTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1, 1, 1 }))
                    .UseAllAvailableWidth();

                // Receitas
                indicadoresTable.AddCell(CriarCelulaIndicadorCompacto(
                    "Receitas",
                    cong.ReceitasAcumuladas,
                    $"Mês: {cong.ReceitasMesAtual:C}",
                    SUCCESS_COLOR,
                    fontBold,
                    fontRegular));

                // Despesas
                indicadoresTable.AddCell(CriarCelulaIndicadorCompacto(
                    "Despesas",
                    cong.DespesasAcumuladas,
                    $"Mês: {cong.DespesasMesAtual:C}",
                    DANGER_COLOR,
                    fontBold,
                    fontRegular));

                // Saldo
                var saldoCor = cong.SaldoAtual >= 0 ? PRIMARY_COLOR : DANGER_COLOR;
                indicadoresTable.AddCell(CriarCelulaIndicadorCompacto(
                    "Saldo Atual",
                    cong.SaldoAtual,
                    $"Rent.: {cong.PercentualLucro:F1}%",
                    saldoCor,
                    fontBold,
                    fontRegular));

                // Rateios
                indicadoresTable.AddCell(CriarCelulaIndicadorCompacto(
                    "Rateios",
                    cong.RateiosEnviados,
                    $"Rec.: {cong.RateiosRecebidos:C}",
                    WARNING_COLOR,
                    fontBold,
                    fontRegular));

                congCell.Add(indicadoresTable);
                congTable.AddCell(congCell);
                document.Add(congTable);
            }
        }

        private static Cell CriarCelulaIndicadorCompacto(
            string titulo,
            decimal valor,
            string subtitulo,
            DeviceRgb cor,
            PdfFont fontBold,
            PdfFont fontRegular)
        {
            var cell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPadding(3); // Reduzido de 4 para 3

            cell.Add(new Paragraph(titulo)
                .SetFont(fontRegular)
                .SetFontSize(7) // Reduzido de 8 para 7
                .SetFontColor(CINZA_MEDIO)
                .SetMarginBottom(1)); // Reduzido de 2 para 1

            cell.Add(new Paragraph(valor.ToString("C"))
                .SetFont(fontBold)
                .SetFontSize(9) // Reduzido de 10 para 9
                .SetFontColor(cor)
                .SetMarginBottom(1)); // Reduzido de 2 para 1

            cell.Add(new Paragraph(subtitulo)
                .SetFont(fontRegular)
                .SetFontSize(6) // Reduzido de 7 para 6
                .SetFontColor(CINZA_MEDIO));

            return cell;
        }

        private static void AdicionarRodape(Document document, PdfFont fontRegular, int totalPaginas)
        {
            var linha = new Paragraph("_".PadRight(100, '_'))
                .SetFontSize(7) // Reduzido de 8 para 7
                .SetFontColor(CINZA_CLARO)
                .SetMarginTop(10) // Reduzido de 20 para 10
                .SetMarginBottom(5) // Reduzido de 10 para 5
                .SetTextAlignment(TextAlignment.CENTER);
            document.Add(linha);

            var rodape = new Paragraph($"Documento gerado em {DateTime.Now:dd/MM/yyyy HH:mm} | Página {totalPaginas} | Sistema de Tesouraria Eclesiástica © {DateTime.Now.Year}")
                .SetFont(fontRegular)
                .SetFontSize(7) // Reduzido de 8 para 7
                .SetFontColor(CINZA_MEDIO)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(3); // Reduzido de 5 para 3
            document.Add(rodape);

            var avisoSeguranca = new Paragraph("Este documento contém informações estratégicas e é de uso exclusivo do pastor e liderança.")
                .SetFont(fontRegular)
                .SetFontSize(6) // Reduzido de 7 para 6
                .SetFontColor(CINZA_MEDIO)
                .SetTextAlignment(TextAlignment.CENTER);
            document.Add(avisoSeguranca);
        }

        private static Cell CriarCelulaCabecalho(string texto, PdfFont font)
        {
            return new Cell()
                .SetBackgroundColor(PRIMARY_COLOR)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(4) // Reduzido de 6 para 4
                .Add(new Paragraph(texto)
                    .SetFont(font)
                    .SetFontSize(8) // Reduzido de 9 para 8
                    .SetFontColor(BRANCO)
                    .SetTextAlignment(TextAlignment.CENTER));
        }

        private static Cell CriarCelulaDado(
            string texto,
            PdfFont font,
            TextAlignment? alignment = null,
            DeviceRgb? textColor = null)
        {
            var paragraph = new Paragraph(texto)
                .SetFont(font)
                .SetFontSize(8) // Reduzido de 9 para 8
                .SetFontColor(textColor ?? CINZA_ESCURO);

            if (alignment.HasValue)
            {
                paragraph.SetTextAlignment(alignment.Value);
            }

            return new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPadding(3) // Reduzido de 5 para 3
                .Add(paragraph);
        }
    }
}
