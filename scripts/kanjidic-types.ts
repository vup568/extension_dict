/**
 * Minimal input types for the jmdict-simplified kanjidic2 JSON format — only
 * the fields the prepare script actually reads.
 * Full format: https://github.com/scriptin/jmdict-simplified
 */

export interface Kanjidic2File {
  version: string
  dictDate: string
  characters: Kanjidic2Character[]
}

export interface Kanjidic2Character {
  literal: string
  misc: {
    strokeCounts: number[]
    grade: number | null
    /** OLD pre-2010 JLPT scale, 1–4. */
    jlptLevel: number | null
    frequency: number | null
  }
  readingMeaning: Kanjidic2ReadingMeaning | null
}

export interface Kanjidic2ReadingMeaning {
  groups: {
    readings: { type: string; value: string }[]
    meanings: { lang: string; value: string }[]
  }[]
  nanori: string[]
}
