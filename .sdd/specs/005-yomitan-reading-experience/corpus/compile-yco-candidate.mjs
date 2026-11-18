import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";

const root = new URL("../../../../", import.meta.url);
const packet = new URL("./yco-20261110.1/", import.meta.url);
const rulesPath = new URL("src/infra/Conjugation/Data/candidate-20261110.1.rules.json", root);
const hash = value => createHash("sha256").update(value).digest("hex");
const readLf = path => readFileSync(path, "utf8").replace(/\r\n/g, "\n");

const rulesText = readLf(rulesPath);
const rules = JSON.parse(rulesText);
const manifestText = readLf(new URL("manifest.json", packet));
const manifest = JSON.parse(manifestText);
assert.equal(hash(manifestText), rules.corpus_manifest_sha256, "Reviewed YCO manifest changed");
for (const name of ["cases.json", "sources.json"]) {
  assert.equal(hash(readLf(new URL(name, packet))), manifest.bindings[name], `Reviewed ${name} changed`);
}

const acceptedBytes = readFileSync(new URL(manifest.baseline.path, root));
assert.equal(hash(acceptedBytes), manifest.baseline.sha256, "Accepted F-09 manifest changed");
const accepted = JSON.parse(acceptedBytes);
const binding = accepted.bindings.candidate;
const baselineBytes = readFileSync(new URL(binding.file, root));
assert.equal(hash(baselineBytes), binding.sha256, "Accepted F-09 dataset changed");
const baseline = JSON.parse(baselineBytes);
assert.equal(baseline.version, rules.extends, "Unexpected base dataset version");
assert.equal(baseline.schema_version, rules.schema_version, "Incompatible dataset schema");
assert.notEqual(baseline.version, rules.version, "YCO must use a separate dataset version");

const existingIds = new Set(baseline.paradigms.map(pattern => pattern.rule_id));
const additions = new Set();
for (const pattern of rules.paradigms) {
  assert(!existingIds.has(pattern.rule_id), `YCO rule would overlap an existing ID: ${pattern.rule_id}`);
  const identity = JSON.stringify([pattern.rule_id, pattern.lexical_class]);
  assert(!additions.has(identity), `Duplicate YCO realization: ${identity}`);
  additions.add(identity);
}

const sourceCatalog = JSON.parse(readLf(new URL("sources.json", packet)));
const sources = rules.source_refs.map(id => {
  const source = sourceCatalog.sources.find(item => item.source_id === id);
  assert(source?.kind === "grammar_reference", `Unknown YCO grammar reference: ${id}`);
  return source.reference;
});

// Compile a complete immutable snapshot for the existing repository and parser.
const candidate = {
  ...baseline,
  version: rules.version,
  provenance: {
    ...baseline.provenance,
    sources: [...new Set([...baseline.provenance.sources, ...sources])],
    baseline_dataset_version: baseline.version,
    baseline_dataset_sha256: binding.sha256,
    yco_corpus_version: manifest.corpus_version,
    yco_corpus_manifest_sha256: rules.corpus_manifest_sha256,
    yco_rules_sha256: hash(rulesText),
  },
  paradigms: [...baseline.paradigms, ...rules.paradigms],
};
process.stdout.write(`${JSON.stringify(candidate)}\n`);
