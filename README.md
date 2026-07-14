<div align="center">
  <h1>🚀 Instalador NewAcesso</h1>
  <p><strong>Instalador automatizado da suíte de produtos NewAcesso para Windows Server e Desktop</strong></p>
  <p>
    <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet" alt=".NET 10">
    <img src="https://img.shields.io/badge/WinForms-10.0-0078D6?logo=windows" alt="WinForms">
    <img src="https://img.shields.io/badge/tests-513_✓-009632" alt="513 tests passing">
    <img src="https://img.shields.io/badge/license-Proprietary-red" alt="License">
  </p>
</div>

---

## 📖 Sobre

O **Instalador NewAcesso** automatiza a instalação completa da plataforma NewAcesso em servidores Windows — desde a configuração do IIS e ativação de Features do Windows até a instalação de MSIs, WebApps, criação de diretórios e agendamento de tarefas.

### ✨ Recursos

| Recurso | Descrição |
|---------|-----------|
| 🖥️ **Dual Interface** | Modo Gráfico (WinForms) e Terminal (Console) compartilhando a mesma lógica |
| 📥 **Download Automático** | Baixa instaladores do Google Drive via API v3 |
| ⚙️ **Features Windows** | Ativa 32 componentes do Windows (IIS, .NET, ASP.NET, MSMQ, etc.) |
| 📂 **Diretórios** | Cria estrutura completa de pastas do NewAcesso |
| 🌐 **IIS** | Configura AppPools e Sites para WebAppDS e WebAppUI |
| 📦 **MSI** | Instala dezenas de MSIs com mapeamento inteligente de pastas |
| 🌍 **WebApps** | Instala WebApps (UI + DS) com fallback Admin Install |
| 📅 **Tarefas** | Agenda tarefas do Windows |
| 🗑️ **Desinstalação** | Remove todos os componentes com auditoria completa |
| 📊 **Painel de Status** | Progresso em tempo real e sumário de erros inline |

---

## 🚀 Começando

### Pré-requisitos

- **Sistema Operacional:** Windows 10+ ou Windows Server 2016+
- **.NET Runtime:** .NET 10.0 (incluído no self-contained publish)
- **Permissões:** Executar como **Administrador**

### Download

> ⚠️ O instalador **precisa ser executado como Administrador**. Sem privilégios de admin, o programa exibe uma mensagem de erro e encerra.

> 💡 O download de instaladores do Google Drive requer uma **chave de API do Google** (API Key).
> Você precisará informá-la na tela de Download durante a execução.
> [Saiba como obter sua chave](https://developers.google.com/drive/api/guides/enable-drive-api).

1. Faça o download do executável mais recente na seção [Releases](../../releases)
2. Extraia o arquivo para uma pasta de sua preferência
3. Execute `InstaladorNewAcesso.Launcher.exe` — ou diretamente:
   - `InstaladorNewAcesso.WinForms.exe` (modo gráfico)
   - `InstaladorNewAcesso.Console.exe` (modo terminal)

### Compilando do Código Fonte

```bash
# Restaurar dependências
dotnet restore

# Compilar toda a solução
dotnet build

# Publicar como single-file
dotnet publish -c Release -o ./publish

# Executar testes
dotnet test
```

> A solution usa `.slnx` (formato XML simplificado). Certifique-se de usar .NET 10 SDK ou superior.

---

## 🧭 Navegação

O instalador segue um fluxo linear de configuração:

```
Main Menu
  │
  ├── 1. 📥 Download           ← Baixar instaladores do Google Drive
  ├── 2. ⚙️ Recursos Windows    ← Ativar features (IIS, .NET, etc.)
  ├── 3. 📂 Diretórios          ← Criar estrutura de pastas
  ├── 4. 🌐 IIS                 ← Configurar AppPools e Sites
  ├── 5. 📦 MSI                 ← Instalar MSIs do sistema
  ├── 6. 🌍 WebApp              ← Instalar WebApps (UI + DS)
  ├── 7. 📅 Agendamento         ← Configurar tarefas agendadas
  └── 8. 🗑️ Desinstalação       ← Remover todos os componentes
```

Cada etapa pode ser acessada individualmente ou em sequência, com botão **← Voltar** disponível para navegação entre telas.

---

## 🏗️ Arquitetura

O projeto segue uma arquitetura em camadas (Onion/Clean Architecture) com **5 projetos + 1 de testes**:

```
src/
├── 📁 InstaladorNewAcesso.Abstractions/   # Interfaces + Models (0 dependências)
├── 📁 InstaladorNewAcesso.Core/           # Lógica de negócio
├── 📁 InstaladorNewAcesso.WinForms/       # Interface gráfica (WinForms)
├── 📁 InstaladorNewAcesso.Console/        # Interface terminal (Spectre.Console)
└── 📁 InstaladorNewAcesso.Launcher/       # Ponto de entrada unificado
```

```
┌─────────────────────────────────────┐
│  WinForms / Console (UI)            │  → Conhece Abstractions + Core
├─────────────────────────────────────┤
│  Core (Lógica de Negócio)           │  → Conhece Abstractions
├─────────────────────────────────────┤
│  Abstractions (Contracts/Models)    │  → Conhece nada
└─────────────────────────────────────┘
```

> 📖 Para detalhes completos da arquitetura, consulte [`ARCHITECTURE.md`](ARCHITECTURE.md) — que cobre decisões arquiteturais, modelos de dados, sistema de temas, navegação, ciclo de vida IView, oportunidades de melhoria e sugestões futuras.

### Projetos em Detalhe

| Projeto | Tipo | Função |
|---------|------|--------|
| **Abstractions** | Library | Interfaces (`IView`, `IUIService`, `IProcessExecutor`, etc.) e Models (`InstallationPaths`, `MsiInstallationModel`, `StepStatus`, etc.) |
| **Core** | Library | Serviços (`MsiInstaller`, `WebAppInstaller`, `GoogleDriveDownloader`), scanners, utilitários IIS, config helpers, factory de features Windows |
| **WinForms** | WinExe | 11 UserControls, `MainForm`, `NavigationManager`, tema escuro (`ThemeColors`+`ThemeFonts`+`UIStyles`) |
| **Console** | Exe | `ConsoleUIService` com Spectre.Console, Views no estilo terminal |
| **Launcher** | WinExe | Diálogo inicial para escolher entre modo Gráfico e Terminal |

---

## 🎨 Tema

A interface WinForms usa um **tema escuro profissional** centralizado:

```css
Background     #12121E  │  TextPrimary    #FFFFFF
Surface        #1E1E32  │  TextAccent     #00FFFF  (ciano)
SurfaceHover   #32324A  │  TextMuted      #646478
InputBg        #1E1E32  │  Success        #009632
Primary        #0078D7  │  Danger         #B42828
```

- **ThemeColors** — 30+ constantes de cor em um único arquivo
- **ThemeFonts** — 14 definições de fonte com cache lazy (prevenindo vazamento GDI)
- **UIStyles** — 20+ métodos factory (`CreateTitle()`, `CreatePrimaryButton()`, `CreateTextBox()`, etc.)

---

## 🧪 Testes

O projeto possui **513 testes unitários** (xUnit + FluentAssertions + NSubstitute):

```bash
dotnet test
# Result: Pass: 513, Fail: 0, Skip: 0 (~9s)
```

| Área | Testes | Status |
|------|--------|--------|
| Models | ~30 | ✅ |
| Services | ~120 | ✅ |
| Utils | ~250 | ✅ |
| Configurations | ~40 | ✅ |
| Controls | ~34 | ✅ |
| Integration | ~9 | ✅ |

---

## 🛠️ Tecnologias

| Tecnologia | Versão | Uso |
|------------|--------|-----|
| .NET | 10.0 | Runtime e SDK |
| Windows Forms | 10.0 | Interface gráfica |
| Spectre.Console | 0.57 | Interface de terminal (Console) |
| xUnit | 2.9 | Framework de testes |
| FluentAssertions | 8.10 | Asserções encadeadas |
| NSubstitute | 5.3 | Mocking |
| Coverlet | 10.0 | Cobertura de código |

---

## 📁 Estrutura de Diretórios do NewAcesso

O instalador cria e gerencia a seguinte estrutura:

```
<BasePath>/
├── Instaladores/          ← MSIs baixados do Google Drive
└── NewAcesso/
    ├── AutoAtendimento/
    ├── ConexBridge/
    ├── ConnectionRecord/
    ├── Controller/
    │   ├── ControleAcesso/
    │   ├── CoreWs/
    │   ├── Fabricantes/
    │   └── Task/
    ├── ControllerOffline/
    │   ├── Arquivos/
    │   ├── WinService_Ex/
    │   └── WinService_In/
    ├── VisitAuthorization/
    ├── WebAppDS/           ← Porta 8080
    ├── WebAppUI/           ← Porta 8081
    │   └── Fabricantes/
    └── Win/
```

---

## 📄 Licença

**Propriedade da NewAcesso.**  
Código interno — não distribuir sem autorização.

---

## 🤝 Contribuindo

1. Faça um fork do projeto
2. Crie uma branch: `git checkout -b feature/nome-da-feature`
3. Commit suas mudanças: `git commit -m 'feat: adiciona nova funcionalidade'`
4. Push: `git push origin feature/nome-da-feature`
5. Abra um Pull Request

### Convenções de Código

- **Nullable habilitado** em toda a solution
- **Análise de código:** `latest-Recommended` com `EnforceCodeStyleInBuild`
- **Testes:** xUnit com `[Fact]`, asserções com FluentAssertions
- **Nomenclatura de testes:** `MethodName_Scenario_ExpectedResult`
- **Cores/Fontes:** nunca hardcoded — sempre via `ThemeColors`/`ThemeFonts`

---

## 📚 Documentação

| Documento | Descrição |
|-----------|-----------|
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Arquitetura completa, decisões, pontos fortes, melhorias e sugestões futuras |
| `README.md` (este) | Visão geral e guia rápido |

---

<div align="center">
  <sub>NewAcesso — Instalador Automatizado | Julho 2026</sub>
</div>
