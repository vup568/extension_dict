import { startSelectionPopup } from "./browser/popup/startSelectionPopup.ts";
import { createRuntimeAnalysisTransport } from "./platform/runtimeTransport.ts";
import { createRuntimeTranslationTransport } from "./platform/translationTransport.ts";
import { createRuntimeScan, createRuntimeSpeech } from "./platform/readingTransport.ts";
import { createReadingPreferenceStore } from "./platform/readingPreferences.ts";

// Browser entry alone wires vendor transport into the DOM adapter [BROWSER-001].
startSelectionPopup({ document, analyze: createRuntimeAnalysisTransport(), translate: createRuntimeTranslationTransport(),
  scan: createRuntimeScan(), speak: createRuntimeSpeech(), preferences: createReadingPreferenceStore() });
