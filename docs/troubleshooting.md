# 🩺 Guia de Diagnóstico e Resolução de Problemas (Troubleshooting)

> **Instalador NewAcesso** — Tratamento de Erros, Logs e Resolução de Falhas

Este guia auxilia engenheiros e analistas de suporte no diagnóstico e resolução rápida de falhas durante a execução do Instalador NewAcesso.

---

## 🛑 1. Problemas de Permissões e Inicialização

### Sintoma: "Este instalador precisa de privilégios de Administrador!"
- **Causa:** O terminal ou executável foi aberto sem elevação de privilégios.
- **Solução:**
  1. Clique com o botão direito no ícone do PowerShell / Prompt de Comando ou no executável `InstaladorNewAcesso.Console.exe`.
  2. Selecione **"Executar como Administrador"**.

---

## 📦 2. Erros em Instalação de MSIs (msiexec)

O instalador gera logs detalhados de cada instalação MSI via [`MsiLogHelper`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Core/Utils/MsiLogHelper.cs). Os logs são gravados no diretório do instalador com a extensão `.log`.

### Tabela de Códigos de Erro Comuns do MSI

| Código de Saída | Significado | Como Resolver |
|---|---|---|
| **`1603`** | Erro fatal durante a instalação | Consulte o arquivo `.log` gerado para o MSI específico. Causas frequentes: pastas de destino bloqueadas, serviço NewAcesso em execução ou falta de pré-requisito C++ Redistributable. |
| **`1618`** | Outra instalação já está em andamento | Outro processo `msiexec.exe` ou Windows Update está travando o mutex. Execute `powershell ./cleanup-orphans.ps1` ou encerre processos `msiexec.exe` no Gerenciador de Tarefas. |
| **`1602`** | Cancelado pelo usuário | O operador cancelou a operação interativamente. |
| **`1638`** | Outra versão deste produto já está instalada | Utilize a opção de Desinstalação (Menu 8) antes de prosseguir com a instalação da nova versão. |

---

## ⚙️ 3. Falhas na Ativação de Recursos do Windows (DISM / ServerManager)

### Sintoma: Erro `0x800F081F` ou `0x800F0906` ao ativar NetFx3 (.NET 3.5)
- **Causa:** O Windows não localizou os arquivos de origem (*payload*) do .NET 3.5 na imagem local e não possui acesso ao Windows Update.
- **Solução:**
  1. Monte a mídia de instalação do Windows (ISO) em uma unidade (ex: `D:`).
  2. Execute manualmente a ativação com o parâmetro de origem:
     ```powershell
     dism /online /enable-feature /featurename:NetFx3 /All /Source:D:\sources\sxs /LimitAccess
     ```

---

## 🌐 4. Problemas no IIS e WebApps

### Sintoma: WebAppUI ou WebAppDS retorna Erro HTTP 500.19 ou 404
- **Causa:** O módulo ASP.NET / .NET Extensibility não foi registrado ou o AppPool está configurado para .NET CLR incorreto.
- **Solução:**
  1. Certifique-se de que a Etapa 2 (Recursos Windows) foi executada com sucesso.
  2. Execute `iisreset` no terminal administrativo.
  3. Verifique se o caminho físico configurado no site do IIS aponta corretamente para a pasta `<BasePath>/NewAcesso/WebApp/UI` ou `<BasePath>/NewAcesso/WebApp/DS`.

---

## 🧹 5. Limpeza de Processos e Trava de Arquivos

Se arquivos de instalação estiverem travados durante um processo de atualização ou reinstalação:
```powershell
# Executar script de limpeza de processos órfãos
powershell -ExecutionPolicy Bypass -File ./cleanup-orphans.ps1
```
Esse script encerra com segurança instâncias pendentes de instaladores e libera arquivos temporários.
