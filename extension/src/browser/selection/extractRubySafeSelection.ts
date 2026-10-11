import type { RubySafeSelectionResult } from "../../boundary/rubySafeSelectionResult.ts";
import {
  assembleSelectedText,
  type SelectedTextToken,
} from "../../core/selectedTextAccumulator.ts";
import { isSemanticBlockElement } from "./semanticBlockElements.ts";

const HTML_NAMESPACE = "http://www.w3.org/1999/xhtml";

export type SelectionReader = () => Selection | null;

interface RangeSnapshot {
  readonly startContainer: Node;
  readonly startOffset: number;
  readonly endContainer: Node;
  readonly endOffset: number;
  readonly commonAncestorContainer: Node;
  readonly document: Document;
}

function ownerDocumentOf(node: Node): Document | null {
  return node.nodeType === Node.DOCUMENT_NODE
    ? (node as Document)
    : node.ownerDocument;
}

function isConnectedToDocument(node: Node, document: Document): boolean {
  return ownerDocumentOf(node) === document && node.isConnected;
}

function createSnapshot(range: Range): RangeSnapshot | null {
  const startDocument = ownerDocumentOf(range.startContainer);
  const endDocument = ownerDocumentOf(range.endContainer);

  if (
    startDocument === null ||
    startDocument !== endDocument ||
    !isConnectedToDocument(range.startContainer, startDocument) ||
    !isConnectedToDocument(range.endContainer, startDocument)
  ) {
    return null;
  }

  return {
    startContainer: range.startContainer,
    startOffset: range.startOffset,
    endContainer: range.endContainer,
    endOffset: range.endOffset,
    commonAncestorContainer: range.commonAncestorContainer,
    document: startDocument,
  };
}

function snapshotRemainsCoherent(
  selection: Selection,
  range: Range,
  snapshot: RangeSnapshot,
): boolean {
  if (selection.rangeCount !== 1 || range.collapsed) {
    return false;
  }

  return (
    range.startContainer === snapshot.startContainer &&
    range.startOffset === snapshot.startOffset &&
    range.endContainer === snapshot.endContainer &&
    range.endOffset === snapshot.endOffset &&
    range.commonAncestorContainer === snapshot.commonAncestorContainer &&
    isConnectedToDocument(range.startContainer, snapshot.document) &&
    isConnectedToDocument(range.endContainer, snapshot.document)
  );
}

function isHtmlRubyAnnotation(element: Element): boolean {
  return (
    element.namespaceURI === HTML_NAMESPACE &&
    (element.localName === "rt" || element.localName === "rp")
  );
}

function hasRubyAnnotationAncestor(node: Node): boolean {
  for (let current = node.parentElement; current !== null; current = current.parentElement) {
    if (isHtmlRubyAnnotation(current)) {
      return true;
    }
  }

  return false;
}

function selectedTextFromNode(node: Text, range: Range): string {
  let start = 0;
  let end = node.data.length;

  if (node === range.startContainer) {
    start = range.startOffset;
  }
  if (node === range.endContainer) {
    end = range.endOffset;
  }

  return end > start ? node.data.slice(start, end) : "";
}

function safelyIntersects(range: Range, node: Node): boolean {
  return range.intersectsNode(node);
}

function collectSelectedTokens(range: Range): SelectedTextToken[] {
  const tokens: SelectedTextToken[] = [];
  const commonAncestor = range.commonAncestorContainer;
  const root =
    commonAncestor.nodeType === Node.TEXT_NODE
      ? commonAncestor.parentNode
      : commonAncestor;

  if (root === null) {
    return tokens;
  }

  const visit = (node: Node): void => {
    if (!safelyIntersects(range, node)) {
      return;
    }

    if (node.nodeType === Node.TEXT_NODE) {
      if (hasRubyAnnotationAncestor(node)) {
        return;
      }

      const value = selectedTextFromNode(node as Text, range);
      if (value.length > 0) {
        tokens.push({ kind: "text", value });
      }
      return;
    }

    if (node.nodeType !== Node.ELEMENT_NODE) {
      return;
    }

    const element = node as Element;
    if (isHtmlRubyAnnotation(element)) {
      return;
    }

    if (
      element.namespaceURI === HTML_NAMESPACE &&
      element.localName === "br"
    ) {
      tokens.push({ kind: "explicit-break" });
      return;
    }

    const tokenCountBeforeChildren = tokens.length;
    for (const child of element.childNodes) {
      visit(child);
    }

    if (
      tokens.length > tokenCountBeforeChildren &&
      isSemanticBlockElement(element)
    ) {
      tokens.push({ kind: "block-boundary" });
    }
  };

  visit(root);
  return tokens;
}

function currentFrameSelection(): Selection | null {
  return window.getSelection();
}

/**
 * EARS[Event]: WHEN exactly one coherent non-collapsed Range is supplied by
 * the current frame, the system SHALL return complete selected base text in
 * document order while excluding HTML rt/rp descendants.
 *
 * EARS[Unwanted]: IF selection acquisition or traversal cannot produce one
 * coherent complete result, the system SHALL return a content-free error and
 * SHALL NOT return partial text.
 */
export function extractRubySafeSelection(
  readSelection: SelectionReader = currentFrameSelection,
): RubySafeSelectionResult {
  let selection: Selection | null;
  try {
    selection = readSelection();
  } catch {
    return { status: "error", code: "inaccessible-context" };
  }

  if (selection === null || selection.rangeCount === 0) {
    return { status: "error", code: "no-selection" };
  }

  if (selection.rangeCount !== 1) {
    return { status: "error", code: "unsupported-multi-range" };
  }

  if (selection.isCollapsed) {
    return { status: "error", code: "no-selection" };
  }

  try {
    const range = selection.getRangeAt(0);
    if (range.collapsed) {
      return { status: "error", code: "no-selection" };
    }

    const snapshot = createSnapshot(range);
    if (snapshot === null) {
      return { status: "error", code: "stale-selection" };
    }

    const tokens = collectSelectedTokens(range);
    if (!snapshotRemainsCoherent(selection, range, snapshot)) {
      return { status: "error", code: "stale-selection" };
    }

    const text = assembleSelectedText(tokens);
    return text.length === 0
      ? { status: "error", code: "no-base-text" }
      : { status: "ok", text };
  } catch {
    return { status: "error", code: "unexpected-failure" };
  }
}
