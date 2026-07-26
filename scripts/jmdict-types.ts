/**
 * Minimal input types for the jmdict-simplified 3.x JSON format — only the
 * fields the prepare script actually reads.
 * Full format: https://github.com/scriptin/jmdict-simplified
 */

export interface JmdictFile {
  version: string
  dictDate: string
  commonOnly: boolean
  tags: Record<string, string>
  words: JmdictWord[]
}

export interface JmdictWord {
  id: string
  kanji: JmdictForm[]
  kana: JmdictForm[]
  sense: JmdictSense[]
}

export interface JmdictForm {
  common: boolean
  text: string
}

export interface JmdictSense {
  partOfSpeech: string[]
  gloss: JmdictGloss[]
}

export interface JmdictGloss {
  lang: string
  text: string | null
}
