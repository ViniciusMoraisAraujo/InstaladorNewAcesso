using System.Globalization;
using System.Text;
using System.Xml;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class ControleAcessoAgendamentoHelper
{
    private const string FileName = "AgendamentoEquipOffline.xml";

    /// <summary>
    /// Apos instalar o ControleAcesso, verifica se o diretorio de destino
    /// contem o AgendamentoEquipOffline.xml. Se existir, le os valores
    /// atuais e permite ao usuario edita-los, mantendo os valores
    /// anteriores como padrao nos prompts.
    /// </summary>
    public static bool UpdateAgendamentoAfterInstall(string targetDirectory)
    {
        var normalizedDir = ConfigHelperBase.NormalizeDirectoryPath(targetDirectory);
        var filePath = Path.Combine(normalizedDir, FileName);

        if (!File.Exists(filePath))
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] AgendamentoEquipOffline.xml (ControleAcesso) nao encontrado em: {MarkupHelper.Escape(filePath)}[/]");
            return false;
        }

        try
        {
            UIScope.WriteMessage($"\n   [bold yellow]Configuracao do Agendamento de Equipamentos Offline (ControleAcesso):[/]");

            // --- Le valores atuais do XML existente ---
            var (currentIds, currentHora, currentMinuto, currentDiasSemana, currentObter, currentEnviar, isLegacy)
                = ReadExistingValues(filePath);

            // --- Prompts com valores atuais como padrao ---
            var idsDefault = currentIds.Count > 0 ? string.Join("|", currentIds) : "3|7|9";
            var idsInput = UIScope.AskInput(
                $"   [bold yellow]IDs dos equipamentos[/] (separados por |, atual: [gray]{MarkupHelper.Escape(idsDefault)}[/]):");
            if (string.IsNullOrWhiteSpace(idsInput))
                idsInput = idsDefault;

            var ids = idsInput
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(id => id.Trim())
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            if (ids.Count == 0)
            {
                UIScope.WriteMessage("[yellow]   [[AVISO]]  Nenhum ID informado. Agendamento nao sera alterado.[/]");
                return false;
            }

            var hora = UIScope.AskInput(
                $"   [bold yellow]Hora[/] (0-23, atual: [gray]{currentHora}[/]):");
            if (!int.TryParse(hora, out var horaVal) || horaVal < 0 || horaVal > 23)
                horaVal = currentHora;

            var minuto = UIScope.AskInput(
                $"   [bold yellow]Minuto[/] (0-59, atual: [gray]{currentMinuto}[/]):");
            if (!int.TryParse(minuto, out var minVal) || minVal < 0 || minVal > 59)
                minVal = currentMinuto;

            var diasSemana = UIScope.AskInput(
                $"   [bold yellow]Dias da semana[/] (0-6 separados por |, atual: [gray]{MarkupHelper.Escape(currentDiasSemana)}[/]):");
            if (string.IsNullOrWhiteSpace(diasSemana))
                diasSemana = currentDiasSemana;

            var obterArquivos = UIScope.Confirm(
                "   [bold yellow]Obter arquivos dos equipamentos?[/]", currentObter);
            var enviarArquivos = UIScope.Confirm(
                "   [bold yellow]Enviar arquivos para os equipamentos?[/]", currentEnviar);

            ConfigBackupService.BackupSingleFile(filePath);

            if (isLegacy)
            {
                var legacyDoc = new XmlDocument();
                legacyDoc.Load(filePath);
                var legacyRoot = legacyDoc.DocumentElement!;

                var idsNode = legacyRoot.SelectSingleNode("IdsEquipamentos") ?? legacyRoot.SelectSingleNode("EquipamentoId");
                if (idsNode != null)
                    idsNode.InnerText = idsInput;

                var horaNode = legacyRoot.SelectSingleNode("HoraInicio");
                if (horaNode != null)
                    horaNode.InnerText = $"{horaVal:D2}:{minVal:D2}";

                var diasNode = legacyRoot.SelectSingleNode("DiasSemana");
                if (diasNode != null)
                    diasNode.InnerText = diasSemana;

                var settingsLegacy = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    Encoding = new UTF8Encoding(false),
                    OmitXmlDeclaration = false
                };

                using var legacyWriter = XmlWriter.Create(filePath, settingsLegacy);
                legacyDoc.Save(legacyWriter);

                UIScope.WriteMessage($"   [green][[OK]] AgendamentoEquipOffline.xml (ControleAcesso) atualizado.[/]");
                return true;
            }

            // --- Gera o XML atualizado (novo formato) ---
            var doc = new XmlDocument();
            var root = doc.CreateElement("Agendamentos");
            doc.AppendChild(root);

            foreach (var id in ids)
            {
                var ag = doc.CreateElement("Agendamento");

                AddComment(ag, "PARA UTLIZAR O AGENDAMENTO PARA MAIS DE UM EQUIPAMENTO\n        SEPARE OS IDENTIFICADORES POR \'|\' Ex: 3|7|9\n    ");
                AddElement(ag, "EquipamentoId", id);

                AddComment(ag, "VALOR ENTRE 0 E 23\n    ");
                AddElement(ag, "Hora", horaVal.ToString(CultureInfo.InvariantCulture));

                AddComment(ag, "VALOR ENTRE 0 E 59\n    ");
                AddElement(ag, "Minuto", minVal.ToString(CultureInfo.InvariantCulture));

                AddComment(ag, "DIA DA SEMANA EM QUE SERA EXECUTADO\n     0 - DOMINGO\n     1 - SEGUNDA-FEIRA\n     2 - TERCA-FEIRA\n     3 - QUARTA-FEIRA\n     4 - QUINTA-FEIRA\n     5 - SEXTA-FEIRA\n     6 - SABADO  \n     Informar valores separados por \'|\' Ex: \'0|2|6\' (executara nos dias; segunda, terca e sabado)\n    ");
                AddElement(ag, "DiasSemana", diasSemana);

                AddComment(ag, "true OU false\n    ");
                AddElement(ag, "Ativo", "true");

                AddComment(ag, "Possibilita somente a obtencao de arquivos\n    ");
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

            UIScope.WriteMessage($"   [green][[OK]] AgendamentoEquipOffline.xml (ControleAcesso) gerado com sucesso.[/]");
            return true;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao configurar Agendamento (ControleAcesso): {MarkupHelper.Escape(ex.Message)}[/]");
            return false;
        }
    }

    /// <summary>
    /// Versao sem interacao com o usuario — aceita parametros diretamente.
    /// Util para integracao com WinForms (ScheduleControl) ou automacao.
    /// Gera o XML AgendamentoEquipOffline.xml com os parametros fornecidos.
    /// </summary>
    public static bool UpdateAgendamento(string targetDirectory, string hora, string diasSemana, string intervalo)
    {
        ArgumentNullException.ThrowIfNull(hora);
        var normalizedDir = ConfigHelperBase.NormalizeDirectoryPath(targetDirectory);
        var filePath = Path.Combine(normalizedDir, FileName);

        try
        {
            if (!int.TryParse(hora.Split(':' )[0], out var horaVal) || horaVal < 0 || horaVal > 23)
                horaVal = 22;
            if (!int.TryParse(hora.Split(':' )[1], out var minVal) || minVal < 0 || minVal > 59)
                minVal = 0;

            if (File.Exists(filePath))
            {
                ConfigBackupService.BackupSingleFile(filePath);
            }

            var doc = new XmlDocument();
            var root = doc.CreateElement("Agendamentos");
            doc.AppendChild(root);

            // IDs padrao 3|7|9
            var ids = new[] { "3", "7", "9" };

            foreach (var id in ids)
            {
                var ag = doc.CreateElement("Agendamento");

                AddComment(ag, "SEPARE OS IDENTIFICADORES POR \'|\' Ex: 3|7|9");
                AddElement(ag, "EquipamentoId", id);

                AddComment(ag, "VALOR ENTRE 0 E 23");
                AddElement(ag, "Hora", horaVal.ToString(CultureInfo.InvariantCulture));

                AddComment(ag, "VALOR ENTRE 0 E 59");
                AddElement(ag, "Minuto", minVal.ToString(CultureInfo.InvariantCulture));

                AddComment(ag, "DIAS DA SEMANA: 0=DOM, 1=SEG, ..., 6=SAB");
                AddElement(ag, "DiasSemana", diasSemana);

                AddComment(ag, "true OU false");
                AddElement(ag, "Ativo", "true");

                AddComment(ag, "true OU false");
                AddElement(ag, "ObterArquivos", "true");

                AddComment(ag, "true OU false");
                AddElement(ag, "EnviarArquivos", "true");

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

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Le o XML existente e extrai os valores atuais para usar como padrao.
    /// Retorna tupla com: lista de IDs, hora, minuto, diasSemana, obterArquivos, enviarArquivos, isLegacy.
    /// </summary>
    private static (List<string> ids, int hora, int minuto, string diasSemana, bool obterArquivos, bool enviarArquivos, bool isLegacy)
        ReadExistingValues(string filePath)
    {
        var defaultIds = new List<string>();
        var defaultHora = 11;
        var defaultMinuto = 13;
        var defaultDiasSemana = "0|1|2|3|4|5|6";
        var defaultObter = true;
        var defaultEnviar = true;
        var isLegacy = false;

        try
        {
            var doc = new XmlDocument();
            doc.Load(filePath);

            var root = doc.DocumentElement;
            if (root == null)
                return (defaultIds, defaultHora, defaultMinuto, defaultDiasSemana, defaultObter, defaultEnviar, false);

            if (root.Name.Equals("Agendamento", StringComparison.OrdinalIgnoreCase))
            {
                isLegacy = true;
                var singleEquipId = root.SelectSingleNode("IdsEquipamentos")?.InnerText?.Trim()
                                 ?? root.SelectSingleNode("EquipamentoId")?.InnerText?.Trim()
                                 ?? root.SelectSingleNode("EquipamentoIDs")?.InnerText?.Trim()
                                 ?? root.SelectSingleNode("IDs")?.InnerText?.Trim();
                if (!string.IsNullOrEmpty(singleEquipId))
                {
                    defaultIds.AddRange(singleEquipId.Split('|', StringSplitOptions.RemoveEmptyEntries));
                }

                var horaInicioStr = root.SelectSingleNode("HoraInicio")?.InnerText?.Trim();
                if (!string.IsNullOrEmpty(horaInicioStr) && int.TryParse(horaInicioStr.Split(':' )[0], out var hi))
                    defaultHora = hi;

                var parts = horaInicioStr?.Split(':' );
                if (parts?.Length > 1 && int.TryParse(parts[1], out var mi))
                    defaultMinuto = mi;

                var ds = root.SelectSingleNode("DiasSemana")?.InnerText?.Trim();
                if (!string.IsNullOrEmpty(ds))
                    defaultDiasSemana = ds;

                if (bool.TryParse(root.SelectSingleNode("ObterArquivos")?.InnerText?.Trim(), out var obter))
                    defaultObter = obter;

                if (bool.TryParse(root.SelectSingleNode("EnviarArquivos")?.InnerText?.Trim(), out var enviar))
                    defaultEnviar = enviar;

                return (defaultIds, defaultHora, defaultMinuto, defaultDiasSemana, defaultObter, defaultEnviar, isLegacy);
            }

            var agendamentos = root.SelectNodes("Agendamento")?.Cast<XmlNode>().ToList() ?? [];

            foreach (XmlNode node in agendamentos)
            {
                if (node is not XmlElement ag) continue;

                var equipId = ag.SelectSingleNode("EquipamentoId")?.InnerText?.Trim()
                           ?? ag.SelectSingleNode("IdsEquipamentos")?.InnerText?.Trim()
                           ?? ag.SelectSingleNode("EquipamentoIDs")?.InnerText?.Trim()
                           ?? ag.SelectSingleNode("IDs")?.InnerText?.Trim();
                if (!string.IsNullOrEmpty(equipId))
                {
                    defaultIds.AddRange(equipId.Split('|', StringSplitOptions.RemoveEmptyEntries));
                }
            }

            var first = (agendamentos.Count > 0 && agendamentos[0] is XmlElement el) ? el : root;

            var horaStr = first.SelectSingleNode("Hora")?.InnerText?.Trim();
            if (int.TryParse(horaStr, out var hora))
            {
                defaultHora = hora;
            }

            var minStr = first.SelectSingleNode("Minuto")?.InnerText?.Trim();
            if (int.TryParse(minStr, out var minuto))
            {
                defaultMinuto = minuto;
            }

            var dias = first.SelectSingleNode("DiasSemana")?.InnerText?.Trim();
            if (!string.IsNullOrEmpty(dias))
                defaultDiasSemana = dias;

            if (bool.TryParse(first.SelectSingleNode("ObterArquivos")?.InnerText?.Trim(), out var obterVal))
                defaultObter = obterVal;

            if (bool.TryParse(first.SelectSingleNode("EnviarArquivos")?.InnerText?.Trim(), out var enviarVal))
                defaultEnviar = enviarVal;
        }
        catch
        {
            // Se falhar ao ler, usa defaults
        }

        return (defaultIds, defaultHora, defaultMinuto, defaultDiasSemana, defaultObter, defaultEnviar, isLegacy);
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