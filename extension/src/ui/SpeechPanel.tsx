import { useLayoutEffect, useRef, useState } from "react";
import { AnalysisFailure } from "../api/analysisContract.ts";
import { decodeSpeechWav, type SpeechSource } from "../api/readingContracts.ts";
import { createInteractionId } from "../interaction/createInteractionId.ts";
import type { Speak } from "../platform/readingTransport.ts";

type State = { phase: "idle" | "loading" | "playing" | "ready" }
  | { phase: "error"; message: string; retryable: boolean };
const idle: State = { phase: "idle" };
const errorMessages: Record<string, string> = {
  RATE_LIMITED: "Đã hết hạn mức đọc. Hãy thử lại sau.", TIMEOUT: "Tạo giọng đọc quá thời gian chờ.",
  SERVICE_UNAVAILABLE: "Giọng đọc AI chưa khả dụng. Với câu dài, hãy thử chọn đoạn ngắn hơn.",
  VALIDATION_FAILED: "Không thể xác minh văn bản hoặc cách đọc đã chọn.", INVALID_RESPONSE: "Âm thanh trả về không hợp lệ.",
};

/** All speech controls in one popup share the same pending request and playback. */
export function createSpeechSession(speak: Speak) {
  let current: {
    owner: object; update: (state: State) => void; abort: AbortController;
    context: AudioContext; buffer?: AudioBuffer; playing?: AudioBufferSourceNode; deadline?: ReturnType<typeof setTimeout>;
  } | undefined;
  type Request = NonNullable<typeof current>;

  function stopSource(request: Request) {
    const playing = request.playing;
    if (!playing) return;
    delete request.playing;
    playing.onended = null;
    playing.stop();
    playing.disconnect();
  }

  function release(owner?: object, notify = true): void {
    if (current === undefined || (owner && current.owner !== owner)) return;
    const previous = current;
    current = undefined;
    clearTimeout(previous.deadline);
    previous.abort.abort();
    stopSource(previous);
    void previous.context.close().catch(() => {});
    if (notify) previous.update(idle);
  }

  function playBuffer(request: Request) {
    if (!request.buffer || request.playing) return;
    if (request.context.state !== "running") {
      request.update({ phase: "ready" });
      return;
    }
    const node = request.context.createBufferSource();
    node.buffer = request.buffer;
    node.connect(request.context.destination);
    request.playing = node;
    node.onended = () => {
      node.disconnect();
      if (current === request && request.playing === node) {
        delete request.playing;
        request.update({ phase: "ready" });
      }
    };
    node.start();
    request.update({ phase: "playing" });
  }

  return {
    release,
    play(owner: object, source: SpeechSource, update: (state: State) => void) {
      if (current?.owner === owner) {
        if (!current.buffer || current.playing) return;
        const request = current;
        void request.context.resume().then(() => { if (current === request) playBuffer(request); }).catch(() => {});
        return;
      }
      release();
      const abort = new AbortController();
      let context: AudioContext;
      try { context = new AudioContext(); }
      catch {
        update({ phase: "error", message: "Trình duyệt không thể phát giọng đọc AI.", retryable: false });
        return;
      }
      const request: Request = { owner, update, abort, context };
      current = request;
      // Resume during the trusted click, then decode in memory without a URL subject to the host's media CSP.
      void context.resume().catch(() => {});
      update({ phase: "loading" });
      const fail = (error: unknown) => {
        if (current !== request) return;
        release(owner, false);
        const code = error instanceof AnalysisFailure ? error.code : "NETWORK_ERROR";
        update({ phase: "error", message: errorMessages[code] ?? "Không thể tạo giọng đọc AI.",
          retryable: !(error instanceof AnalysisFailure) || error.retryable });
      };
      request.deadline = setTimeout(() => fail(new AnalysisFailure("TIMEOUT", true)), 32000);
      const id = createInteractionId(bytes => crypto.getRandomValues(bytes));
      void Promise.resolve().then(() => speak(source, id, abort.signal)).then(async response => {
        if (current !== request) return;
        const bytes = decodeSpeechWav(response.audio_base64);
        const buffer = await context.decodeAudioData(bytes.buffer).catch(() => { throw new AnalysisFailure("INVALID_RESPONSE", true); });
        if (current !== request) return;
        clearTimeout(request.deadline);
        request.buffer = buffer;
        playBuffer(request);
      }).catch(fail);
    },
    stop(owner: object) {
      if (current?.owner !== owner) return;
      if (!current.buffer) { release(owner); return; }
      stopSource(current);
      current.update({ phase: "ready" });
    },
  };
}

export function SpeechPanel({ source, session }: { source: SpeechSource; session: ReturnType<typeof createSpeechSession> }) {
  const owner = useRef({}).current;
  const [state, setState] = useState<State>(idle);
  useLayoutEffect(() => () => session.release(owner, false), [session, owner]);
  return <div className="jp-speech">
    <p className="jp-muted">Giọng đọc AI. {source.kind === "selection" ? "Vùng bôi đen" : "Cách đọc đã chọn"} sẽ được gửi tới Google khi bạn nhấn Nghe. Google có thể dùng dữ liệu của gói miễn phí để cải thiện sản phẩm.</p>
    {state.phase !== "loading" && state.phase !== "playing" && (state.phase !== "error" || state.retryable) &&
      <button type="button" onClick={event => { if (event.nativeEvent.isTrusted) session.play(owner, source, setState); }}>
        {state.phase === "ready" ? "Nghe lại" : state.phase === "error" ? "Thử nghe lại" : source.kind === "selection" ? "Nghe vùng bôi đen" : "Nghe cách đọc"}
      </button>}
    {(state.phase === "loading" || state.phase === "playing") &&
      <button type="button" onClick={() => session.stop(owner)}>{state.phase === "loading" ? "Hủy tạo giọng đọc" : "Dừng đọc"}</button>}
    {state.phase === "loading" && <p role="status">Đang tạo giọng đọc…</p>}
    {state.phase === "error" && <p role="status">{state.message}</p>}
  </div>;
}
