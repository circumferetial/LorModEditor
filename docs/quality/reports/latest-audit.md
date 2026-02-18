# Quality Audit Report

- Generated: 2026-02-19 00:41:21 +08:00
- Branch: Backup
- Baseline SHA: 7dd76c178b13175634a65ca60bdbe782db5dd8d5 (7dd76c1)
- Working tree dirty: True

## Gate A — Baseline Freeze
- Baseline metadata written to docs\quality\reports\baseline.json
- Assembly parity: Assembly parity comparison executed.
- Type diff count: 0
- Critical type method diff count: 0

## Gate B — Automated Static Audit
- Build failed: False
- Warning count: 0
- Error count: 0

### Pattern Scan Summary
- async void: 2
- empty catch block: 0
- catch without exception variable: 0
- Task.Run usage: 3
- MessageBox usage: 64
- lock statement: 0
- Core Abstraction namespace (for layering checks): 4

### Largest Files (Top 10)
- Synthesis\Feature\SkinEditor\SkinEditorViewModel.cs: 521 lines
- Synthesis\Feature\Book\UnifiedBook.cs: 467 lines
- Synthesis\Core\ProjectManager.cs: 371 lines
- Synthesis\Feature\SkinEditor\SkinRepository.cs: 340 lines
- Synthesis\Feature\Card\UnifiedCard.cs: 331 lines
- Synthesis\Feature\SkinEditor\SkinEditorView.xaml.cs: 315 lines
- Synthesis\Feature\SkinEditor\SkinCompatibilityGuard.cs: 284 lines
- Synthesis\Feature\Enemy\UnifiedEnemy.cs: 278 lines
- Synthesis\Core\Tools\DesignExporter.cs: 273 lines
- Synthesis\Core\Abstraction\BaseRepository.cs: 264 lines

### Method-Heavy Files (Top 10)
- Synthesis\Core\Abstraction\BaseRepository.cs: 28 methods
- Synthesis\Core\Abstraction\XWrapper.cs: 26 methods
- Synthesis\Feature\SkinEditor\SkinEditorViewModel.cs: 18 methods
- Synthesis\Feature\SkinEditor\SkinEditorView.xaml.cs: 17 methods
- Synthesis\Feature\SkinEditor\SkinRepository.cs: 16 methods
- Synthesis\Feature\Stage\StageEditorView.xaml.cs: 15 methods
- Synthesis\Core\ProjectManager.cs: 15 methods
- Synthesis\Feature\Enemy\EnemyRepository.cs: 14 methods
- Synthesis\Core\Tools\UnityRichTextHelper.cs: 14 methods
- Synthesis\Feature\SkinEditor\SkinCompatibilityGuard.cs: 12 methods

### Project Quality Baseline
- .editorconfig present: True
- Directory.Build.props present: True
- Test projects found: 1

## Gate C — Manual Deep Review Targets
- Repository/domain layering: check MessageBox usage in Core/Repository paths
- Error handling observability: replace silent catches and popup-only failures with structured logging
- Concurrency/lifecycle: migrate async void command paths to async Task patterns
- Maintainability hotspots: split SkinEditorViewModel/SkinEditorView code-behind and simplify orchestration in ProjectManager

## Findings
Format: `P级 | 文件:行 | 问题 | 影响 | 建议修复`
- P1 | Synthesis\Core\Abstraction\BaseRepository.cs:240 | Repository base class displays MessageBox directly | Data access layer depends on UI and cannot be cleanly reused/tested | Replace popup calls with structured error returns/logging; let ViewModel decide UI prompts
- P1 | Synthesis\Feature\MainWindow\MainWindowViewModel.cs:59 | async void command/event chain in viewmodel | Unhandled exceptions bypass normal flow and are harder to test | Use async Task command handlers and central exception pipeline
- P1 | Synthesis\Feature\Setting\SettingsViewModel.cs:39 | async void command/event chain in viewmodel | Unhandled exceptions bypass normal flow and are harder to test | Use async Task command handlers and central exception pipeline
- P1 | Synthesis\Feature\SkinEditor\SkinRepository.cs:210 | UI interaction inside repository/core data path | Layering leakage, difficult unit testing and reuse | Move interaction to viewmodel/service; repositories return result objects or domain errors
- P1 | Synthesis\Feature\SkinEditor\SkinRepository.cs:210 | SkinRepository performs save-time interactive UI decisions | Repository side effects complicate batch processing and error automation | Move user decision flow to ViewModel/service and keep repository side-effect free
- P1 | Synthesis\Feature\SkinEditor\SkinRepository.cs:212 | UI interaction inside repository/core data path | Layering leakage, difficult unit testing and reuse | Move interaction to viewmodel/service; repositories return result objects or domain errors
- P1 | Synthesis\Feature\SkinEditor\SkinRepository.cs:243 | UI interaction inside repository/core data path | Layering leakage, difficult unit testing and reuse | Move interaction to viewmodel/service; repositories return result objects or domain errors
- P2 | Synthesis\Feature\SkinEditor\SkinEditorViewModel.cs | Very large file with concentrated responsibilities | High maintenance cost and regression probability | Split into focused services/helpers and keep viewmodel slim

## Open Questions
- Should repository-layer user prompts be migrated to ViewModel immediately (batch 1), or behind a staged adapter?
- For async command migration, do we standardize on Prism Async DelegateCommand wrappers in this cycle?
- Is strict DLL parity required after every remediation commit, or only at batch boundaries?

## Remediation Plan
1. Batch 1 (P0/P1): compile blockers, exception visibility, async command safety, remove repository-layer UI prompts
2. Batch 2 (P2): split oversized classes, reduce orchestration coupling, add result/error transport contracts
3. Batch 3 (P3): naming/style consistency and low-risk cleanup

### Regression and Acceptance
- Rebuild Release for both projects after each batch
- Re-run audit script and compare finding counts
- Re-run DLL parity checks at batch boundary commits
