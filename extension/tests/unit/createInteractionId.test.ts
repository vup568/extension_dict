import { describe, expect, it } from "vitest";
import { createInteractionId } from "../../src/interaction/createInteractionId.ts";

describe("portable interaction UUID", () => {
  it("formats a backend-accepted RFC4122 version4/variant UUID", () => {
    expect(createInteractionId((bytes) => bytes.fill(255))).toBe("ffffffff-ffff-4fff-bfff-ffffffffffff");
  });
  it("preserves injected entropy, with zero-padding and mandated version/variant bits", () => {
    expect(createInteractionId((bytes) => bytes)).toBe("00000000-0000-4000-8000-000000000000");
    expect(createInteractionId((bytes) => { bytes[0] = 1; return bytes; })).toBe("01000000-0000-4000-8000-000000000000");
  });
});
