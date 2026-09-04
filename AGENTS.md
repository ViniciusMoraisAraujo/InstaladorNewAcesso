# 🤖 AGENTS.md — Diretrizes para Agentes de IA & Engenheiros

> **Projeto:** Instalador NewAcesso  
> **Tecnologia:** .NET 10.0 / C# 13  
> **Plataforma Alvo:** Windows Server 2016+ / Windows 10+  
> **Interface:** Terminal Interativo (Spectre.Console)  

Este documento define os princípios arquiteturais, regras de codificação, diretrizes de segurança, comandos operacionais e convenções que **qualquer agente de IA ou desenvolvedor** deve seguir rigorosamente ao interagir com este repositório.

---

## 🏛️ 1. Visão Geral e Estrutura da Solução

O **Instalador NewAcesso** automatiza a preparação de ambiente, ativação de recursos do Windows (IIS, ASP.NET, MSMQ, etc.), criação de diretórios, instalação de pacotes MSI, configuração de WebApps no IIS e agendamento de tarefas do sistema.

A solution utiliza o formato XML simplificado `.slnx` e é dividida em **4 projetos**:

```
InstaladorNewAcesso.slnx
├── src/
│   ├── InstaladorNewAcesso.Abstractions/   # Contratos, Interfaces e Modelos de Domínio
│   ├── InstaladorNewAcesso.Core/           # Lógica de negócio, instaladores, scanners e utilitários
│   └── InstaladorNewAcesso.Console/        # Interface de Linha de Comando (CLI / Spectre.Console)
└── tests/
    └── InstaladorNewAcesso.Tests/          # Testes Unitários e de Integração
```

### Regras de Dependência entre Camadas (Clean/Onion Architecture)

```
┌────────────────────────────────────────────────────────┐
│  InstaladorNewAcesso.Console (UI / Ponto de Entrada)   │  → Depende de Abstractions e Core
├────────────────────────────────────────────────────────┤
│  InstaladorNewAcesso.Core (Lógica de Negócio)          │  → Depende exclusivamente de Abstractions
├────────────────────────────────────────────────────────┤
│  InstaladorNewAcesso.Abstractions (Contratos & Modelos)│  → Nenhuma dependência de outros projetos
└────────────────────────────────────────────────────────┘
```

> ⚠️ **REGRA INVIOLÁVEL:**  
> - `Abstractions` **NUNCA** deve referenciar `Core` ou `Console`.  
> - `Core` **NUNCA** deve referenciar `Console` ou bibliotecas específicas de UI (como `Spectre.Console` ou `System.Windows.Forms`).  
> - Toda interação com o usuário na camada `Core` deve ocorrer por meio da abstração [`IUIService`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Abstractions/Interfaces/IUIService.cs).

---

## 💻 2. Convenções e Padrões de Código C# 13 / .NET 10

### 2.1. Organização de Arquivos e Namespaces
- Utilize **File-Scoped Namespaces** em todos os novos arquivos C#:
  ```csharp
  namespace InstaladorNewAcesso.Core.Services;
  ```
- Mantenha a tipagem nula estrita ativada (`#nullable enable`).
- Evite aninhamento desnecessário de classes em arquivos múltiplos; adote o princípio de um tipo público principal por arquivo.

### 2.2. Nomenclatura e Estilo
- **Classes, Métodos, Propriedades, Eventos e Enums:** `PascalCase`.
- **Campos privados de instância:** `_camelCase` (com prefixo underscore).
- **Campos privados estáticos:** `s_camelCase` (com prefixo `s_`).
- **Interfaces:** Prefixadas com `I` em `PascalCase` (ex: `IProcessExecutor`).
- **Métodos Assíncronos:** Devem sempre possuir o sufixo `Async` e aceitar `CancellationToken` quando executam operações assíncronas reais de I/O ou processos externos.

### 2.3. Boas Práticas de Implementação
- **Validação de Argumentos:** Utilize métodos auxiliares modernos como `ArgumentNullException.ThrowIfNull(param)` e `ArgumentException.ThrowIfNullOrWhiteSpace(param)`.
- **Tratamento de Exceções:** Nunca faça *swallowing* de exceções (blocos `catch` vazios). Sempre registre o erro no [`AuditLogger`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Core/Utils/AuditLogger.cs) ou exiba uma mensagem informativa via `IUIService.WriteError`.
- **Uso de Recursos Descartáveis:** Sempre utilize declarações `using var` para classes que implementam `IDisposable` ou `IAsyncDisposable`.
- **Encodings e Cultura:** Ao manipular arquivos de configuração (.ini, .xml, .json) ou executar formatações numéricas e de data, garanta compatibilidade explícita com `CultureInfo.InvariantCulture` ou codificação UTF-8 / ANSI conforme a exigência do módulo legado.

---

## 🔒 3. Diretrizes de Segurança e Sistema Operacional

1. **Privilégios Administrativos:**
   - A aplicação é projetada para rodar exclusivamente em modo elevado (`Administrator`). O `Program.cs` valida os privilégios na inicialização via `WindowsPrincipal`.
2. **Execução Segura de Comandos (PowerShell / DISM / msiexec):**
   - Todas as invocações de executáveis nativos e scripts de automação devem passar por [`ProcessExecutor`](file:///c:/dev/InstaladorNewAcesso-main/src/InstaladorNewAcesso.Core/Utils/ProcessExecutor.cs).
   - Sanitize parâmetros e utilize interpolação segura para prevenir injeção de argumentos em shells.
3. **Registro do Windows (Registry):**
   - Acesse chaves do registro com escopo mínimo necessário (somente leitura quando não for gravar).
   - Sempre proteja operações em chaves de 64-bit e 32-bit (`RegistryView.Registry64` / `RegistryView.Registry32`).
4. **Idempotência de Operações:**
   - Criação de pastas, ativação de features, cópia de arquivos e configuração do IIS devem ser idempotentes (executar mais de uma vez não deve gerar erros ou corrupção de estado).

---

## 🧪 4. Estratégia e Convenções de Testes

- **Framework:** `xUnit` + `FluentAssertions` + `NSubstitute`.
- **Projetos de Testes:** Localizados em `tests/InstaladorNewAcesso.Tests/`.
- **Padrão AAA:** Todos os testes unitários devem seguir estritamente o padrão `Arrange, Act, Assert`.
- **Testes com I/O Real:** Testes que manipulam arquivos devem utilizar pastas temporárias isoladas em `Path.GetTempPath()` e implementar `IDisposable` para garantir a limpeza completa após a execução.
- **Mocks:** Isole chamadas de UI (`IUIService`) e de processos externos (`IProcessExecutor`) usando substitutos do NSubstitute para manter testes unitários rápidos e determinísticos.

---

## ⚡ 5. Comandos Úteis para Agentes

| Ação | Comando PowerShell / dotnet |
|---|---|
| **Restaurar Dependências** | `dotnet restore` |
| **Compilar Solução** | `dotnet build` |
| **Compilar Release** | `dotnet build -c Release` |
| **Executar Testes** | `dotnet test --verbosity normal` |
| **Executar Teste Específico** | `dotnet test --filter "FullyQualifiedName~MsiInstallerTests"` |
| **Publicar Executável Único** | `powershell -ExecutionPolicy Bypass -File ./scripts/publish.ps1` |
| **Limpar Processos Órfãos** | `powershell -ExecutionPolicy Bypass -File ./cleanup-orphans.ps1` |

---

## 🤖 6. Checklist de Validação para Agentes de IA

Antes de submeter ou concluir qualquer tarefa, o agente deve verificar:
- [ ] O código compila sem erros (`dotnet build`).
- [ ] A suíte de testes passa com sucesso (`dotnet test`).
- [ ] Nenhuma quebra na fronteira de camadas foi introduzida (Core não conhece Console).
- [ ] Comentários XML e documentação pública foram preservados ou atualizados.
- [ ] Arquivos novos possuem formatação compatível com [.editorconfig](file:///c:/dev/InstaladorNewAcesso-main/.editorconfig).
