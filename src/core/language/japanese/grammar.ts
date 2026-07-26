/**
 * Short, offline Vietnamese explanations for the auxiliary tokens that
 * attach to a verb/adjective stem in a conjugated grammar unit. Keyed by the
 * token's kuromoji basic_form — e.g. まし → ます, なかっ → ない — so
 * conjugated auxiliary surfaces resolve to one entry each.
 */
const AUX_DESCRIPTIONS: Record<string, string> = {
  ます: 'đuôi lịch sự (〜ます)',
  た: 'thì quá khứ (〜た)',
  ない: 'phủ định (〜ない)',
  ぬ: 'phủ định, văn cổ/trang trọng (〜ぬ)',
  ん: 'phủ định rút gọn (〜ん)',
  て: 'dạng nối て (〜て)',
  で: 'dạng nối て (〜で)',
  ちゃ: 'rút gọn của 〜ては',
  じゃ: 'rút gọn của 〜では',
  いる: 'đang diễn ra / trạng thái duy trì (〜ている)',
  ある: 'trạng thái có sẵn (〜てある)',
  いく: 'biến đổi dần về sau (〜ていく)',
  くる: 'biến đổi đến hiện tại / quay lại (〜てくる)',
  しまう: 'làm xong hẳn, thường kèm tiếc nuối (〜てしまう)',
  ちゃう: 'rút gọn khẩu ngữ của 〜てしまう (〜ちゃう)',
  じゃう: 'rút gọn khẩu ngữ của 〜でしまう (〜じゃう)',
  おく: 'làm sẵn trước (〜ておく)',
  みる: 'thử làm (〜てみる)',
  あげる: 'làm cho ai đó (〜てあげる)',
  くれる: 'ai đó làm cho mình (〜てくれる)',
  もらう: 'nhờ/được ai đó làm (〜てもらう)',
  いただく: 'được ai đó làm cho — khiêm nhường (〜ていただく)',
  くださる: 'xin hãy làm ~ (〜てください)',
  なさる: 'kính ngữ của する; 〜なさい là mệnh lệnh nhẹ',
  たい: 'muốn làm (〜たい)',
  たがる: 'người khác tỏ ý muốn (〜たがる)',
  がる: 'tỏ vẻ, biểu lộ cảm xúc (〜がる)',
  れる: 'bị động hoặc khả năng (〜れる)',
  られる: 'bị động hoặc khả năng (〜られる)',
  せる: 'sai khiến (〜せる)',
  させる: 'sai khiến (〜させる)',
  そう: 'trông có vẻ / sắp (〜そう)',
  よう: 'thể ý chí hoặc phỏng đoán (〜よう)',
  う: 'thể ý chí (〜う)',
  まい: 'phủ định ý chí (〜まい)',
  です: 'trợ từ khẳng định lịch sự (です)',
  だ: 'trợ từ khẳng định thường (だ)',
  らしい: 'hình như / nghe nói (〜らしい)',
  ほしい: 'muốn ai đó làm (〜てほしい)',
  やすい: 'dễ ~ (〜やすい)',
  にくい: 'khó ~ (〜にくい)',
  すぎる: 'quá mức (〜すぎる)',
  始める: 'bắt đầu làm (〜始める)',
  終わる: 'làm xong (〜終わる)',
  いける: 'được phép (いけない: không được)',
  なる: 'trở nên (なる)',
}

/** Explanation for an auxiliary token; falls back to its POS class. */
export function describeAuxiliary(basicForm: string, pos: string): string {
  const known = AUX_DESCRIPTIONS[basicForm]
  if (known !== undefined) return known
  return pos === '助動詞' ? 'trợ động từ' : 'hậu tố / động từ bổ trợ'
}
