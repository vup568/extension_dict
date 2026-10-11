const HTML_NAMESPACE = "http://www.w3.org/1999/xhtml";

const SEMANTIC_BLOCK_LOCAL_NAMES = new Set([
  "address",
  "article",
  "aside",
  "blockquote",
  "dd",
  "div",
  "dl",
  "dt",
  "fieldset",
  "figcaption",
  "figure",
  "footer",
  "form",
  "h1",
  "h2",
  "h3",
  "h4",
  "h5",
  "h6",
  "header",
  "hgroup",
  "hr",
  "li",
  "main",
  "nav",
  "ol",
  "p",
  "pre",
  "search",
  "section",
  "table",
  "tbody",
  "td",
  "tfoot",
  "th",
  "thead",
  "tr",
  "ul",
]);

/** The reviewed F-02 block policy is fixed and independent of host CSS. */
export function isSemanticBlockElement(element: Element): boolean {
  return (
    element.namespaceURI === HTML_NAMESPACE &&
    SEMANTIC_BLOCK_LOCAL_NAMES.has(element.localName)
  );
}
