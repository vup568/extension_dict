import { AnalysisFailure } from "./analysisContract.ts";

export interface TranslationResponse {
  data: { source_language: "ja"; target_language: "en"; translated_text: string };
  meta: { request_id: string; interaction_id: string };
}

/** Reject malformed, empty, wrong-language and uncorrelated results at the boundary. */
export function parseTranslationResponse(value: unknown, interactionId: string): TranslationResponse {
  if (!value || typeof value !== "object" || Array.isArray(value)) throw new AnalysisFailure("INVALID_RESPONSE", true);
  const candidate = value as Record<string, unknown>;
  if (!candidate.data || typeof candidate.data !== "object" || Array.isArray(candidate.data) ||
      !candidate.meta || typeof candidate.meta !== "object" || Array.isArray(candidate.meta)) throw new AnalysisFailure("INVALID_RESPONSE", true);
  const data = candidate.data as Record<string, unknown>, meta = candidate.meta as Record<string, unknown>;
  if (data.source_language !== "ja" || data.target_language !== "en" ||
      typeof data.translated_text !== "string" || data.translated_text.trim().length === 0 || data.translated_text.length > 20000 ||
      typeof meta.request_id !== "string" || meta.request_id.length === 0 || meta.interaction_id !== interactionId)
    throw new AnalysisFailure("INVALID_RESPONSE", true);
  return { data: { source_language: "ja", target_language: "en", translated_text: data.translated_text },
    meta: { request_id: meta.request_id, interaction_id: interactionId } };
}
