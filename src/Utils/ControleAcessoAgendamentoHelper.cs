using System.Text;
using System.Xml;
using Spectre.Console;

namespace InstaladorNewAcesso.Utils;

public static class ControleAcessoAgendamentoHelper
{
    private const string FileName = "AgendamentoEquipOffline.xml";

    /// <summary>
    /// Após instalar o ControleAcesso, verifica se o diretório de destino
    /// contém o AgendamentoEquipOffline.xml. Se existir, lê os valores
    /// atuais e permite ao usuário editá-los, mantendo os valores
    /// anteriores como padrão nos prompts.
    /// </summary>
    public static bool UpdateAgendamentoAfterInstall(string targetDirectory)
    {
        var filePath = Path.Combine(targetDirectory, FileName);

        if (!File.Exists(filePath))
        {
            AnsiConsole.MarkupLine($"[gray]   [[INFO]] AgendamentoEquipOffline.xml (ControleAcesso) não encontrado em: {filePath.EscapeMarkup()}[/]");
            return false;
        }

        try
        {
            AnsiConsole.MarkupLine($"\n   [bold yellow]Configuração do Agendamento de Equipamentos Offline (ControleAcesso):[/]");

            // --- Lê valores atuais do XML existente ---
            var (currentIds, currentHora, currentMinuto, currentDiasSemana, currentObter, currentEnviar)
                = ReadExistingValues(filePath);
            // Ativo é sempre true (definição do sistema), não exposto ao usuário

            // --- Prompts com valores atuais como padrão ---
            var idsDefault = currentIds.Count > 0 ? string.Join("|", currentIds) : "3|7|9";
            var idsInput = AnsiConsole.Ask<string>(
                $"   [bold yellow]IDs dos equipamentos[/] (separados por |, atual: [gray]{idsDefault.EscapeMarkup()}[/]):");
            if (string.IsNullOrWhiteSpace(idsInput))
                idsInput = idsDefault;

            var ids = idsInput
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(id => id.Trim())
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            if (ids.Count == 0)
            {
                AnsiConsole.MarkupLine("   [yellow][AVISO] Nenhum ID informado. Agendamento não será alterado.[/]");
                return false;
            }

            var hora = AnsiConsole.Ask<string>(
                $"   [bold yellow]Hora[/] (0-23, atual: [gray]{currentHora}[/]):");
            if (!int.TryParse(hora, out var horaVal) || horaVal < 0 || horaVal > 23)
                horaVal = currentHora;

            var minuto = AnsiConsole.Ask<string>(
                $"   [bold yellow]Minuto[/] (0-59, atual: [gray]{currentMinuto}[/]):");
            if (!int.TryParse(minuto, out var minVal) || minVal < 0 || minVal > 59)
                minVal = currentMinuto;

            var diasSemana = AnsiConsole.Ask<string>(
                $"   [bold yellow]Dias da semana[/] (0-6 separados por |, atual: [gray]{currentDiasSemana.EscapeMarkup()}[/]):");
            if (string.IsNullOrWhiteSpace(diasSemana))
                diasSemana = currentDiasSemana;

            var obterArquivos = AnsiConsole.Confirm(
                "   [bold yellow]Obter arquivos dos equipamentos?[/]", currentObter);
            var enviarArquivos = AnsiConsole.Confirm(
                "   [bold yellow]Enviar arquivos para os equipamentos?[/]", currentEnviar);

            // --- Gera o XML atualizado ---
            var doc = new XmlDocument();
            var root = doc.CreateElement("Agendamentos");
            doc.AppendChild(root);

            foreach (var id in ids)
            {
                var ag = doc.CreateElement("Agendamento");

                AddComment(ag, "PARA UTLIZAR O AGENDAMENTO PARA MAIS DE UM EQUIPAMENTO\n        SEPARE OS IDENTIFICADORES POR '|' Ex: 3|7|9\n    ");
                AddElement(ag, "EquipamentoId", id);

                AddComment(ag, "VALOR ENTRE 0 E 23\n    ");
                AddElement(ag, "Hora", horaVal.ToString());

                AddComment(ag, "VALOR ENTRE 0 E 59\n    ");
                AddElement(ag, "Minuto", minVal.ToString());

                AddComment(ag, "DIA DA SEMANA EM QUE SERÁ EXECUTADO\n     0 - DOMINGO\n     1 - SEGUNDA-FEIRA\n     2 - TERÇA-FEIRA\n     3 - QUARTA-FEIRA\n     4 - QUINTA-FEIRA\n     5 - SEXTA-FEIRA\n     6 - SÁBADO  \n     Informar valores separados por '|' Ex: '0|2|6' (executará nos dias; segunda, terça e sábado)\n    ");
                AddElement(ag, "DiasSemana", diasSemana);

                AddComment(ag, "DIA DA SEMANA EM QUE SERÁ EXECUTADO\n     0 - DOMINGO\n     1 - SEGUNDA-FEIRA\n     2 - TERÇA-FEIRA\n     3 - QUARTA-FEIRA\n     4 - QUINTA-FEIRA\n     5 - SEXTA-FEIRA\n     6 - SÁBADO  \n     Informar valores separados por '|' Ex: '0|2|6' (executará nos dias; segunda, terça e sábado)\n    ");
                AddElement(ag, "DiasSemana", diasSemana);

                AddComment(ag, "true OU false\n    ");
                AddElement(ag, "Ativo", "true");

                AddComment(ag, "Possibilita somente a obtenção de arquivos\n    ");
                AddElement(ag, "ObterArquivos", obterArquivos ? "true" : "false");

                AddComment(ag, "Possibilita somente o Envio dos arquivos\n    ");
                AddElement(ag, "EnviarArquivos", enviarArquivos ? "true" : "false");

                root.AppendChild(ag);
            }

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                Encoding = new UTF8Encoding(false),
                OmitXmlDeclaration = false
            };

            using var writer = XmlWriter.Create(filePath, settings);
            doc.Save(writer);

            AnsiConsole.MarkupLine($"   [green][[OK]] AgendamentoEquipOffline.xml (ControleAcesso) atualizado com {ids.Count} equipamento(s).[/]");
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]   [ERRO] Falha ao configurar Agendamento (ControleAcesso): {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
    }

    /// <summary>
    /// Lê o XML existente e extrai os valores atuais para usar como padrão.
    /// Retorna tupla com: lista de IDs, hora, minuto, diasSemana, obterArquivos, enviarArquivos.
    /// </summary>
    private static (List<string> ids, int hora, int minuto, string diasSemana, bool obterArquivos, bool enviarArquivos)
        ReadExistingValues(string filePath)
    {
        var defaultIds = new List<string>();
        var defaultHora = 11;
        var defaultMinuto = 13;
        var defaultDiasSemana = "0|1|2|3|4|5|6";
        var defaultObter = true;
        var defaultEnviar = true;
        // Ativo sempre true por padrão (definição do sistema)

        try
        {
            var doc = new XmlDocument();
            doc.Load(filePath);

            var root = doc.DocumentElement;
            if (root?.Name != "Agendamentos")
                return (defaultIds, defaultHora, defaultMinuto, defaultDiasSemana, defaultObter, defaultEnviar);

            var agendamentos = root.SelectNodes("Agendamento");
            if (agendamentos == null || agendamentos.Count == 0)
                return (defaultIds, defaultHora, defaultMinuto, defaultDiasSemana, defaultObter, defaultEnviar);

            // Extrai IDs de todos os Agendamento
            foreach (XmlNode node in agendamentos)
            {
                if (node is not XmlElement ag) continue;

                var equipId = ag.SelectSingleNode("EquipamentoId")?.InnerText?.Trim();
                if (!string.IsNullOrEmpty(equipId))
                    defaultIds.Add(equipId);
            }

            // Extrai valores do primeiro Agendamento (todos compartilham o mesmo padrão)
            var first = (XmlElement)agendamentos[0]!;

            if (int.TryParse(first.SelectSingleNode("Hora")?.InnerText?.Trim(), out var hora))
                defaultHora = hora;

            if (int.TryParse(first.SelectSingleNode("Minuto")?.InnerText?.Trim(), out var minuto))
                defaultMinuto = minuto;

            var ds = first.SelectSingleNode("DiasSemana")?.InnerText?.Trim();
            if (!string.IsNullOrEmpty(ds))
                defaultDiasSemana = ds;

            if (bool.TryParse(first.SelectSingleNode("ObterArquivos")?.InnerText?.Trim(), out var obter))
                defaultObter = obter;

            if (bool.TryParse(first.SelectSingleNode("EnviarArquivos")?.InnerText?.Trim(), out var enviar))
                defaultEnviar = enviar;
        }
        catch
        {
            // Se falhar ao ler, usa defaults
        }

        return (defaultIds, defaultHora, defaultMinuto, defaultDiasSemana, defaultObter, defaultEnviar);
    }

    private static void AddComment(XmlElement parent, string text)
    {
        var comment = parent.OwnerDocument!.CreateComment(text);
        parent.AppendChild(comment);
    }

    private static void AddElement(XmlElement parent, string name, string value)
    {
        var el = parent.OwnerDocument!.CreateElement(name);
        el.InnerText = value;
        parent.AppendChild(el);
    }
}
