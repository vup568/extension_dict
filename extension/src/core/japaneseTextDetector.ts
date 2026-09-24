import { JAPANESE_TRIGGER_RANGES } from "./unicode17TriggerRanges.generated.ts";

function isJapaneseTriggerCodePoint(codePoint: number): boolean {
  let low = 0;
  let high = JAPANESE_TRIGGER_RANGES.length - 1;

  while (low <= high) {
    const middle = low + Math.floor((high - low) / 2);
    const range = JAPANESE_TRIGGER_RANGES[middle];
    if (range === undefined) {
      return false;
    }

    const [start, end] = range;
    if (codePoint < start) {
      high = middle - 1;
    } else if (codePoint > end) {
      low = middle + 1;
    } else {
      return true;
    }
  }

  return false;
}

/**
 * EARS[Event]: WHEN a caller supplies a string containing at least one
 * Unicode 17.0.0 F-01 trigger character, the detector SHALL return true.
 *
 * The input is inspected as received. This function does not normalize,
 * decode, repair, truncate, log, store, transmit, or mutate the string.
 */
export function containsJapaneseText(text: string): boolean {
  for (let index = 0; index < text.length; ) {
    const codePoint = text.codePointAt(index);
    if (codePoint === undefined) {
      return false;
    }

    if (isJapaneseTriggerCodePoint(codePoint)) {
      return true;
    }

    index += codePoint > 0xffff ? 2 : 1;
  }

  return false;
}
