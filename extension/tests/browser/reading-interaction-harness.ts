import { startSelectionPopup } from "../../src/browser/popup/startSelectionPopup.ts";
import type { ScanInput, ScanResponse, SpeechResponse, SpeechSource } from "../../src/api/readingContracts.ts";
import { defaultReadingPreferences, type PopupSize } from "../../src/platform/readingPreferences.ts";
import { analysisFixture } from "../unit/analysisFixtures.ts";

const attempts: { input: ScanInput; signal: AbortSignal; resolve: (value: ScanResponse) => void; id: string }[] = [];
const speech: { source: SpeechSource; signal: AbortSignal; resolve: (value: SpeechResponse) => void }[] = [];
const sizes: PopupSize[] = [];
const settings: boolean[] = [];
const dispose = startSelectionPopup({ document,
  analyze: async (text, id) => {
    const value = analysisFixture(id);
    value.data.normalized_text = text;
    return value;
  },
  scan: (input, id, signal) => new Promise(resolve => attempts.push({ input, id, signal, resolve })),
  speak: (source, _, signal) => new Promise(resolve => speech.push({ source, signal, resolve })),
  preferences: {
    subscribe(listener) { listener(defaultReadingPreferences); return () => {}; },
    setSize: size => { sizes.push(size); },
    setScanEnabled: enabled => { settings.push(enabled); },
  },
});

const readingHarness = {
  select(selector: string) {
    const range = document.createRange();
    range.selectNodeContents(document.querySelector(selector)!);
    window.getSelection()?.removeAllRanges();
    window.getSelection()?.addRange(range);
    document.dispatchEvent(new Event("selectionchange"));
  },
  attempts: () => attempts.map(({ input, signal }) => ({ ...input, aborted: signal.aborted })),
  speech: () => speech.map(({ source, signal }) => ({ source, aborted: signal.aborted })),
  preferences: () => ({ sizes, settings }),
  succeed(index: number) {
    const { input, resolve, id } = attempts[index]!;
    const analysis = analysisFixture(id);
    analysis.data.normalized_text = input.text;
    analysis.meta.contract_version = "3";
    const targetSpan = { start: 0, end: input.text.length, unit: "utf16" } as const;
    analysis.data.vocabulary_occurrences = [{ surface: input.text, span: targetSpan, matches: analysis.data.dictionary_matches, conjugations: [] }];
    resolve({ analysis, targetSpan });
  },
  finishSpeech(index: number) {
    const audioBase64 = "UklGRiYAAABXQVZFZm10IBAAAAABAAEAwF0AAIC7AAACABAAZGF0YQIAAAAAAA==";
    speech[index]!.resolve({ audio_base64: audioBase64, mime_type: "audio/wav", language_tag: "ja", source: "ai" });
  },
  dispose,
};
declare global { interface Window { readingHarness: typeof readingHarness } }
window.readingHarness = readingHarness;
