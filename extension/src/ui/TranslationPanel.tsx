import { useEffect, useRef, useState, type MouseEvent } from "react";
import { AnalysisFailure } from "../api/analysisContract.ts";
import type { Translate } from "../platform/translationTransport.ts";

type TranslationState =
  | { phase: "idle" }
  | { phase: "loading" }
  | { phase: "result"; text: string }
  | { phase: "error"; code: string; retryable: boolean };

const messages: Record<string, string> = {
  SERVICE_UNAVAILABLE: "Dịch vụ chưa khả dụng hoặc chưa được cấu hình. Hãy kiểm tra cấu hình backend.",
  TIMEOUT: "Dịch câu quá thời gian chờ.",
  NETWORK_ERROR: "Không thể kết nối dịch vụ dịch.",
  TRANSPORT_UNAVAILABLE: "Không thể kết nối extension. Hãy tải lại trang.",
  RATE_LIMITED: "Dịch vụ đang nhận quá nhiều yêu cầu. Hãy thử lại sau.",
  VALIDATION_FAILED: "Nội dung dịch không hợp lệ.",
  INVALID_RESPONSE: "Dịch vụ trả về kết quả không hợp lệ.",
};

/** Mounted per analysis interaction: closing/reselection releases its ephemeral text. */
export function TranslationPanel({ text, interactionId, translate }: { text: string; interactionId: string; translate: Translate }) {
  const [state, setState] = useState<TranslationState>({ phase: "idle" });
  const request = useRef<AbortController | null>(null);
  useEffect(() => () => { const current = request.current; request.current = null; current?.abort(); }, []);

  function start(event: MouseEvent<HTMLButtonElement>): void {
    if (!event.nativeEvent.isTrusted || request.current !== null) return;
    const controller = new AbortController(); request.current = controller;
    setState({ phase: "loading" });
    // EARS[Event]: Translation starts only after the reader has seen the provider disclosure and pressed the button.
    let result: ReturnType<Translate>;
    try { result = translate(text, interactionId, controller.signal); }
    catch (error: unknown) { result = Promise.reject(error); }
    result.then(response => {
      if (request.current !== controller || controller.signal.aborted) return;
      request.current = null;
      if (response.meta.interaction_id !== interactionId || response.data.source_language !== "ja" || response.data.target_language !== "en") {
        setState({ phase: "error", code: "INVALID_RESPONSE", retryable: true }); return;
      }
      setState({ phase: "result", text: response.data.translated_text });
    }, (error: unknown) => {
      if (request.current !== controller || controller.signal.aborted) return;
      request.current = null;
      const failure = error instanceof AnalysisFailure ? error : new AnalysisFailure("NETWORK_ERROR", true);
      setState({ phase: "error", code: failure.code, retryable: failure.retryable });
    });
  }

  return <details className="jp-section jp-disclosure">
    <summary><h3>Dịch câu</h3></summary>
    <p className="jp-muted">Khi bạn chọn dịch, văn bản đã chọn sẽ được gửi đến Google Gemini để dịch và có thể được Google dùng để cải thiện sản phẩm theo điều khoản gói miễn phí.</p>
    {state.phase === "idle" && <button type="button" onClick={start}>Dịch sang tiếng Anh</button>}
    {state.phase === "loading" && <p role="status">Đang dịch…</p>}
    {state.phase === "result" && <p lang="en">{state.text}</p>}
    {state.phase === "error" && <div role="alert">
      <p>{messages[state.code] ?? "Không thể dịch câu lúc này."}</p>
      {state.retryable && <button type="button" onClick={start}>Thử dịch lại</button>}
    </div>}
  </details>;
}
