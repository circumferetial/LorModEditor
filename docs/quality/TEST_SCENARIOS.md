# Regression Scenarios

This checklist is executed after each remediation batch.

## Functional

1. Open a valid project from `MainWindow` and verify all modules load.
2. Switch tabs/modules and confirm viewmodels initialize without exceptions.
3. Edit and save data in `Card`, `Book`, `Enemy`, `Skin` modules.
4. Run export flow from `MainWindow` and verify output files are produced.
5. In skin editor, verify image load, action add/remove, and save behavior.

## Stability

1. No unhandled exception dialogs during normal editing workflows.
2. Expected failures are logged through `Logger` with file and operation context.
3. Async command paths do not silently swallow exceptions.

## Consistency

1. `dotnet build` for `Synthesis` and plugin projects succeeds in `Release`.
2. DLL parity check (types/signatures/critical targets) is re-run at batch boundaries.
3. `scripts/quality/Invoke-QualityAudit.ps1` produces reduced or stable high-severity findings.
