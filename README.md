<div align="center">
  <h1>🚀 Instalador NewAcesso</h1>
  <p><strong>Instalador automatizado da suíte de produtos NewAcesso para Windows Server e Desktop</strong></p>
  <p>
    <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet" alt=".NET 10">
    <img src="https://img.shields.io/badge/CLI-Spectre.Console-0078D6" alt="Spectre.Console">
    <img src="https://img.shields.io/badge/tests-500+_✓-009632" alt="500+ tests passing">
    <img src="https://img.shields.io/badge/architecture-Clean%2FOnion-blue" alt="Clean Architecture">
    <img src="https://img.shields.io/badge/license-Proprietary-red" alt="License">
  </p>
</div>

---

## 📖 Sobre

O **Instalador NewAcesso** é uma aplicação de terminal interativo moderna e robusta para automatizar a instalação, configuração e manutenção de servidores Windows da plataforma NewAcesso — gerenciando a ativação de Recursos do Windows (IIS, ASP.NET, MSMQ), criação da árvore de diretórios, configuração do IIS, instalação de pacotes MSI, configuração de WebApps e agendamento de tarefas.

### ✨ Recursos Principais

| Recurso | Descrição |
|---------|-----------|
| 🖥️ **Terminal Interativo Rico** | Interface CLI moderna com cores, spinners, barras de progresso e menus com [Spectre.Console](https://spectreconsole.net/) |
| ⚙️ **Recursos do Windows** | Ativação automatizada de 32 componentes do Windows (IIS, .NET 3.5/4.x, ASP.NET, MSMQ, WAS, WCF) via DISM e ServerManager |
| 📂 **Gestão de Diretórios** | Criação, verificação de integridade e idempotência de toda a estrutura de pastas do NewAcesso |
| 🌐 **Automação IIS** | Criação e configuração de AppPools e Sites para WebAppDS e WebAppUI |
| 📦 **Instalador de MSIs** | Mapeamento inteligente de pastas de destino e instalação silenciosa com log detalhado |
| 🌍 **Gestão de WebApps** | Instalação de WebApps com suporte a extração e fallback de instalação administrativa |
| 📅 **Tarefas Agendadas** | Criação e configuração de tarefas do Windows Task Scheduler |
| 🗑️ **Desinstalação & Auditoria** | Desinstalação completa e registro de auditoria via `AuditLogger` |
| 📊 **Painel de Status** | Feedback em tempo real com rastreamento detalhado de passos e relatórios |

---

## 🚀 Começando

### Pré-requisitos

- **Sistema Operacional:** Windows 10+ ou Windows Server 2016+ (64-bit)
- **.NET Runtime:** .NET 10.0 (incluso no executável *self-contained*)
- **Permissões:** Executar como **Administrador**

### Execução

> ⚠️ O instalador **precisa ser executado como Administrador**. Sem privilégios elevados, o programa exibe uma mensagem de erro e é finalizado.

1. Baixe o executável da versão mais recente ou compile o projeto.
2. Abra o terminal elevado e execute:
   ```powershell
   .\InstaladorNewAcesso.Console.exe
   ```

### Compilação a partir do Código-Fonte

```powershell
# Restaurar dependências
dotnet restore

# Compilar toda a solução (.slnx)
dotnet build

# Executar a suíte de testes automatizados
dotnet test

# Publicar como executável único autônomo (Self-Contained)
powershell -ExecutionPolicy Bypass -File ./scripts/publish.ps1
```

---

## 🧭 Menu de Navegação

O instalador conta com um menu interativo completo:

```
╔══════════════════════════════════════════════════════╗
║               INSTALADOR NEWACESSO                   ║
╚══════════════════════════════════════════════════════╝
  1. ⚙️  Recursos Windows     ← Ativar 32 features (IIS, .NET, MSMQ, etc.)
  2. 📂 Diretórios           ← Criar estrutura de pastas do NewAcesso
  3. 🌐 IIS                  ← Configurar AppPools e Sites WebApp
  4. 📦 MSI                  ← Instalar pacotes MSI do sistema
  5. 🌍 WebApp               ← Instalar e configurar WebApps (UI + DS)
  6. 📅 Agendamento          ← Configurar tarefas agendadas do Windows
  7. 🗑️  Desinstalação        ← Desinstalar componentes e auditar remoções
  0. 🚪 Sair
```

---

## 🏗️ Arquitetura da Solução

A solução adota **Clean / Onion Architecture** com separação estrita de responsabilidades em **3 camadas principais + 1 projeto de testes**:

```
src/
├── 📁 InstaladorNewAcesso.Abstractions/   # Interfaces (IUIService, IProcessExecutor, etc.) e Models
├── 📁 InstaladorNewAcesso.Core/           # Lógica de negócio, scanners, instaladores e helpers
└── 📁 InstaladorNewAcesso.Console/        # Interface de terminal (Spectre.Console) e views
tests/
└── 📁 InstaladorNewAcesso.Tests/          # Suíte de testes unitários e de integração
```

```
┌───────────────────────────────────────────────────────────┐
│  InstaladorNewAcesso.Console (UI / Ponto de Entrada)      │  → Depende de Abstractions e Core
├───────────────────────────────────────────────────────────┤
│  InstaladorNewAcesso.Core (Lógica de Negócio)             │  → Depende de Abstractions
├───────────────────────────────────────────────────────────┤
│  InstaladorNewAcesso.Abstractions (Contratos e Modelos)   │  → 0 dependências externas
└───────────────────────────────────────────────────────────┘
```

### Projetos

| Projeto | Tipo | Responsabilidade |
|---------|------|------------------|
| **`InstaladorNewAcesso.Abstractions`** | Class Library | Define interfaces (`IUIService`, `IProcessExecutor`, `IIisInstaller`, `IFeatureInstaller`), modelos (`InstallationPaths`, `MsiInstallModel`, `StepState`) e enums. |
| **`InstaladorNewAcesso.Core`** | Class Library | Implementa lógica de scanners (`MsiScanner`, `WebAppScanner`), instaladores (`MsiInstaller`, `WebAppInstaller`), configurações e utilitários. |
| **`InstaladorNewAcesso.Console`** | Console Executable | Entrypoint da aplicação, implementação de `ConsoleUIService` e views interativas do terminal. |
| **`InstaladorNewAcesso.Tests`** | Test Project | Suíte de testes unitários e de integração com xUnit, FluentAssertions e NSubstitute. |

---

## 📚 Documentação e Guias

Toda a documentação técnica, operacional e arquitetural está disponível no repositório:

| Documento | Descrição |
|---|---|
| [**`AGENTS.md`**](AGENTS.md) | Diretrizes e regras essenciais para Agentes de IA e engenheiros |
| [**`ARCHITECTURE.md`**](ARCHITECTURE.md) | Documento aprofundado de arquitetura, fluxo de dados e decisões técnicas |
| [**`docs/setup-guide.md`**](docs/setup-guide.md) | Guia completo de configuração de ambiente, compilação e publicação |
| [**`docs/features-and-msi-mapping.md`**](docs/features-and-msi-mapping.md) | Mapeamento dos 32 recursos Windows, pacotes MSI e diretórios |
| [**`docs/troubleshooting.md`**](docs/troubleshooting.md) | Guia de diagnóstico de erros, logs do MSI e soluções operacionais |

---

## 🧪 Estratégia de Testes

A solução possui ampla cobertura de testes cobrindo regras de negócio, scanners, criação de arquivos e integrações:

```powershell
dotnet test --verbosity normal
```

---

## 📄 Licença

**Propriedade da NewAcesso.**  
Código interno para distribuição e uso autorizado.
