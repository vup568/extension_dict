export type CodePointRange = readonly [start: number, end: number];

function parseHexRange(value: string): CodePointRange {
  const [startText, endText = startText] = value.trim().split("..");
  const start = Number.parseInt(startText ?? "", 16);
  const end = Number.parseInt(endText ?? "", 16);

  if (!Number.isInteger(start) || !Number.isInteger(end) || start > end) {
    throw new Error(`Invalid Unicode range: ${value}`);
  }

  return [start, end];
}

export function parsePropertyRanges(
  content: string,
  acceptedProperties: ReadonlySet<string>,
): CodePointRange[] {
  const ranges: CodePointRange[] = [];

  for (const rawLine of content.split(/\r?\n/u)) {
    const line = rawLine.replace(/#.*/u, "").trim();
    if (line.length === 0) {
      continue;
    }

    const separator = line.indexOf(";");
    if (separator < 0) {
      continue;
    }

    const property = line.slice(separator + 1).trim();
    if (acceptedProperties.has(property)) {
      ranges.push(parseHexRange(line.slice(0, separator)));
    }
  }

  return ranges;
}

export function parseLetterOtherRanges(content: string): CodePointRange[] {
  const ranges: CodePointRange[] = [];
  let pendingRangeStart: number | null = null;

  for (const line of content.split(/\r?\n/u)) {
    if (line.length === 0) {
      continue;
    }

    const fields = line.split(";");
    const codePoint = Number.parseInt(fields[0] ?? "", 16);
    const name = fields[1] ?? "";
    const category = fields[2] ?? "";

    if (category !== "Lo") {
      continue;
    }

    if (name.endsWith(", First>")) {
      pendingRangeStart = codePoint;
      continue;
    }

    if (name.endsWith(", Last>")) {
      if (pendingRangeStart === null) {
        throw new Error(`UnicodeData range ended without a start at ${fields[0]}`);
      }
      ranges.push([pendingRangeStart, codePoint]);
      pendingRangeStart = null;
      continue;
    }

    ranges.push([codePoint, codePoint]);
  }

  if (pendingRangeStart !== null) {
    throw new Error("UnicodeData ended with an open First/Last range");
  }

  return ranges;
}

export function intersectRanges(
  left: readonly CodePointRange[],
  right: readonly CodePointRange[],
): CodePointRange[] {
  const leftSorted = [...left].sort((a, b) => a[0] - b[0]);
  const rightSorted = [...right].sort((a, b) => a[0] - b[0]);
  const result: CodePointRange[] = [];
  let leftIndex = 0;
  let rightIndex = 0;

  while (leftIndex < leftSorted.length && rightIndex < rightSorted.length) {
    const leftRange = leftSorted[leftIndex];
    const rightRange = rightSorted[rightIndex];
    if (leftRange === undefined || rightRange === undefined) {
      break;
    }

    const start = Math.max(leftRange[0], rightRange[0]);
    const end = Math.min(leftRange[1], rightRange[1]);
    if (start <= end) {
      result.push([start, end]);
    }

    if (leftRange[1] < rightRange[1]) {
      leftIndex += 1;
    } else {
      rightIndex += 1;
    }
  }

  return result;
}

export function mergeRanges(
  ranges: readonly CodePointRange[],
): CodePointRange[] {
  const sorted = [...ranges].sort((a, b) => a[0] - b[0] || a[1] - b[1]);
  const merged: Array<[number, number]> = [];

  for (const [start, end] of sorted) {
    const previous = merged.at(-1);
    if (previous === undefined || start > previous[1] + 1) {
      merged.push([start, end]);
    } else {
      previous[1] = Math.max(previous[1], end);
    }
  }

  return merged;
}
