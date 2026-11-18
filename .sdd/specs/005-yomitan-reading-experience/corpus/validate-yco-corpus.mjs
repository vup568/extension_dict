import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import { isAbsolute, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";

const repository = fileURLToPath(new URL("../../../../", import.meta.url));
const version = "yco-20261110.1";
const packet = fileURLToPath(new URL(`./${version}/`, import.meta.url));
const baselinePath = ".sdd/specs/feat-conjugation-explanation/evidence/acceptance-manifest.json";
const auxiliaryConstructions = ["te-shimau-nonpast", "te-shimau-past"];
const utf8 = new TextDecoder("utf-8", { fatal: true, ignoreBOM: true });

function repositoryPath(path) {
  assert(typeof path === "string" && !isAbsolute(path), "Binding path must be repository-relative");
  const resolved = resolve(repository, path);
  const local = relative(repository, resolved);
  assert(local && local !== ".." && !local.startsWith(`..${sep}`) && !isAbsolute(local), `Binding outside repository: ${path}`);
  return resolved;
}

function verifyHash(bytes, expected, label) {
  assert(/^[a-f0-9]{64}$/.test(expected), `Invalid SHA-256 for ${label}`);
  assert.equal(createHash("sha256").update(bytes).digest("hex"), expected, `SHA-256 mismatch for ${label}`);
}

function readCanonical(path) {
  // Hash canonical LF bytes so normal Windows checkout conversion does not change the packet identity.
  return utf8.decode(readFileSync(path)).replace(/\r\n?/g, "\n");
}

function readPacketFile(name, bindings) {
  const text = readCanonical(resolve(packet, name));
  verifyHash(text, bindings[name], name);
  return JSON.parse(text);
}

const nonblank = value => typeof value === "string" && value.trim().length > 0;
const boundary = (text, index) => Number.isInteger(index) && index >= 0 && index <= text.length &&
  !(text.charCodeAt(index - 1) >= 0xd800 && text.charCodeAt(index - 1) <= 0xdbff &&
    text.charCodeAt(index) >= 0xdc00 && text.charCodeAt(index) <= 0xdfff);

function uniqueStrings(values, label) {
  assert(Array.isArray(values) && values.every(nonblank), `Invalid ${label}`);
  const unique = new Set(values);
  assert.equal(unique.size, values.length, `Duplicate ${label}`);
  return unique;
}

function sliceSpan(text, span, label) {
  assert(Array.isArray(span) && span.length === 2 && boundary(text, span[0]) && boundary(text, span[1]) &&
    span[0] < span[1], `Invalid UTF-16 span in ${label}`);
  return text.slice(...span);
}

function verifyBaseline(baseline) {
  assert.equal(baseline.path, baselinePath, "Expected the accepted F-09 manifest");
  const bytes = readFileSync(repositoryPath(baseline.path));
  verifyHash(bytes, baseline.sha256, baseline.path);
  const accepted = JSON.parse(utf8.decode(bytes));
  assert.equal(accepted.state, "accepted_for_scoped_corpus");
  assert.equal(accepted.corpus_version, baseline.version);
  assert.equal(accepted.candidate_version, baseline.dataset_version);
  const bindings = Object.values(accepted.bindings);
  assert.equal(bindings.length, 12, "Expected all 12 accepted F-09 bindings");
  uniqueStrings(bindings.map(binding => binding.file), "F-09 binding paths");
  for (const binding of bindings) {
    verifyHash(readFileSync(repositoryPath(binding.file)), binding.sha256, binding.file);
  }
  const oldCases = JSON.parse(utf8.decode(readFileSync(repositoryPath(accepted.bindings.cases.file))));
  const oldIds = uniqueStrings(oldCases.cases.map(item => item.case_id), "F-09 case IDs");
  const references = uniqueStrings(baseline.case_ids, "F-09 case references");
  assert(references.size > 0, "Missing F-09 case references");
  for (const id of references) assert(oldIds.has(id), `Unknown F-09 case ${id}`);
  return bindings.length;
}

// Read the generated F-01 Unicode policy used by the extension without loading TypeScript at runtime.
const triggerRanges = Array.from(readFileSync(repositoryPath("extension/src/core/unicode17TriggerRanges.generated.ts"), "utf8")
  .matchAll(/\[(0x[0-9a-f]+),\s*(0x[0-9a-f]+)\]/gi), ([, start, end]) => [Number(start), Number(end)]);
assert(triggerRanges.length > 0, "Missing generated Japanese trigger ranges");

function validateScans(scans, text, occurrences, label) {
  const offsets = new Set();
  for (const scan of scans) {
    const { offset, status, target_span: target, error_code: errorCode } = scan;
    assert(Number.isInteger(offset) && !offsets.has(offset), `Invalid or duplicate scan offset in ${label}`);
    offsets.add(offset);
    const codePoint = text.codePointAt(offset);
    const validPoint = text.length <= 64 && boundary(text, offset) &&
      triggerRanges.some(([start, end]) => codePoint >= start && codePoint <= end);
    if (status === 400) {
      assert(!validPoint && errorCode === "VALIDATION_FAILED" && target === undefined,
        `Invalid scan rejection in ${label}`);
      continue;
    }

    assert(status === 200 && validPoint && errorCode === undefined, `Invalid scan success in ${label}`);
    const match = occurrences.find(item => item.span[0] <= offset && offset < item.span[1]);
    assert.deepEqual(target, match?.span ?? null, `Scan target must equal the matching occurrence span in ${label}`);
  }
}

function validateSegments(segments, surface, label) {
  assert(Array.isArray(segments) && segments.length > 0, `Missing segments in ${label}`);
  let previousEnd = 0;
  for (const segment of segments) {
    const slice = sliceSpan(surface, segment.span, label);
    assert(segment.span[0] === previousEnd && segment.surface === slice && nonblank(segment.role),
      `Invalid segment sequence in ${label}`);
    previousEnd = segment.span[1];
  }
  assert.equal(previousEnd, surface.length, `Incomplete segment coverage in ${label}`);
}

function validateCase(item, fixtureIds, sourceIds, coverage) {
  const { case_id: label, stage, input, expected } = item;
  assert(["regression", "candidate", "linguistic_only"].includes(stage), `Unknown stage in ${label}`);
  const sources = uniqueStrings(item.source_refs, `source references in ${label}`);
  assert(sources.size > 0 && [...sources].every(id => sourceIds.has(id)), `Missing or unknown source in ${label}`);
  assert(["word", "sentence"].includes(input?.kind) && nonblank(input?.text) && input.text === input.text.trim() &&
    input.text.isWellFormed() && input.text.length <= 1000, `Invalid input in ${label}`);
  assert(["base_only", "reference_only", "sentence", "exact_surface", "restricted_entry", "baseline_sentence"]
    .includes(input.fixture_policy), `Unknown fixture policy in ${label}`);
  const setupFixtures = uniqueStrings(input.fixture_ids, `fixture setup in ${label}`);
  assert([...setupFixtures].every(id => fixtureIds.has(id)), `Unknown fixture in setup for ${label}`);
  const { kind, text } = input;
  assert(Array.isArray(expected?.occurrences), `Missing occurrences in ${label}`);
  let previousEnd = 0;
  for (const occurrence of expected.occurrences) {
    const surface = sliceSpan(text, occurrence.span, label);
    assert(surface === occurrence.surface && occurrence.span[0] >= previousEnd && !/[\p{P}\s]/u.test(surface),
      `Invalid occurrence surface or punctuation boundary in ${label}`);
    previousEnd = occurrence.span[1];
    assert(Array.isArray(occurrence.matches), `Missing matches in ${label}`);
    assert(occurrence.matches.length === 0 ? occurrence.match_policy === "any" : occurrence.match_policy === undefined,
      `Invalid match policy in ${label}`);
    uniqueStrings(occurrence.matches.map(match => JSON.stringify([match.fixture_id, match.construction])), `matches in ${label}`);
    for (const match of occurrence.matches) {
      assert(setupFixtures.has(match.fixture_id) && nonblank(match.construction), `Unknown or unconfigured fixture or construction in ${label}`);
      assert(match.surface_reading === undefined || nonblank(match.surface_reading), `Invalid surface reading in ${label}`);
      assert(match.interpretation_label === undefined || nonblank(match.interpretation_label), `Invalid interpretation label in ${label}`);
      if (coverage.has(match.construction)) {
        assert.equal(match.interpretation_label, "Cách hiểu có thể", `Missing interpretation label in ${label}`);
        assert(match.segments !== undefined, `Missing auxiliary segments in ${label}`);
      }
      if (match.segments !== undefined) validateSegments(match.segments, surface, label);
      if (stage === "candidate" && kind === "word" && coverage.has(match.construction)) {
        assert.equal(surface, text, `Candidate word occurrence must cover the input in ${label}`);
        coverage.get(match.construction).positives.add(text);
      }
    }
  }

  const forbidden = expected.forbidden ?? [];
  assert(Array.isArray(forbidden), `Invalid forbidden constructions in ${label}`);
  for (const entry of forbidden) {
    assert(nonblank(entry.construction) && (entry.fixture_id === undefined || setupFixtures.has(entry.fixture_id)),
      `Invalid forbidden construction or fixture in ${label}`);
    if (entry.span !== undefined) sliceSpan(text, entry.span, label);
    const contradiction = expected.occurrences.some(occurrence =>
      (entry.span === undefined || (occurrence.span[0] === entry.span[0] && occurrence.span[1] === entry.span[1])) &&
      occurrence.matches.some(match => match.construction === entry.construction &&
        (entry.fixture_id === undefined || match.fixture_id === entry.fixture_id)));
    assert(!contradiction, `Expected match is also forbidden in ${label}`);
    if (kind === "word" && coverage.has(entry.construction)) coverage.get(entry.construction).negatives.add(text);
  }

  if (expected.scan !== undefined) {
    assert(Array.isArray(expected.scan) && expected.scan.length > 0, `Missing scan expectations in ${label}`);
    validateScans(expected.scan, text, expected.occurrences, label);
  }
}

function validatePacket(corpus, sources, manifest, catalogGaps) {
  for (const document of [corpus, sources, manifest]) {
    assert(document.schema_version === 1 && document.corpus_version === version, "Unexpected YCO packet version");
  }
  const fixtureIds = uniqueStrings(corpus.dictionary_fixtures.map(item => item.fixture_id), "dictionary fixture IDs");
  uniqueStrings(corpus.dictionary_fixtures.map(item =>
    JSON.stringify([item.canonical_id, item.form, item.reading, item.pos?.slice().sort(), item.sense_ordinal])), "dictionary fixture identities");
  for (const fixture of corpus.dictionary_fixtures) {
    assert(/^dictionary:(?:jmdict:[0-9]+|synthetic-[a-z0-9-]+)$/.test(fixture.canonical_id) &&
      nonblank(fixture.form) && nonblank(fixture.reading) && uniqueStrings(fixture.pos, `POS for ${fixture.fixture_id}`).size > 0 &&
      (fixture.sense_ordinal === undefined || (Number.isSafeInteger(fixture.sense_ordinal) && fixture.sense_ordinal > 0)),
      `Invalid dictionary fixture ${fixture.fixture_id}`);
  }

  const sourceIds = uniqueStrings(sources.sources.map(item => item.source_id), "source IDs");
  uniqueStrings(corpus.cases.map(item => item.case_id), "case IDs");
  const requiredGaps = uniqueStrings(manifest.required_gap_ids, "required gap IDs");
  assert.deepEqual(requiredGaps, catalogGaps, "Required gap IDs must match the frozen catalog");
  const coveredGaps = new Set();
  const coverage = new Map(auxiliaryConstructions.map(construction => [construction, { positives: new Set(), negatives: new Set() }]));
  for (const item of corpus.cases) {
    assert(requiredGaps.has(item.gap_id), `Unknown gap ${item.gap_id}`);
    coveredGaps.add(item.gap_id);
    validateCase(item, fixtureIds, sourceIds, coverage);
  }
  assert.deepEqual(coveredGaps, requiredGaps, "Required gaps must all have cases");
  for (const [construction, { positives, negatives }] of coverage) {
    assert(positives.size >= 2 && negatives.size > 0, `Insufficient positive or negative word coverage for ${construction}`);
  }
  return coveredGaps.size;
}

function selfTest(corpus, sources, manifest, catalogGaps) {
  const mutations = [
    ["duplicate case", draft => draft.cases.push(draft.cases[0]), /Duplicate case IDs/],
    ["unknown source", draft => { draft.cases[0].source_refs = ["unbound-source"]; }, /Missing or unknown source/],
    ["fixture absent from setup", draft => {
      const item = draft.cases.find(entry => entry.expected.occurrences.some(occurrence => occurrence.matches.length));
      item.input.fixture_ids = [];
    }, /Unknown or unconfigured fixture/],
    ["split surrogate", draft => {
      const item = draft.cases.find(entry => entry.input.text.startsWith("🐈"));
      item.expected.occurrences[0].span[0] = 1;
    }, /Invalid UTF-16 span/],
    ["segment mismatch", draft => {
      const matches = draft.cases.flatMap(item => item.expected.occurrences.flatMap(occurrence => occurrence.matches));
      matches.find(match => match.segments !== undefined).segments[0].surface = "違";
    }, /Invalid segment sequence/],
    ["implicit empty matches", draft => {
      const occurrences = draft.cases.flatMap(item => item.expected.occurrences);
      delete occurrences.find(item => item.match_policy === "any").match_policy;
    }, /Invalid match policy/],
    ["wrong scan target", draft => {
      const item = draft.cases.find(entry => entry.expected.scan?.some(scan => scan.status === 200));
      item.expected.scan.find(scan => scan.status === 200).target_span = [0, 1];
    }, /Scan target must equal/],
    ["missing positive word coverage", draft => {
      for (const item of draft.cases) if (item.stage === "candidate") item.stage = "regression";
    }, /Insufficient positive or negative word coverage/],
  ];
  for (const [label, mutate, error] of mutations) {
    const draft = structuredClone(corpus);
    mutate(draft);
    assert.throws(() => validatePacket(draft, sources, manifest, catalogGaps), error, `Mutation accepted: ${label}`);
  }
  assert.throws(() => verifyHash("changed bytes", manifest.bindings["cases.json"], "cases.json"), /SHA-256 mismatch/);
  return mutations.length + 1;
}

const manifest = JSON.parse(utf8.decode(readFileSync(resolve(packet, "manifest.json"))));
const catalog = readCanonical(repositoryPath(manifest.catalog.path));
verifyHash(catalog, manifest.catalog.sha256, manifest.catalog.path);
assert.equal(manifest.catalog.version, "yco-gap-20261110.1");
const catalogGaps = uniqueStrings(Array.from(catalog.matchAll(/^\| `(YCO-[A-Z]+-\d+)`/gm), ([, id]) => id), "frozen catalog gap IDs");
assert(catalogGaps.size > 0, "Missing frozen catalog gaps");
const baselineBindings = verifyBaseline(manifest.baseline);
const corpus = readPacketFile("cases.json", manifest.bindings);
const sources = readPacketFile("sources.json", manifest.bindings);
const gaps = validatePacket(corpus, sources, manifest, catalogGaps);
const selfTests = process.argv.includes("--self-test") ? selfTest(corpus, sources, manifest, catalogGaps) : 0;

console.log(JSON.stringify({ corpus_version: version, cases: corpus.cases.length, gaps,
  fixtures: corpus.dictionary_fixtures.length, sources: sources.sources.length,
  baseline_bindings_verified: baselineBindings, self_tests_passed: selfTests }));
