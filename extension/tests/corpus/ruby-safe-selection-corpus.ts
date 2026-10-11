import type { RubySafeSelectionResult } from "../../src/boundary/rubySafeSelectionResult";

export interface SelectionBoundaryDescriptor {
  readonly selector: string;
  readonly childNodePath?: readonly number[];
  readonly offset: number;
}

export type SelectionRangeDescriptor =
  | Readonly<{
      kind: "contents";
      selector: string;
      direction?: "forward" | "backward";
    }>
  | Readonly<{
      kind: "boundaries";
      start: SelectionBoundaryDescriptor;
      end: SelectionBoundaryDescriptor;
      direction?: "forward" | "backward";
    }>;

export interface RubySafeSelectionCorpusCase {
  readonly id: `F02-${string}`;
  readonly acceptanceCriteria: readonly `F02-AC-${string}`[];
  readonly html: string;
  readonly textOverrides?: readonly Readonly<{
    selector: string;
    value: string;
  }>[];
  readonly range: SelectionRangeDescriptor;
  readonly expected: RubySafeSelectionResult;
}

export const rubySafeSelectionCorpus: readonly RubySafeSelectionCorpusCase[] = [
  {
    id: "F02-plain-base-text",
    acceptanceCriteria: ["F02-AC-003"],
    html: "<span id='target'>選択した日本語</span>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "選択した日本語" },
  },
  {
    id: "F02-ruby-reading-excluded",
    acceptanceCriteria: ["F02-AC-001"],
    html: "<ruby id='target'>日本<rt>にほん</rt></ruby>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "日本" },
  },
  {
    id: "F02-ruby-rp-excluded",
    acceptanceCriteria: ["F02-AC-002"],
    html:
      "<ruby id='target'>日本<rp>(</rp><rt>にほん</rt><rp>)</rp></ruby>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "日本" },
  },
  {
    id: "F02-multiple-ruby-document-order",
    acceptanceCriteria: ["F02-AC-003"],
    html:
      "<span id='target'><ruby>今日<rt>きょう</rt></ruby>は<ruby>晴れ<rt>はれ</rt></ruby></span>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "今日は晴れ" },
  },
  {
    id: "F02-repeated-text-ancestry",
    acceptanceCriteria: ["F02-AC-004"],
    html: "<ruby id='target'>かな<rt>かな</rt></ruby>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "かな" },
  },
  {
    id: "F02-partial-base-to-annotation",
    acceptanceCriteria: ["F02-AC-005", "F02-AC-006"],
    html:
      "<ruby><span id='base'>日本</span><rt id='reading'>にほん</rt></ruby>",
    range: {
      kind: "boundaries",
      start: { selector: "#base", childNodePath: [0], offset: 1 },
      end: { selector: "#reading", childNodePath: [0], offset: 2 },
    },
    expected: { status: "ok", text: "本" },
  },
  {
    id: "F02-partial-annotation-to-base",
    acceptanceCriteria: ["F02-AC-006"],
    html:
      "<ruby><rt id='reading'>かな</rt><span id='base'>仮名</span></ruby>",
    range: {
      kind: "boundaries",
      start: { selector: "#reading", childNodePath: [0], offset: 1 },
      end: { selector: "#base", childNodePath: [0], offset: 1 },
    },
    expected: { status: "ok", text: "仮" },
  },
  {
    id: "F02-annotation-only",
    acceptanceCriteria: ["F02-AC-007"],
    html: "<ruby>日本<rt id='target'>にほん</rt></ruby>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "error", code: "no-base-text" },
  },
  {
    id: "F02-nested-inline",
    acceptanceCriteria: ["F02-AC-008"],
    html:
      "<span id='target'><span>今日は</span><a href='#'><b>晴れ</b></a></span>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "今日は晴れ" },
  },
  {
    id: "F02-unicode-preservation",
    acceptanceCriteria: ["F02-AC-012"],
    html: "<span id='target'>か\u3099𠀀󠄀�\ud800</span>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "か\u3099𠀀󠄀�\ud800" },
  },
  {
    id: "F02-japanese-annotation-non-japanese-base",
    acceptanceCriteria: ["F02-AC-028"],
    html:
      "<ruby id='target'><rt>ぜん</rt>ABC<rt>ご</rt></ruby>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "ABC" },
  },
  {
    id: "F02-explicit-br",
    acceptanceCriteria: ["F02-AC-009"],
    html: "<span id='target'>一行<br>二行</span>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "一行\n二行" },
  },
  {
    id: "F02-adjacent-blocks",
    acceptanceCriteria: ["F02-AC-010"],
    html: "<div id='target'><p>一</p><p>二</p></div>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "一\n二" },
  },
  {
    id: "F02-consecutive-br",
    acceptanceCriteria: ["F02-AC-011"],
    html: "<span id='target'>一<br><br>三</span>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "一\n\n三" },
  },
  {
    id: "F02-existing-lf-before-block",
    acceptanceCriteria: ["F02-AC-010", "F02-AC-012"],
    html: "<div id='target'><p>一\n</p><p>二</p></div>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "一\n二" },
  },
  {
    id: "F02-whitespace-preserved",
    acceptanceCriteria: ["F02-AC-012"],
    html: "<span id='target'> 前\t中  後 </span>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: " 前\t中  後 " },
  },
  {
    id: "F02-forward-direction",
    acceptanceCriteria: ["F02-AC-013"],
    html: "<span id='target'>前<span>日本</span>後</span>",
    range: { kind: "contents", selector: "#target", direction: "forward" },
    expected: { status: "ok", text: "前日本後" },
  },
  {
    id: "F02-backward-direction",
    acceptanceCriteria: ["F02-AC-013"],
    html: "<span id='target'>前<span>日本</span>後</span>",
    range: { kind: "contents", selector: "#target", direction: "backward" },
    expected: { status: "ok", text: "前日本後" },
  },
  {
    id: "F02-contenteditable-ruby",
    acceptanceCriteria: ["F02-AC-014"],
    html:
      "<div id='target' contenteditable='true'>編集<ruby>日本<rt>にほん</rt></ruby><br>後</div>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "編集日本\n後" },
  },
  {
    id: "F02-contenteditable-unicode",
    acceptanceCriteria: ["F02-AC-012", "F02-AC-014"],
    html:
      "<div id='target' contenteditable='true'>か\u3099𠀀󠄀�\ud800</div>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "か\u3099𠀀󠄀�\ud800" },
  },
  {
    id: "F02-long-complete-selection",
    acceptanceCriteria: ["F02-AC-026"],
    html: `<div id='target'><span><span>${"あ".repeat(99_999)}終</span></span></div>`,
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: `${"あ".repeat(99_999)}終` },
  },
  {
    id: "F02-css-generated-and-neighbor-excluded",
    acceptanceCriteria: ["F02-AC-027"],
    html:
      "<style>#target::before{content:'生成'}</style><span>隣接</span><span id='target'>本文</span><span>近隣</span>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "本文" },
  },
  {
    id: "F02-foreign-namespace-rt-preserved",
    acceptanceCriteria: ["F02-AC-003"],
    html:
      "<svg id='target' xmlns='http://www.w3.org/2000/svg'><rt>SVGRT</rt><text>本文</text></svg>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "SVGRT本文" },
  },
  {
    id: "F02-null-code-unit-preserved",
    acceptanceCriteria: ["F02-AC-012"],
    html: "<span id='target'></span>",
    textOverrides: [{ selector: "#target", value: "前\u0000後" }],
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "前\u0000後" },
  },
];
