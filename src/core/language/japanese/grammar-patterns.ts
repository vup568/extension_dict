/**
 * Offline JLPT N5–N4 grammar-pattern rules, matched against the kuromoji
 * IPADIC token stream (see grammar-matcher.ts).
 *
 * Every matcher constraint was written against REAL tokenizer output (see
 * scripts/test-grammar.ts, which asserts each rule's example still matches).
 * Vietnamese descriptions are our own wording; the point list and formation
 * coverage were cross-checked against the CC BY-SA 4.0 dataset
 * jkindrix/japanese-language-data (see README licensing notes).
 *
 * Matching model: a rule is an ordered list of token matchers over
 * consecutive tokens; every non-optional matcher must match. The matcher
 * engine prefers the longest match at each position, so 〜たことがある wins
 * over plain 〜た, 〜ましょう over 〜ます, and so on.
 */

/** Constraints one token must satisfy. Absent field = no constraint. */
export interface TokenMatcher {
  /** Exact surface form is one of these. */
  readonly surface?: readonly string[]
  /** basic_form (dictionary form) is one of these. */
  readonly base?: readonly string[]
  /** 品詞 (pos) is one of these. */
  readonly pos?: readonly string[]
  /** 品詞細分類1 (pos_detail_1) is one of these. */
  readonly detail?: readonly string[]
  /** 活用形 (conjugated_form) is one of these. */
  readonly conj?: readonly string[]
  /** This slot may be skipped when it does not match. */
  readonly optional?: boolean
}

export interface GrammarRule {
  readonly id: string
  /** Canonical form shown to the user, e.g. 〜くなる. */
  readonly display: string
  readonly level: 'N5' | 'N4'
  /** Vietnamese explanation (our own wording). */
  readonly description: string
  readonly matchers: readonly TokenMatcher[]
  /** A sentence that must trigger this rule — used by scripts/test-grammar.ts. */
  readonly example: string
}

// Shared matcher fragments -------------------------------------------------

/** Connective て/で particle. */
const TE: TokenMatcher = { surface: ['て', 'で'], pos: ['助詞'], detail: ['接続助詞'] }
/** ませ in ません — negative stem of ます. */
const MASU_NEG: TokenMatcher = { base: ['ます'], conj: ['未然形'] }
/** Contracted negative ん (as in ません). */
const NN: TokenMatcher = { base: ['ん'], pos: ['助動詞'] }
/** Optional trailing negative (ない or ません) after a base like いける/なる. */
const OPT_NAI: TokenMatcher = { base: ['ない'], optional: true }
const OPT_MASE: TokenMatcher = { base: ['ます'], conj: ['未然形'], optional: true }
const OPT_NN: TokenMatcher = { base: ['ん'], pos: ['助動詞'], optional: true }
/**
 * A CONTENT noun (excludes 非自立 nouns like こと/よう/ため, so the generic
 * 〜になる/〜にする rules don't shadow ことになる/ようになる etc.).
 */
const CONTENT_NOUN: TokenMatcher = {
  pos: ['名詞'],
  detail: ['一般', '形容動詞語幹', 'サ変接続', '固有名詞', '副詞可能'],
}

export const GRAMMAR_RULES: readonly GrammarRule[] = [
  // --- Polite forms, copula, tense (N5) -----------------------------------
  {
    id: 'masendeshita',
    display: '〜ませんでした',
    level: 'N5',
    description: 'Quá khứ phủ định lịch sự: "đã không ~".',
    matchers: [MASU_NEG, NN, { base: ['です'] }, { base: ['た'] }],
    example: '食べませんでした',
  },
  {
    id: 'mashita',
    display: '〜ました',
    level: 'N5',
    description: 'Quá khứ lịch sự của động từ: "đã ~".',
    matchers: [{ base: ['ます'], conj: ['連用形'] }, { base: ['た'] }],
    example: '食べました',
  },
  {
    id: 'masen',
    display: '〜ません',
    level: 'N5',
    description: 'Phủ định lịch sự: "không ~".',
    matchers: [MASU_NEG, NN],
    example: '行きません',
  },
  {
    id: 'mashou',
    display: '〜ましょう',
    level: 'N5',
    description: 'Rủ rê / đề nghị lịch sự: "cùng ~ nào".',
    matchers: [{ base: ['ます'], conj: ['未然ウ接続'] }, { base: ['う'] }],
    example: '行きましょう',
  },
  {
    id: 'masu',
    display: '〜ます',
    level: 'N5',
    description: 'Đuôi lịch sự của động từ (hiện tại/tương lai).',
    matchers: [{ base: ['ます'], pos: ['助動詞'] }],
    example: '食べます',
  },
  {
    id: 'deshita',
    display: '〜でした',
    level: 'N5',
    description: 'Quá khứ của です: "đã là ~".',
    matchers: [{ base: ['です'], conj: ['連用形'] }, { base: ['た'] }],
    example: '学生でした',
  },
  {
    id: 'deshou',
    display: '〜でしょう',
    level: 'N5',
    description: 'Phỏng đoán lịch sự: "có lẽ ~ nhỉ".',
    matchers: [{ base: ['です'], conj: ['未然形'] }, { base: ['う'] }],
    example: '明日は晴れるでしょう',
  },
  {
    id: 'desu',
    display: '〜です',
    level: 'N5',
    description: 'Trợ từ khẳng định lịch sự: "là / thì".',
    matchers: [{ base: ['です'], pos: ['助動詞'] }],
    example: '学生です',
  },
  {
    id: 'janai',
    display: '〜じゃない／じゃありません',
    level: 'N5',
    description: 'Phủ định của danh từ/tính từ な: "không phải là ~".',
    matchers: [{ surface: ['じゃ'], pos: ['助詞'] }, { base: ['ある', 'ない'] }, OPT_MASE, OPT_NN],
    example: '学生じゃありません',
  },
  {
    id: 'darou',
    display: '〜だろう',
    level: 'N4',
    description: 'Phỏng đoán thể thường: "có lẽ ~".',
    matchers: [{ base: ['だ'], conj: ['未然形'] }, { base: ['う'] }],
    example: '明日は雨だろう',
  },
  {
    id: 'datta',
    display: '〜だった',
    level: 'N5',
    description: 'Quá khứ thể thường của だ: "đã là ~".',
    matchers: [{ base: ['だ'], conj: ['連用タ接続'] }, { base: ['た'], conj: ['基本形'] }],
    example: '学生だった',
  },
  {
    id: 'da',
    display: '〜だ',
    level: 'N5',
    description: 'Trợ từ khẳng định thể thường: "là".',
    matchers: [{ base: ['だ'], surface: ['だ'] }],
    example: '学生だ',
  },
  {
    id: 'katta',
    display: '〜かった',
    level: 'N5',
    description: 'Quá khứ của tính từ đuôi い: "đã ~".',
    matchers: [{ pos: ['形容詞'], conj: ['連用タ接続'] }, { base: ['た'], conj: ['基本形'] }],
    example: '寒かったです',
  },
  {
    id: 'kunai',
    display: '〜くない',
    level: 'N5',
    description: 'Phủ định của tính từ đuôi い: "không ~".',
    matchers: [{ pos: ['形容詞'], conj: ['連用テ接続'] }, { base: ['ない'] }],
    example: '寒くないです',
  },
  {
    id: 'nakatta',
    display: '〜なかった',
    level: 'N5',
    description: 'Quá khứ phủ định thể thường: "đã không ~".',
    matchers: [{ base: ['ない'], conj: ['連用タ接続'] }, { base: ['た'], conj: ['基本形'] }],
    example: '食べなかった',
  },
  {
    id: 'nai',
    display: '〜ない',
    level: 'N5',
    description: 'Phủ định thể thường: "không ~".',
    matchers: [{ base: ['ない'], pos: ['助動詞'], conj: ['基本形'] }],
    example: '食べない',
  },
  {
    id: 'ta',
    display: '〜た',
    level: 'N5',
    description: 'Quá khứ thể thường: "đã ~".',
    matchers: [{ base: ['た'], pos: ['助動詞'], conj: ['基本形'] }],
    example: '食べた',
  },

  // --- て-form family ------------------------------------------------------
  {
    id: 'teiru',
    display: '〜ている',
    level: 'N5',
    description: 'Đang làm ~ / trạng thái đang duy trì.',
    matchers: [TE, { base: ['いる'], pos: ['動詞'], detail: ['非自立'] }],
    example: '食べています',
  },
  {
    id: 'tearu',
    display: '〜てある',
    level: 'N4',
    description: 'Trạng thái có sẵn do ai đó đã chuẩn bị.',
    matchers: [TE, { base: ['ある'], pos: ['動詞'], detail: ['非自立'] }],
    example: '名前を書いてあります',
  },
  {
    id: 'teoku',
    display: '〜ておく',
    level: 'N4',
    description: 'Làm sẵn trước (chuẩn bị cho sau này).',
    matchers: [TE, { base: ['おく'], detail: ['非自立'] }],
    example: '準備しておきます',
  },
  {
    id: 'temiru',
    display: '〜てみる',
    level: 'N4',
    description: 'Thử làm ~ xem sao.',
    matchers: [TE, { base: ['みる'], detail: ['非自立'] }],
    example: '食べてみます',
  },
  {
    id: 'teshimau',
    display: '〜てしまう',
    level: 'N4',
    description: 'Làm xong hẳn / lỡ làm mất (thường kèm tiếc nuối).',
    matchers: [TE, { base: ['しまう'], detail: ['非自立'] }],
    example: '食べてしまいました',
  },
  {
    id: 'chau',
    display: '〜ちゃう',
    level: 'N4',
    description: 'Rút gọn khẩu ngữ của 〜てしまう.',
    matchers: [{ base: ['ちゃう', 'じゃう'], detail: ['非自立'] }],
    example: '食べちゃった',
  },
  {
    id: 'teiku',
    display: '〜ていく',
    level: 'N4',
    description: 'Biến đổi dần về sau / làm rồi đi.',
    matchers: [TE, { base: ['いく'], detail: ['非自立'] }],
    example: '持っていきます',
  },
  {
    id: 'tekuru',
    display: '〜てくる',
    level: 'N4',
    description: 'Biến đổi cho đến giờ / làm rồi quay lại.',
    matchers: [TE, { base: ['くる'], detail: ['非自立'] }],
    example: '帰ってきました',
  },
  {
    id: 'teageru',
    display: '〜てあげる',
    level: 'N4',
    description: 'Làm giúp cho ai đó.',
    matchers: [TE, { base: ['あげる'], detail: ['非自立'] }],
    example: '買ってあげます',
  },
  {
    id: 'tekureru',
    display: '〜てくれる',
    level: 'N4',
    description: 'Ai đó làm cho mình (mình biết ơn).',
    matchers: [TE, { base: ['くれる'], detail: ['非自立'] }],
    example: '教えてくれました',
  },
  {
    id: 'temorau',
    display: '〜てもらう',
    level: 'N4',
    description: 'Nhờ / được ai đó làm cho.',
    matchers: [TE, { base: ['もらう'], detail: ['非自立'] }],
    example: '手伝ってもらいました',
  },
  {
    id: 'teitadaku',
    display: '〜ていただく',
    level: 'N4',
    description: 'Được ai đó làm cho (khiêm nhường, lịch sự hơn てもらう).',
    matchers: [TE, { base: ['いただく'], detail: ['非自立'] }],
    example: 'ご飯を食べていただきます',
  },
  {
    id: 'tekudasai',
    display: '〜てください',
    level: 'N5',
    description: 'Hãy làm ~ (yêu cầu lịch sự).',
    matchers: [TE, { base: ['くださる'], detail: ['非自立'] }],
    example: '待ってください',
  },
  {
    id: 'naidekudasai',
    display: '〜ないでください',
    level: 'N5',
    description: 'Xin đừng làm ~.',
    matchers: [{ base: ['ない'], conj: ['連用デ接続'] }, { surface: ['で'] }, { base: ['くださる'] }],
    example: '入らないでください',
  },
  {
    id: 'temoii',
    display: '〜てもいい',
    level: 'N5',
    description: 'Được phép làm ~ / làm ~ cũng được.',
    matchers: [TE, { surface: ['も'] }, { base: ['いい', 'よい'] }],
    example: '食べてもいいですか',
  },
  {
    id: 'tewaikenai',
    display: '〜てはいけない',
    level: 'N5',
    description: 'Không được làm ~ (cấm đoán).',
    matchers: [TE, { surface: ['は'] }, { base: ['いける'], detail: ['非自立'] }, OPT_NAI, OPT_MASE, OPT_NN],
    example: '入ってはいけません',
  },
  {
    id: 'tekara',
    display: '〜てから',
    level: 'N5',
    description: 'Sau khi làm ~ rồi mới.',
    matchers: [TE, { surface: ['から'], pos: ['助詞'], detail: ['格助詞'] }],
    example: '食べてから行きます',
  },
  {
    id: 'kute',
    display: '〜くて',
    level: 'N5',
    description: 'Dạng nối của tính từ đuôi い: "vừa ~ vừa".',
    matchers: [{ pos: ['形容詞'], conj: ['連用テ接続'] }, { surface: ['て'] }],
    example: '安くておいしいです',
  },
  {
    id: 'te',
    display: '〜て',
    level: 'N5',
    description: 'Dạng nối て: nối tiếp hành động.',
    matchers: [TE],
    example: '朝起きて、ご飯を食べます',
  },

  // --- Obligation / permission --------------------------------------------
  {
    id: 'nakerebanaranai',
    display: '〜なければならない',
    level: 'N4',
    description: 'Phải làm ~ (nghĩa vụ, bắt buộc).',
    matchers: [{ base: ['ない'], conj: ['仮定形'] }, { surface: ['ば'] }, { base: ['なる'] }, OPT_NAI, OPT_MASE, OPT_NN],
    example: '行かなければならない',
  },
  {
    id: 'nakutewaikenai',
    display: '〜なくてはいけない',
    level: 'N4',
    description: 'Phải làm ~ (không làm thì không được).',
    matchers: [
      { base: ['ない'], conj: ['連用テ接続'] },
      { surface: ['て'] },
      { surface: ['は'] },
      { base: ['いける'] },
      OPT_NAI,
      OPT_MASE,
      OPT_NN,
    ],
    example: '行かなくてはいけない',
  },
  {
    id: 'nakya',
    display: '〜なきゃ',
    level: 'N4',
    description: 'Phải ~ (rút gọn khẩu ngữ của なければ).',
    matchers: [{ base: ['ない'], conj: ['仮定縮約２'] }],
    example: '行かなきゃ',
  },
  {
    id: 'nakutemoii',
    display: '〜なくてもいい',
    level: 'N4',
    description: 'Không làm ~ cũng được (không cần).',
    matchers: [{ base: ['ない'], conj: ['連用テ接続'] }, { surface: ['て'] }, { surface: ['も'] }, { base: ['いい', 'よい'] }],
    example: '食べなくてもいいです',
  },
  {
    id: 'nakute',
    display: '〜なくて',
    level: 'N5',
    description: 'Dạng nối phủ định: "không ~ nên / và không ~".',
    matchers: [{ base: ['ない'], conj: ['連用テ接続'] }, { surface: ['て'] }],
    example: '朝ご飯を食べなくて、お腹が空きました',
  },
  {
    id: 'nasai',
    display: '〜なさい',
    level: 'N4',
    description: 'Mệnh lệnh nhẹ (bố mẹ / thầy cô nói với trẻ).',
    matchers: [{ base: ['なさる'], conj: ['命令ｉ'] }],
    example: '早く寝なさい',
  },

  // --- Desire ---------------------------------------------------------------
  {
    id: 'takunai',
    display: '〜たくない',
    level: 'N5',
    description: 'Không muốn làm ~.',
    matchers: [{ base: ['たい'], conj: ['連用テ接続'] }, { base: ['ない'] }],
    example: '行きたくないです',
  },
  {
    id: 'tagaru',
    display: '〜たがる',
    level: 'N4',
    description: 'Người khác tỏ ý muốn làm ~.',
    matchers: [{ base: ['たい'], conj: ['ガル接続'] }, { base: ['がる'] }],
    example: '飲みたがっています',
  },
  {
    id: 'tai',
    display: '〜たい',
    level: 'N5',
    description: 'Muốn làm ~.',
    matchers: [{ base: ['たい'], pos: ['助動詞'] }],
    example: '食べたいです',
  },
  {
    id: 'gahoshii',
    display: '〜がほしい',
    level: 'N5',
    description: 'Muốn có ~ (đồ vật).',
    matchers: [{ surface: ['が'], detail: ['格助詞'] }, { base: ['ほしい'], pos: ['形容詞'], detail: ['自立'] }],
    example: '水がほしいです',
  },
  {
    id: 'tehoshii',
    display: '〜てほしい',
    level: 'N4',
    description: 'Muốn ai đó làm ~.',
    matchers: [TE, { base: ['ほしい'], detail: ['非自立'] }],
    example: '手伝ってほしいです',
  },

  // --- Become / make --------------------------------------------------------
  {
    id: 'kunaru',
    display: '〜くなる',
    level: 'N5',
    description: 'Trở nên ~ (thay đổi trạng thái, với tính từ đuôi い).',
    matchers: [{ pos: ['形容詞'], conj: ['連用テ接続'] }, { base: ['なる'], detail: ['自立'] }],
    example: '寒くなりました',
  },
  {
    id: 'ninaru',
    display: '〜になる',
    level: 'N5',
    description: 'Trở thành ~ (với danh từ / tính từ đuôi な).',
    matchers: [CONTENT_NOUN, { surface: ['に'], detail: ['格助詞'] }, { base: ['なる'], detail: ['自立'] }],
    example: 'きれいになりました',
  },
  {
    id: 'kusuru',
    display: '〜くする',
    level: 'N4',
    description: 'Làm cho ~ (chủ động thay đổi, với tính từ đuôi い).',
    matchers: [{ pos: ['形容詞'], conj: ['連用テ接続'] }, { base: ['する'], detail: ['自立'] }],
    example: '部屋を暖かくします',
  },
  {
    id: 'nisuru',
    display: '〜にする',
    level: 'N4',
    description: 'Làm cho thành ~ / quyết định chọn ~.',
    matchers: [CONTENT_NOUN, { surface: ['に'], detail: ['格助詞'] }, { base: ['する'], detail: ['自立'] }],
    example: '部屋を静かにする',
  },

  // --- Ability / experience / decision --------------------------------------
  {
    id: 'kotogadekiru',
    display: '〜ことができる',
    level: 'N4',
    description: 'Có thể làm ~ (năng lực, khả năng).',
    matchers: [{ surface: ['こと'], detail: ['非自立'] }, { surface: ['が'] }, { base: ['できる'] }],
    example: '泳ぐことができます',
  },
  {
    id: 'takotogaaru',
    display: '〜たことがある',
    level: 'N4',
    description: 'Đã từng làm ~ (kinh nghiệm).',
    matchers: [{ base: ['た'], conj: ['基本形'] }, { surface: ['こと'] }, { surface: ['が'] }, { base: ['ある'] }],
    example: '行ったことがあります',
  },
  {
    id: 'kotogaaru',
    display: '〜ことがある',
    level: 'N4',
    description: 'Thỉnh thoảng có khi ~.',
    matchers: [{ pos: ['動詞'], conj: ['基本形'] }, { surface: ['こと'] }, { surface: ['が'] }, { base: ['ある'] }],
    example: '行くことがあります',
  },
  {
    id: 'kotonisuru',
    display: '〜ことにする',
    level: 'N4',
    description: 'Quyết định làm ~.',
    matchers: [{ surface: ['こと'], detail: ['非自立'] }, { surface: ['に'] }, { base: ['する'] }],
    example: '行くことにしました',
  },
  {
    id: 'kotoninaru',
    display: '〜ことになる',
    level: 'N4',
    description: 'Được quyết định là ~ / thành ra ~.',
    matchers: [{ surface: ['こと'], detail: ['非自立'] }, { surface: ['に'] }, { base: ['なる'] }],
    example: '行くことになりました',
  },

  // --- Intent / conjecture ---------------------------------------------------
  {
    id: 'youtoomou',
    display: '〜(よ)うと思う',
    level: 'N4',
    description: 'Định làm ~ (nói ra ý định của mình).',
    matchers: [
      { pos: ['動詞'], conj: ['未然ウ接続'] },
      { base: ['う'] },
      { surface: ['と'], detail: ['格助詞'] },
      { base: ['思う'] },
    ],
    example: '行こうと思います',
  },
  {
    id: 'volitional',
    display: '〜う／〜よう',
    level: 'N4',
    description: 'Thể ý chí: "làm ~ thôi / định làm ~".',
    matchers: [{ pos: ['動詞'], conj: ['未然ウ接続'] }, { base: ['う'] }],
    example: '行こう',
  },
  {
    id: 'toomou',
    display: '〜と思う',
    level: 'N4',
    description: 'Tôi nghĩ rằng ~.',
    matchers: [{ surface: ['と'], detail: ['格助詞'] }, { base: ['思う'] }],
    example: '行くと思います',
  },
  {
    id: 'toiu',
    display: '〜と言う',
    level: 'N4',
    description: 'Nói rằng ~ (dẫn lời).',
    matchers: [{ surface: ['と'], detail: ['格助詞'] }, { base: ['言う', 'いう'] }],
    example: '行くと言いました',
  },
  {
    id: 'kamoshirenai',
    display: '〜かもしれない',
    level: 'N4',
    description: 'Có lẽ / không chừng ~.',
    matchers: [{ base: ['かも'], pos: ['助詞'] }, { base: ['しれる'] }, OPT_NAI, OPT_MASE, OPT_NN],
    example: '行くかもしれません',
  },
  {
    id: 'hazu',
    display: '〜はずだ',
    level: 'N4',
    description: 'Theo suy luận thì chắc chắn ~.',
    matchers: [{ base: ['はず'], detail: ['非自立'] }],
    example: '学生のはずです',
  },
  {
    id: 'tsumori',
    display: '〜つもりだ',
    level: 'N4',
    description: 'Định làm ~ (dự định).',
    matchers: [{ base: ['つもり'], detail: ['非自立'] }],
    example: '行くつもりです',
  },

  // --- Hearsay / appearance --------------------------------------------------
  {
    id: 'souda-yotei',
    display: '〜そうだ（trông có vẻ）',
    level: 'N4',
    description: 'Trông có vẻ ~ / sắp ~ (nhìn mà đoán).',
    matchers: [{ base: ['そう'], pos: ['名詞'], detail: ['接尾'] }],
    example: '雨が降りそうです',
  },
  {
    id: 'souda-denbun',
    display: '〜そうだ（nghe nói）',
    level: 'N4',
    description: 'Nghe nói ~ (truyền đạt thông tin).',
    matchers: [{ base: ['そう'], pos: ['名詞'], detail: ['特殊'] }],
    example: '雨が降るそうです',
  },
  {
    id: 'rashii',
    display: '〜らしい',
    level: 'N4',
    description: 'Hình như / nghe nói ~.',
    matchers: [{ base: ['らしい'], pos: ['助動詞'] }],
    example: '雨らしいです',
  },
  {
    id: 'youninaru',
    display: '〜ようになる',
    level: 'N4',
    description: 'Trở nên (có thể) ~ / dần thành thói quen.',
    matchers: [{ base: ['よう'], detail: ['非自立'] }, { surface: ['に'] }, { base: ['なる'] }],
    example: '日本語が話せるようになりました',
  },
  {
    id: 'younisuru',
    display: '〜ようにする',
    level: 'N4',
    description: 'Cố gắng duy trì làm ~.',
    matchers: [{ base: ['よう'], detail: ['非自立'] }, { surface: ['に'] }, { base: ['する'] }],
    example: '毎日運動するようにしています',
  },
  {
    id: 'youda',
    display: '〜ようだ',
    level: 'N4',
    description: 'Giống như / có vẻ ~.',
    matchers: [{ base: ['よう'], pos: ['名詞'], detail: ['非自立'] }],
    example: '雨のようです',
  },
  {
    id: 'mitai',
    display: '〜みたい',
    level: 'N4',
    description: 'Giống như ~ (khẩu ngữ).',
    matchers: [{ base: ['みたい'], detail: ['非自立'] }],
    example: '雨みたいです',
  },

  // --- Conditionals ----------------------------------------------------------
  {
    id: 'ba',
    display: '〜ば',
    level: 'N4',
    description: 'Nếu ~ (điều kiện giả định).',
    matchers: [{ pos: ['動詞', '形容詞'], conj: ['仮定形'] }, { surface: ['ば'], detail: ['接続助詞'] }],
    example: '行けば分かります',
  },
  {
    id: 'tara',
    display: '〜たら',
    level: 'N4',
    description: 'Nếu ~ / sau khi ~ thì.',
    matchers: [{ base: ['た'], conj: ['仮定形'] }],
    example: '行ったら電話します',
  },
  {
    id: 'nara',
    display: '〜なら',
    level: 'N4',
    description: 'Nếu là ~ (điều kiện theo chủ đề đang nói).',
    matchers: [{ base: ['だ'], conj: ['仮定形'] }],
    example: '行くなら早くして',
  },
  {
    id: 'to-cond',
    display: '〜と（điều kiện）',
    level: 'N4',
    description: 'Hễ ~ là (kết quả xảy ra tất nhiên).',
    matchers: [{ surface: ['と'], pos: ['助詞'], detail: ['接続助詞'] }],
    example: '春になると暖かくなります',
  },

  // --- Passive / causative ---------------------------------------------------
  {
    id: 'saserareru',
    display: '〜させられる',
    level: 'N4',
    description: 'Bị bắt làm ~ (bị động của sai khiến).',
    matchers: [{ base: ['させる', 'せる'], detail: ['接尾'] }, { base: ['られる'], detail: ['接尾'] }],
    example: '食べさせられました',
  },
  {
    id: 'rareru',
    display: '〜られる／〜れる',
    level: 'N4',
    description: 'Bị/được ~ (bị động) hoặc thể khả năng.',
    matchers: [{ base: ['られる', 'れる'], pos: ['動詞'], detail: ['接尾'] }],
    example: '先生に褒められました',
  },
  {
    id: 'saseru',
    display: '〜させる／〜せる',
    level: 'N4',
    description: 'Bắt / cho ai làm ~ (sai khiến).',
    matchers: [{ base: ['させる', 'せる'], pos: ['動詞'], detail: ['接尾'] }],
    example: '子供に食べさせます',
  },

  // --- Advice / comparison ---------------------------------------------------
  {
    id: 'tahougaii',
    display: '〜たほうがいい',
    level: 'N4',
    description: 'Nên làm ~ (khuyên nhủ).',
    matchers: [{ base: ['た'], conj: ['基本形'] }, { surface: ['ほう', '方'] }, { surface: ['が'] }, { base: ['いい', 'よい'] }],
    example: '行ったほうがいいです',
  },
  {
    id: 'naihougaii',
    display: '〜ないほうがいい',
    level: 'N4',
    description: 'Không nên làm ~.',
    matchers: [{ base: ['ない'], conj: ['基本形'] }, { surface: ['ほう', '方'] }, { surface: ['が'] }, { base: ['いい', 'よい'] }],
    example: '行かないほうがいいです',
  },
  {
    id: 'yori',
    display: '〜より',
    level: 'N5',
    description: 'So với ~ (so sánh).',
    matchers: [{ surface: ['より'], detail: ['格助詞'] }],
    example: '東京より大きいです',
  },
  {
    id: 'nogasuki',
    display: '〜のが好き／上手／下手',
    level: 'N5',
    description: 'Thích / giỏi / kém làm việc ~.',
    matchers: [
      { surface: ['の'], detail: ['非自立'] },
      { surface: ['が'] },
      { base: ['好き', '嫌い', '上手', '下手', '得意', '苦手'] },
    ],
    example: '泳ぐのが好きです',
  },

  // --- Time / reason / connection ---------------------------------------------
  {
    id: 'maeni',
    display: '〜前に',
    level: 'N5',
    description: 'Trước khi làm ~.',
    matchers: [{ pos: ['動詞'], conj: ['基本形'] }, { surface: ['前'] }, { surface: ['に'] }],
    example: '食べる前に手を洗います',
  },
  {
    id: 'taatode',
    display: '〜たあとで',
    level: 'N5',
    description: 'Sau khi làm ~.',
    matchers: [{ base: ['た'], conj: ['基本形'] }, { surface: ['あと', '後'] }, { surface: ['で'] }],
    example: '食べたあとで歯を磨きます',
  },
  {
    id: 'toki',
    display: '〜とき',
    level: 'N5',
    description: 'Khi ~ / lúc ~.',
    matchers: [{ surface: ['とき', '時'], pos: ['名詞'], detail: ['非自立'] }],
    example: '食べるときに読みます',
  },
  {
    id: 'kara-reason',
    display: '〜から（lý do）',
    level: 'N5',
    description: 'Vì ~ (nêu nguyên nhân).',
    matchers: [{ surface: ['から'], pos: ['助詞'], detail: ['接続助詞'] }],
    example: '雨だから行きません',
  },
  {
    id: 'node',
    display: '〜ので',
    level: 'N4',
    description: 'Vì ~ (nguyên nhân, nhẹ nhàng khách quan hơn から).',
    matchers: [{ base: ['ので'], pos: ['助詞'], detail: ['接続助詞'] }],
    example: '雨なので行きません',
  },
  {
    id: 'nanoni',
    display: '〜のに（mặc dù）',
    level: 'N4',
    description: 'Mặc dù ~ mà lại (trái mong đợi).',
    matchers: [{ base: ['だ'], conj: ['体言接続'] }, { surface: ['の'], detail: ['非自立'] }, { surface: ['に'] }],
    example: '雨なのに行きました',
  },
  {
    id: 'noni-verb',
    display: '〜のに',
    level: 'N4',
    description: 'Mặc dù ~ / để làm ~ (thì cần).',
    matchers: [
      { pos: ['動詞'], conj: ['基本形'] },
      { surface: ['の'], detail: ['非自立'] },
      { surface: ['に'], detail: ['格助詞'] },
    ],
    example: '行くのに時間がかかる',
  },
  {
    id: 'nagara',
    display: '〜ながら',
    level: 'N4',
    description: 'Vừa ~ vừa ~ (hai hành động cùng lúc).',
    matchers: [{ base: ['ながら'], detail: ['接続助詞'] }],
    example: '音楽を聞きながら勉強します',
  },
  {
    id: 'tari',
    display: '〜たり〜たりする',
    level: 'N4',
    description: 'Lúc thì ~ lúc thì ~ (liệt kê hành động tiêu biểu).',
    matchers: [{ base: ['たり', 'だり'], pos: ['助詞'], detail: ['並立助詞'] }],
    example: '食べたり飲んだりします',
  },
  {
    id: 'shi',
    display: '〜し',
    level: 'N4',
    description: 'Vừa ~ lại còn ~ (liệt kê lý do).',
    matchers: [{ surface: ['し'], pos: ['助詞'], detail: ['接続助詞'] }],
    example: '安いし美味しいし',
  },
  {
    id: 'tameni',
    display: '〜ために',
    level: 'N4',
    description: 'Để ~ (mục đích) / vì ~.',
    matchers: [{ surface: ['ため'], detail: ['非自立'] }, { surface: ['に'] }],
    example: '勉強するために来ました',
  },
  {
    id: 'ndesu',
    display: '〜んです',
    level: 'N4',
    description: 'Giải thích / nhấn mạnh lý do.',
    matchers: [{ surface: ['ん', 'の'], pos: ['名詞'], detail: ['非自立'] }, { base: ['です', 'だ'] }],
    example: '行くんです',
  },

  // --- Degree / tendency -------------------------------------------------------
  {
    id: 'yasui',
    display: '〜やすい',
    level: 'N4',
    description: 'Dễ ~ (dễ làm, dễ xảy ra).',
    matchers: [{ base: ['やすい'], pos: ['形容詞'], detail: ['非自立'] }],
    example: '食べやすいです',
  },
  {
    id: 'nikui',
    display: '〜にくい',
    level: 'N4',
    description: 'Khó ~ (khó làm).',
    matchers: [{ base: ['にくい'], detail: ['非自立'] }],
    example: '読みにくいです',
  },
  {
    id: 'sugiru',
    display: '〜すぎる',
    level: 'N4',
    description: 'Quá ~ (vượt mức).',
    matchers: [{ base: ['すぎる'], detail: ['非自立'] }],
    example: '食べすぎました',
  },
  {
    id: 'hajimeru',
    display: '〜始める',
    level: 'N4',
    description: 'Bắt đầu làm ~.',
    matchers: [{ pos: ['動詞'], conj: ['連用形'], detail: ['自立'] }, { base: ['始める'], detail: ['非自立'] }],
    example: '食べ始めました',
  },
  {
    id: 'owaru',
    display: '〜終わる',
    level: 'N4',
    description: 'Làm xong ~.',
    matchers: [{ pos: ['動詞'], conj: ['連用形'], detail: ['自立'] }, { base: ['終わる'], detail: ['非自立'] }],
    example: '食べ終わりました',
  },
  {
    id: 'shika',
    display: '〜しか…ない',
    level: 'N4',
    description: 'Chỉ ~ (luôn đi với phủ định).',
    matchers: [{ surface: ['しか'], pos: ['助詞'] }],
    example: '水しかありません',
  },
  {
    id: 'bakari',
    display: '〜ばかり',
    level: 'N4',
    description: 'Toàn / chỉ toàn ~.',
    matchers: [{ base: ['ばかり'], pos: ['助詞'], detail: ['副助詞'] }],
    example: '遊んでばかりいる',
  },
  {
    id: 'kadouka',
    display: '〜かどうか',
    level: 'N4',
    description: 'Liệu có ~ hay không.',
    matchers: [{ surface: ['か'], pos: ['助詞'] }, { base: ['どうか'] }],
    example: '行くかどうか分かりません',
  },
  {
    id: 'sa',
    display: '〜さ',
    level: 'N4',
    description: 'Danh từ hóa tính từ: độ ~ (高さ: chiều cao).',
    matchers: [{ pos: ['形容詞'], conj: ['ガル接続'] }, { surface: ['さ'], pos: ['名詞'], detail: ['接尾'] }],
    example: 'このビルの高さ',
  },
]
