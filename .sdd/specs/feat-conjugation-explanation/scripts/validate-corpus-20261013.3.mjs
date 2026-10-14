// Evidence validator. It checks editorial completeness; it never certifies grammar by running the matcher.
import { readFileSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
const feature = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const packet = resolve(process.argv[2] || resolve(feature,'corpus/expanded-20261013.3'));
const structuralOnly = process.argv.includes('--structural-only');
const json = path => JSON.parse(readFileSync(path,'utf8'));
const hash = path => createHash('sha256').update(readFileSync(path)).digest('hex');
const requireValue = (ok, message) => { if(!ok)throw new Error(message); };
const nonblank = value => typeof value==='string'&&value.trim().length>0;
function unique(list,label){requireValue(new Set(list).size===list.length,`Duplicate ${label}`);}
function boundary(text,i){return Number.isInteger(i)&&i>=0&&i<=text.length&&!(i>0&&i<text.length&&
  text.charCodeAt(i-1)>=0xd800&&text.charCodeAt(i-1)<=0xdbff&&text.charCodeAt(i)>=0xdc00&&text.charCodeAt(i)<=0xdfff);}
function validate(corpus,manifest,sources,observations){
  requireValue(corpus.schema_version===1&&manifest.schema_version===1&&sources.schema_version===1,'Unsupported editorial schema');
  requireValue(corpus.corpus_version===manifest.corpus_version&&corpus.corpus_version===sources.corpus_version&&
    corpus.corpus_version===observations.corpus_version,'Version mismatch');
  requireValue(manifest.slots.length===60,'Expected 60 required slots');
  unique(corpus.cases.map(c=>c.case_id),'case IDs');unique(manifest.slots.map(s=>s.slot_id),'slot IDs');
  unique(manifest.rules.map(r=>r.rule_id),'rule IDs');unique(corpus.dictionary_fixtures.map(f=>f.fixture_id),'fixture IDs');
  unique(corpus.dictionary_fixtures.map(f=>f.canonical_id),'fixture canonical IDs');
  unique(sources.sources.map(s=>s.source_id),'source IDs');unique(observations.observations.map(o=>o.text),'observed texts');
  const slotIds=new Set(manifest.slots.map(s=>s.slot_id)),ruleIds=new Set(manifest.rules.map(r=>r.rule_id));
  const fixtures=new Map(corpus.dictionary_fixtures.map(f=>[f.fixture_id,f]));
  const srcIds=new Set(sources.sources.map(s=>s.source_id));const allCaseIds=new Set(corpus.cases.map(c=>c.case_id));
  const observed=new Map(observations.observations.map(o=>[o.text,o]));
  const positive=new Map(manifest.rules.map(r=>[r.rule_id,new Set()]));const negative=new Map(manifest.rules.map(r=>[r.rule_id,new Set()]));
  const outcomes=new Set(['supported','exact_first','unavailable','completed_empty','failed','discarded','cancelled']);
  const groupCounts={verb:22,i_adjective:12,na_adjective:14,noun_copula:12};
  for(const [g,count]of Object.entries(groupCounts))requireValue(manifest.slots.filter(s=>s.word_group===g).length===count,`Word group ${g} count`);
  for(const item of sources.sources){requireValue(item.source_id&&nonblank(item.use),'Incomplete source record');
    requireValue(item.license_status!=='pending_review','Unresolved reference-use decision');
    if(item.sha256)requireValue(/^[a-f0-9]{64}$/.test(item.sha256),'Invalid source hash');
    if(item.kind==='grammar_reference')requireValue(item.external_content_imported===false&&item.publication_permission==='not_granted_by_reference_use',
      'Reference-only scope missing');}
  for(const c of corpus.cases){const text=c.input.text,e=c.expected;
    requireValue(nonblank(c.case_id)&&nonblank(text)&&outcomes.has(e.outcome),`Incomplete case ${c.case_id}`);
    requireValue(!/[()（）]/u.test(text),`Explanatory annotation in input ${c.case_id}`);
    requireValue(c.slot_ids.length>0&&c.slot_ids.every(s=>slotIds.has(s)),`Unknown case slot ${c.case_id}`);
    requireValue(c.source_refs.length>0&&c.source_refs.every(s=>srcIds.has(s)),`Unknown case source ${c.case_id}`);
    requireValue(Array.isArray(c.dictionary_fixture_ids)&&c.dictionary_fixture_ids.every(f=>fixtures.has(f)),`Unknown fixture ${c.case_id}`);
    if(!structuralOnly)requireValue(observed.has(text),`Unobserved input ${c.case_id}`);
    requireValue(Array.isArray(e.alternatives),`Alternatives missing ${c.case_id}`);
    unique(e.alternatives.map(a=>`${a.rule_id}@${a.rule_version}`),`alternative identities ${c.case_id}`);
    for(const rid of e.forbidden_rule_ids||[]){requireValue(ruleIds.has(rid),`Unknown forbidden rule ${c.case_id}`);
      requireValue(!e.alternatives.some(a=>a.rule_id===rid),`False-positive expectation ${c.case_id}`);negative.get(rid).add(c.case_id);}
    if(e.outcome==='supported'){
      requireValue(c.fixture_policy==='base_entries_only_exact_surface_absent','Supported fixture policy must state exact miss');
      requireValue(e.capability==='completed'&&e.alternatives.length>0,`Empty supported ${c.case_id}`);
      requireValue(e.span.start===0&&e.span.end===text.length&&e.span.unit==='utf16',`Occurrence span ${c.case_id}`);
      requireValue(e.contract3.count===e.alternatives.length&&e.contract3.capability==='completed',`Contract3 count ${c.case_id}`);
      requireValue(e.contract2.count===(e.alternatives.length>1?0:1)&&e.contract2.capability===(e.alternatives.length>1?'unavailable':'completed'),
        `Contract2 ambiguity regression ${c.case_id}`);
      for(const a of e.alternatives){const r=manifest.rules.find(r=>r.rule_id===a.rule_id);const f=fixtures.get(a.dictionary_fixture_id);
        requireValue(r&&r.rule_version===a.rule_version&&slotIds.has(a.slot_id),`Unknown alternative identity ${c.case_id}`);
        requireValue(f&&c.dictionary_fixture_ids.includes(f.fixture_id)&&f.forms.includes(a.base_form)&&f.word_group===a.word_group,
          `Alternative base/fixture identity ${c.case_id}`);
        requireValue(a.language_tag==='vi'&&nonblank(a.explanation_vi)&&!/(fixture|provider|test)/i.test(a.explanation_vi),`VI content ${c.case_id}`);
        requireValue(a.interpretation_label==='Cách hiểu có thể','Interpretation label');
        requireValue(Array.isArray(a.segments)&&a.segments.length>0,`Segments missing ${c.case_id}`);
        let pos=0;
        for(const s of a.segments){requireValue(nonblank(s.surface)&&nonblank(s.explanation_vi)&&nonblank(s.role),`Incomplete segment ${c.case_id}`);
          requireValue(s.span.unit==='utf16'&&s.span.start===pos&&s.span.end>s.span.start&&boundary(text,s.span.start)&&boundary(text,s.span.end),
            `Segment gap/overlap/surrogate ${c.case_id}`);
          requireValue(text.slice(s.span.start,s.span.end)===s.surface,`Segment surface ${c.case_id}`);pos=s.span.end;}
        requireValue(pos===text.length&&a.segments.map(s=>s.surface).join('')===text,`Segment coverage ${c.case_id}`);
        positive.get(a.rule_id).add(c.case_id);
      }
    } else requireValue(e.alternatives.length===0,`Fake alternatives ${c.case_id}`);
    if(e.outcome==='exact_first')requireValue(c.fixture_policy==='exact_surface_present'&&e.capability==='unavailable'&&
      c.dictionary_fixture_ids.some(id=>fixtures.get(id).forms.includes(text)),`Exact-first fixture ${c.case_id}`);
    if(e.outcome==='completed_empty')requireValue(c.dictionary_fixture_ids.length===0&&e.capability==='completed',`Missing base ${c.case_id}`);
  }
  for(const r of manifest.rules){requireValue(positive.get(r.rule_id).size>=2,`Fewer than two positives ${r.rule_id}`);
    requireValue(negative.get(r.rule_id).size>=1,`No negative ${r.rule_id}`);
    requireValue(r.positive_case_ids.every(id=>allCaseIds.has(id)&&positive.get(r.rule_id).has(id)),`Positive rule refs ${r.rule_id}`);
    requireValue(r.negative_case_ids.every(id=>allCaseIds.has(id)&&negative.get(r.rule_id).has(id)),`Negative rule refs ${r.rule_id}`);
    requireValue(r.positive_case_ids.length===positive.get(r.rule_id).size&&r.negative_case_ids.length===negative.get(r.rule_id).size,`Coverage ref counts ${r.rule_id}`);}
  for(const s of manifest.slots){requireValue(s.inclusion==='required'&&s.case_ids.every(id=>allCaseIds.has(id)),`Incomplete slot ${s.slot_id}`);
    requireValue(s.source_refs.length>0&&s.source_refs.every(id=>srcIds.has(id)),`Slot source refs ${s.slot_id}`);
    requireValue(s.case_ids.length===corpus.cases.filter(c=>c.slot_ids.includes(s.slot_id)).length,`Slot case refs ${s.slot_id}`);
    requireValue(corpus.cases.some(c=>c.slot_ids.includes(s.slot_id)&&c.expected.outcome===(s.slot_id.endsWith('00')?'exact_first':'supported')),
      `Uncovered slot ${s.slot_id}`);}
  const honorCase=corpus.cases.find(c=>c.case_id==='V11-P1');
  requireValue(honorCase.expected.alternatives.length===3&&
    honorCase.expected.alternatives.some(a=>a.segments.some(s=>s.role==='honorific')),'Missing confirmed honorific interpretation');
  const causativeCase=corpus.cases.find(c=>c.case_id==='V13-P1');
  requireValue(causativeCase.expected.alternatives.length===2&&
    causativeCase.expected.alternatives.some(a=>a.rule_id==='conjugation:verb-causative-potential')&&
    causativeCase.expected.alternatives.some(a=>a.rule_id==='conjugation:verb-causative-passive'),
    'Missing confirmed causative interpretations');
  requireValue(manifest.rules.filter(r=>r.rule_id.startsWith('conjugation:verb-honorific')).length===8,
    'Missing finite honorific states');
  const decisions=json(resolve(feature,'reviews/scope-decisions-20261013.json'));
  requireValue(decisions.decisions.length===2&&decisions.decisions.every(d=>d.scope_behavior_confirmed),
    'Missing user scope decisions');
  const prior=resolve(feature,'corpus/expanded-20261013.2');
  requireValue(hash(resolve(prior,'scope-material.json'))===manifest.prior_scope_material_sha256,'Prior material hash changed');
  for(const f of json(resolve(prior,'scope-material.json')).files)
    requireValue(hash(resolve(prior,f.path))===f.sha256,'Prior frozen snapshot changed');
  const baselineEvidence=json(resolve(feature,'evidence/baseline.json'));
  const baselinePath=resolve(feature,'../../..',baselineEvidence.dataset.path);
  requireValue(hash(baselinePath)===baselineEvidence.dataset.sha256,'Baseline source changed');
  const baselineData=json(baselinePath);
  const retained=manifest.slots.filter(s=>s.baseline_rule_version===1&&s.proposed_rule_version===2);
  requireValue(retained.length===9&&baselineData.rules.every(r=>retained.some(s=>s.proposed_rule_id===r.rule_id&&r.rule_version===s.baseline_rule_version)),
    'Baseline review missing');
  requireValue(manifest.baseline_live_examples.length===13&&manifest.baseline_live_examples.every(b=>
    corpus.cases.some(c=>c.input.text===b.surface&&c.expected.alternatives.some(a=>a.base_form===b.base_form))),
    'Original 13 live positives not carried');
  const receipt=json(resolve(feature,'reviews/user-review-receipt-20261013.json'));
  requireValue(receipt.exact_draft_accepted&&!receipt.completed_revision_approved,'Previous receipt scope');
  requireValue(receipt.approved_materials.every(f=>hash(resolve(feature,f.path))===f.sha256),'Reviewed draft was modified');
  if(manifest.approval.state==='pending_final_review')requireValue(manifest.approval.approver===null&&manifest.approval.approved_at_utc===null,
    'Fabricated final approval');
  for(const o of observations.observations){let pos=0;requireValue(o.tokens.length>0,'Empty observed input');
    for(const t of o.tokens){requireValue(t.span.unit==='utf16'&&t.span.start===pos&&t.span.end>pos&&boundary(o.text,t.span.start)&&boundary(o.text,t.span.end)&&
      o.text.slice(t.span.start,t.span.end)===t.surface,'Provider UTF-16 surface mismatch');
      requireValue(t.provider_pos.split(',').length===6,'Provider POS fields');pos=t.span.end;}
    requireValue(pos===o.text.length,'Provider token coverage');}
  const uniqueInputs=new Set(corpus.cases.map(c=>c.input.text));
  if(!structuralOnly){
    requireValue(uniqueInputs.size===observations.observations.length,'Observation input-set mismatch');
    requireValue(observations.transport==='real grpc loopback inside isolated Docker container'&&!observations.semantic_authority,'Observation boundary');
  }
  const comparison=corpus.cases.filter(c=>c.expected.outcome==='supported'&&observed.has(c.input.text)).map(c=>{
    const ts=observed.get(c.input.text).tokens;return {unchanged:ts[0].surface===ts[0].base_form,
      differentBase:!c.expected.alternatives.some(a=>a.base_form===ts[0].base_form),
      differentParts:c.expected.alternatives.some(a=>a.segments.map(s=>s.surface).join('|')!==ts.map(t=>t.surface).join('|'))};});
  return {slots:manifest.slots.length,rules:manifest.rules.length,cases:corpus.cases.length,fixtures:fixtures.size,
    group_counts:groupCounts,baseline_live_cases_carried:13,prior_user_review_bytes_preserved:true,
    outcomes:Object.fromEntries([...outcomes].map(o=>[o,corpus.cases.filter(c=>c.expected.outcome===o).length])),
    unique_inputs:uniqueInputs.size,grpc_observed_inputs:observations.observations.length,
    multi_alternative_cases:corpus.cases.filter(c=>c.expected.alternatives.length>1).length,
    minimum_positive_per_rule:Math.min(...[...positive.values()].map(s=>s.size)),minimum_negative_per_rule:Math.min(...[...negative.values()].map(s=>s.size)),
    provider_differences:structuralOnly?null:{unchanged_head_cases:comparison.filter(c=>c.unchanged).length,first_base_differs_cases:comparison.filter(c=>c.differentBase).length,
      component_layout_differs_cases:comparison.filter(c=>c.differentParts).length},
    checks:['unique IDs','known refs','four-group slot coverage','per-rule 2 positive + 1 negative','fixture identity and exact-first',
      'nonblank VI/components','UTF-16 contiguous reconstructed spans','v2/v3 ambiguity cardinality',
      ...(!structuralOnly?['real gRPC observation coverage']:[]),
      'semantic expected independent of actual','baseline version advancement','user-confirmed honorific and causative alternatives','prior frozen snapshot unchanged']};
}
const corpus=json(resolve(packet,'cases.json')),manifest=json(resolve(packet,'manifest.json')),sources=json(resolve(packet,'sources.json'));
const observationPath=resolve(feature,'evidence/expanded-sudachi-observations-20261013.3.json');
const observations=structuralOnly?{corpus_version:corpus.corpus_version,observations:[]}:json(observationPath);
const result=validate(corpus,manifest,sources,observations);
const selfTests=[];
if(process.argv.includes('--self-test')) {
  const mutations=[['duplicate case',c=>c.cases.push(structuredClone(c.cases[0]))],
    ['blank VI',c=>c.cases[0].expected.alternatives[0].explanation_vi=' '],
    ['segment gap',c=>c.cases[0].expected.alternatives[0].segments[1].span.start=1],
    ['wrong base fixture',c=>c.cases[0].expected.alternatives[0].base_form='学生'],
    ['unknown source',c=>c.cases[0].source_refs=['missing']],
    ['ambiguous v2 result',c=>c.cases.find(x=>x.expected.alternatives.length>1).expected.contract2.count=1],
    ['fake exact explanation',c=>{const x=c.cases.find(x=>x.expected.outcome==='exact_first');x.expected.alternatives=[structuredClone(c.cases[0].expected.alternatives[0])];}],
    ['half surrogate',c=>{c.cases.find(x=>x.case_id==='UNI-02').expected.alternatives[0].segments[0].span.end=1;}]];
  for(const [name,mutate]of mutations){const copy=structuredClone(corpus);mutate(copy);let error=null;
    try{validate(copy,manifest,sources,observations);}catch(e){error=e.message;}
    requireValue(error,`Guard accepted ${name}`);selfTests.push({name,result:'rejected',reason:error});}
}
// Frozen-material binding does not require human approval to validate hashes; approval is a separate gate.
const materialPath=resolve(packet,'scope-material.json');
if(process.argv.includes('--verify-freeze')){
  const material=json(materialPath);
  requireValue(material.corpus_version===corpus.corpus_version&&!('scope_material_sha256' in material),'Self hash or material version');
  for(const f of material.files){
    const target=resolve(packet,f.path);
    requireValue(target.toLowerCase()!==materialPath.toLowerCase(),'Frozen material cannot contain its own hash');
    requireValue(hash(target)===f.sha256,`Frozen hash mismatch ${f.path}`);
  }
  requireValue(manifest.cases_sha256===hash(resolve(packet,'cases.json'))&&manifest.sources_sha256===hash(resolve(packet,'sources.json'))&&
    manifest.scope_material_sha256===hash(materialPath),'Manifest material hash mismatch');
}
process.stdout.write(JSON.stringify({schema_version:1,corpus_version:corpus.corpus_version,
  stage:'corpus_preparation',structural_status:'pass',human_g1:'pending_final_packet_confirmation',human_g2:'pending_final_packet_confirmation',
  runtime_corpus_match_tested:false, ...result,guard_tests:selfTests,
  case_source_sha256:hash(resolve(packet,'cases.json')),source_catalog_sha256:hash(resolve(packet,'sources.json')),
  observation_source_sha256:structuralOnly?null:hash(observationPath),
  grpc_validation:structuralOnly?'not_run':'pass',
  freeze_verified:process.argv.includes('--verify-freeze')}));


