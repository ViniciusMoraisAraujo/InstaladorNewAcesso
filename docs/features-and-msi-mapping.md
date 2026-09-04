# 📦 Mapeamento de Recursos Windows, MSIs e WebApps

> **Instalador NewAcesso** — Catálogo de Componentes e Estrutura de Pastas

Este documento detalha todos os componentes gerenciados pelo Instalador NewAcesso: Features do Windows ativadas por sistema operacional, pacotes MSI suportados, estrutura de diretórios e configuração dos WebApps no IIS.

---

## ⚙️ 1. Recursos do Windows (Windows Features)

O instalador detecta automaticamente se está executando em **Windows Server** (usando `Install-WindowsFeature` via PowerShell / ServerManager) ou **Windows Desktop** (usando `dism.exe` / `pkgmgr`).

Abaixo está a matriz dos 32 recursos ativados:

| Componente / Recurso | Nome no Windows Server | Nome no Windows Desktop (DISM) |
|---|---|---|
| Extensibilidade .NET 3.5 | `Web-Net-Ext` | `IIS-NetFxExtensibility` |
| Extensibilidade .NET 4.6/4.8+ | `Web-Net-Ext45` | `IIS-NetFxExtensibility45` |
| Inicialização de Aplicativos | `Web-App-Init` | `IIS-ApplicationInit` |
| ASP Clássico | `Web-ASP` | `IIS-ASP` |
| ASP.NET 3.5 | `Web-Asp-Net` | `IIS-ASPNET` |
| ASP.NET 4.5/4.6+ | `Web-Asp-Net45` | `IIS-ASPNET45` |
| Extensões ISAPI | `Web-ISAPI-Ext` | `IIS-ISAPIExtensions` |
| Filtros ISAPI | `Web-ISAPI-Filter` | `IIS-ISAPIFilters` |
| Inclusões do Lado do Servidor (SSI) | `Web-Includes` | `IIS-ServerSideIncludes` |
| Protocolo WebSocket | `Web-WebSockets` | `IIS-WebSockets` |
| Console de Gerenciamento do IIS | `Web-Mgmt-Console` | `IIS-ManagementConsole` |
| Compatibilidade Metabase do IIS 6 | `Web-Lgcy-Metabase` | `IIS-Metabase` |
| Console de Gerenciamento do IIS 6 | `Web-Lgcy-Mgmt-Console` | `IIS-LegacyManagementConsole` |
| Ferramentas de Script do IIS 6 | `Web-Scripting-Tools` | `IIS-LegacyScripts` |
| Compatibilidade com WMI do IIS 6 | `Web-WMI` | `IIS-WMICompatibility` |
| Serviço de Gerenciamento | `Web-Mgmt-Service` | `IIS-ManagementService` |
| .NET Framework 3.5 Core | `NET-Framework-Core` | `NetFx3` |
| Ativação HTTP (Framework 3.5) | `NET-HTTP-Activation` | `WCF-HTTP-Activation` |
| Ativação Não-HTTP (Framework 3.5) | `NET-Non-HTTP-Activ` | `WCF-NonHTTP-Activation` |
| Serviços WCF - Ativação HTTP | `NET-WCF-HTTP-Activation45` | `WCF-HTTP-Activation45` |
| Ativação de Pipe Nomeado WCF | `NET-WCF-Pipe-Activation45` | `WCF-Pipe-Activation45` |
| Ativação TCP WCF | `NET-WCF-TCP-Activation45` | `WCF-TCP-Activation45` |
| Compartilhamento de Porta TCP WCF | `NET-WCF-TCP-PortSharing45` | `WCF-TCP-PortSharing45` |
| Windows PowerShell 2.0 Engine | `PowerShell-V2` | `MicrosoftWindowsPowerShellV2` |
| Cliente Telnet | `Telnet-Client` | `TelnetClient` |
| Cliente TFTP | `TFTP-Client` | `TFTP` |
| Enfileiramento de Mensagens (MSMQ) | `MSMQ-Services` | `MSMQ-Container` |
| Ativação de MSMQ WCF | `NET-WCF-MSMQ-Activation45` | `WCF-MSMQ-Activation45` |
| Modelo de Processo (WAS) | `WAS-Process-Model` | `WAS-ProcessModel` |
| Ambiente .NET 3.5 (WAS) | `WAS-NET-Environment` | `WAS-NetFxEnvironment` |
| APIs de Configuração (WAS) | `WAS-Config-APIs` | `WAS-ConfigurationAPI` |
| Servidor Telnet | `Telnet-Server` | `TelnetServer` |

---

## 📂 2. Estrutura de Diretórios Gerenciada

A árvore de diretórios do NewAcesso é configurada com base no `BasePath` definido pelo operador (por padrão `C:\`):

```
<BasePath>/
├── Instaladores/                       # Armazenamento dos pacotes MSIs
└── NewAcesso/
    ├── AutoAtendimento/                # Módulo de autoatendimento
    ├── ConexBridge/                    # Serviço de ponte de comunicação
    ├── ConnectionRecord/               # Gravação de conexões e logs
    ├── Controller/                     # Módulos de controle
    │   ├── ControleAcesso/             # Módulo de controle de acesso
    │   ├── CoreWs/                     # WebService Core de comunicação
    │   ├── Fabricantes/                # Drivers e integrações de hardwares
    │   └── Task/                       # Tarefas de sincronismo
    ├── ControllerOffline/              # Controladores em modo autônomo/offline
    │   ├── Arquivos/
    │   ├── WinService_Ex/              # StandAloneEx Service
    │   └── WinService_In/              # StandAloneIn Service
    ├── OffLine/
    ├── VisitAuthorization/             # Módulo de autorização de visitas
    ├── WebApp/
    │   ├── UI/                         # Portal Web do Usuário (IIS)
    │   │   └── Fabricantes/
    │   └── DS/                         # DataService WebApp (IIS)
    └── Win/                            # Aplicação Desktop Windows
```

---

## 📦 3. Mapeamento de Pacotes MSI

O [`MsiScanner`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Core/Services/MsiScanner.cs) resolve o destino de cada MSI através de regras hierárquicas (pela pasta de origem ou pelo prefixo do nome do arquivo):

| MSI / Subpasta | Pasta de Destino Resolvida | Observação |
|---|---|---|
| `AutoAtendimento` | `<BasePath>/NewAcesso/AutoAtendimento` | |
| `ConexBridge` | `<BasePath>/NewAcesso/ConexBridge` | |
| `ConnectionRecord` | `<BasePath>/NewAcesso/ConnectionRecord` | |
| `Controller` | `<BasePath>/NewAcesso/Controller` | |
| `ControleAcesso` | `<BasePath>/NewAcesso/Controller/ControleAcesso` | |
| `CoreWs` | `<BasePath>/NewAcesso/Controller/CoreWs` | |
| `Fabricantes` | `<BasePath>/NewAcesso/Controller/Fabricantes` | |
| `Task` | `<BasePath>/NewAcesso/Controller/Task` | |
| `StandAloneEx` | `<BasePath>/NewAcesso/ControllerOffline/WinService_Ex` | Serviço de entrada/saída offline |
| `StandAloneIn` | `<BasePath>/NewAcesso/ControllerOffline/WinService_In` | Serviço de entrada/saída offline |
| `VisitAuthorization` | `<BasePath>/NewAcesso/VisitAuthorization` | |
| `Win` | `<BasePath>/NewAcesso/Win` | |
| `SQLServer/` ou `Oracle/` | Mapeado conforme a escolha de SGBD | Subpastas exclusivas de banco |

---

## 🌐 4. Configuração de WebApps no IIS

Os WebApps `WebAppUI` e `WebAppDS` são instalados através de extração e publicação no IIS:

1. **AppPools:**
   - Criação de Application Pools dedicados com .NET CLR v4.0 e modo integrado.
2. **Sites / Aplicações:**
   - **WebAppUI:** Porta configurada (padrão 8080 ou 80) apontando para `<BasePath>/NewAcesso/WebApp/UI`.
   - **WebAppDS:** Aplicação vinculada apontando para `<BasePath>/NewAcesso/WebApp/DS`.
3. **Fallback Admin Install:**
   - Caso a extração padrão falhe, o [`WebAppInstaller`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Core/Services/WebAppInstaller.cs) executa `msiexec.exe /a <msi> TARGETDIR=<caminho>` para extrair o conteúdo de forma administrativa.
