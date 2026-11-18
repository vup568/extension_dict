import { isScanInput, type ScanInput } from "../../api/readingContracts.ts";
import { isSemanticBlockElement } from "./semanticBlockElements.ts";

export interface PointerWindow { input: ScanInput; anchor: Range }
const rubyAnnotation = "rt,rp";
const excluded = "input,textarea,select,script,style,noscript,template,canvas,iframe,svg,[inert],[hidden],[aria-hidden='true'],[data-jp-reading-popup]";
const maxTraversalSteps = 96;

function isBlocked(element: Element, view: Window): boolean {
  if (element.matches(excluded) || (element as HTMLElement).isContentEditable) return true;
  const style = view.getComputedStyle(element);
  return style.display === "none" || style.visibility !== "visible" || style.contentVisibility === "hidden";
}

function isBlock(element: Element, view: Window): boolean {
  return isSemanticBlockElement(element) || /^(?:block|flow-root|list-item|flex|grid|table)(?:\b|$)/u.test(view.getComputedStyle(element).display);
}

function lowSurrogate(code: number): boolean { return code >= 0xdc00 && code <= 0xdfff; }
function highSurrogate(code: number): boolean { return code >= 0xd800 && code <= 0xdbff; }

/** Use a glyph rectangle, since caret offsets may point after the glyph under the mouse. */
function characterAtPoint(doc: Document, node: Text, caret: number, x: number, y: number) {
  const sample = node.substringData(Math.max(0, caret - 2), 4);
  const sampleStart = Math.max(0, caret - 2);
  const char = (offset: number) => sample.charCodeAt(offset - sampleStart);
  for (let start of [caret, caret - 1]) {
    if (start < 0 || start >= node.length) continue;
    if (lowSurrogate(char(start)) && highSurrogate(char(start - 1))) start--;
    const length = highSurrogate(char(start)) && lowSurrogate(char(start + 1)) ? 2 : 1;
    const text = node.substringData(start, length);
    if (!isScanInput({ text, offset: 0 })) continue;
    const range = doc.createRange();
    range.setStart(node, start);
    range.setEnd(node, start + length);
    if ([...range.getClientRects()].some(rect => x >= rect.left && x <= rect.right && y >= rect.top && y <= rect.bottom)) {
      return { start, range };
    }
  }
  return null;
}

function readSide(point: Text, offset: number, root: Element, view: Window, before: boolean): string {
  const limit = before ? 16 : 48;
  let result = before ? point.substringData(Math.max(0, offset - limit), Math.min(offset, limit)) : point.substringData(offset, limit);
  let cursor: Node | null = point;
  let descend = false;
  for (let step = 0; step < maxTraversalSteps && cursor !== null && result.length < limit; step++) {
    if (descend) {
      const child: ChildNode | null = before ? cursor.lastChild : cursor.firstChild;
      if (child !== null) { cursor = child; descend = false; }
      else descend = false;
    } else {
      if (cursor === root) break;
      const sibling: ChildNode | null = before ? cursor.previousSibling : cursor.nextSibling;
      if (sibling === null) { cursor = cursor.parentNode; continue; }
      cursor = sibling;
    }
    if (cursor.nodeType === Node.ELEMENT_NODE) {
      const element = cursor as Element;
      if (element.matches(rubyAnnotation)) continue;
      if (element.localName === "br" || isBlock(element, view) || isBlocked(element, view)) break;
      descend = element.hasChildNodes();
    } else if (cursor.nodeType === Node.TEXT_NODE) {
      const text = cursor as Text;
      const remaining = limit - result.length;
      const part = text.substringData(before ? Math.max(0, text.length - remaining) : 0, remaining);
      result = before ? part + result : result + part;
    }
  }
  if (before && lowSurrogate(result.charCodeAt(0))) return result.slice(1);
  if (!before && highSurrogate(result.charCodeAt(result.length - 1))) return result.slice(0, -1);
  return result;
}

/** EARS[Event] YSC-03/04/05: Read a bounded, ruby-safe window only after opt-in and dwell. */
export function extractPointerWindow(doc: Document, x: number, y: number): PointerWindow | null {
  const view = doc.defaultView;
  const hit = doc.elementFromPoint(x, y);
  if (view === null || hit === null || hit.closest(rubyAnnotation) || hit.closest(excluded)) return null;
  const caret = doc.caretPositionFromPoint?.(x, y) ?? doc.caretRangeFromPoint?.(x, y);
  if (caret == null) return null;
  const { node, offset } = "offsetNode" in caret ? { node: caret.offsetNode, offset: caret.offset }
    : { node: caret.startContainer, offset: caret.startOffset };
  if (node.nodeType !== Node.TEXT_NODE || node.getRootNode() !== doc || node.parentElement === null || !hit.contains(node)) return null;
  let root: Element | null = null;
  for (let element: Element | null = node.parentElement; element !== null; element = element.parentElement) {
    if (element.matches(rubyAnnotation) || isBlocked(element, view)) return null;
    if (root === null && (isBlock(element, view) || element === doc.body)) root = element;
  }
  if (root === null) return null;
  const point = characterAtPoint(doc, node as Text, offset, x, y);
  if (point === null) return null;
  const before = readSide(node as Text, point.start, root, view, true).trimStart();
  const after = readSide(node as Text, point.start, root, view, false).trimEnd();
  const input = { text: before + after, offset: before.length };
  return isScanInput(input) ? { input, anchor: point.range } : null;
}
