import type { EntryMatch } from "../api/analysisContract.ts";
import { isSpeechSource, type SpeechSource } from "../api/readingContracts.ts";

type DictionarySource = Extract<SpeechSource, { kind: "dictionary" }>;

export interface DictionarySpeechOption {
  readonly source: DictionarySource;
  readonly label: string;
}

export function dictionarySpeechOptions(entry: EntryMatch): DictionarySpeechOption[] {
  const options: DictionarySpeechOption[] = [];
  for (const reading of entry.readings) {
    const kanaOnly = reading.info?.includes("reading:no-kanji") || entry.writtenForms.length === 0;
    const writtenForms = kanaOnly ? [null] : entry.writtenForms;
    for (const written of writtenForms) {
      const restriction = reading.restrictedToForms;
      if (written && restriction?.length && !restriction.includes(written.form)) continue;
      const matchesForm = written !== null && written.form === entry.matchedWrittenForm;
      const matchesReading = reading.readingText === entry.matchedReading;
      if ((entry.matchedWrittenForm !== null || entry.matchedReading !== null) && !matchesForm && !matchesReading) continue;
      // EARS[Unwanted] YAU-02: Do not offer a pair when no projected sense applies to it.
      if (!entry.senses.some(sense => sense.restrictions.length === 0 || sense.restrictions.some(pair =>
        (pair.writtenFormId === null || pair.writtenFormId === written?.id) &&
        (pair.readingId === null || pair.readingId === reading.id)))) continue;

      const source: DictionarySource = { kind: "dictionary", canonical_id: entry.canonicalId, reading_id: reading.id };
      if (written) source.written_form_id = written.id;
      if (isSpeechSource(source)) options.push({
        source, label: written && written.form !== reading.readingText
          ? `${written.form} · ${reading.readingText}` : reading.readingText,
      });
    }
  }
  return options;
}
