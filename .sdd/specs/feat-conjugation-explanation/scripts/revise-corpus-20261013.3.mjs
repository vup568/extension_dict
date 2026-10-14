// Editorial test/review preparation only. Never imported by application runtime.
// This revision preserves frozen .2 answers and adds the user's explicit scope decisions.
import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const feature = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const prior = resolve(feature, 'corpus/expanded-20261013.2');
const readJson = path => JSON.parse(readFileSync(path, 'utf8'));
const sha = path => createHash('sha256').update(readFileSync(path)).digest('hex');
const assert = (condition, message) => { if (!condition) throw new Error(message); };
const oldMaterial = readJson(resolve(prior, 'scope-material.json'));
for (const file of oldMaterial.files) {
  assert(sha(resolve(prior, file.path)) === file.sha256, `Prior snapshot changed: ${file.path}`);
}
const corpus = readJson(resolve(prior, 'cases.json'));
const manifest = readJson(resolve(prior, 'manifest.json'));
const sources = readJson(resolve(prior, 'sources.json'));
const version = 'f09-expanded-20261013.3';
for (const item of [corpus, manifest, sources]) item.corpus_version = version;
manifest.supersedes = 'f09-expanded-20261013.2';
manifest.prior_scope_material_sha256 = sha(resolve(prior, 'scope-material.json'));
manifest.cases_sha256 = manifest.sources_sha256 = manifest.scope_material_sha256 = null;
manifest.scope_decisions_ref = '../../reviews/scope-decisions-20261013.json';
manifest.approval.note = 'User confirmed honorific scope and both causative interpretations; new exact component wording awaits human review.';
manifest.excluded = manifest.excluded.filter(x => x.id !== 'keigo_interpretations');
manifest.excluded.push(
  { id: 'spontaneous_interpretations', reason: 'Not included in this finite catalog; no claim to enumerate every possible use of reru/rareru.' },
  { id: 'other_keigo_constructions', reason: 'User confirmed reru/rareru honorific use, not lexical replacement verbs or o/go constructions.' },
  { id: 'recursive_causative_honorific', reason: 'This finite catalog covers honorifics of simple lexical verbs; compounded honorific readings need separate source/corpus review.' }
);
manifest.composition_families.push('V21');
const honorSourceIds = ['jf-kyozai-honorific-rareru', 'tufs-honorific-rareru', 'f09-project-editorial-b'];
manifest.slots.push({
  slot_id: 'V21', word_group: 'verb', form: 'Kính ngữ tôn kính れる/られる',
  example: '食べられる→食べる; 書かれる→書く', proposal: 'add',
  proposed_rule_id: 'conjugation:verb-honorific', proposed_rule_version: 1,
  inclusion: 'required', po_decision: 'scope_behavior_confirmed_exact_answers_pending',
  scope_basis: 'User: Bao gồm cả cách dùng kính ngữ', source_refs: honorSourceIds,
  case_ids: [], variants: []
});

const honorRules = new Set();
const amendedIds = [];
for (const c of corpus.cases) {
  const passive = c.expected.alternatives.find(a => a.slot_id === 'V11');
  if (!passive) continue;
  // Independent editorial interpretation; not derived from observed provider output.
  const honor = structuredClone(passive);
  assert(['食べる', '書く', 'する', '来る'].includes(honor.base_form), 'Unreviewed honorific lexeme');
  honor.rule_id = passive.rule_id.replace('verb-passive', 'verb-honorific');
  honor.slot_id = 'V21';
  const tenseText = passive.explanation_vi.slice(passive.explanation_vi.indexOf(';') + 1).trim();
  honor.explanation_vi = `Kính ngữ tôn kính: diễn đạt hành động của người được kính trọng; không mặc định là bị động. ${tenseText}`;
  let replaced = 0;
  for (const s of honor.segments) {
    if (s.role !== 'passive') continue;
    s.role = 'honorific';
    s.explanation_vi = 'Phần tôn kính trong cách hiểu này; thể hiện sự kính trọng đối với người thực hiện hành động.';
    replaced++;
  }
  assert(replaced === 1, `Unexpected passive component layout: ${c.case_id}`);
  c.expected.alternatives.push(honor);
  c.expected.contract3.count = c.expected.alternatives.length;
  c.expected.contract2 = { count: 0, capability: 'unavailable' };
  c.slot_ids = [...new Set([...c.slot_ids, 'V21'])];
  c.tags = [...new Set([...c.tags, 'honorific', 'ambiguous'])];
  c.source_refs = [...new Set([...c.source_refs, ...honorSourceIds])];
  honorRules.add(honor.rule_id);
  amendedIds.push(c.case_id);
}
assert(honorRules.size === 8, 'Expected eight finite honorific states');
// Potential forms are not automatically honorifics of the underlying lexical verb.
// These remain valid potential positives, but explicitly forbid the honorific interpretation.
for (const c of corpus.cases) {
  const potential = c.expected.alternatives.find(a => a.slot_id === 'V10' && ['書く', '読む'].includes(a.base_form));
  if (!potential || potential.rule_id.includes('causative')) continue;
  const rid = potential.rule_id.replace('verb-potential', 'verb-honorific');
  if (!honorRules.has(rid)) continue;
  c.expected.forbidden_rule_ids = [...new Set([...(c.expected.forbidden_rule_ids || []), rid])];
  c.slot_ids = [...new Set([...c.slot_ids, 'V21'])];
  c.tags.push('negative_for_honorific');
  c.source_refs = [...new Set([...c.source_refs, ...honorSourceIds])];
}
for (const id of ['A05-P1', 'N05-P1']) {
  const c = corpus.cases.find(x => x.case_id === id);
  c.expected.forbidden_rule_ids = [...new Set([...(c.expected.forbidden_rule_ids || []), ...honorRules])];
  c.slot_ids.push('V21');
  c.tags.push('negative_for_honorific', 'polite_not_honorific');
  c.source_refs = [...new Set([...c.source_refs, ...honorSourceIds])];
}
for (const [index, text, fixture] of [[1, 'できられる', 'fixture:017'], [2, 'できられた', 'fixture:017'],
  [3, '書けられる', 'fixture:016'], [4, '書けられた', 'fixture:016']]) {
  corpus.cases.push({ case_id: `V21-EXCLUDED-${index}`, slot_ids: ['V21'],
    tags: ['negative_for_honorific', 'potential_verb_exclusion'], input: { text },
    dictionary_fixture_ids: [fixture], fixture_policy: 'base_entries_only_exact_surface_absent',
    source_refs: honorSourceIds, expected: { outcome: 'unavailable', capability: 'unavailable',
      alternatives: [], forbidden_rule_ids: [...honorRules], reason: 'Do not generate a honorific by attaching rareru to a potential verb.' },
    review_state: 'completed_proposal_pending_final_review' });
}
const causalCase = corpus.cases.find(c => c.case_id === 'V13-P1');
assert(causalCase.expected.alternatives.length === 2 && causalCase.expected.alternatives.some(a =>
  a.rule_id === 'conjugation:verb-causative-potential'), 'Both confirmed causative readings must remain');
for (const rid of honorRules) manifest.rules.push({ rule_id: rid, rule_version: 1,
  slot_ids: ['V21'], positive_case_ids: [], negative_case_ids: [],
  proposal_status: 'add', language_review: 'pending_final_packet_review' });
for (const r of manifest.rules) {
  r.positive_case_ids = corpus.cases.filter(c => c.expected.alternatives.some(a => a.rule_id === r.rule_id)).map(c => c.case_id);
  r.negative_case_ids = corpus.cases.filter(c => c.expected.forbidden_rule_ids?.includes(r.rule_id)).map(c => c.case_id);
}
for (const s of manifest.slots) {
  s.case_ids = corpus.cases.filter(c => c.slot_ids.includes(s.slot_id)).map(c => c.case_id);
  s.variants = [...new Set(corpus.cases.filter(c => c.slot_ids.includes(s.slot_id)).flatMap(c => c.tags))];
}
const grammarSource = (source_id, publisher, title, url, use) => ({ source_id, kind: 'grammar_reference',
  publisher, title, url, accessed_date: '2026-10-13', edition: 'Web page consulted on accessed_date; byte snapshot not archived',
  sha256: null, license_status: 'reference_only_no_content_import', external_content_imported: false,
  publication_permission: 'not_granted_by_reference_use', use });
sources.sources.push(
  grammarSource('jf-kyozai-honorific-rareru', 'The Japan Foundation', 'BMA00085: V(r)a remasu honorific',
    'https://www.kyozai.jpf.go.jp/kyozai/material/BMA00085/ja/render.do',
    'Reference for honorific/passive ambiguity and exclusion of potential verbs; no page text or examples imported.'),
  grammarSource('tufs-honorific-rareru', 'Tokyo University of Foreign Studies', 'Japanese grammar module: Keigo section 3',
    'https://www.coelang.tufs.ac.jp/mt/ja/gmod/contents/explanation/092.html',
    'Reference for godan/ichidan/suru/kuru honorific forms, ordinary/polite forms and potential exclusions; Vietnamese independently authored.')
);
const editorial = sources.sources.find(s => s.source_id === 'f09-project-editorial-b');
editorial.sha256 = null; // Bound to the actual new source bytes during freeze, not the old .2 file.
editorial.inferences[0].user_scope_confirmed = true;
editorial.inferences[0].decision_receipt = '../../reviews/scope-decisions-20261013.json';
const observation = sources.sources.find(s => s.source_id === 'f09-sudachi-observations');
observation.use += '; retained as historical .2 evidence only, not observation coverage for .3';
const section = process.argv[2] || 'summary';
const summary = { corpus_version: version, slots: manifest.slots.length, cases: corpus.cases.length,
  rules: manifest.rules.length, amended_honorific_cases: amendedIds.length, amended_case_ids: amendedIds,
  unique_inputs: new Set(corpus.cases.map(c => c.input.text)).size,
  multi_alternative_cases: corpus.cases.filter(c => c.expected.alternatives.length > 1).length,
  minimum_positive: Math.min(...manifest.rules.map(r => r.positive_case_ids.length)),
  minimum_negative: Math.min(...manifest.rules.map(r => r.negative_case_ids.length)) };
const result = section === 'summary' ? summary : section === 'manifest' ? manifest : section === 'sources' ? sources :
  section === 'header' ? { ...corpus, cases: [] } : section === 'case_batch' ? corpus.cases.slice(
    Number(process.argv[3] || 0), Number(process.argv[3] || 0) + Number(process.argv[4] || 8)) : null;
assert(result, `Unknown section ${section}`);
process.stdout.write(JSON.stringify(result));
