export type SelectedTextToken =
  | Readonly<{ kind: "text"; value: string }>
  | Readonly<{ kind: "explicit-break" }>
  | Readonly<{ kind: "block-boundary" }>;

/**
 * EARS[State]: WHILE ordered extraction tokens are assembled, the system SHALL
 * preserve text and explicit breaks exactly while coalescing semantic block
 * boundaries without artificial leading or trailing line breaks.
 */
export function assembleSelectedText(
  tokens: readonly SelectedTextToken[],
): string {
  let output = "";
  let blockBoundaryPending = false;

  for (const token of tokens) {
    switch (token.kind) {
      case "text": {
        if (token.value.length === 0) {
          break;
        }

        if (
          blockBoundaryPending &&
          output.length > 0 &&
          !output.endsWith("\n") &&
          !token.value.startsWith("\n")
        ) {
          output += "\n";
        }

        blockBoundaryPending = false;
        output += token.value;
        break;
      }

      case "explicit-break":
        blockBoundaryPending = false;
        output += "\n";
        break;

      case "block-boundary":
        if (output.length > 0) {
          blockBoundaryPending = true;
        }
        break;
    }
  }

  return output;
}
