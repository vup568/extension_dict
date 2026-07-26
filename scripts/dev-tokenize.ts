/**
 * DEV ONLY (not part of the build): dumps kuromoji/IPADIC features for test
 * phrases so grammar-pattern matchers are written against real tokenizer
 * output. Run: npx tsx scripts/dev-tokenize.ts [phrase...]
 */
import { readFile } from 'node:fs/promises'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import type { IpadicFeatures, Tokenizer } from '@aiktb/kuromoji'

const ROOT = fileURLToPath(new URL('..', import.meta.url))
const DICT_DIR = join(ROOT, 'public', 'kuromoji')

// The fork loads its dictionary via fetch(); serve those requests from disk.
globalThis.fetch = (async (input: RequestInfo | URL) => {
  const name = String(input).split('/').pop() ?? ''
  const buf = await readFile(join(DICT_DIR, name))
  return new Response(new Uint8Array(buf))
}) as typeof fetch

const DEFAULT_PHRASES = [
  '食べました',
  '食べませんでした',
  '食べています',
  '書いてあります',
  '準備しておきます',
  '食べてみます',
  '食べてしまいました',
  '持っていきます',
  '帰ってきました',
  '買ってあげます',
  '教えてくれました',
  '手伝ってもらいました',
  '待ってください',
  '入らないでください',
  '食べてもいいですか',
  '入ってはいけません',
  '行かなければならない',
  '行かなくてはいけない',
  '行かなきゃ',
  '食べたいです',
  '飲みたがっています',
  '行きたくないです',
  '寒くなりました',
  'きれいになりました',
  '部屋を静かにする',
  '泳ぐことができます',
  '行ったことがあります',
  '行くつもりです',
  '勉強するために来ました',
  '雨なのに行きました',
  '雨なので行きません',
  '雨だから行きません',
  '行けば分かります',
  '行ったら電話します',
  '行くなら早くして',
  '春になると暖かくなります',
  '音楽を聞きながら勉強します',
  '食べたり飲んだりします',
  '食べやすいです',
  '読みにくいです',
  '食べすぎました',
  '高すぎます',
  '雨が降りそうです',
  '雨が降るそうです',
  '雨らしいです',
  '雨のようです',
  '雨みたいです',
  '行くでしょう',
  '行くだろう',
  '行こうと思います',
  '食べようと思っています',
  '先生に褒められました',
  '子供に食べさせます',
  '食べさせられました',
  '行ったほうがいいです',
  '行かないほうがいいです',
  '食べる前に手を洗います',
  '食べたあとで歯を磨きます',
  '食べてから行きます',
  '行くかもしれません',
  '行くと思います',
  '行くと言いました',
  '学生のはずです',
  '早く寝なさい',
  '食べなくてもいいです',
  '水がほしいです',
  '手伝ってほしいです',
  'まだ食べていません',
  '日本語が話せます',
  '窓が開いています',
  '名前を書いてあります',
]

const phrases = process.argv.slice(2).length > 0 ? process.argv.slice(2) : DEFAULT_PHRASES

function fmt(t: IpadicFeatures): string {
  return [
    t.surface_form.padEnd(6, '　'),
    `${t.pos},${t.pos_detail_1}`.padEnd(14),
    `base=${t.basic_form}`.padEnd(14),
    `${t.conjugated_type}/${t.conjugated_form}`,
  ].join(' | ')
}

// The package's ESM entry only default-exports; grab builder dynamically.
interface KuromojiModule {
  builder: (opts: { dicPath: string }) => { build: (cb: (err: unknown, tk: Tokenizer<IpadicFeatures>) => void) => void }
}
const imported = (await import('@aiktb/kuromoji')) as unknown as KuromojiModule & { default?: KuromojiModule }
const builder = (imported.default ?? imported).builder

const tokenizer = await new Promise<Tokenizer<IpadicFeatures>>((resolve, reject) => {
  const dicPath =
    process.env.KUROMOJI_GZ_DIR ?? 'public/kuromoji'
  builder({ dicPath }).build((err, tk) => {
    if (err !== null && err !== undefined) reject(err instanceof Error ? err : new Error(String(err)))
    else resolve(tk)
  })
})

for (const phrase of phrases) {
  console.log(`\n=== ${phrase}`)
  for (const token of tokenizer.tokenize(phrase)) console.log('  ' + fmt(token))
}
