import { registerBackground } from "./platform/backgroundHandler.ts";
import { getExtensionRuntime, type RuntimeBackground } from "./platform/runtimeTransport.ts";
const runtime = getExtensionRuntime();
if (runtime) registerBackground(runtime as RuntimeBackground, import.meta.env.VITE_API_BASE_URL ?? "http://127.0.0.1:5000");
