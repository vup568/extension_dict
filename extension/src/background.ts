import { registerBackground } from "./platform/backgroundHandler.ts";
import { getExtensionRuntime, type RuntimeBackground } from "./platform/runtimeTransport.ts";
import { registerTranslationBackground } from "./platform/translationTransport.ts";
import { registerReadingBackground } from "./platform/readingTransport.ts";
const runtime = getExtensionRuntime();
if (runtime) {
  const origin = import.meta.env.VITE_API_BASE_URL ?? "http://127.0.0.1:5000";
  registerBackground(runtime as RuntimeBackground, origin);
  registerTranslationBackground(runtime as RuntimeBackground, origin);
  registerReadingBackground(runtime as RuntimeBackground, origin);
}
