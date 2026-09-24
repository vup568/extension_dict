import { expect, test } from "@playwright/test";

import {
  rubySafeSelectionCorpus,
  type RubySafeSelectionCorpusCase,
} from "../corpus/ruby-safe-selection-corpus";

const profileIds = [
  "F02-plain-base-text",
  "F02-multiple-ruby-document-order",
  "F02-adjacent-blocks",
  "F02-contenteditable-ruby",
  "F02-long-complete-selection",
] as const;

const measuredSamples = 30;

test("records cold and warm extraction profiles with exact correctness", async ({
  browser,
  page,
}, testInfo) => {
  await page.goto("/tests/browser/ruby-safe-selection-harness.html");

  for (const profileId of profileIds) {
    const fixture = rubySafeSelectionCorpus.find(({ id }) => id === profileId);
    if (fixture === undefined || fixture.expected.status !== "ok") {
      throw new Error(`missing successful performance profile: ${profileId}`);
    }

    const measurement = await page.evaluate(
      ({ corpusCase, sampleCount }: {
        corpusCase: RubySafeSelectionCorpusCase;
        sampleCount: number;
      }) => {
        window.f02SetCase(corpusCase);

        const coldStartedAt = performance.now();
        const coldResult = window.f02Extract();
        const coldDurationMs = performance.now() - coldStartedAt;

        const warmSamples: { durationMs: number; result: unknown }[] = [];
        for (let index = 0; index < sampleCount; index += 1) {
          const startedAt = performance.now();
          const result = window.f02Extract();
          warmSamples.push({
            durationMs: performance.now() - startedAt,
            result,
          });
        }

        return { coldDurationMs, coldResult, warmSamples };
      },
      { corpusCase: fixture, sampleCount: measuredSamples },
    );

    expect(measurement.coldResult, `${profileId} cold correctness`).toEqual(
      fixture.expected,
    );
    for (const sample of measurement.warmSamples) {
      expect(sample.result, `${profileId} warm correctness`).toEqual(
        fixture.expected,
      );
    }

    const sortedWarmDurations = measurement.warmSamples
      .map(({ durationMs }) => durationMs)
      .sort((left, right) => left - right);
    const p95Index = Math.ceil(sortedWarmDurations.length * 0.95) - 1;
    const warmP95Ms = sortedWarmDurations[p95Index];
    if (warmP95Ms === undefined) {
      throw new Error(`missing warm p95 for ${profileId}`);
    }

    const evidence = {
      browser: testInfo.project.name,
      browserVersion: browser.version(),
      profileId,
      inputLength: fixture.expected.text.length,
      warmupSamples: 1,
      measuredSamples,
      coldDurationMs: Number(measurement.coldDurationMs.toFixed(3)),
      warmP95Ms: Number(warmP95Ms.toFixed(3)),
    };
    testInfo.annotations.push({
      type: "f02-performance",
      description: JSON.stringify(evidence),
    });
    console.log(`F02_PERFORMANCE ${JSON.stringify(evidence)}`);
  }
});
