import { describe, expect, it } from "vitest";

import {
  assembleSelectedText,
  type SelectedTextToken,
} from "../../src/core/selectedTextAccumulator";

const text = (value: string): SelectedTextToken => ({ kind: "text", value });
const explicitBreak = (): SelectedTextToken => ({ kind: "explicit-break" });
const blockBoundary = (): SelectedTextToken => ({ kind: "block-boundary" });

describe("assembleSelectedText", () => {
  it("preserves text payloads exactly without trimming or normalization", () => {
    expect(
      assembleSelectedText([
        text(" か\t"),
        text("き\n"),
        text("か\u3099\u{20000}\u{E0100}\ud800"),
      ]),
    ).toBe(" か\tき\nか\u3099\u{20000}\u{E0100}\ud800");
  });

  it("preserves every explicit break, including consecutive breaks", () => {
    expect(
      assembleSelectedText([
        text("一行"),
        explicitBreak(),
        explicitBreak(),
        text("三行"),
      ]),
    ).toBe("一行\n\n三行");
  });

  it("holds and coalesces block boundaries until later eligible text", () => {
    expect(
      assembleSelectedText([
        text("一"),
        blockBoundary(),
        blockBoundary(),
        text("二"),
      ]),
    ).toBe("一\n二");
  });

  it("suppresses leading and trailing block boundaries", () => {
    expect(
      assembleSelectedText([
        blockBoundary(),
        text("本文"),
        blockBoundary(),
      ]),
    ).toBe("本文");
  });

  it("does not duplicate an existing LF at a block boundary", () => {
    expect(
      assembleSelectedText([
        text("一\n"),
        blockBoundary(),
        text("二"),
        explicitBreak(),
        blockBoundary(),
        text("三"),
      ]),
    ).toBe("一\n二\n三");
  });

  it("ignores empty text without consuming a pending boundary", () => {
    expect(
      assembleSelectedText([
        text("前"),
        blockBoundary(),
        text(""),
        text("後"),
      ]),
    ).toBe("前\n後");
  });
});
