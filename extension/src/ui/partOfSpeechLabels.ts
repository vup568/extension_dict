// Imported entries carry expanded JMdict entity descriptions; fixtures and older data carry the short entity codes.
const exact: Record<string, string> = {
  "n": "Danh từ", "noun (common) (futsuumeishi)": "Danh từ",
  "n-t": "Danh từ thời gian", "noun (temporal) (jisoumeishi)": "Danh từ thời gian",
  "n-adv": "Danh từ làm trạng từ", "adverbial noun (fukushitekimeishi)": "Danh từ làm trạng từ",
  "n-pref": "Danh từ làm tiền tố", "noun, used as a prefix": "Danh từ làm tiền tố",
  "n-suf": "Danh từ làm hậu tố", "noun, used as a suffix": "Danh từ làm hậu tố",
  "pn": "Đại từ", "pronoun": "Đại từ",
  "vs": "Đi với する", "noun or participle which takes the aux. verb suru": "Đi với する",
  "vt": "Tha động từ", "transitive verb": "Tha động từ",
  "vi": "Tự động từ", "intransitive verb": "Tự động từ",
  "vk": "Động từ くる", "Kuru verb - special class": "Động từ くる",
  "adj-i": "Tính từ đuôi い", "adjective (keiyoushi)": "Tính từ đuôi い",
  "adj-ix": "Tính từ đuôi い", "adjective (keiyoushi) - yoi/ii class": "Tính từ đuôi い",
  "adj-na": "Tính từ đuôi な", "adjectival nouns or quasi-adjectives (keiyodoshi)": "Tính từ đuôi な",
  "adj-no": "Đi với の", "nouns which may take the genitive case particle 'no'": "Đi với の",
  "adj-pn": "Liên thể từ", "pre-noun adjectival (rentaishi)": "Liên thể từ",
  "adj-t": "Tính từ đuôi たる", "'taru' adjective": "Tính từ đuôi たる",
  "adv": "Trạng từ", "adverb (fukushi)": "Trạng từ",
  "adv-to": "Trạng từ đi với と", "adverb taking the 'to' particle": "Trạng từ đi với と",
  "prt": "Trợ từ", "particle": "Trợ từ",
  "aux": "Trợ từ phụ", "auxiliary": "Trợ từ phụ",
  "aux-v": "Trợ động từ", "auxiliary verb": "Trợ động từ",
  "aux-adj": "Trợ tính từ", "auxiliary adjective": "Trợ tính từ",
  "conj": "Liên từ", "conjunction": "Liên từ",
  "cop": "Hệ từ", "copula": "Hệ từ",
  "int": "Thán từ", "interjection (kandoushi)": "Thán từ",
  "exp": "Cụm từ", "expressions (phrases, clauses, etc.)": "Cụm từ",
  "ctr": "Lượng từ", "counter": "Lượng từ",
  "num": "Số từ", "numeric": "Số từ",
  "pref": "Tiền tố", "prefix": "Tiền tố",
  "suf": "Hậu tố", "suffix": "Hậu tố",
  "unc": "Chưa phân loại", "unclassified": "Chưa phân loại",
};

/** Vietnamese label for one JMdict part of speech; unknown values stay visible unchanged instead of being guessed. */
export function partOfSpeechLabel(value: string): string {
  const known = exact[value];
  if (known) return known;
  if (/^v5/u.test(value) || value.startsWith("Godan verb")) return "Động từ nhóm 1";
  if (/^v1/u.test(value) || value.startsWith("Ichidan verb")) return "Động từ nhóm 2";
  if (/^vs-/u.test(value) || value.startsWith("suru verb")) return "Động từ する";
  return value;
}
