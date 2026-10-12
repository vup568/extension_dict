import type { ReactNode } from "react";
import type { AnalysisState } from "../interaction/analysisController.ts";
import type { CapabilityStatus, EntryMatch, KanjiDetail, GrammarOccurrence } from "../api/analysisContract.ts";

interface PopupShellProps {
  readonly state: Exclude<AnalysisState, { phase: "hidden" }>;
  readonly onClose: () => void;
  readonly onRetry: () => void;
}

const failures: Record<string, string> = {
  TIMEOUT: "Phân tích quá thời gian chờ.",
  INPUT_TOO_LONG: "Vùng chọn quá dài. Hãy chọn tối đa 1.000 ký tự để phân tích.",
  VALIDATION_FAILED: "Nội dung phân tích không hợp lệ.",
  INVALID_RESPONSE: "Dịch vụ trả về kết quả không hợp lệ.",
  SERVICE_UNAVAILABLE: "Dịch vụ phân tích tạm thời chưa khả dụng.",
  TRANSPORT_UNAVAILABLE: "Không thể kết nối Extension. Hãy tải lại trang.",
  NETWORK_ERROR: "Không thể kết nối dịch vụ phân tích.",
};

function ResultSection({ title, status, empty, children }: {
  title: string; status: CapabilityStatus; empty: boolean; children: ReactNode;
}) {
  return <section className="jp-section">
    <h3>{title}</h3>
    {status !== "completed" ? <p className="jp-muted">{status === "failed" ? "Phân tích mục này gặp lỗi." : "Mục này chưa khả dụng."}</p>
      : empty ? <p className="jp-muted">Không tìm thấy kết quả.</p> : children}
  </section>;
}

function Vocabulary({ entry }: { entry: EntryMatch }) {
  const forms = new Map(entry.writtenForms.map((form) => [form.id, form.form]));
  const readings = new Map(entry.readings.map((reading) => [reading.id, reading.readingText]));
  return <article className="jp-entry" data-canonical-id={entry.canonicalId}>
    <h4 lang="ja">{entry.matchedWrittenForm ?? entry.matchedReading ?? entry.writtenForms[0]?.form ?? entry.readings[0]?.readingText}</h4>
    {entry.matchedWrittenForm !== null && <p className="jp-muted">Khớp dạng viết: <span lang="ja">{entry.matchedWrittenForm}</span></p>}
    {entry.matchedReading !== null && <p className="jp-muted">Khớp cách đọc: <span lang="ja">{entry.matchedReading}</span></p>}
    {entry.readings.map((reading) => <p key={reading.id} lang="ja" className="jp-reading">
      {reading.readingText}{(reading.restrictedToForms?.length ?? 0) > 0 && <small> — chỉ dùng với {reading.restrictedToForms?.join("、")}</small>}
    </p>)}
    <ol>{entry.senses.map((sense) => <li key={sense.senseKey}>
      <span className="jp-pos">{sense.partOfSpeech.join(" · ")}</span>
      {sense.glosses.map((gloss, index) => <p key={index} lang={gloss.languageTag}>
        {gloss.languageTag !== "vi" && <span className="jp-language">{gloss.languageTag.toUpperCase()} </span>}{gloss.glossText}
      </p>)}
      {sense.restrictions.length > 0 && <p className="jp-restriction">Giới hạn nghĩa: {sense.restrictions.map((restriction) => [
        restriction.writtenFormId === null ? null : forms.get(restriction.writtenFormId) ?? `dạng #${restriction.writtenFormId}`,
        restriction.readingId === null ? null : readings.get(restriction.readingId) ?? `cách đọc #${restriction.readingId}`,
      ].filter((value) => value !== null).join(" / ")).join("; ")}</p>}
    </li>)}</ol>
  </article>;
}

function Kanji({ item }: { item: KanjiDetail }) {
  const hasVietnamese = item.meaningsVi.length > 0;
  return <article className="jp-entry" data-canonical-id={item.canonicalId}>
    <h4 lang="ja">{item.character}{item.hanViet && <small lang="vi"> · {item.hanViet}</small>}</h4>
    <p lang={hasVietnamese ? "vi" : "en"}>{!hasVietnamese && item.meaningsEn.length > 0 && <span className="jp-language">EN </span>}{(hasVietnamese ? item.meaningsVi : item.meaningsEn).join("; ") || "Chưa có nghĩa."}</p>
    {item.onReadings.length > 0 && <p>On: <span lang="ja">{item.onReadings.join("、")}</span></p>}
    {item.kunReadings.length > 0 && <p>Kun: <span lang="ja">{item.kunReadings.join("、")}</span></p>}
    <p className="jp-muted">{item.strokeCount !== null && `${item.strokeCount} nét`}{item.jlptLevel !== null && ` · JLPT N${item.jlptLevel} (${item.jlptProvenance ?? "chưa rõ nguồn"})`}</p>
  </article>;
}

function Grammar({ item }: { item: GrammarOccurrence }) {
  return <article className="jp-entry" data-canonical-id={item.grammar_id}>
    <h4 lang="ja">{item.pattern} {item.jlpt_level && <small>{item.jlpt_level}</small>}</h4>
    <p lang="ja">{item.matched_text}</p>
    <p>{item.meaning_vi ?? "Chưa có nghĩa tiếng Việt."}</p>
    <p className="jp-muted">Vị trí {item.span.start}–{item.span.end}</p>
  </article>;
}

export function PopupShell({ state, onClose, onRetry }: PopupShellProps) {
  const data = state.phase === "result" ? state.response.data : null;
  const partial = data !== null && Object.values(data.capabilities).some((status) => status !== "completed");
  return <div className="jp-panel" role="dialog" aria-label="Tra cứu tiếng Nhật" lang="vi" dir="ltr">
    <header className="jp-header jp:flex jp:items-center jp:justify-between">
      <h2>Tra cứu tiếng Nhật</h2><button type="button" onClick={onClose} aria-label="Đóng tra cứu">Đóng <span aria-hidden="true">×</span></button>
    </header>
    <p className="jp-selected" lang="ja">{state.text}</p>
    <div className="jp-content" aria-busy={state.phase === "loading"}>
      {state.phase === "loading" && <p role="status" className="jp-state">Đang phân tích…</p>}
      {state.phase === "error" && <div role="alert" className="jp-state">
        <p>{failures[state.code] ?? "Dịch vụ phân tích gặp lỗi."}</p>
        {state.retryable && <button type="button" className="jp-retry" onClick={onRetry}>Thử lại</button>}
      </div>}
      {data !== null && <>
        <p role="status" className="jp-result-status">{partial ? "Có kết quả một phần" : "Đã phân tích"}</p>
        <ResultSection title="Từ vựng" status={data.capabilities.vocabulary} empty={data.dictionary_matches.length === 0}>
          {data.dictionary_matches.map((entry) => <Vocabulary key={entry.canonicalId} entry={entry} />)}
        </ResultSection>
        <ResultSection title="Kanji" status={data.capabilities.kanji} empty={data.kanji.length === 0}>
          {data.kanji.map((item) => <Kanji key={item.canonicalId} item={item} />)}
        </ResultSection>
        <ResultSection title="Ngữ pháp" status={data.capabilities.grammar} empty={data.grammar_occurrences.length === 0}>
          {data.grammar_occurrences.map((item, index) => <Grammar key={`${item.grammar_id}:${item.span.start}:${item.span.end}:${index}`} item={item} />)}
        </ResultSection>
        {(data.capabilities.morphology !== "completed" || data.capabilities.conjugation !== "completed") && <p className="jp-footnote">Tách từ và biến đổi từ: {data.capabilities.morphology === "failed" || data.capabilities.conjugation === "failed" ? "gặp lỗi" : "chưa khả dụng"}.</p>}
      </>}
    </div>
  </div>;
}
