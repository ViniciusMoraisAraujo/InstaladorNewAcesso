# 🛠️ Guia de Configuração e Compilação (Setup Guide)

> **Instalador NewAcesso** — .NET 10.0 / Windows

Este documento fornece instruções completas para configurar o ambiente de desenvolvimento, compilar a solução, executar localmente e gerar os binários de distribuição (publicação).

---

## 📋 1. Pré-requisitos

### Ambiente de Desenvolvimento
- **Sistema Operacional:** Windows 10 (Build 19041+) ou Windows Server 2016+ (64-bit).
- **.NET SDK:** .NET 10.0 SDK instalado ([Download .NET SDK](https://dotnet.microsoft.com/download)).
- **PowerShell:** PowerShell 5.1 ou PowerShell 7+ (pwsh).
- **Editor / IDE Recomendado:** Visual Studio 2026 / Visual Studio Code / JetBrains Rider com suporte a C# 13 e `.slnx`.
- **Privilégios:** Acesso de **Administrador Local** no Windows (obrigatório para testes que interagem com IIS, Registro e Features do Windows).

---

## 🚀 2. Clonando e Restaurando a Solução

```powershell
# Clonar o repositório
git clone <url-do-repositorio>
cd InstaladorNewAcesso-main

# Restaurar pacotes NuGet
dotnet restore
```

---

## 🔨 3. Compilação da Solução

A solução utiliza o formato XML moderno `.slnx` (`InstaladorNewAcesso.slnx`).

```powershell
# Compilar em modo Debug
dotnet build

# Compilar em modo Release
dotnet build -c Release
```

---

## 🧪 4. Executando os Testes

A suíte possui mais de 500 testes automatizados que cobrem modelos, regras de validação, scanners e serviços de instalação.

```powershell
# Executar todos os testes
dotnet test --verbosity normal

# Executar testes com filtro de categoria/classe
dotnet test --filter "FullyQualifiedName~DirectorySetupTests"
dotnet test --filter "FullyQualifiedName~MsiInstallerTests"
```

---

## 📦 5. Publicação (Single-File / Self-Contained)

O projeto está pré-configurado no [`Directory.Build.props`](file:///c:/dev/InstaladorNewAcesso-main/Directory.Build.props) para gerar um binário único auto-contido (`self-contained`), dispensando a pré-instalação do runtime .NET 10 na máquina alvo.

### Via Script PowerShell
```powershell
# Executar o script automatizado de publicação
powershell -ExecutionPolicy Bypass -File ./scripts/publish.ps1
```
O executável gerado será colocado em `dist/InstaladorNewAcesso.Console.exe`.

### Via Linha de Comando Direta
```powershell
dotnet publish src/InstaladorNewAcesso.Console/InstaladorNewAcesso.Console.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o ./dist
```

---

## 🔑 6. Configuração da Google Drive API Key

Para utilizar o recurso de download automatizado de instaladores a partir do Google Drive:
1. Acesse o [Google Cloud Console](https://console.cloud.google.com/).
2. Crie ou selecione um projeto e ative a **Google Drive API v3**.
3. Crie uma **API Key** (Chave de API) na seção de Credenciais.
4. Na tela inicial do instalador (Opção 1 - Download), informe a chave gerada e o ID ou link da pasta compartilhada no Google Drive.
