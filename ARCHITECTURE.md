# 🏗️ Arquitetura — Instalador NewAcesso

> **Versão:** 2.0  
> **Framework:** .NET 10.0 / C# 13  
> **Plataforma:** Windows Server 2016+ / Windows 10+ (x64)  
> **Interface:** Terminal Interativo (Spectre.Console)  
> **Propósito:** Documentar as decisões arquiteturais, estrutura de camadas, modelos de domínio, serviços de negócio, tratamento de erros e diretrizes de evolução do Instalador NewAcesso.

---

## 📋 Índice

1. [Visão Geral](#visão-geral)
2. [Decisões Arquiteturais](#decisões-arquiteturais)
3. [Estrutura da Solução e Camadas](#estrutura-da-solução-e-camadas)
4. [Abstrações e Modelos de Domínio](#abstrações-e-modelos-de-domínio)
5. [Serviços e Componentes do Core](#serviços-e-componentes-do-core)
6. [Interface de Usuário (Console / Spectre.Console)](#interface-de-usuário-console--spectreconsole)
7. [Tratamento de Erros, Logs e Auditoria](#tratamento-de-erros-logs-e-auditoria)
8. [Estratégia de Testes](#estratégia-de-testes)
9. [Pontos Fortes e Oportunidades de Evolução](#pontos-fortes-e-oportunidades-de-evolução)

---

## 📖 1. Visão Geral

O **Instalador NewAcesso** automatiza o ciclo completo de preparação de ambiente, ativação de recursos do sistema operacional, criação da árvore de diretórios, instalação de pacotes MSI, configuração de WebApps no IIS e agendamento de tarefas do sistema.

A aplicação é projetada sob o paradigma **Clean / Onion Architecture**, separando estritamente os contratos e modelos de domínio da lógica de negócio e da interface de apresentação no terminal.

### Público-Alvo e Casos de Uso
- **Técnicos de Implantação e TI:** Instalação e reinstalação de servidores NewAcesso de forma guiada e sem intervenções manuais propensas a erro.
- **Equipes de Suporte e Diagnóstico:** Verificação de integridade, reparo e desinstalação auditada de componentes.
- **Pipelines de Automação / DevOps:** Execução modularizada e reprodutível.

---

## 🏛️ 2. Decisões Arquiteturais

### 2.1. Separação Estrita em 3 Camadas + Testes (`.slnx`)
A solução utiliza o formato XML moderno `.slnx` e mantém 4 projetos com responsabilidades ortogonais:

```
┌────────────────────────────────────────────────────────────┐
│  InstaladorNewAcesso.Console (UI / Ponto de Entrada)       │  → Conhece Abstractions + Core
├────────────────────────────────────────────────────────────┤
│  InstaladorNewAcesso.Core (Lógica de Negócio)              │  → Conhece exclusivamente Abstractions
├────────────────────────────────────────────────────────────┤
│  InstaladorNewAcesso.Abstractions (Contratos & Modelos)    │  → 0 dependências externas
└────────────────────────────────────────────────────────────┘
```

- **Isolamento de Domínio:** `Abstractions` é uma biblioteca pura sem dependências de pacotes externos ou frameworks de UI.
- **Core Agnóstico:** Toda comunicação visual na camada de negócio passa pela abstração [`IUIService`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Abstractions/Interfaces/IUIService.cs).
- **Testabilidade:** Mocks e substitutos (NSubstitute) permitem testar toda a lógica de negócio sem abrir janelas ou depender de terminal real.

### 2.2. Interface `IUIService` e Escopo Assíncrono (`UIScope`)
- A interface `IUIService` abstrai escrita formatada (markup, cores, regras horizontais, tabelas, painéis), entradas com validação (`AskInput`, `AskPassword`, `Confirm`, `AskOption`), diálogos de progresso (`ShowProgress`) e status com spinners (`ShowStatus`).
- O `ConsoleUIService` implementa `IUIService` delegando para o [Spectre.Console](https://spectreconsole.net/).
- A classe [`UIScope.Current`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Core/Services/UIScope.cs) utiliza `AsyncLocal<IUIService?>` para permitir acesso seguro em fluxos assíncronos onde a injeção via construtor não esteja disponível.

### 2.3. Idempotência e Execução Segura de Processos
- Criação de diretórios, ativação de componentes do Windows e alteração de arquivos de configuração (.ini, .config, .xml) são idempotentes.
- Invocações do Windows (PowerShell, DISM, msiexec) são encapsuladas por [`ProcessExecutor`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Core/Utils/ProcessExecutor.cs), garantindo captura segura de stdout/stderr, timeouts e logging.

---

## 📁 3. Estrutura da Solução e Camadas

```
InstaladorNewAcesso/
├── Directory.Build.props                         # Configurações globais de compilação e publicação
├── InstaladorNewAcesso.slnx                      # Solution XML simplificado
├── AGENTS.md                                     # Diretrizes para agentes de IA e desenvolvedores
├── ARCHITECTURE.md                               # Este documento
├── README.md                                     # Apresentação e guia rápido
├── cleanup-orphans.ps1                           # Script de encerramento de processos órfãos
│
├── docs/                                         # Documentação técnica e operacional
│   ├── setup-guide.md                            # Guia de compilação e publicação
│   ├── features-and-msi-mapping.md               # Catálogo de recursos e mapeamento de MSIs
│   └── troubleshooting.md                        # Diagnóstico e solução de problemas
│
├── scripts/
│   ├── generate-icon.ps1                         # Geração de ícones da aplicação
│   └── publish.ps1                               # Script de publicação self-contained
│
├── src/
│   ├── InstaladorNewAcesso.Abstractions/         # Interfaces e Modelos de Domínio
│   ├── InstaladorNewAcesso.Core/                 # Lógica de negócio, instaladores e helpers
│   └── InstaladorNewAcesso.Console/              # Interface de terminal (Spectre.Console)
│
└── tests/
    └── InstaladorNewAcesso.Tests/                # 500+ testes unitários e de integração
```

---

## 📐 4. Abstrações e Modelos de Domínio

### Interfaces Principais (`InstaladorNewAcesso.Abstractions.Interfaces`)
- [`IUIService`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Abstractions/Interfaces/IUIService.cs): Contrato de apresentação, entrada e saída.
- [`IProcessExecutor`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Abstractions/Interfaces/IProcessExecutor.cs): Execução assíncrona de comandos PowerShell e captura de stdout.
- [`IFeatureInstaller`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Abstractions/Interfaces/IFeatureInstaller.cs): Verificação e instalação de recursos do sistema operacional.
- [`IIisInstaller`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Abstractions/Interfaces/IIisInstaller.cs): Gestão de AppPools e Sites no IIS.
- [`IView`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Abstractions/Interfaces/IView.cs): Ciclo de vida de execução de views (`ExecuteAsync`).

### Modelos de Domínio (`InstaladorNewAcesso.Abstractions.Models`)
- [`InstallationPaths`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Abstractions/Models/InstallationPaths.cs): Centraliza a estrutura de diretórios do NewAcesso com base em uma raiz configurável.
- [`MsiInstallationModel`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Abstractions/Models/MsiInstallModel.cs): Encapsula o caminho de um MSI, diretório de destino e parâmetros de instalação.
- [`WebAppModel`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Abstractions/Models/WebAppModel.cs): Metadados para configuração de aplicações Web no IIS (Porta, AppPool, Nome do Site).
- [`WindowsFeature`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Abstractions/Models/WindowsFeature.cs): Mapeamento de nome amigável, identificador ServerManager e identificador DISM.
- [`StepStatus`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Abstractions/Models/StepStatus.cs) & [`StepState`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Abstractions/Models/StepState.cs): Rastreamento de progresso de cada etapa (Pendente, Em Execução, Sucesso, Falha, Alerta).

---

## ⚙️ 5. Serviços e Componentes do Core

### 5.1. Instaladores e Scanners
| Serviço | Responsabilidade |
|---|---|
| [`MsiScanner`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Core/Services/MsiScanner.cs) | Varre o diretório de instaladores e mapeia cada arquivo para sua pasta de destino correta no `InstallationPaths`. |
| [`MsiInstaller`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Core/Services/MsiInstaller.cs) | Executa `msiexec.exe /i` de forma silenciosa com geração de log verbose e invoca os Config Helpers pós-instalação. |
| [`MsiUninstaller`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Core/Services/MsiUninstaller.cs) | Desinstala os pacotes via `msiexec.exe /x`, remove diretórios remanescentes e audita todas as ações. |
| [`WebAppScanner`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Core/Services/WebAppScanner.cs) | Localiza os MSIs de WebAppUI e WebAppDS. |
| [`WebAppInstaller`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Core/Services/WebAppInstaller.cs) | Realiza a instalação de WebApps, configuração no IIS e fallback para Admin Install (`msiexec /a`). |

### 5.2. Helpers de Configuração (Pós-Instalação)
Após a extração de cada MSI, arquivos de configuração locais (.config, .ini, .xml) são ajustados por helpers especializados que herdam de `ConfigHelperBase` ou `IniHelperBase`:
- `ConnectionRecordConfigHelper`
- `ControleAcessoConfigHelper`
- `ControleAcessoAgendamentoHelper`
- `CoreWsConfigHelper`
- `TaskConfigHelper`
- `StandAloneExConfigHelper`
- `StandAloneImConfigHelper`
- `WebAppConfigHelper`

Antes de qualquer modificação, o [`ConfigBackupService`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Core/Utils/ConfigBackupService.cs) cria uma cópia de segurança com carimbo de data e hora.

---

## 🖥️ 6. Interface de Usuário (Console / Spectre.Console)

A interface de terminal é estruturada em **Views** orientadas a tarefas:
- [`MainMenuView`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Console/Views/MainMenuView.cs): Menu mestre com 8 opções.
- [`ResourceView`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Console/Views/ResourceView.cs): Ativação das 32 features Windows com barra de progresso.
- [`DirectoryView`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Console/Views/DirectoryView.cs): Criação da estrutura de pastas.
- [`IisView`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Console/Views/IisView.cs): Configuração dos AppPools e Sites do IIS.
- [`MsiView`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Console/Views/MsiView.cs): Execução em lote dos instaladores MSI com feedback visual.
- [`WebAppView`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Console/Views/WebAppView.cs): Instalação dos portais Web.
- [`UninstallMenuView`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Console/Views/UninstallMenuView.cs): Desinstalação completa e limpeza.
- [`SummaryPanelView`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Console/Views/SummaryPanelView.cs): Painel consolidado com resumo e erros.

---

## 📋 7. Tratamento de Erros, Logs e Auditoria

1. **Auditoria Geral (`AuditLogger`):** Grava um arquivo de log textual registrando cada ação, início de etapa, comandos executados, sucessos e erros com data e hora UTC/Local.
2. **Logs do Windows Installer (`MsiLogHelper`):** Captura a saída detalhada do `msiexec` e identifica padrões de erro comuns (como 1603 e 1618).
3. **Resiliência:** Operações de I/O em disco e chamadas PowerShell utilizam validações prévias para evitar travamentos ou estados inconsistentes.

---

## 🧪 8. Estratégia de Testes

- **Projetos:** `tests/InstaladorNewAcesso.Tests/`.
- **Cobertura:**
  - Testes unitários para todos os Config Helpers, Scanners, Models e Utilitários.
  - Testes de integração em [`InstallationIntegrationTests.cs`](file:///c:/dev/InstaladorNewAcesso-main/tests/InstaladorNewAcesso.Tests/InstallationIntegrationTests.cs) validando a criação e limpeza real de estruturas de diretório em `Path.GetTempPath()`.
- **Execução:**
  ```powershell
  dotnet test
  ```

---

## 🚀 9. Pontos Fortes e Oportunidades de Evolução

### Pontos Fortes
- **Total desacoplamento entre UI e Core:** Permite evolução, manutenção ou criação de novos frontends sem alterações na regra de negócio.
- **Experiência rica de terminal:** Spectre.Console entrega visual moderno, barras de progresso e facilidade de operação.
- **Alta cobertura de testes automatizados:** Mais de 500 testes assegurando estabilidade em refatorações.

### Oportunidades de Evolução Futura
1. **Passagem explícita de `CancellationToken`:** Evoluir todas as assinaturas assíncronas do Core para aceitar `CancellationToken` opcional, facilitando cancelamentos graciosos via Ctrl+C.
2. **Injeção de Dependência Formal:** Adotar `Microsoft.Extensions.DependencyInjection` para registro e resolução formal de serviços caso a complexidade aumente.
