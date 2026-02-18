# Quality Audit

This folder contains the quality-audit framework executed after DLL parity and build consistency are verified.

## Gates

- Gate A: Freeze baseline (commit + parity evidence)
- Gate B: Automated static audit (build + pattern scans + project checks)
- Gate C: Manual deep review prompts and hotspot validation
- Gate D: Prioritized remediation backlog (`P0..P3`) and delivery batches

## Run

```powershell
pwsh -File scripts/quality/Invoke-QualityAudit.ps1 -Configuration Release
```

Optional parity source assembly:

```powershell
pwsh -File scripts/quality/Invoke-QualityAudit.ps1 \
  -Configuration Release \
  -ReferenceAssemblyPath "C:\Users\Castanea\Desktop\net10.0-windows\Synthesis.dll"
```

## Outputs

- `docs/quality/reports/latest-audit.md`
- timestamped report: `docs/quality/reports/YYYYMMDD-HHmmss-audit.md`
- baseline metadata: `docs/quality/reports/baseline.json`
