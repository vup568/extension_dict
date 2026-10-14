// Offline, reproducible compilation of approved editorial grammar. Runtime never reads the corpus.
import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
const feature = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const packet = resolve(feature, 'corpus/expanded-20261013.3');
const json = p => JSON.parse(readFileSync(p, 'utf8'));
const hash = p => createHash('sha256').update(readFileSync(p)).digest('hex');
const assert = (ok, message) => { if (!ok) throw new Error(message); };
const manifest = json(resolve(packet, 'manifest.json'));
const corpus = json(resolve(packet, 'cases.json'));
const observations = new Map(json(resolve(feature, 'evidence/expanded-sudachi-observations-20261013.3.json'))
  .observations.map(o => [o.text, o.tokens]));
const receipt = json(resolve(feature, 'reviews/user-review-receipt-20261013.3.json'));
assert(manifest.approval.state === 'approved' && receipt.g1_scope === 'approved' && receipt.g2_answers === 'accepted_by_user_batch_approval', 'Human G1/G2 required');
assert(hash(resolve(packet, 'cases.json')) === receipt.cases_sha256 && hash(resolve(packet, 'sources.json')) === receipt.sources_sha256, 'Reviewed answers changed');
const baseline = json(resolve(feature, '../../../src/infra/Conjugation/Data/development-20261013.1.json'));
const version = 'candidate-20261013.1';
const posTags = {
  ichidan: ['v1', 'verb:ichidan'], 'godan-く': ['v5k', 'v5k-s', 'verb:godan-k'], 'godan-む': ['v5m', 'verb:godan-m'],
  suru: ['vs-i', 'vs-s', 'vs', 'verb:suru'], kuru: ['vk', 'verb:kuru'],
  i_adjective: ['adj-i', 'i_adjective'], na_adjective: ['adj-na', 'na_adjective'], noun_copula: ['n', 'noun', 'noun_copula']
};
const categoryLabels = { '動詞': 'verb', '形容詞': 'i_adjective', '形状詞': 'na_adjective',
  '名詞': 'noun', '副詞': 'adverb', '助動詞': 'auxiliary', '助詞': 'particle',
  '補助記号': 'symbol', '接尾辞': 'suffix', '連体詞': 'attributive', '感動詞': 'interjection', '接続詞': 'conjunction' };
const headCategory = pos => { const p = pos.split(','); return p[0] === '名詞' && p[2] === '形状詞可能' ? 'na_adjective' : categoryLabels[p[0]]; };
const fixtures = new Map(corpus.dictionary_fixtures.map(f => [f.fixture_id, f]));
const patterns = new Map();
for (const c of corpus.cases.filter(c => c.expected.outcome === 'supported')) for (const a of c.expected.alternatives) {
  const fixture = fixtures.get(a.dictionary_fixture_id), base = a.base_form;
  const verb = fixture.word_group === 'verb';
  const kind = !verb ? fixture.word_group : fixture.conjugation_class === 'irregular' ? (base === 'する' ? 'suru' : 'kuru') :
    fixture.conjugation_class === 'godan' ? `godan-${base.at(-1)}` : 'ichidan';
  const suffix = verb ? (kind === 'suru' || kind === 'kuru' ? base : base.at(-1)) : kind === 'i_adjective' ? 'い' : '';
  const stem = suffix ? base.slice(0, -suffix.length) : base;
  assert(a.segments[0].surface.startsWith(stem), `Cannot parameterize lexical stem: ${c.case_id}`);
  const required = kind === 'suru' || kind === 'kuru' || c.tags.includes('ii_yoi') || base === '行く' ? base : null;
  const parts = a.segments.map((s, i) => ({ surface_template: i === 0 ? '{stem}' + s.surface.slice(stem.length) : s.surface,
    explanation_vi: s.explanation_vi.replaceAll(base, '{base_form}') }));
  const key = JSON.stringify([a.rule_id, a.rule_version, kind, suffix, required, parts.map(p => p.surface_template)]);
  let pattern = patterns.get(key);
  if (!pattern) {
    assert(posTags[kind], `Missing canonical dictionary class ${kind}`);
    pattern = { rule_id: a.rule_id, rule_version: a.rule_version, word_group: a.word_group,
      lexical_class: kind, canonical_suffix: suffix, required_base: required, excluded_bases: [],
      dictionary_pos: posTags[kind], explanation_vi: a.explanation_vi.replaceAll(base, '{base_form}'),
      components: parts, head_base_templates: [], head_categories: [] };
    patterns.set(key, pattern);
  }
  // Provider observations constrain the normalized head, not the expected meaning or Vietnamese wording.
  const first = observations.get(c.input.text)[0];
  const head = first.base_form.startsWith(stem) ? '{stem}' + first.base_form.slice(stem.length) : first.base_form;
  assert(first.base_form.startsWith(stem) || required, `Provider alias needs a lexical exception: ${c.case_id}`);
  pattern.head_base_templates = [...new Set([...pattern.head_base_templates, head])];
  pattern.head_categories = [...new Set([...pattern.head_categories, headCategory(first.provider_pos)])];
  assert(pattern.head_categories.every(Boolean), 'Unnormalized provider label');
}
const rowData = json(resolve(feature, '../../../src/infra/Conjugation/Data/godan-rows-20261013.1.json'));
// Reuse the reviewed godan frame with a versioned kana row, rather than fixing it to 書く/読む.
for (const source of [...patterns.values()].filter(p => p.lexical_class === 'godan-く' && !p.required_base)) {
  for (const row of rowData.rows.filter(r => r.dictionary !== 'く')) {
    if (source.rule_id.includes(':contracted') && rowData.contracted_causative_passive_excluded_endings.includes(row.dictionary)) continue;
    const p = structuredClone(source);
    p.lexical_class = `godan-${row.dictionary}`; p.canonical_suffix = row.dictionary; p.dictionary_pos = row.dictionary_pos;
    const headChanges = { 'く': row.dictionary, 'か': row.a, 'き': row.i, 'け': row.e, 'こ': row.o, 'い': row.onbin };
    const changeHead = s => s.replace(/\{stem\}([くかきけこい])/gu, (_, kana) => '{stem}' + headChanges[kana]);
    p.components = p.components.map(c => ({ ...c, surface_template: changeHead(c.surface_template) }));
    p.head_base_templates = p.head_base_templates.map(changeHead);
    if (row.voiced && p.components[0].surface_template === '{stem}' + row.onbin) {
      for (const c of p.components.slice(1, 2)) {
        if (c.surface_template === 'て') c.surface_template = 'で';
        if (c.surface_template === 'た') c.surface_template = 'だ';
        if (c.surface_template === 'たら') c.surface_template = 'だら';
      }
    }
    const key = JSON.stringify([p.rule_id, p.rule_version, p.lexical_class, p.canonical_suffix, null, p.components.map(c => c.surface_template)]);
    // Prefer independently reviewed realizations when a row already has corpus evidence.
    if (!patterns.has(key)) patterns.set(key, p);
  }
}
const paradigms = [...patterns.values()];
for (const special of rowData.lexical_onbin_overrides) {
  for (const p of [...paradigms].filter(p => !p.required_base && p.canonical_suffix === special.canonical_suffix &&
    p.components[0].surface_template === '{stem}' + special.regular)) {
    p.excluded_bases.push(special.base);
    const q = structuredClone(p); q.required_base = special.base; q.excluded_bases = []; q.dictionary_pos = special.dictionary_pos;
    q.components[0].surface_template = '{stem}' + special.replacement;
    if (!paradigms.some(other => other.rule_id === q.rule_id && other.required_base === q.required_base &&
      JSON.stringify(other.components.map(c => c.surface_template)) === JSON.stringify(q.components.map(c => c.surface_template)))) paradigms.push(q);
  }
}
// A reviewed irregular realization supersedes a generic realization for that lexical entry only.
for (const p of paradigms.filter(p => !p.required_base)) {
  p.excluded_bases = [...new Set([...p.excluded_bases, ...paradigms.filter(q => q.required_base && q.rule_id === p.rule_id && q.lexical_class === p.lexical_class)
    .map(q => q.required_base)])];
  for (const [family, exclusions] of Object.entries(rowData.lexical_exclusions))
    if (p.rule_id.includes('verb-' + family)) p.excluded_bases = [...new Set([...p.excluded_bases, ...exclusions])];
}
assert(new Set(paradigms.map(p => p.rule_id)).size === manifest.rules.length, 'Missing reviewed rule');
const categories = [{ value: 'na_adjective', conditions: [{ index: 0, equals: '名詞' }, { index: 2, equals: '形状詞可能' }] },
  ...Object.entries(categoryLabels).map(([label, value]) => ({ value, conditions: [{ index: 0, equals: label }] }))];
const candidate = { schema_version: 1, version, engine: 'full_word_paradigms',
  provenance: { status: 'development', sources: json(resolve(packet, 'sources.json')).sources.filter(s => s.kind === 'grammar_reference').map(s => s.url),
    editorial: 'Independently authored Vietnamese and parameterized reviewed grammar; no external text/images imported.',
    parameterization_sources: rowData.provenance.sources, row_data_sha256: hash(resolve(feature, '../../../src/infra/Conjugation/Data/godan-rows-20261013.1.json')),
    corpus_version: corpus.corpus_version, corpus_sha256: manifest.cases_sha256, review_material_sha256: manifest.scope_material_sha256,
    production_review: 'pending_publication_gate' }, provider_mappings: { categories, inflections: baseline.provider_mappings.inflections },
  paradigms };
assert(!JSON.stringify(candidate).includes('fixture:'), 'Fixture IDs cannot enter runtime knowledge');
const section = process.argv[2] || 'summary';
const result = section === 'summary' ? { version, rule_ids: manifest.rules.length, paradigms: paradigms.length, surface_lookup_records: 0 } :
  section === 'full' ? candidate :
  section === 'header' ? { ...candidate, paradigms: [] } : section === 'batch' ? paradigms.slice(Number(process.argv[3] || 0),
    Number(process.argv[3] || 0) + Number(process.argv[4] || 8)) : null;
assert(result, `Unknown section ${section}`); process.stdout.write(JSON.stringify(result));
