// Editorial fixture preparation only. The application must never import this script.
// Grammar and lexeme declarations below are proposed answers, independent of Sudachi output.
import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const feature = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const previous = resolve(feature, 'corpus/expanded-20261013.1');
const oldManifest = JSON.parse(readFileSync(resolve(previous, 'manifest.json'), 'utf8'));
const reviewedText = readFileSync(resolve(previous, 'case-review.md'), 'utf8');
const version = 'f09-expanded-20261013.2';
const baselineLiveExamples=[['食べます','食べる'],['食べました','食べる'],['食べない','食べる'],['食べなかった','食べる'],
  ['食べた','食べる'],['食べません','食べる'],['高かった','高い'],['高くない','高い'],['高くなかった','高い'],
  ['書いた','書く'],['読んだ','読む'],['しました','する'],['来ました','来る']].map(([surface,base_form])=>({surface,base_form}));
const groups = { V: 'verb', I: 'i_adjective', A: 'na_adjective', N: 'noun_copula' };
const slots = oldManifest.slots.map(s => ({ ...s, inclusion: 'required',
  scope_basis: 'User approved previous case-review.md; completed material awaits final review',
  po_decision: 'previous_draft_accepted_final_packet_pending', case_ids: [], variants: [] }));
const slotMap = new Map(slots.map(s => [s.slot_id, s]));
const baseline = Object.fromEntries(slots.filter(s => s.proposed_rule_id).map(s => [s.slot_id, s.proposed_rule_id]));
const ruleNames = {
  V04:'verb-polite-negative-past', V08:'verb-te', V09:'verb-negative-connective',
  V10:'verb-potential', V11:'verb-passive', V12:'verb-causative', V13:'verb-causative-passive',
  V14:'verb-conditional-ba', V15:'verb-conditional-tara', V16:'verb-volitional',
  V17:'verb-imperative', V18:'verb-prohibition', V19:'verb-desiderative', V20:'verb-te-iru',
  I04:'adjective-polite-nonpast', I05:'adjective-polite-past', I06:'adjective-polite-negative',
  I07:'adjective-polite-negative-past', I08:'adjective-connective', I09:'adjective-adverbial',
  I10:'adjective-conditional-ba', I11:'adjective-conditional-tara',
  A01:'na-copula-nonpast', A02:'na-copula-past', A03:'na-copula-negative',
  A04:'na-copula-negative-past', A05:'na-copula-polite-nonpast', A06:'na-copula-polite-past',
  A07:'na-copula-polite-negative', A08:'na-copula-polite-negative-past', A09:'na-attributive',
  A10:'na-adverbial', A11:'na-connective', A12:'na-conditional-nara', A13:'na-conditional-tara',
  N01:'noun-copula-nonpast', N02:'noun-copula-past', N03:'noun-copula-negative',
  N04:'noun-copula-negative-past', N05:'noun-copula-polite-nonpast', N06:'noun-copula-polite-past',
  N07:'noun-copula-polite-negative', N08:'noun-copula-polite-negative-past',
  N09:'noun-conditional-nara', N10:'noun-conditional-tara', N11:'noun-copula-connective'
};
const meanings = {
  V01:'Dạng lịch sự, không quá khứ', V02:'Dạng lịch sự, quá khứ',
  V03:'Dạng lịch sự, phủ định, không quá khứ', V04:'Dạng lịch sự, phủ định, quá khứ',
  V05:'Dạng quá khứ thông thường', V06:'Dạng phủ định, không quá khứ thông thường',
  V07:'Dạng phủ định, quá khứ thông thường', V08:'Thể nối て/で; quan hệ với phần câu tiếp theo phụ thuộc ngữ cảnh',
  V09:'Thể nối phủ định なくて; có thể nối trạng thái hoặc nêu lý do tùy câu',
  V10:'Thể khả năng: có thể thực hiện hành động',
  V11:'Thể bị động: chủ thể chịu hoặc nhận tác động; không mặc định là bất lợi',
  V12:'Thể sai khiến: cho phép, yêu cầu hoặc khiến người khác thực hiện hành động tùy ngữ cảnh',
  V13:'Thể sai khiến-bị động: bị yêu cầu hoặc bắt thực hiện hành động',
  V14:'Thể điều kiện ば: nếu thỏa điều kiện', V15:'Thể điều kiện たら: nếu/khi sự việc xảy ra, tùy ngữ cảnh',
  V16:'Thể ý chí/đề nghị: dự định, quyết định hoặc đề nghị cùng thực hiện, tùy ngữ cảnh',
  V17:'Thể mệnh lệnh trực tiếp', V18:'Thể cấm đoán: đừng thực hiện hành động',
  V19:'Dạng mong muốn: muốn thực hiện hành động', V20:'Dạng ている: hành động đang diễn ra hoặc trạng thái, tùy động từ và ngữ cảnh',
  I01:'Tính từ い ở dạng quá khứ thông thường', I02:'Tính từ い ở dạng phủ định, không quá khứ',
  I03:'Tính từ い ở dạng phủ định quá khứ', I04:'Tính từ い ở dạng không quá khứ lịch sự',
  I05:'Tính từ い ở dạng quá khứ lịch sự', I06:'Tính từ い ở dạng phủ định lịch sự',
  I07:'Tính từ い ở dạng phủ định quá khứ lịch sự', I08:'Thể nối くて của tính từ い',
  I09:'Dạng trạng từ く của tính từ い', I10:'Thể điều kiện ければ của tính từ い',
  I11:'Thể điều kiện かったら của tính từ い'
};
for (const prefix of ['A','N']) {
  const head = prefix === 'A' ? 'Tính từ な' : 'Danh từ đi với copula; bản thân danh từ không tự chia';
  const forms = { '01':'khẳng định thông thường, không quá khứ', '02':'khẳng định thông thường, quá khứ',
    '03':'phủ định thông thường, không quá khứ', '04':'phủ định thông thường, quá khứ',
    '05':'khẳng định lịch sự, không quá khứ', '06':'khẳng định lịch sự, quá khứ',
    '07':'phủ định lịch sự, không quá khứ', '08':'phủ định lịch sự, quá khứ' };
  for (const [id, meaning] of Object.entries(forms)) meanings[prefix+id] = `${head}: ${meaning}`;
}
Object.assign(meanings, { A09:'Dạng な để bổ nghĩa cho danh từ theo sau', A10:'Dạng に dùng làm trạng từ của tính từ な',
  A11:'Dạng nối bằng copula で của tính từ な', A12:'Điều kiện なら của tính từ な',
  A13:'Điều kiện だったら của tính từ な', N09:'Danh từ với điều kiện なら',
  N10:'Danh từ với điều kiện だったら',
  N11:'Cách hiểu có thể: danh từ nối bằng copula で; cùng bề mặt cũng có thể chứa trợ từ で, cần câu để xác định' });

const lexemes = [
  ['食べる','たべる','verb','Ăn','ichidan'], ['書く','かく','verb','Viết','godan'],
  ['読む','よむ','verb','Đọc','godan'], ['行く','いく','verb','Đi','godan'],
  ['する','する','verb','Làm','irregular'], ['来る','くる','verb','Đến','irregular'],
  ['高い','たかい','i_adjective','Cao/đắt',null], ['安い','やすい','i_adjective','Rẻ',null],
  ['よい','よい','i_adjective','Tốt',null], ['静か','しずか','na_adjective','Yên tĩnh',null],
  ['便利','べんり','na_adjective','Tiện lợi',null], ['きれい','きれい','na_adjective','Đẹp/sạch',null],
  ['学生','がくせい','noun_copula','Học sinh/sinh viên',null], ['先生','せんせい','noun_copula','Thầy/cô',null],
  ['𠮷','よし','noun_copula','Tên riêng tổng hợp dùng để test UTF-16',null],
  ['書ける','かける','verb','Mục từ khả năng dùng riêng cho exact-first',null],
  ['できる','できる','verb','Mục từ dùng riêng cho exact-first',null]
];
const fixtures = lexemes.map(([base, reading, group, gloss, conjugationClass], index) => ({
  fixture_id:`fixture:${String(index+1).padStart(3,'0')}`, canonical_id:`fixture:word:${String(index+1).padStart(3,'0')}`,
  preferred_form:base, forms:base === 'よい' ? ['よい','いい','良い'] : [base],
  readings:[reading], word_group:group, conjugation_class:conjugationClass,
  gloss_vi:gloss, provenance:'Project-authored synthetic test fixture, not a JMdict ID or imported entry'
}));
const fixtureMap = new Map(fixtures.map(f => [f.preferred_form, f]));
const ruleId = slot => baseline[slot] || `conjugation:${ruleNames[slot]}`;
const C = (surface, role, explanation) => ({ surface, role, explanation_vi:explanation });
const root = (surface, base, group='verb') => C(surface,'lexical_head',
  `Phần ${group==='verb'?'động từ':group==='i_adjective'?'tính từ い':group==='na_adjective'?'tính từ な':'danh từ'} của ${base} ở dạng quan sát này.`);
const p = (surface, role, explanation) => C(surface, role, explanation);
const endings = {
  plain_nonpast:[p('る','nonpast','Đuôi không quá khứ của dạng đang được phân tích.')],
  plain_past:[p('た','past','Đánh dấu quá khứ.')],
  plain_negative:[p('ない','negative','Phủ định, không quá khứ.')],
  plain_negative_past:[p('なかっ','negative','Phần phủ định nối với đuôi quá khứ.'),p('た','past','Đánh dấu quá khứ.')],
  polite_nonpast:[p('ます','polite','Lịch sự, không quá khứ.')],
  polite_past:[p('まし','polite','Phần lịch sự nối với quá khứ.'),p('た','past','Đánh dấu quá khứ.')],
  polite_negative:[p('ませ','polite','Phần lịch sự nối với phủ định.'),p('ん','negative','Phủ định trong chuỗi lịch sự.')],
  polite_negative_past:[p('ませ','polite','Phần lịch sự nối với phủ định.'),p('ん','negative','Phủ định trong chuỗi lịch sự.'),
    p('でし','polite_past_link','Phần lịch sự nối với quá khứ sau phủ định.'),p('た','past','Đánh dấu quá khứ.')]
};
const stateVi = { plain_nonpast:'không quá khứ, thông thường', plain_past:'quá khứ, thông thường',
  plain_negative:'phủ định, không quá khứ, thông thường', plain_negative_past:'phủ định, quá khứ, thông thường',
  polite_nonpast:'không quá khứ, lịch sự', polite_past:'quá khứ, lịch sự',
  polite_negative:'phủ định, không quá khứ, lịch sự', polite_negative_past:'phủ định, quá khứ, lịch sự' };
const cases = [];
const seen = new Set();
function alt(slot, base, components, id=ruleId(slot), meaning=meanings[slot]) {
  let pos=0;
  const segments=components.map(part => { const start=pos; pos+=part.surface.length;
    return { ...part, span:{start,end:pos,unit:'utf16'} }; });
  return { rule_id:id, rule_version:Object.values(baseline).includes(id)?2:1, slot_id:slot,
    word_group:fixtureMap.get(base).word_group, base_form:base,
    dictionary_fixture_id:fixtureMap.get(base).fixture_id,
    explanation_vi:`${meaning}. Dạng gốc: ${base}.`, language_tag:'vi',
    interpretation_label:'Cách hiểu có thể', segments };
}
function supported(id, slot, alternatives, tags=[], negativeFor=[]) {
  const text=alternatives[0].segments.map(s=>s.surface).join('');
  alternatives.sort((a,b)=>a.rule_id < b.rule_id ? -1 : a.rule_id > b.rule_id ? 1 : 0);
  if (alternatives.some(a=>a.segments.map(s=>s.surface).join('')!==text)) throw Error(`Surface mismatch ${id}`);
  if (seen.has(id)) throw Error(`Duplicate case ${id}`); seen.add(id);
  cases.push({case_id:id, slot_ids:[slot,...alternatives.map(a=>a.slot_id)].filter((s,i,a)=>a.indexOf(s)===i),
    tags, input:{text}, dictionary_fixture_ids:[...new Set(alternatives.map(a=>a.dictionary_fixture_id))],
    fixture_policy:'base_entries_only_exact_surface_absent', source_refs:slotMap.get(slot).source_refs,
    expected:{outcome:'supported',capability:'completed',span:{start:0,end:text.length,unit:'utf16'}, alternatives,
      contract2:alternatives.length>1?{count:0,capability:'unavailable'}:{count:1,capability:'completed'},
      contract3:{count:alternatives.length,capability:'completed'}, forbidden_rule_ids:negativeFor},
    review_state:'completed_proposal_pending_final_review'});
}
function unavailable(id, slot, text, reason, fixtureIds=[], forbidden=[ruleId(slot)], tags=[]) {
  if(seen.has(id)) throw Error(`Duplicate ${id}`); seen.add(id);
  cases.push({case_id:id,slot_ids:[slot],tags,input:{text},dictionary_fixture_ids:fixtureIds,
    fixture_policy:'base_entries_only_exact_surface_absent',source_refs:slotMap.get(slot).source_refs,
    expected:{outcome:'unavailable',capability:'unavailable',alternatives:[],reason,forbidden_rule_ids:forbidden},
    review_state:'completed_proposal_pending_final_review'});
}
function simple(id, slot, base, components, tags=[], meaning=meanings[slot], rid=ruleId(slot)) {
  supported(id,slot,[alt(slot,base,components,rid,meaning)],tags);
}

// Explicit lexeme paradigms: finite editorial examples, not an application suffix guesser.
const verb = {
  '食べる':{regular:'食べ',irrealis:'食べ',past:'食べ',te:'食べ',teParticle:'て',conditional:'食べれ',
    volitional:['食べ','よう'],imperative:'食べろ',passive:['食べ','られ'],causative:['食べ','させ'],causativePassive:['食べ','させ','られ']},
  '書く':{regular:'書き',irrealis:'書か',past:'書い',te:'書い',teParticle:'て',conditional:'書け',
    volitional:['書こ','う'],imperative:'書け',potential:['書け'],passive:['書か','れ'],causative:['書か','せ'],causativePassive:['書か','せ','られ']},
  '読む':{regular:'読み',irrealis:'読ま',past:'読ん',pastParticle:'だ',te:'読ん',teParticle:'で',conditional:'読め',
    volitional:['読も','う'],imperative:'読め',potential:['読め'],passive:['読ま','れ'],causative:['読ま','せ'],causativePassive:['読ま','せ','られ']},
  '行く':{regular:'行き',irrealis:'行か',past:'行っ',te:'行っ',teParticle:'て',conditional:'行け',volitional:['行こ','う'],imperative:'行け'},
  'する':{regular:'し',irrealis:'し',past:'し',te:'し',teParticle:'て',conditional:'すれ',volitional:['し','よう'],imperative:'しろ',
    passive:['さ','れ'],causative:['さ','せ'],causativePassive:['さ','せ','られ']},
  '来る':{regular:'来',irrealis:'来',past:'来',te:'来',teParticle:'て',conditional:'来れ',volitional:['来','よう'],imperative:'来い',
    passive:['来','られ'],causative:['来','させ'],causativePassive:['来','させ','られ']}
};
const families = { V10:'potential',V11:'passive',V12:'causative',V13:'causativePassive',V20:'progressive' };
function familyParts(slot,base) {
  const v=verb[base];
  if(slot==='V20') return [root(v.te,base),p(v.teParticle,'te_connective','Thể nối て/で trước いる.'),p('い','progressive','Phần いる biểu thị tiếp diễn hoặc trạng thái tùy câu.')];
  let values=v[families[slot]];
  if(slot==='V10' && (base==='食べる'||base==='来る')) values=v.passive;
  if(!values) throw Error(`No family ${slot} ${base}`);
  return values.map((value,i)=>i===0?root(value,base):p(value,
    (value==='させ'||value==='せ')?'causative':slot==='V10'?'potential':'passive',
    (value==='させ'||value==='せ')?'Phần sai khiến: cho phép, yêu cầu hoặc khiến làm, tùy câu.':slot==='V10'?
      'Phần khả năng trong cách hiểu này.':'Phần bị động trong cách hiểu này.'));
}
function familyAlt(slot,base,state='plain_nonpast') {
  const id=state==='plain_nonpast'?ruleId(slot):`${ruleId(slot)}:${state.replaceAll('_','-')}`;
  return alt(slot,base,[...familyParts(slot,base),...endings[state]],id,
    `${meanings[slot]}; ${stateVi[state]}`);
}
function alternateReadings(slot,base,state='plain_nonpast') {
  const primary=familyAlt(slot,base,state);
  if(slot==='V11'&&(base==='食べる'||base==='来る'))return[primary,familyAlt('V10',base,state)];
  if(slot==='V13'){
    // Inference from the referenced ichidan potential paradigm applied to a causative form.
    // This is a new editorial answer requiring final human review, not a provider diagnosis.
    const id=state==='plain_nonpast'?'conjugation:verb-causative-potential':
      `conjugation:verb-causative-potential:${state.replaceAll('_','-')}`;
    const components=familyParts(slot,base).map(part=>part.surface==='られ'?
      p('られ','potential','Phần khả năng của dạng sai khiến trong cách hiểu này.'):part);
    return[primary,alt(slot,base,[...components,...endings[state]],id,
      `Sai khiến kết hợp khả năng: có thể cho phép/khiến thực hiện hành động; ${stateVi[state]}`)];
  }
  return[primary];
}
const coreVerbs=['食べる','書く'];
for(const slot of slots.filter(s=>s.slot_id.startsWith('V')&&!s.slot_id.endsWith('00')).map(s=>s.slot_id)) {
  const bases=slot==='V10'?['書く','読む']:slot==='V17'?['食べる','読む']:coreVerbs;
  for(let i=0;i<bases.length;i++) {
    const base=bases[i],v=verb[base]; let components;
    if(families[slot]) {
      const alts=alternateReadings(slot,base);
      supported(`${slot}-P${i+1}`,slot,alts,['core',...(alts.length>1?['ambiguous']:[])]); continue;
    }
    if(slot==='V01')components=[root(v.regular,base),...endings.polite_nonpast];
    else if(slot==='V02')components=[root(v.regular,base),...endings.polite_past];
    else if(slot==='V03')components=[root(v.regular,base),...endings.polite_negative];
    else if(slot==='V04')components=[root(v.regular,base),...endings.polite_negative_past];
    else if(slot==='V05')components=[root(v.past,base),p(v.pastParticle||'た','past',v.pastParticle==='だ'?'Đuôi quá khứ だ sau biến âm mũi.':'Đánh dấu quá khứ.')];
    else if(slot==='V06')components=[root(v.irrealis,base),...endings.plain_negative];
    else if(slot==='V07')components=[root(v.irrealis,base),...endings.plain_negative_past];
    else if(slot==='V08')components=[root(v.te,base),p(v.teParticle,'te_connective','Thể nối て/で; quan hệ với phần tiếp theo phụ thuộc câu.')];
    else if(slot==='V09')components=[root(v.irrealis,base),p('なく','negative','Phủ định ở dạng nối.'),p('て','connective','Nối với phần câu tiếp theo.')];
    else if(slot==='V14')components=[root(v.conditional,base),p('ば','conditional','Đánh dấu điều kiện ば.')];
    else if(slot==='V15')components=[root(v.past,base),p(v.pastParticle==='だ'?'だら':'たら','conditional','Đánh dấu điều kiện たら/だら.')];
    else if(slot==='V16')components=[root(v.volitional[0],base),p(v.volitional[1],'volitional','Ý chí, dự định hoặc đề nghị cùng làm tùy câu.')];
    else if(slot==='V17')components=[p(v.imperative,'imperative',`Toàn bộ dạng mệnh lệnh của ${base}.`)];
    else if(slot==='V18')components=[root(base,base),p('な','prohibition','Cấm thực hiện hành động; đừng …')];
    else if(slot==='V19')components=[root(v.regular,base),p('たい','desiderative','Mong muốn thực hiện hành động.')];
    else throw Error(slot);
    simple(`${slot}-P${i+1}`,slot,base,components,['core']);
  }
}

// All eight polarity/tense/politeness states for the approved advanced families.
for(const slot of Object.keys(families)) {
  const bases=slot==='V10'?['書く','読む']:['食べる','書く'];
  for(const state of Object.keys(endings).filter(s=>s!=='plain_nonpast')) for(let i=0;i<2;i++) {
    const base=bases[i];const alts=alternateReadings(slot,base,state);
    supported(`${slot}-C-${state}-P${i+1}`,slot,alts,['composition',state,...(alts.length>1?['ambiguous']:[])]);
  }
}
// たい follows an adjective-like paradigm; these are explicit reviewed-scope proposals.
const desireEnds={plain_past:['たかっ','た'],plain_negative:['たく','ない'],plain_negative_past:['たく','なかっ','た'],
  polite_nonpast:['たい','です'],polite_past:['たかっ','た','です'],polite_negative:['たく','ない','です'],
  polite_negative_past:['たく','なかっ','た','です']};
for(const [state,parts] of Object.entries(desireEnds)) for(let i=0;i<2;i++) {
  const base=coreVerbs[i]; const rid=`${ruleId('V19')}:${state.replaceAll('_','-')}`;
  const comp=[root(verb[base].regular,base),...parts.map((part,j)=>p(part,j===0?'desiderative':part==='です'?'polite':part==='た'?'past':'negative',
    j===0?'Phần mong muốn たい ở dạng nối tương ứng.':part==='です'?'Lịch sự.':part==='た'?'Quá khứ.':'Phần phủ định.'))];
  simple(`V19-C-${state}-P${i+1}`,'V19',base,comp,['composition',state],`${meanings.V19}; ${stateVi[state]}`,rid);
}

// Irregular and sound-change examples supplement (do not replace) core coverage.
for(const base of ['する','来る']) {
  const v=verb[base]; const tag=base==='する'?'suru':'kuru';
  const bySlot={V01:[root(v.regular,base),...endings.polite_nonpast],V02:[root(v.regular,base),...endings.polite_past],
    V03:[root(v.regular,base),...endings.polite_negative],V04:[root(v.regular,base),...endings.polite_negative_past],
    V05:[root(v.past,base),p('た','past','Quá khứ.')],V06:[root(v.irrealis,base),...endings.plain_negative],
    V07:[root(v.irrealis,base),...endings.plain_negative_past],V08:[root(v.te,base),p('て','te_connective','Thể nối て.')],
    V09:[root(v.irrealis,base),p('なく','negative','Phủ định ở dạng nối.'),p('て','connective','Nối câu.')],
    V14:[root(v.conditional,base),p('ば','conditional','Điều kiện ば.')],V15:[root(v.past,base),p('たら','conditional','Điều kiện たら.')],
    V16:[root(v.volitional[0],base),p(v.volitional[1],'volitional','Ý chí/đề nghị tùy câu.')],
    V17:[p(v.imperative,'imperative',`Dạng mệnh lệnh bất quy tắc của ${base}.`)],
    V18:[root(base,base),p('な','prohibition','Cấm thực hiện hành động.')],
    V19:[root(v.regular,base),p('たい','desiderative','Mong muốn.')],
    V20:[...familyParts('V20',base),...endings.plain_nonpast]};
  for(const [slot,comp] of Object.entries(bySlot)) simple(`${slot}-IRR-${tag}`,slot,base,comp,['irregular']);
  for(const slot of ['V11','V12','V13']) {
    const aa=alternateReadings(slot,base);
    supported(`${slot}-IRR-${tag}`,slot,aa,['irregular',...(aa.length>1?['ambiguous']:[])]);
  }
}
simple('V10-IRR-suru','V10','する',[p('できる','irregular_potential','Dạng khả năng bất quy tắc của する: có thể làm.')],['irregular','lexical_relation']);
supported('V10-IRR-kuru','V10',[familyAlt('V10','来る'),familyAlt('V11','来る')],['irregular','ambiguous']);
for(const base of ['行く','読む']) simple(`V08-ONBIN-${base}`,'V08',base,
  [root(verb[base].te,base),p(verb[base].teParticle,'te_connective','Biến thể thể nối sau biến âm.')],['sound_change']);
simple('V05-ONBIN-yomu','V05','読む',[root('読ん','読む'),p('だ','past','Quá khứ sau biến âm mũi.')],['sound_change']);
for(let i=0;i<2;i++) {const base=coreVerbs[i];simple(`V09-WITHOUT-P${i+1}`,'V09',base,
  [root(verb[base].irrealis,base),p('ない','negative','Phủ định.'),p('で','without','Không làm hành động này; quan hệ với phần tiếp theo tùy câu.')],
  ['variant','naide'],'Dạng ないで: không thực hiện hành động, phân biệt với なくて',`${ruleId('V09')}:without`);}
for(const [base,stem] of [['書く','書か'],['読む','読ま']])simple(`V13-SHORT-${base}`,'V13',base,
  [root(stem,base),p('され','causative_passive','Dạng rút gọn sai khiến-bị động ở cách hiểu này.'),p('る','nonpast','Không quá khứ.')],
  ['variant','contracted'],'Sai khiến-bị động dạng rút gọn, không quá khứ',`${ruleId('V13')}:contracted`);

const adjParts=(slot,base,variant='default')=>{
  const stem=base==='よい'?'よ':base.slice(0,-1); const group='i_adjective';
  const r=s=>root(s,base,group);const past=p('た','past','Quá khứ.');
  const neg=p('ない','negative','Phủ định, không quá khứ.');const np=p('なかっ','negative','Phần phủ định nối với quá khứ.');
  const polite=p('です','polite','Lịch sự.');const arp=[p('あり','negative_polite_link','Phần ある trong cách phủ định lịch sự.'),...endings.polite_negative];
  if(slot==='I01')return[r(stem+'かっ'),past];if(slot==='I02')return[r(stem+'く'),neg];
  if(slot==='I03')return[r(stem+'く'),np,past];if(slot==='I04')return[r(base),polite];
  if(slot==='I05')return[r(stem+'かっ'),past,polite];
  if(slot==='I06')return variant==='arimasen'?[r(stem+'く'),...arp]:[r(stem+'く'),neg,polite];
  if(slot==='I07')return variant==='arimasen'?[r(stem+'く'),...arp,...endings.polite_negative_past.slice(2)]:[r(stem+'く'),np,past,polite];
  if(slot==='I08')return[r(stem+'く'),p('て','connective','Nối với phần tiếp theo.')];
  if(slot==='I09')return[r(stem+'く')];if(slot==='I10')return[r(stem+'けれ'),p('ば','conditional','Điều kiện ば.')];
  if(slot==='I11')return[r(stem+'かっ'),p('たら','conditional','Điều kiện たら.')];throw Error(slot);
};
for(const slot of slots.filter(s=>s.slot_id.startsWith('I')&&!s.slot_id.endsWith('00')).map(s=>s.slot_id)) {
  for(let i=0;i<2;i++)simple(`${slot}-P${i+1}`,slot,['高い','安い'][i],adjParts(slot,['高い','安い'][i]),['core']);
  if(slot==='I06'||slot==='I07') for(const base of ['高い','安い'])simple(`${slot}-ALT-${base}`,slot,base,adjParts(slot,base,'arimasen'),['variant','arimasen']);
  if(['I01','I02','I03','I08','I09','I10','I11'].includes(slot))simple(`${slot}-IRR-ii`,slot,'よい',adjParts(slot,'よい'),['irregular','ii_yoi'],`${meanings[slot]}; よい và いい là hai dạng tương ứng của tính từ này`);
}
const copula = {
  '01':[['だ','copula_nonpast','Copula khẳng định thông thường, không quá khứ.']],
  '02':[['だっ','copula_link','Copula nối với quá khứ.'],['た','past','Quá khứ.']],
  '03':[['じゃ','copula_negative_link','Dạng nói rút gọn của では trước phủ định.'],['ない','negative','Phủ định.']],
  '04':[['じゃ','copula_negative_link','Dạng nói rút gọn của では.'],['なかっ','negative','Phủ định nối quá khứ.'],['た','past','Quá khứ.']],
  '05':[['です','polite_copula','Copula khẳng định lịch sự, không quá khứ.']],
  '06':[['でし','polite_copula_link','Copula lịch sự nối quá khứ.'],['た','past','Quá khứ.']],
  '07':[['じゃ','copula_negative_link','Dạng nói của では.'],['ない','negative','Phủ định.'],['です','polite','Lịch sự.']],
  '08':[['じゃ','copula_negative_link','Dạng nói của では.'],['なかっ','negative','Phủ định nối quá khứ.'],['た','past','Quá khứ.'],['です','polite','Lịch sự.']]
};
function copulaParts(slot,base,variant='default') {
  const group=slot.startsWith('A')?'na_adjective':'noun_copula';
  const n=slot.slice(1);let tail;
  if(copula[n]) tail=copula[n].map(a=>[...a]);
  else if(slot==='A09')tail=[['な','attributive','Nối tính từ な với danh từ theo sau.']];
  else if(slot==='A10')tail=[['に','adverbial','Chuyển tính từ な sang cách dùng trạng từ.']];
  else if(slot==='A11'||slot==='N11')tail=[['で','copula_connective','Ở cách hiểu copula, nối với phần câu theo sau; cần ngữ cảnh để phân biệt trợ từ で.']];
  else if(slot==='A12'||slot==='N09')tail=[['なら','conditional','Điều kiện なら.']];
  else if(slot==='A13'||slot==='N10')tail=[['だっ','copula_link','Copula nối với điều kiện.'],['たら','conditional','Điều kiện たら.']];
  else throw Error(slot);
  if(variant==='dewa')tail[0]=['では','copula_negative_link','Phần では trong phủ định; じゃ là dạng nói tương ứng.'];
  if(variant==='ja_arimasen'||variant==='dewa_arimasen') {
    tail=[variant==='dewa_arimasen'?['では','copula_negative_link','Phần では trước phủ định.']:['じゃ','copula_negative_link','Dạng nói của では trước phủ định.'],
      ['あり','negative_polite_link','Phần ある dùng trong cách nói phủ định lịch sự.'],['ませ','polite','Lịch sự trước phủ định.'],['ん','negative','Phủ định.']];
    if(n==='08')tail.push(['でし','polite_past_link','Phần lịch sự nối với quá khứ.'],['た','past','Quá khứ.']);
  }
  return [root(base,base,group),...tail.map(([s,r,e])=>p(s,r,e))];
}
for(const prefix of ['A','N']) for(const slot of slots.filter(s=>s.slot_id.startsWith(prefix)&&!s.slot_id.endsWith('00')).map(s=>s.slot_id)) {
  const bases=prefix==='A'?['静か','便利']:['学生','先生'];
  for(let i=0;i<2;i++)simple(`${slot}-P${i+1}`,slot,bases[i],copulaParts(slot,bases[i]),['core']);
  if(['03','04'].includes(slot.slice(1)))for(const base of bases)simple(`${slot}-ALT-dewa-${base}`,slot,base,copulaParts(slot,base,'dewa'),['variant','dewa']);
  if(['07','08'].includes(slot.slice(1)))for(const variant of ['ja_arimasen','dewa_arimasen'])for(const base of bases)
    simple(`${slot}-ALT-${variant}-${base}`,slot,base,copulaParts(slot,base,variant),['variant',variant]);
}
simple('POS-01','A01','きれい',copulaParts('A01','きれい'),['cross_group','not_i_adjective']);
simple('UNI-02','N05','𠮷',copulaParts('N05','𠮷'),['unicode','supplementary']);
simple('BND-01','N08','学生',copulaParts('N08','学生','dewa_arimasen'),['long_chain','provider_layout']);
simple('AMB-02','V17','書く',[p('書け','imperative','Dạng mệnh lệnh trực tiếp của 書く.')],['provider_base_mismatch']);
supported('AMB-01','V11',[familyAlt('V11','食べる'),familyAlt('V10','食べる')],['ambiguous','reader_choice']);

// Exact dictionary hits are a separate fixture setup; all derived-entry positives above use base-only setup.
for(const [slot,bases] of [['V00',['食べる','書く']],['I00',['高い','安い']],['A00',['静か','便利']],['N00',['学生','先生']]])
  for(let i=0;i<2;i++){const base=bases[i];cases.push({case_id:`${slot}-P${i+1}`,slot_ids:[slot],tags:['exact_first'],input:{text:base},
    dictionary_fixture_ids:[fixtureMap.get(base).fixture_id],fixture_policy:'exact_surface_present',source_refs:slotMap.get(slot).source_refs,
    expected:{outcome:'exact_first',capability:'unavailable',alternatives:[],dictionary_match_ids:[fixtureMap.get(base).canonical_id]},review_state:'pending_final_review'});}
for(const base of ['書ける','できる'])cases.push({case_id:`EXACT-DERIVED-${base}`,slot_ids:['V10'],tags:['exact_first','derived_entry'],input:{text:base},
  dictionary_fixture_ids:[fixtureMap.get(base).fixture_id],fixture_policy:'exact_surface_present',source_refs:['f09-project-editorial-b'],
  expected:{outcome:'exact_first',capability:'unavailable',alternatives:[],dictionary_match_ids:[fixtureMap.get(base).canonical_id]},review_state:'pending_final_review'});

// The previous table had explanatory annotations mixed into some input cells. Only actual Japanese is retained.
const rows=reviewedText.split(/\r?\n/).map(l=>l.match(/^\| ([VIAN]\d{2}) \| (.*?) \| (.*?) \| (.*?) \|$/)).filter(Boolean);
if(rows.length!==59)throw Error('Unexpected reviewed table shape');
for(const m of rows.filter(m=>!m[1].endsWith('00'))) {
  const slot=m[1],text=m[4].replace(/\s*[（(].*$/u,'').trim();
  const group=groups[slot[0]];let opposite;
  if(slot==='I09')opposite=alt('A10','きれい',copulaParts('A10','きれい'));
  else if(text.startsWith('学生') && slot.startsWith('A')&&!['A09','A10'].includes(slot)) {
    const dest=slot==='A11'?'N11':slot==='A12'?'N09':slot==='A13'?'N10':`N${slot.slice(1)}`;
    opposite=alt(dest,'学生',copulaParts(dest,'学生',text.includes('ではありません')?'dewa_arimasen':'default'));
  } else if(text.startsWith('静か')&&slot.startsWith('N')) {
    const dest=slot==='N09'?'A12':slot==='N10'?'A13':`A${slot.slice(1)}`;
    opposite=alt(dest,'静か',copulaParts(dest,'静か',text.includes('ではありません')?'dewa_arimasen':'default'));
  }
  if(opposite){supported(`${slot}-N1`,slot,[opposite],['negative_for_slot','cross_group'],[ruleId(slot)]);
    if(cases.at(-1).input.text!==text)throw Error(`Negative text mismatch ${slot}: ${text}`);}
  else unavailable(`${slot}-N1`,slot,text,text.endsWith('。')?'punctuation_not_whole_word':'not_this_word_group_or_suffix',[],[ruleId(slot)],['negative_for_slot']);
}
// Each additional composed/variant rule gets its own negative boundary case.
const allRules=[...new Set(cases.flatMap(c=>c.expected.alternatives.map(a=>a.rule_id)))];
for(const rid of allRules){const positives=cases.filter(c=>c.expected.alternatives.some(a=>a.rule_id===rid));
  if(!cases.some(c=>c.expected.forbidden_rule_ids?.includes(rid))){const first=positives[0];
    unavailable(`NEG-RULE-${rid.replace('conjugation:','')}`,first.slot_ids[0],first.input.text+'。',
      'punctuation_not_whole_word',first.dictionary_fixture_ids,[rid],['negative_for_rule']);}}

const faults=[
  ['MIS-01','missing_base_entry','completed_empty','completed',[]],
  ['ERR-01','sidecar_unavailable','unavailable','unavailable',['fixture:001']],
  ['ERR-02','sidecar_timeout_500ms','unavailable','unavailable',['fixture:001']],
  ['DATA-01','dataset_missing','unavailable','unavailable',['fixture:001']],
  ['VI-01','vietnamese_content_missing','unavailable','unavailable',['fixture:001']],
  ['DB-01','dictionary_repository_failure','failed','failed',[]]
];
for(const [id,fault,outcome,capability,fixtureIds]of faults)cases.push({case_id:id,slot_ids:['V02'],tags:['fault'],input:{text:'食べました'},
  dictionary_fixture_ids:fixtureIds,fixture_policy:fixtureIds.length?'base_entries_only_exact_surface_absent':'base_entry_absent',
  scenario:{fault},source_refs:['f09-project-editorial-b'],expected:{outcome,capability,alternatives:[]},review_state:'pending_final_review'});
unavailable('UNI-01','N05','𠮷さんです','multiple_lexical_units_not_whole_word',[],[],['unicode','word_boundary']);
unavailable('SENT-01','V02','昨日食べました','multiple_lexical_units_not_whole_word',[],[],['word_boundary']);
cases.push({case_id:'ASYNC-01',slot_ids:['V11','V10'],tags:['async'],input:{text:'食べられる'},
  dictionary_fixture_ids:['fixture:001'],fixture_policy:'base_entries_only_exact_surface_absent',source_refs:['f09-project-editorial-b'],
  scenario:{fault:'selection_changed_before_response',next_text:'学生です'},expected:{outcome:'discarded',alternatives:[],render_old_result:false},review_state:'pending_final_review'});
cases.push({case_id:'CANCEL-01',slot_ids:['V02'],tags:['async'],input:{text:'食べました'},dictionary_fixture_ids:['fixture:001'],
  fixture_policy:'base_entries_only_exact_surface_absent',source_refs:['f09-project-editorial-b'],scenario:{fault:'caller_cancelled'},
  expected:{outcome:'cancelled',alternatives:[],success_partial:false},review_state:'pending_final_review'});

for(const slot of slots){slot.case_ids=cases.filter(c=>c.slot_ids.includes(slot.slot_id)).map(c=>c.case_id);
  slot.proposed_rule_id=slot.slot_id.endsWith('00')?null:ruleId(slot.slot_id);
  if(baseline[slot.slot_id]){slot.proposal='revise_editorial_text';slot.baseline_rule_version=1;slot.proposed_rule_version=2;}
  else slot.proposed_rule_version=slot.slot_id.endsWith('00')?null:1;
  slot.variants=[...new Set(cases.filter(c=>c.slot_ids.includes(slot.slot_id)).flatMap(c=>c.tags.filter(t=>!['core','negative_for_slot','negative_for_rule'].includes(t))))];}
const ruleCatalog=allRules.map(rid=>({rule_id:rid,rule_version:Object.values(baseline).includes(rid)?2:1,
  slot_ids:[...new Set(cases.flatMap(c=>c.expected.alternatives.filter(a=>a.rule_id===rid).map(a=>a.slot_id)))],
  positive_case_ids:cases.filter(c=>c.expected.alternatives.some(a=>a.rule_id===rid)).map(c=>c.case_id),
  negative_case_ids:cases.filter(c=>c.expected.forbidden_rule_ids?.includes(rid)).map(c=>c.case_id),
  proposal_status:Object.values(baseline).includes(rid)?'revise_editorial_text':'add',
  language_review:'pending_final_packet_review'}));
const corpus={schema_version:1,corpus_version:version,status:'complete_candidate_pending_final_review',
  expected_policy:'Semantic answers and component declarations authored before Sidecar observation; actual output cannot rewrite expected.',
  input_policy:'Synthetic public examples only',dictionary_fixtures:fixtures,cases};
const manifest={schema_version:1,corpus_version:version,status:'complete_candidate_pending_final_review',
  baseline_dataset_version:oldManifest.baseline_dataset_version,supersedes:'f09-expanded-20261013.1-draft',
  baseline_live_examples:baselineLiveExamples,
  composition_families:['V10','V11','V12','V13','V19','V20'],composition_states:Object.keys(endings),
  excluded:[{id:'generic_morphology',reason:'Separate feature'}, {id:'sentence_segmentation',reason:'Separate feature'},
    {id:'keigo_interpretations',reason:'This reviewed inventory covers potential/passive, not honorific/spontaneous interpretations'},
    {id:'literary_imperative_yo_seyo',reason:'Modern ordinary ろ/しろ/来い examples selected; literary register is not this corpus target'},
    {id:'open_ended_recursive_compositions',reason:'Only finite listed eight-state combinations are promised'}],
  slots,rules:ruleCatalog,cases_file:'cases.json',sources_file:'sources.json',scope_material_ref:'scope-material.json',
  cases_sha256:null,sources_sha256:null,scope_material_sha256:null,
  approval:{state:'pending_final_review',approver:null,approved_at_utc:null,reviewer_id:null,
    prior_receipt:'../../reviews/user-review-receipt-20261013.json',note:'Previous conceptual packet approved; this completed revision contains new component/case details.'}};
const section=process.argv[2]||'summary';const group=process.argv[3];
const result=section==='case_batch'?cases.slice(Number(process.argv[3]||0),Number(process.argv[3]||0)+Number(process.argv[4]||50)):
  section==='cases'?{...corpus,cases:group?cases.filter(c=>c.slot_ids[0].startsWith(group)):cases}:
  section==='manifest'?manifest:section==='fixtures'?fixtures:section==='summary'?{
    corpus_version:version,slots:slots.length,rules:ruleCatalog.length,cases:cases.length,
    outcomes:Object.fromEntries([...new Set(cases.map(c=>c.expected.outcome))].map(o=>[o,cases.filter(c=>c.expected.outcome===o).length])),
    minimum_positive:Math.min(...ruleCatalog.map(r=>r.positive_case_ids.length)),minimum_negative:Math.min(...ruleCatalog.map(r=>r.negative_case_ids.length))}:null;
if(result===null)throw Error(`Unknown section ${section}`);
process.stdout.write(JSON.stringify(result));
