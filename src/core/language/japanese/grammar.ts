/**
 * Short, offline explanations for the auxiliary tokens that attach to a
 * verb/adjective stem in a conjugated grammar unit. Keyed by the token's
 * kuromoji basic_form — e.g. まし → ます, なかっ → ない — so conjugated
 * auxiliary surfaces resolve to one entry each.
 */
const AUX_DESCRIPTIONS: Record<string, string> = {
  ます: 'polite ending (〜ます)',
  た: 'past tense (〜た)',
  ない: 'negative (〜ない)',
  ぬ: 'negative, archaic/formal (〜ぬ)',
  ん: 'negative contraction (〜ん)',
  て: 'connective te-form (〜て)',
  で: 'connective te-form (〜で)',
  ちゃ: 'contraction of 〜ては',
  じゃ: 'contraction of 〜では',
  いる: 'ongoing action or state (〜ている)',
  ある: 'resultant state (〜てある)',
  いく: 'change proceeding onward (〜ていく)',
  くる: 'change up to now / coming (〜てくる)',
  しまう: 'completion, often with regret (〜てしまう)',
  おく: 'do in advance (〜ておく)',
  みる: 'try doing (〜てみる)',
  あげる: 'do for someone (〜てあげる)',
  くれる: 'someone does for the speaker (〜てくれる)',
  もらう: 'have someone do (〜てもらう)',
  くださる: 'please do / respectful (〜てください)',
  たい: 'want to do (〜たい)',
  たがる: 'shows signs of wanting (〜たがる)',
  れる: 'passive or potential (〜れる)',
  られる: 'passive or potential (〜られる)',
  せる: 'causative (〜せる)',
  させる: 'causative (〜させる)',
  そう: 'looks like / about to (〜そう)',
  よう: 'volitional or conjecture (〜よう)',
  う: 'volitional (〜う)',
  まい: 'negative volitional (〜まい)',
  です: 'polite copula (です)',
  だ: 'plain copula (だ)',
  らしい: 'apparently / seems (〜らしい)',
  ほしい: 'want someone to do (〜てほしい)',
  やすい: 'easy to do (〜やすい)',
  にくい: 'hard to do (〜にくい)',
  すぎる: 'too much / excessively (〜すぎる)',
}

/** Explanation for an auxiliary token; falls back to its POS class. */
export function describeAuxiliary(basicForm: string, pos: string): string {
  const known = AUX_DESCRIPTIONS[basicForm]
  if (known !== undefined) return known
  return pos === '助動詞' ? 'auxiliary' : 'suffix / auxiliary verb'
}
