# 🏗️ Arquitetura — Instalador NewAcesso

> **Versão:** 1.0  
> **Última atualização:** Julho 2026  
> **Propósito:** Documentar as decisões arquiteturais, estrutura, pontos fortes, fragilidades e sugestões de evolução do instalador unificado da plataforma NewAcesso.

---

## 📋 Índice

1. [Visão Geral](#visão-geral)
2. [Decisões Arquiteturais](#decisões-arquiteturais)
3. [Estrutura do Projeto](#estrutura-do-projeto)
4. [Camadas e Responsabilidades](#camadas-e-responsabilidades)
5. [Modelos de Dados](#modelos-de-dados)
6. [Navegação e Ciclo de Vida](#navegação-e-ciclo-de-vida)
7. [Sistema de Temas](#sistema-de-temas)
8. [Painel de Status e Erros](#painel-de-status-e-erros)
9. [Estratégia de Testes](#estratégia-de-testes)
10. [Pontos Fortes](#pontos-fortes)
11. [Oportunidades de Melhoria](#oportunidades-de-melhoria)
12. [Sugestões para o Futuro](#sugestões-para-o-futuro)

---

## Visão Geral

O **Instalador NewAcesso** é uma aplicação Windows responsável por automatizar a instalação completa da suíte de produtos NewAcesso em servidores — desde a configuração do IIS e ativação de Features do Windows até a instalação de MSIs, WebApps, criação de diretórios e agendamento de tarefas.

A aplicação suporta **duas interfaces de usuário**:

| Interface | Projeto | Tecnologia | Uso |
|-----------|---------|-----------|-----|
| **Gráfica (WinForms)** | `InstaladorNewAcesso.WinForms` | Windows Forms (.NET 10) | Operadores de TI |
| **Terminal (Console)** | `InstaladorNewAcesso.Console` | Console + Spectre.Console | Automação / CI |

Ambas compartilham a mesma camada de negócio (`Core`) e contratos (`Abstractions`), garantindo comportamento consistente independente da interface escolhida.

### Público-alvo

- Técnicos de TI que precisam instalar ou reinstalar servidores NewAcesso
- Equipes de suporte que realizam diagnósticos remotos
- Scripts de automação (modo console)

---

## Decisões Arquiteturais

### 1. Separação em Múltiplos Projetos (Solution .slnx)

**Decisão:** Dividir a solução em 5 projetos + 1 de testes, cada um com responsabilidade bem definida.

**Motivação:**
- Isolamento de responsabilidades (Separation of Concerns)
- Possibilidade de reuso do Core em outros contextos (ex: CLI headless)
- Facilidade para testar cada camada independentemente
- Prevenção de acoplamento entre UI e lógica de negócio

**Trade-off:** A complexidade inicial de navegação entre projetos é maior que uma abordagem monolítica, mas o ganho em manutenibilidade a longo prazo compensa.

### 2. Interface `IUIService` como Abstração de UI

**Decisão:** Toda interação com o usuário passa pela interface `IUIService`, com implementações distintas para Console e WinForms.

**Motivação:**
- Permite que a lógica de negócio (`Core`) seja completamente agnóstica de UI
- Facilita testes (pode-se mockar o IUIService)
- Uma única base de código para dois frontends

**Implementação:**
- `ConsoleUIService` — usa Spectre.Console (markup, tabelas, painéis, spinners)
- `WinFormsUIService` — usa RichTextBox + MessageDialogs, converte markup Spectre para texto limpo
- Ambas são injetadas via construtor nos controles/views do Core
- `UIScope.Current` é um `AsyncLocal` que permite acesso ao IUIService em contextos estáticos (usado em `MsiInstaller`, `WebAppInstaller`, `InstallerFactory`)

```csharp
public interface IUIService
{
    void WriteMessage(string text, string? color = null);
    Task ShowProgress(string title, Func<Action<double, string>, Task> action);
    string AskInput(string prompt, string? defaultValue = null);
    // ... +30 métodos
}
```

### 3. Arquitetura em Camadas (Onion/Clean Architecture)

**Decisão:** Organizar o código em camadas concêntricas com dependências apontando para dentro.

```
┌─────────────────────────────────────┐
│  WinForms / Console (UI)            │  → Conhece Abstractions + Core
├─────────────────────────────────────┤
│  Core (Lógica de Negócio)           │  → Conhece Abstractions
├─────────────────────────────────────┤
│  Abstractions (Contracts/Models)    │  → Conhece nada (projeto raiz)
└─────────────────────────────────────┘
```

**Motivação:**
- Desacoplamento total entre UI e regras de negócio
- Testabilidade: Core e Abstractions têm 0 dependência de UI
- Flexibilidade para substituir a camada de UI sem tocar em lógica

### 4. NavigationManager com Ciclo de Vida `IView`

**Decisão:** Implementar navegação própria (sem frameworks) usando um `NavigationManager` que gerencia troca de `UserControl` em um `Panel` contentor, com suporte a histórico "voltar" e chamadas de ciclo de vida assíncronas.

**Motivação:**
- Evitar dependência externa de navegação (ex: WPF NavigationService)
- Controle total sobre o ciclo de vida: `ActivateAsync()`/`DeactivateAsync()`
- Histórico simples com `Stack<string>`

**Ordem de execução no SwitchTo:**
```
1. DeactivateAsync() na tela antiga (ainda parenteada)
2. Remove + Dispose da tela antiga
3. Adiciona nova tela + Dock.Fill
4. ActivateAsync() na nova tela (já parenteada)
5. Notifica NavigationChanged + CanGoBackChanged
```

### 5. Sistema de Temas Centralizado

**Decisão:** Toda cor, fonte e estilo visual reside em classes estáticas no namespace `Styles`.

- `ThemeColors` — 30+ constantes de cor (Background, Surface, TextPrimary, Success, Danger, etc.)
- `ThemeFonts` — 14 definições de fonte (TitleMain, ButtonAction, Body, Caption, Output, etc.) com cache lazy
- `UIStyles` — 20+ métodos factory para criar controles já estilizados (CreateTitle, CreatePrimaryButton, CreateTextBox, etc.)

**Motivação:**
- Consistência visual em toda a aplicação
- Tema escuro profissional sem repetição de código
- Alteração de tema em um único lugar
- Cache de fontes evita vazamento de recursos GDI

### 6. Launcher como Ponto de Entrada

**Decisão:** Um projeto separado (`InstaladorNewAcesso.Launcher`) pergunta ao usuário qual modo iniciar (WinForms ou Console), sem precisar de múltiplos executáveis na área de trabalho.

**Motivação:**
- Experiência unificada para o usuário final
- Único atalho na área de trabalho
- Launcher leve (não referencia Core nem WinForms diretamente)

### 7. UIScope como Service Locator para IUIService

**Decisão:** Usar `AsyncLocal<IUIService>` (`UIScope.Current`) para disponibilizar o serviço de UI em contextos estáticos dentro do Core.

**Motivação:**
- Classes no Core (como `MsiInstaller`, `WebAppInstaller`) precisam escrever no log da UI
- Evita injeção por construtor em dezenas de classes de serviço
- `AsyncLocal` garante isolamento por fluxo de execução assíncrono

---

## Estrutura do Projeto

```
InstaladorNewAcesso/
│
├── Directory.Build.props                    # Propriedades compartilhadas (TFM, nullable, warnings)
├── InstaladorNewAcesso.slnx                 # Solution file (formato .slnx)
├── ARCHITECTURE.md                          ← Este documento
│
├── src/
│   ├── InstaladorNewAcesso.Abstractions/    # Interfaces + Models (0 dependências)
│   ├── InstaladorNewAcesso.Core/            # Lógica de negócio (depende de Abstractions)
│   ├── InstaladorNewAcesso.WinForms/        # UI WinForms (depende de Core + Abstractions)
│   ├── InstaladorNewAcesso.Console/         # UI Console (depende de Core + Abstractions)
│   └── InstaladorNewAcesso.Launcher/        # Ponto de entrada único
│
├── tests/
│   └── InstaladorNewAcesso.Tests/           # 513 testes (xUnit + FluentAssertions + NSubstitute)
│
└── dist/                                    # Build output (runtimeconfigs)
```

### Descrição dos Projetos

| Projeto | Tipo | Depende de | Responsabilidade |
|---------|------|-----------|-----------------|
| `Abstractions` | Library | — | Interfaces (IView, IUIService, IProcessExecutor), Models (InstallationPaths, MsiInstallationModel, StepStatus, etc.) |
| `Core` | Library | Abstractions | Serviços de instalação (MsiInstaller, WebAppInstaller, GoogleDriveDownloader), scanners, utilitários de configuração, helpers de IIS, factory de features do Windows |
| `WinForms` | WinExe | Core, Abstractions | Forms, UserControls (11 controles), NavigationManager, Styles (ThemeColors, ThemeFonts), WinFormsUIService |
| `Console` | Exe | Core, Abstractions | Program.cs com verificação de Admin, ConsoleUIService, Views (MainMenuView, MsiView, etc.) |
| `Launcher` | WinExe | — (autônomo) | LauncherForm que pergunta "Modo Gráfico" ou "Terminal" e dispara o .exe correspondente |
| `Tests` | Test | Abstractions, Core, WinForms | Testes unitários (xUnit) |

---

## Camadas e Responsabilidades

### Abstractions (Contratos)

**Interfaces:**
- `IView` — Ciclo de vida das telas (`Title`, `ActivateAsync()`, `DeactivateAsync()`)
- `IViewModel` — Padrão MVVM simplificado (`IsBusy`, `StatusMessage`, `LoadAsync()`, `UnloadAsync()`)
- `IUIService` — Abstração completa de UI (output, input, progresso, diálogos)
- `INavigationService` — Navegação entre telas (`NavigateTo()`, `GoBack()`, `ReplaceWith()`)
- `IFeatureInstaller` — Instalação de Features do Windows (`IsFeatureInstalledAsync()`, `InstallFeatureAsync()`)
- `IIisInstaller` — Gerenciamento de IIS (AppPools, Sites, physicalPath)
- `IProcessExecutor` — Execução de comandos PowerShell (`RunPowerShellCommandAsync()`, `RunPowerShellWithOutputAsync()`)

**Models:**
- `InstallationPaths` — Estrutura de diretórios do NewAcesso
- `MsiInstallationModel` — Dados para instalação de MSI
- `WebAppModel` — Dados para instalação de WebApp (SiteName, AppPoolName, Port, etc.)
- `WindowsFeature` — Record com FriendlyName, ServerName, DesktopName
- `SummaryResult` — Resultado de uma etapa (Sucesso/Falha)
- `StepState` — Enum: Pending, Running, Success, Failed, Warning
- `StepStatus` — Model com Start/Complete/Fail/Warn, duração calculada

### Core (Lógica de Negócio)

**Serviços:**
| Classe | Função |
|--------|--------|
| `MsiInstaller` | Executa `msiexec /i`, aciona config helpers pós-instalação |
| `MsiScanner` | Varre diretório de instaladores, mapeia MSIs → InstallationPaths |
| `MsiUninstaller` | Desinstala MSI via `msiexec /x`, remove diretórios |
| `WebAppInstaller` | Instala WebApp (MSI → IIS → config), com fallback Admin Install |
| `WebAppScanner` | Identifica MSIs de WebApp (UI ou DS) no diretório de instaladores |
| `GoogleDriveDownloader` | Baixa pasta inteira do Google Drive via API v3 (recursivo com paginação) |

**Utilitários:**
| Classe | Função |
|--------|--------|
| `IisInstaller` | Cria/remove AppPools e Sites, atualiza physicalPath via PowerShell |
| `ProcessExecutor` / `ProcessExecutorService` | Executa comandos PowerShell com output |
| `SummaryStore` | Armazena resultados em memória com estatísticas |
| `AuditLogger` | Log de auditoria em arquivo TXT |
| `MsiLogHelper` | Análise de logs verbose do msiexec |
| `ConfigBackupService` | Backup de arquivos de configuração (`.config`, `.ini`, `.xml`) antes de modificá-los, com timestamp no nome |

**Config Helpers** (atualizam arquivos de configuração após instalação):
- `ConnectionRecordConfigHelper`
- `ControleAcessoConfigHelper`
- `ControleAcessoAgendamentoHelper`
- `CoreWsConfigHelper`
- `TaskConfigHelper`
- `StandAloneExConfigHelper`
- `StandAloneImConfigHelper`
- `WebAppConfigHelper`
- `ConfigHelperBase` / `IniHelperBase` (classes base)

**Configurações:**
- `FeatureSetup` — Lista de 32 Windows Features com nomes para Server e Desktop
- `DirectorySetup` — Gera todos os paths de diretório a partir do InstallationPaths

**Implementações:**
- `WindowsDesktopInstaller` — Instala features via DISM (`Enable-WindowsOptionalFeature`)
- `WindowsServerInstaller` — Instala features via ServerManager (`Install-WindowsFeature`)
- `InstallerFactory` — Detecta Windows Server vs Desktop e retorna implementação correta

### WinForms (Interface Gráfica)

**Forms:**
- `MainForm` — Form principal com SplitContainer (conteúdo + log), painel lateral (StatusPanel + ErrorSummary), NavigationManager, barra de navegação

**Controls** (11 UserControls):
| Control | Função |
|---------|--------|
| `MainMenuControl` | Menu principal com botões de navegação |
| `DownloadControl` | Download de instaladores do Google Drive |
| `ResourcesControl` | Instalação de Features do Windows (DISM/ServerManager) |
| `DirectoryControl` | Criação da estrutura de diretórios |
| `IisControl` | Configuração de IIS (AppPools, Sites) |
| `MsiControl` | Instalação de MSIs do sistema |
| `WebAppControl` | Instalação de WebApps (UI + DS) |
| `ScheduleControl` | Agendamento de tarefas |
| `UninstallControl` | Desinstalação completa do sistema |
| `StatusPanel` | Painel de progresso em tempo real (inline, não modal) |
| `ErrorSummaryControl` | Lista filtrável de erros/avisos com detalhes expansíveis |

**Componentes:**
- `NavigationManager` — Gerencia pilha de navegação, ciclo de vida IView

**Styles:**
- `ThemeColors` — 30+ constantes de cor
- `ThemeFonts` — 14 definições de fonte com cache
- `UIStyles` — 20+ métodos factory para controles estilizados

### Console (Interface Terminal)

- `Program.cs` — Verificação de Admin, inicialização do ConsoleUIService, execução do MainMenuView
- `ConsoleUIService` — Implementação do IUIService usando Spectre.Console

As **Views** do Console residem no Core (`Core/Views/`), permitindo que ambos os frontends compartilhem a mesma lógica de apresentação.

---

## Modelos de Dados

### InstallationPaths

Gera toda a estrutura de diretórios a partir de um `BasePath`:

```
📁 <BasePath>/
├── 📁 Instaladores/
└── 📁 NewAcesso/
    ├── 📁 AutoAtendimento/
    ├── 📁 ConexBridge/
    ├── 📁 ConnectionRecord/
    ├── 📁 Controller/
    │   ├── 📁 ControleAcesso/
    │   ├── 📁 CoreWs/
    │   ├── 📁 Fabricantes/
    │   └── 📁 Task/
    ├── 📁 ControllerOffline/
    │   ├── 📁 Arquivos/
    │   ├── 📁 WinService_Ex/
    │   └── 📁 WinService_In/
    ├── 📁 VisitAuthorization/
    ├── 📁 WebAppDS/
    ├── 📁 WebAppUI/
    │   └── 📁 Fabricantes/
    └── 📁 Win/
```

### StepState (Enum)

```
Pending  (0) → Aguardando execução
Running  (1) → Em execução
Success  (2) → Concluída com sucesso
Failed   (3) → Concluída com falha
Warning  (4) → Concluída com aviso
```

### StepStatus

Model com ciclo de vida controlado por métodos (`Start()`, `Complete()`, `Fail()`, `Warn()`) que gerenciam automáticamente `StartedAt`, `CompletedAt` e `Duration` (calculada).

---

## Navegação e Ciclo de Vida

### Fluxo de Navegação

```
MainForm
├── MainMenuControl
│   ├── Download → DownloadControl
│   │   └── (após download, volta ao MainMenu)
│   ├── Resources → ResourcesControl
│   ├── Directory → DirectoryControl
│   ├── IIS → IisControl
│   ├── MSI → MsiControl
│   ├── WebApp → WebAppControl
│   ├── Schedule → ScheduleControl
│   └── Uninstall → UninstallControl
│
└── (Qualquer tela pode navegar para qualquer outra via NavigationManager)
```

### Ciclo de Vida IView

Implementado via `NavigationManager.SwitchTo()`:

```csharp
// 1. DeactivateAsync na tela antiga (ainda no container)
if (_currentControl is IView oldView)
    _ = oldView.DeactivateAsync();

// 2. Remove + Dispose
_container.Controls.Remove(_currentControl);
_currentControl.Dispose();

// 3. Adiciona nova tela ao container (parenteia)
_currentControl = newControl;
_currentControl.Dock = DockStyle.Fill;
_container.Controls.Add(_currentControl);

// 4. ActivateAsync na nova tela (já parenteada)
if (newControl is IView newView)
    _ = newView.ActivateAsync();

// 5. Notifica ouvintes
NavigationChanged?.Invoke(name);
CanGoBackChanged?.Invoke(CanGoBack);
```

---

## Sistema de Temas

### ThemeColors — Paleta Escura

```
Background     #12121E  (fundo principal)
Surface        #1E1E32  (painéis, headers)
SurfaceHover   #32324A  (hover)
InputBg        #1E1E32  (inputs)

TextPrimary    #FFFFFF
TextSecondary  #808080
TextAccent     #00FFFF  (ciano)
TextMuted      #646478

Primary        #0078D7  (azul ação)
Success        #009632  (verde)
Warning        #B45000  (laranja)
Danger         #B42828  (vermelho)
DangerDark     #B40000  (vermelho escuro)
Neutral        #323246  (neutro)
```

### ThemeFonts — Cache Lazy

Todas as fontes são criadas sob demanda e cacheadas em `Dictionary<string, Font>`. O cache é limpo via `ThemeFonts.ClearCache()` no `Dispose` do `MainForm`, prevenindo vazamento de recursos GDI.

### UIStyles — Métodos Factory

```csharp
// Headers
CreateTitle("text")         → Label 20pt Bold Cyan
CreateSectionTitle("text")  → Label 16pt Bold Cyan
CreateDescription("text")   → Label 11pt Gray

// Buttons
CreatePrimaryButton("text")   → Azul, 11pt Bold
CreateSuccessButton("text")   → Verde, 11pt Bold
CreateDangerButton("text")    → Vermelho, 11pt Bold
CreateWarningButton("text")   → Laranja, 11pt Bold
CreateSecondaryButton("text") → Neutro, 11pt Regular

// Inputs
CreateTextBox("default", 350) → Tema escuro, FixedSingle
CreatePasswordBox(350)        → Com UseSystemPasswordChar
CreateComboBox(["opt1",..])   → DropDownList, tema escuro

// Layout
CreateFlowPanel()       → FlowLayoutPanel horizontal transparente
CreateTableLayout()     → TableLayoutPanel dock fill
CreateActionPanel(btns) → Painel com botões de ação
```

---

## Painel de Status e Erros

### StatusPanel

Exibido no lado direito do `MainForm`, substitui o antigo `ProgressDialog` modal:

```
┌─────────────────────────┐
│ Progresso: 3/5 concluído│
│ ████████░░░░ 60%        │
│                         │
│ Etapa         Duração   │
│ ✓ MSI-01      12s       │
│ ✓ MSI-02      8s        │
│ ▶ MSI-03      5s        │  ← scroll automático
│ ◻ MSI-04               │
│ ◻ MSI-05               │
│                         │
│ Pronto / Falha: MSI-03  │
└─────────────────────────┘
```

**Características:**
- Timer único (não 1 por etapa) para atualizar durações
- Scroll automático para etapa atual (`ScrollControlIntoView`)
- Barra de progresso fica vermelha se houver falhas
- Proteção contra double-count (não incrementa se etapa já concluída)
- Timer é parado/disposed no Unload

### ErrorSummaryControl

Abaixo do StatusPanel, exibe erros e avisos:

```
┌─────────────────────────────┐
│ ⚠️ Erros e Avisos           │
│ 2 erros  1 aviso            │
│ [Todos] [Erros] [Avisos]    │
├─────────────────────────────┤
│ ✗ [MSI] MSI-01 falhou      │
│   Detalhe: Arquivo não...   │  ← clicável para expandir
│ ⚠ [MSI] MSI-02 config      │
│ ℹ [Sis] Instalação iniciada │
├─────────────────────────────┤
│ 📋 Copiar  🗑️ Limpar        │
└─────────────────────────────┘
```

**Características:**
- OwnerDraw com `TextRenderer` (texto nítido em high-DPI)
- Botão "Copiar" exporta para clipboard
- Botão "Limpar" reseta a lista
- Filtros: Todos, Erros, Avisos
- Contadores com cores (vermelho para erros, amarelo para avisos)

---

## Estratégia de Testes

O projeto possui **513 testes unitários** (xUnit + FluentAssertions + NSubstitute).

### Cobertura

| Área | Testes | O que cobre |
|------|--------|-------------|
| Models | ~30 | MsiInstallationModel, WebAppModel, InstallationPaths, WindowsFeature, StepState, StepStatus |
| Services | ~120 | MsiInstaller, MsiScanner, MsiUninstaller, WebAppInstaller, WebAppScanner, GoogleDriveDownloader |
| Configurations | ~40 | DirectorySetup, FeatureSetup |
| Utils | ~250 | IisInstaller, ProcessExecutor, SummaryStore, AuditLogger, ConfigHelpers, MsiLogHelper, ViewHelper |
| Implementations | ~30 | WindowsDesktopInstaller, WindowsServerInstaller |
| Controls | ~34 | StatusPanel (20) + ErrorSummaryControl (14) apenas |
| **Demais controles** | **0** | MsiControl, WebAppControl, IisControl, DirectoryControl, DownloadControl, etc. |
| Integration | ~9 | Fluxos completos de instalação |
| **Total** | **513** | **0 falhas, 0 pulados (~9s execução)** |

### Padrões

- `[Fact]` para testes simples
- `[Theory]` + `[InlineData]` para testes parametrizados
- `[Collection("IntegrationTests")]` para testes com estado estático (SummaryStore, AuditLogger)
- FluentAssertions para asserções encadeadas
- NSubstitute para mocks (IProcessExecutor)
- Nomenclatura: `MethodName_Scenario_ExpectedResult` (ex: `AddError_ShouldIncrementErrorCount`)

### Limitações

- Testes de WinForms Controls criam instâncias reais (sem message pump), testando apenas API pública
- Não há testes de UI automatizados (Selenium, WinAppDriver, etc.)
- Cobertura de integração limitada (não executa msiexec nem PowerShell reais)

---

## Pontos Fortes

### ✅ Arquitetura Sólida

1. **Separação clara de responsabilidades** — Abstractions → Core → UI, com dependências unidirecionais
2. **Dual UI sem duplicação** — Console e WinForms compartilham 100% do Core
3. **Tema centralizado** — Alterar a paleta inteira é editar um arquivo
4. **Ciclo de vida IView** — `ActivateAsync()`/`DeactivateAsync()` garantem inicialização/limpeza consistente
5. **Navegação com histórico** — Botão "voltar" funcional com pilha de navegação

### ✅ Código Limpo

1. **Zero hardcoded colors/fonts** — Tudo referenciado de ThemeColors/ThemeFonts
2. **Factory methods** — UIStyles elimina repetição de criação de controles
3. **513 testes** — Cobertura sólida com 0 falhas
4. **Nullable habilitado** — Em toda a solution
5. **Análise de código** — `latest-Recommended` + EnforceCodeStyleInBuild

### ✅ Experiência do Usuário

1. **Painel de status inline** — Substitui modais bloqueantes por feedback em tempo real
2. **Sumário de erros filtrável** — Erros e avisos organizados, copiáveis para clipboard
3. **Log textual completo** — RichTextBox com output de todas as operações
4. **Duas interfaces** — Gráfica para operadores, terminal para automação
5. **Launcher unificado** — Único atalho, escolha do modo na inicialização

---

## Oportunidades de Melhoria

### ⚠️ Dívida Técnica

| Item | Impacto | Sugestão |
|------|---------|----------|
| **Chamadas diretas a `AnsiConsole` no Core** | Alto | `MsiInstaller`, `WebAppInstaller` e `WebAppScanner` usam `AnsiConsole.MarkupLine()` diretamente em vez de `UIScope.Current?.WriteMessage()`. Isso fere a abstração IUIService e amarra o Core ao Spectre.Console. | Migrar todo `AnsiConsole` no Core para `UIScope.Current.WriteMessage()` |

| Item | Impacto | Sugestão |
|------|---------|----------|
| **UIScope (Service Locator)** | Médio | `UIScope.Current` é um anti-pattern Service Locator. Dificulta testes e esconde dependências. | Migrar para injeção por construtor nas classes do Core que usam IUIService |
| **ViewModels não utilizados** | Baixo | `IViewModel` foi definido mas nenhum controle implementa. O padrão é MVVM pela metade. | Decidir: remover a interface ou implementá-la nos controles |
| **Static mutable state** | Médio | `SummaryStore` e `AuditLogger` usam estado estático mutável. Podem causar interferência em testes paralelos. | Usar `[Collection]` (já feito) ou migrar para instâncias injetadas |
| **Console Views no Core** | Médio | As Views do Console (`Core/Views/`) estão no projeto Core, não no Console. Mistura responsabilidades. | Mover para o projeto Console, ou criar um projeto SharedViews |
| **`ScreenName` helper sem destaque** | Baixo | Classe `ScreenName` mapeia nomes de tela para display names (`"MainMenu" → "🚀 Menu Principal"`) mas não está documentada | Adicionar à seção de navegação |
| **Tratamento de exceções genérico** | Baixo | Muitos `catch { return false; }` engolem exceções sem log | Loggar a exceção antes de retornar false |
| **Fire-and-forget no NavigationManager** | Baixo | `_ = oldView.DeactivateAsync()` ignora exceções assíncronas | Tratar exceções no fire-and-forget com try-catch |

### ⚠️ Cobertura de Testes

| Área | Status | Ação |
|------|--------|------|
| **Views do Console** | ❌ Não testadas | Adicionar testes para MainMenuView, MsiView, etc. |
| **Controles WinForms** | ✅ Testados | StatusPanel e ErrorSummaryControl cobertos |
| **ConfigHelpers (arquivos reais)** | ✅ Testados | Com mocks de sistema de arquivos |
| **Integração real (msiexec/PowerShell)** | ❌ Não testada | Testes de integração true exigem sandbox Windows |
| **IisInstaller** | ✅ Testado | Com NSubstitute mockando IProcessExecutor |

### ⚠️ Performance

| Área | Observação |
|------|------------|
| **MsiScanner** | Varre diretórios recursivamente, pode ser lento em rede |
| **GoogleDriveDownloader** | Download serial de arquivos (não paralelo) |
| **ConfigHelpers** | Vários helpers executam I/O sequencial após cada MSI |
| **WinFormsUIService** | Uso de `Invoke` para acesso ao RichTextBox — pode causar travamentos em operações muito rápidas |

---

## Sugestões para o Futuro

### 🎯 Curto Prazo (1-3 meses)

1. **Remover `AnsiConsole` direto no Core** — Migrar `MsiInstaller`, `WebAppInstaller` e `WebAppScanner` para usar `UIScope.Current?.WriteMessage()` em vez de `AnsiConsole.MarkupLine()`, fortalecendo a abstração IUIService
2. **Implementar IViewModel nos controles** — Isolar lógica de negócio dos UserControls, facilitando testes e manutenção
3. **Migrar Console Views para projeto Console** — Remover acoplamento indevido entre Core e apresentação de terminal
3. **Adicionar logging estruturado** — Substituir `AuditLogger` textual por algo como Serilog, com sinks para arquivo + console + (opcional) Seq
4. **Paralelizar downloads do Google Drive** — Usar `Parallel.ForEachAsync` ou `SemaphoreSlim` para baixar múltiplos arquivos simultaneamente

### 🎯 Médio Prazo (3-6 meses)

5. **DI Container formal** — Introduzir Microsoft.Extensions.DependencyInjection para substituir `UIScope` e construtores manuais
6. **Tema claro/escuro** — Adicionar toggle de tema (ThemeColors poderia vir de um `ThemeProvider`)
7. **Relatório de instalação** — Gerar PDF/HTML com resumo da instalação (produtos instalados, erros, duração)
8. **Progresso real** — Substituir barra de progresso baseada em contagem por feedback com `System.Threading.Channels` para operações paralelas

### 🎯 Longo Prazo (6-12 meses)

9. **Migração para MAUI/WPF** — Windows Forms é legado; WPF oferece melhor suporte a theming, data binding e high-DPI. MAUI permitiria (teoricamente) executar em terminal Windows
10. **Modo headless completo** — `InstaladorNewAcesso.Headless` com `--silent`, `--config json`, `--log-format json` para integração com SCCM/Intune
11. **Instalador web (Blazor)** — Interface web para instalação remota em múltiplos servidores simultaneamente
12. **Self-update** — O próprio instalador verificar e baixar novas versões automaticamente

### 💡 Ideias Pontuais

- **Validação de pré-requisitos mais robusta** — Verificar versão do .NET, espaço em disco, versão do Windows, etc.
- **Rollback automático** — Em caso de falha, desfazer todas as operações já realizadas (transação distribuída)
- **Modo dark/light sync com o sistema** — Detectar tema do Windows e aplicar automaticamente
- **Internacionalização (i18n)** — Suporte a múltiplos idiomas (atualmente todo em PT-BR)
- **Gravação de sessão** — Log em formato estruturado (JSON) para debugging remoto
- **Notificações** — Toast notifications ao finalizar instalação (mesmo com janela minimizada)

---

## Glossário

| Termo | Significado |
|-------|-------------|
| **MSI** | Windows Installer Package (.msi) |
| **IIS** | Internet Information Services |
| **AppPool** | Application Pool (IIS) — isolamento de aplicações web |
| **DISM** | Deployment Image Servicing and Management (Desktop) |
| **ServerManager** | PowerShell module para gerenciar Windows Server features |
| **WebAppUI** | Interface web do NewAcesso |
| **WebAppDS** | WebService de dados do NewAcesso |
| **SxS** | Side-by-side — fonte de instalação para Features do Windows |
| **Admin Install** | `msiexec /a` — extrai MSI sem instalar (modo administrativo) |
| **Robocopy** | Robust File Copy — utilitário Windows para cópia de arquivos |

---

### 🗑️ Componentes Legados (Ainda Presentes)

| Componente | Localização | Substituído por | Status |
|-----------|-------------|----------------|--------|
| `ProgressDialog` | `WinForms/Dialogs/ProgressDialog.cs` | `StatusPanel` (inline) | 🟡 Ainda usado pelo `WinFormsUIService.ShowProgress()`/`ShowStatus()`; convive lado a lado com o StatusPanel |
| `MainMenuView` (Console) | `Core/Views/MainMenuView.cs` | — | 🟢 Em uso ativo no modo Console |

---

> Este documento reflete o estado atual do código e deve ser atualizado conforme a arquitetura evolui.
