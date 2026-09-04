using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class AutoAtendimentoConfigHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static bool UpdateConfig(string autoAtendimentoDir, string? urlApi = null, string? urlUi = null, string? apiKey = null, string? dbConnectionString = null)
    {
        var apiDir = Path.Combine(autoAtendimentoDir, "WebAPI");
        var appDir = Path.Combine(autoAtendimentoDir, "WebAPP");

        var apiOk = UpdateApiConfig(apiDir, apiKey, dbConnectionString);
        var appOk = UpdateAppConfig(appDir, urlApi, urlUi, apiKey);

        return apiOk || appOk;
    }

    private static bool UpdateApiConfig(string apiDir, string? apiKey, string? dbConnectionString)
    {
        var configPath = Path.Combine(apiDir, "appsettings.json");
        if (!File.Exists(configPath))
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] AutoAtendimento WebAPI appsettings.json nao encontrado em: {MarkupHelper.Escape(apiDir)}[/]");
            return false;
        }

        try
        {
            ConfigBackupService.BackupSingleFile(configPath);

            var jsonText = File.ReadAllText(configPath);
            var rootNode = JsonNode.Parse(jsonText) as JsonObject ?? [];

            if (!string.IsNullOrEmpty(apiKey))
            {
                rootNode["ApiKey"] = apiKey;
            }

            if (!string.IsNullOrEmpty(dbConnectionString))
            {
                var connStrings = rootNode["ConnectionStrings"] as JsonObject ?? [];
                connStrings["AutoAtendimentoSqlServer"] = dbConnectionString;
                rootNode["ConnectionStrings"] = connStrings;
            }

            File.WriteAllText(configPath, rootNode.ToJsonString(JsonOptions));
            UIScope.WriteMessage("   [green][[OK]] AutoAtendimento WebAPI appsettings.json configurado.[/]");
            return true;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao atualizar AutoAtendimento WebAPI appsettings: {MarkupHelper.Escape(ex.Message)}[/]");
            return false;
        }
    }

    private static bool UpdateAppConfig(string appDir, string? urlApi, string? urlUi, string? apiKey)
    {
        if (!Directory.Exists(appDir))
            return false;

        var configPaths = new List<string>();
        var direct = Path.Combine(appDir, "appsettings.json");
        if (File.Exists(direct)) configPaths.Add(direct);

        var nested = Path.Combine(appDir, "WebAPP", "appsettings.json");
        if (File.Exists(nested)) configPaths.Add(nested);

        if (configPaths.Count == 0)
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] AutoAtendimento WebAPP appsettings.json nao encontrado em: {MarkupHelper.Escape(appDir)}[/]");
            return false;
        }

        var anyUpdated = false;
        foreach (var configPath in configPaths)
        {
            try
            {
                ConfigBackupService.BackupSingleFile(configPath);

                var jsonText = File.ReadAllText(configPath);
                var rootNode = JsonNode.Parse(jsonText) as JsonObject ?? [];

                var resolvedApi = urlApi ?? "http://localhost:8082";
                var resolvedUi = urlUi ?? "http://localhost:8081";

                rootNode["URLapi"] = resolvedApi;
                rootNode["URLnewAcessoUI"] = resolvedUi;

                if (!string.IsNullOrEmpty(apiKey))
                {
                    var apiKeysNode = rootNode["ApiKeys"] as JsonObject ?? [];
                    apiKeysNode["VisitasApiKey"] = apiKey;
                    rootNode["ApiKeys"] = apiKeysNode;
                }

                File.WriteAllText(configPath, rootNode.ToJsonString(JsonOptions));
                anyUpdated = true;
                UIScope.WriteMessage($"   [green][[OK]] AutoAtendimento WebAPP appsettings.json ({MarkupHelper.Escape(Path.GetFileName(Path.GetDirectoryName(configPath)) ?? "")}) configurado.[/]");
            }
            catch (Exception ex)
            {
                UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao atualizar WebAPP appsettings em {MarkupHelper.Escape(configPath)}: {MarkupHelper.Escape(ex.Message)}[/]");
            }
        }

        return anyUpdated;
    }
}
