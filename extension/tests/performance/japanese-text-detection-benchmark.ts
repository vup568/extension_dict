import { performance } from "node:perf_hooks";

import { containsJapaneseText } from "../../src/core/japaneseTextDetector.ts";

interface BenchmarkScenario {
  readonly name: string;
  readonly length: number;
  readonly samples: number;
  readonly input: string;
  readonly expected: boolean;
}

interface BenchmarkResult {
  readonly name: string;
  readonly inputLength: number;
  readonly samples: number;
  readonly expected: boolean;
  readonly coldMilliseconds: number;
  readonly warmP95Milliseconds: number;
}

function percentile(values: readonly number[], percentileValue: number): number {
  const sorted = [...values].sort((left, right) => left - right);
  const index = Math.min(
    sorted.length - 1,
    Math.ceil((percentileValue / 100) * sorted.length) - 1,
  );
  return sorted[index] ?? 0;
}

function runScenario(scenario: BenchmarkScenario): BenchmarkResult {
  const coldStart = performance.now();
  const coldResult = containsJapaneseText(scenario.input);
  const coldMilliseconds = performance.now() - coldStart;
  if (coldResult !== scenario.expected) {
    throw new Error(`Unexpected cold result for ${scenario.name}`);
  }

  for (let warmup = 0; warmup < 20; warmup += 1) {
    containsJapaneseText(scenario.input);
  }

  const samples: number[] = [];
  for (let sample = 0; sample < scenario.samples; sample += 1) {
    const start = performance.now();
    const result = containsJapaneseText(scenario.input);
    samples.push(performance.now() - start);
    if (result !== scenario.expected) {
      throw new Error(`Unexpected warm result for ${scenario.name}`);
    }
  }

  return {
    name: scenario.name,
    inputLength: scenario.input.length,
    samples: scenario.samples,
    expected: scenario.expected,
    coldMilliseconds,
    warmP95Milliseconds: percentile(samples, 95),
  };
}

const profiles = [
  { label: "small", length: 16, samples: 2_000 },
  { label: "medium", length: 1_000, samples: 1_000 },
  { label: "large", length: 100_000, samples: 200 },
] as const;

const scenarios: BenchmarkScenario[] = profiles.flatMap((profile) => [
  {
    name: `${profile.label}-positive-first`,
    length: profile.length,
    samples: profile.samples,
    input: `日${"a".repeat(profile.length - 1)}`,
    expected: true,
  },
  {
    name: `${profile.label}-positive-last`,
    length: profile.length,
    samples: profile.samples,
    input: `${"a".repeat(profile.length - 1)}日`,
    expected: true,
  },
  {
    name: `${profile.label}-negative`,
    length: profile.length,
    samples: profile.samples,
    input: "a".repeat(profile.length),
    expected: false,
  },
  {
    name: `${profile.label}-supplementary-last`,
    length: profile.length,
    samples: profile.samples,
    input: `${"a".repeat(Math.max(0, profile.length - 2))}𠮟`,
    expected: true,
  },
]);

const results = scenarios.map(runScenario);
process.stdout.write(
  `${JSON.stringify(
    {
      node: process.version,
      platform: process.platform,
      architecture: process.arch,
      unicodePolicy: "17.0.0",
      results,
    },
    null,
    2,
  )}\n`,
);
