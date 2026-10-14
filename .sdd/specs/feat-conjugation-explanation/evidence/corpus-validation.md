# F-09 proposed corpus validation — 2026-10-13

Result: **mechanical checks pass for the draft packet**. This is not G1/G2 approval or runtime acceptance.

| Check | Result |
|---|---|
| Baseline rule IDs and unique manifest IDs | 9/9 identical to immutable development dataset |
| Scope decisions | 9 `keep` + `required`; excluded groups are explicit |
| Required per-rule cases | Every rule has at least 2 supported and 1 negative |
| Case totals | 21 supported, 9 negative, 6 cross-cutting |
| ID and reference integrity | Unique case IDs; manifest case lists, fixture IDs and source IDs resolve |
| Supported expected values | Base entry fixture present; Vietnamese explanation and each segment nonblank |
| Half-open UTF-16 | All supported spans start at 0, end at JavaScript/.NET UTF-16 length; ordered segment slices concatenate to input without gaps or overlap |
| Negative outcomes | No conjugation detail in any negative/fault case |
| Privacy | All examples are synthetic public strings; no selected user text is stored |
| Irregular and groups | する/来る in polite past; verbs and い-adjectives covered |

The locally pinned Sudachi image was observed on the 21 supported surfaces and selected out-of-scope surfaces; its segmentation matched the proposed surface slices. Provider output was not used to approve grammar meaning or Vietnamese text. The provider technical observation remains separate from editorial review.

## Pending human checks

- G1: user/PO approval of this exact scope packet version and hash.
- G2: independent review of the nine explanations, base forms, segment labels and original editorial/source-rights decisions. Source catalog intentionally has pending review fields.
- G3: fresh runtime comparison of approved expected cases and final acceptance; no runtime pass is claimed here.
