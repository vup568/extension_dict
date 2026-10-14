# F-09 environment availability — 2026-10-13

| Check | Observed |
|---|---|
| .NET SDK | `10.0.400`; unit/integration/infra restore assets exist |
| Node | `v22.15.0`; `extension/node_modules/.bin/vitest.cmd` exists |
| Docker Engine | `29.6.1`; Docker pipe requires elevated sandbox permission in this session |
| Existing tokenizer image | `jp-reading-tokenizer:verified-20261013`, image ID `sha256:14ece806bfac24843e05a79a1c1c899a929621ad0d26ff4c02bf4c894585a1f9` |
| Browsers | Chrome, Edge and Brave executables available; Firefox executable not found at the standard installed location |
| Spec Kit | Prerequisite script resolves this F-09 feature directory and available documents; checklist `requirements.md` 16/16 complete; no `.specify/extensions.yml` |
| Ignore files | `.gitignore` and `.dockerignore` exist and exclude generated/build data; source conjugation JSON has an explicit `.gitignore` exception |

No package or browser was installed. Docker was used only with an existing image and no mounted volume while observing synthetic example tokenization. Runtime tests and browser parity are later gates, not established by this environment inventory.
