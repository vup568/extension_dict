import { startSelectionPopup } from "./browser/popup/startSelectionPopup.ts";
import { createRuntimeAnalysisTransport } from "./platform/runtimeTransport.ts";

// Browser entry alone wires vendor transport into the DOM adapter [BROWSER-001].
startSelectionPopup({ document, analyze: createRuntimeAnalysisTransport() });
