import { extractRubySafeSelection } from "../../src/browser/selection/extractRubySafeSelection";
import { classifyJapaneseText } from "../../src/boundary/classifyJapaneseText";
import type { JapaneseTextDetectionResult } from "../../src/boundary/japaneseTextDetectionResult";
import type { RubySafeSelectionResult } from "../../src/boundary/rubySafeSelectionResult";
import type {
  RubySafeSelectionCorpusCase,
  SelectionBoundaryDescriptor,
  SelectionRangeDescriptor,
} from "../corpus/ruby-safe-selection-corpus";

function fixtureRoot(): HTMLElement {
  const root = document.querySelector<HTMLElement>("[data-f02-fixture]");
  if (root === null) {
    throw new Error("F-02 fixture root is missing");
  }
  return root;
}

function resolveBoundary(descriptor: SelectionBoundaryDescriptor): Node {
  let node: Node | null = fixtureRoot().querySelector(descriptor.selector);
  if (node === null) {
    throw new Error("F-02 fixture boundary is missing");
  }

  for (const childIndex of descriptor.childNodePath ?? []) {
    node = node.childNodes.item(childIndex);
    if (node === null) {
      throw new Error("F-02 fixture child boundary is missing");
    }
  }

  return node;
}

function createRange(descriptor: SelectionRangeDescriptor): Range {
  const range = document.createRange();

  if (descriptor.kind === "contents") {
    const target = fixtureRoot().querySelector(descriptor.selector);
    if (target === null) {
      throw new Error("F-02 fixture contents target is missing");
    }
    range.selectNodeContents(target);
    return range;
  }

  range.setStart(resolveBoundary(descriptor.start), descriptor.start.offset);
  range.setEnd(resolveBoundary(descriptor.end), descriptor.end.offset);
  return range;
}

function applyRange(range: Range, direction: "forward" | "backward"): void {
  const selection = window.getSelection();
  if (selection === null) {
    throw new Error("F-02 Selection API is unavailable");
  }

  selection.removeAllRanges();
  if (direction === "backward" && "setBaseAndExtent" in selection) {
    selection.setBaseAndExtent(
      range.endContainer,
      range.endOffset,
      range.startContainer,
      range.startOffset,
    );
    return;
  }

  selection.addRange(range);
}

function setFixture(corpusCase: RubySafeSelectionCorpusCase): Range {
  const root = fixtureRoot();
  root.innerHTML = corpusCase.html;
  for (const override of corpusCase.textOverrides ?? []) {
    const target = root.querySelector(override.selector);
    if (target === null) {
      throw new Error("F-02 fixture text override target is missing");
    }
    target.textContent = override.value;
  }
  const range = createRange(corpusCase.range);
  applyRange(range, corpusCase.range.direction ?? "forward");
  return range;
}

type ErrorScenario =
  | "none"
  | "collapsed"
  | "multi-range"
  | "annotation-only"
  | "stale"
  | "inaccessible"
  | "unexpected";

function createMarkerFixture(marker: string): Range {
  const corpusCase: RubySafeSelectionCorpusCase = {
    id: "F02-error-marker",
    acceptanceCriteria: ["F02-AC-025"],
    html: `<span id="target">${marker}</span>`,
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: marker },
  };
  return setFixture(corpusCase);
}

function currentSelection(): Selection {
  const selection = window.getSelection();
  if (selection === null) {
    throw new Error("F-02 Selection API is unavailable");
  }
  return selection;
}

function runErrorCase(scenario: ErrorScenario, marker: string): RubySafeSelectionResult {
  const selection = currentSelection();

  switch (scenario) {
    case "none":
      selection.removeAllRanges();
      return extractRubySafeSelection(() => selection);

    case "collapsed": {
      const range = createMarkerFixture(marker);
      range.collapse(true);
      applyRange(range, "forward");
      return extractRubySafeSelection(() => selection);
    }

    case "multi-range": {
      const unsupportedSelection = {
        rangeCount: 2,
        // Range count is authoritative even if a browser reports a collapsed
        // anchor/focus pair for a synthetic or multi-range selection.
        isCollapsed: true,
        getRangeAt: () => {
          throw new Error(marker);
        },
      } as unknown as Selection;
      return extractRubySafeSelection(() => unsupportedSelection);
    }

    case "annotation-only": {
      const annotationCase: RubySafeSelectionCorpusCase = {
        id: "F02-error-annotation-only",
        acceptanceCriteria: ["F02-AC-007"],
        html: `<ruby>基<rt id="target">${marker}</rt></ruby>`,
        range: { kind: "contents", selector: "#target" },
        expected: { status: "error", code: "no-base-text" },
      };
      setFixture(annotationCase);
      return extractRubySafeSelection(() => selection);
    }

    case "stale": {
      const range = createMarkerFixture(marker);
      const intersectsNode = range.intersectsNode.bind(range);
      let invalidated = false;
      range.intersectsNode = (node) => {
        if (!invalidated) {
          invalidated = true;
          selection.removeAllRanges();
        }
        return intersectsNode(node);
      };
      return extractRubySafeSelection(() => selection);
    }

    case "inaccessible":
      return extractRubySafeSelection(() => {
        throw new DOMException(marker, "SecurityError");
      });

    case "unexpected": {
      const range = createMarkerFixture(marker);
      range.intersectsNode = () => {
        throw new Error(marker);
      };
      return extractRubySafeSelection(() => selection);
    }
  }
}

function selectFrameText(frame: HTMLIFrameElement, text: string): Selection {
  const frameDocument = frame.contentDocument;
  const frameWindow = frame.contentWindow;
  if (frameDocument === null || frameWindow === null) {
    throw new Error("F-02 same-origin frame is unavailable");
  }

  frameDocument.open();
  frameDocument.write(`<p id="target">${text}</p>`);
  frameDocument.close();
  const target = frameDocument.querySelector("#target");
  const selection = frameWindow.getSelection();
  if (target === null || selection === null) {
    throw new Error("F-02 frame target is unavailable");
  }

  const range = frameDocument.createRange();
  range.selectNodeContents(target);
  selection.removeAllRanges();
  selection.addRange(range);
  return selection;
}

function runFrameCase(): RubySafeSelectionResult {
  const root = fixtureRoot();
  root.innerHTML =
    "<p id='parent-marker'>PARENT_PRIVATE_MARKER</p><iframe id='event-frame'></iframe><iframe id='sibling-frame'></iframe>";

  const eventFrame = root.querySelector<HTMLIFrameElement>("#event-frame");
  const siblingFrame = root.querySelector<HTMLIFrameElement>("#sibling-frame");
  const parentMarker = root.querySelector("#parent-marker");
  if (eventFrame === null || siblingFrame === null || parentMarker === null) {
    throw new Error("F-02 frame fixture is missing");
  }

  const parentRange = document.createRange();
  parentRange.selectNodeContents(parentMarker);
  applyRange(parentRange, "forward");
  const eventSelection = selectFrameText(eventFrame, "対象日本");
  selectFrameText(siblingFrame, "SIBLING_PRIVATE_MARKER");

  return extractRubySafeSelection(() => eventSelection);
}

declare global {
  interface Window {
    f02Extract(): RubySafeSelectionResult;
    f02ExtractThenClassify(): {
      extraction: RubySafeSelectionResult;
      classification?: JapaneseTextDetectionResult;
    };
    f02RunCase(corpusCase: RubySafeSelectionCorpusCase): RubySafeSelectionResult;
    f02RunErrorCase(
      scenario: string,
      marker: string,
    ): RubySafeSelectionResult;
    f02RunFrameCase(): RubySafeSelectionResult;
    f02SetCase(corpusCase: RubySafeSelectionCorpusCase): void;
  }
}

window.f02Extract = () => extractRubySafeSelection();
window.f02ExtractThenClassify = () => {
  const extraction = extractRubySafeSelection();
  return extraction.status === "ok"
    ? { extraction, classification: classifyJapaneseText(extraction.text) }
    : { extraction };
};
window.f02SetCase = (corpusCase) => {
  setFixture(corpusCase);
};
window.f02RunCase = (corpusCase) => {
  setFixture(corpusCase);
  return extractRubySafeSelection();
};
window.f02RunErrorCase = (scenario, marker) =>
  runErrorCase(scenario as ErrorScenario, marker);
window.f02RunFrameCase = runFrameCase;
