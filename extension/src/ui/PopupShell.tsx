import { Fragment, useState, type ReactNode } from "react";
import type { AnalysisState } from "../interaction/analysisController.ts";
import type { CapabilityStatus, ConjugationExplanation, ConjugationSpan, EntryMatch, KanjiDetail, GrammarOccurrence, VocabularyOccurrence } from "../api/analysisContract.ts";
import type { SpeechSource } from "../api/readingContracts.ts";
import type { Translate } from "../platform/translationTransport.ts";
import { dictionarySpeechOptions } from "./dictionarySpeechOptions.ts";
import { TranslationPanel } from "./TranslationPanel.tsx";

interface PopupShellProps {
  readonly state: Exclude<AnalysisState, { phase: "hidden" }>;
  readonly onClose: () => void;
  readonly onRetry: () => void;
  readonly translate?: Translate | undefined;
  readonly controls?: ReactNode;
  readonly isScan?: boolean | undefined;
  readonly targetSpan?: ConjugationSpan | null | undefined;
  readonly scanOffset?: number | undefined;
  readonly renderSpeech?: RenderSpeech | undefined;
}

type RenderSpeech = (source: SpeechSource) => ReactNode;

const failures: Record<string, string> = {
  TIMEOUT: "Phân tích quá thời gian chờ.",
  INPUT_TOO_LONG: "Vùng chọn quá dài. Hãy chọn tối đa 1.000 ký tự để phân tích.",
  VALIDATION_FAILED: "Nội dung phân tích không hợp lệ.",
  INVALID_RESPONSE: "Dịch vụ trả về kết quả không hợp lệ.",
  SERVICE_UNAVAILABLE: "Dịch vụ phân tích tạm thời chưa khả dụng.",
  TRANSPORT_UNAVAILABLE: "Không thể kết nối Extension. Hãy tải lại trang.",
  NETWORK_ERROR: "Không thể kết nối dịch vụ phân tích.",
  RATE_LIMITED: "Đã đạt giới hạn tra từ. Hãy đợi rồi thử lại.",
};

function ResultSection({ title, status, empty, emptyMessage = "Không tìm thấy kết quả.", disclosure, notice, children }: {
  title: string;
  status: CapabilityStatus;
  empty: boolean;
  emptyMessage?: string;
  disclosure?: "open" | "closed" | undefined;
  notice?: ReactNode;
  children: ReactNode;
}) {
  if (status === "completed" && !empty && disclosure) {
    return <details className="jp-section jp-disclosure" open={disclosure === "open"}>
      <summary><h3>{title}</h3></summary>
      {children}
      {notice}
    </details>;
  }
  return <section className="jp-section">
    <h3>{title}</h3>
    {status !== "completed" ? <p className="jp-muted">{status === "failed" ? "Phân tích mục này gặp lỗi." : "Mục này chưa khả dụng."}</p>
      : empty ? <p className="jp-muted">{emptyMessage}</p> : children}
    {notice}
  </section>;
}

function entryKey(entry: EntryMatch): string {
  return JSON.stringify([entry.canonicalId, entry.matchedWrittenForm, entry.matchedReading]);
}

function ReadingControls({ entry, renderSpeech }: { entry: EntryMatch; renderSpeech: RenderSpeech }) {
  const options = dictionarySpeechOptions(entry);
  const [chosen, setChosen] = useState("");
  if (options.length === 0) return null;
  const key = (source: (typeof options)[number]["source"]) => `${source.reading_id}:${source.written_form_id ?? ""}`;
  const source = options.length === 1 ? options[0]?.source : options.find(option => key(option.source) === chosen)?.source;
  return <details className="jp-reading-controls jp-disclosure">
    <summary>Phát âm</summary>
    {options.length > 1 && <label>
      Cách đọc để nghe
      <select value={chosen} onChange={event => setChosen(event.target.value)}>
        <option value="">Chọn cách đọc</option>
        {options.map(option => <option key={key(option.source)} value={key(option.source)} lang="ja">{option.label}</option>)}
      </select>
    </label>}
    {source && <Fragment key={key(source)}>{renderSpeech(source)}</Fragment>}
  </details>;
}

function Vocabulary({ entry, surface, renderSpeech }: { entry: EntryMatch; surface: string; renderSpeech?: RenderSpeech | undefined }) {
  const forms = new Map(entry.writtenForms.map((form) => [form.id, form.form]));
  const readings = new Map(entry.readings.map((reading) => [reading.id, reading.readingText]));
  const writtenHeading = entry.matchedWrittenForm ?? (entry.writtenForms.map((item) => item.form).join("、") || undefined);
  const headword = writtenHeading ?? entry.matchedReading ?? entry.readings[0]?.readingText;
  return <article className="jp-entry" data-canonical-id={entry.canonicalId}>
    {headword !== undefined && headword !== surface && <h4 className="jp-headword">
      {writtenHeading !== undefined && <span className="jp-muted">Dạng từ điển </span>}<span lang="ja">{headword}</span>
    </h4>}
    {entry.readings.map((reading) => {
      const kanaOnly = reading.info?.includes("reading:no-kanji") === true;
      const allowedForms = reading.restrictedToForms?.join("、");
      const showReading = kanaOnly || Boolean(allowedForms) || (reading.readingText !== surface && reading.readingText !== headword);
      if (!showReading) return null;
      return <p key={reading.id} className="jp-reading">
        <span lang="ja">{reading.readingText}</span>
        {(kanaOnly || allowedForms) && " — "}
        {kanaOnly ? <small>chỉ dùng kana</small>
          : allowedForms && <small>chỉ dùng với <span lang="ja">{allowedForms}</span></small>}
      </p>;
    })}
    <ol>{entry.senses.map((sense) => <li key={sense.senseKey}>
      <span className="jp-pos">{sense.partOfSpeech.join(" · ")}</span>
      {sense.glosses.map((gloss, index) => <p key={index} lang={gloss.languageTag}>
        {gloss.languageTag !== "vi" && <span className="jp-language">{gloss.languageTag.toUpperCase()} </span>}{gloss.glossText}
      </p>)}
      {sense.restrictions.length > 0 && <p className="jp-restriction">Giới hạn nghĩa: <span lang="ja">{sense.restrictions.map((restriction) => [
        restriction.writtenFormId === null ? null : forms.get(restriction.writtenFormId) ?? `#${restriction.writtenFormId}`,
        restriction.readingId === null ? null : readings.get(restriction.readingId) ?? `#${restriction.readingId}`,
      ].filter((value) => value !== null).join(" / ")).join("; ")}</span></p>}
    </li>)}</ol>
    <details className="jp-entry-details jp-disclosure">
      <summary>Dạng viết, cách đọc và nguồn</summary>
      {entry.writtenForms.length > 0 && <ul aria-label="Dạng viết">
        {entry.writtenForms.map((item) => <li key={item.id}>
          <span lang="ja">{item.form}</span>{item.form === entry.matchedWrittenForm && <small> · Khớp dạng viết</small>}
          {item.info?.length ? <small> · {item.info.join(" · ")}</small> : null}
        </li>)}
      </ul>}
      {entry.readings.length > 0 && <ul aria-label="Cách đọc">
        {entry.readings.map((reading) => <li key={reading.id}>
          <span lang="ja">{reading.readingText}</span>{reading.readingText === entry.matchedReading && <small> · Khớp cách đọc</small>}
          {reading.info?.filter((value) => value !== "reading:no-kanji").map((value) => <small key={value}> · {value}</small>)}
        </li>)}
      </ul>}
      <p className="jp-muted">Mục từ: <code translate="no">{entry.canonicalId}</code></p>
    </details>
    {renderSpeech && <ReadingControls entry={entry} renderSpeech={renderSpeech} />}
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

function Conjugation({ item, alternative }: { item: ConjugationExplanation; alternative: boolean }) {
  return <article className="jp-entry" data-rule-id={item.rule_id}>
    {alternative && <p className="jp-muted">Cách hiểu có thể</p>}
    <h4 lang="ja">{item.surface} → {item.base_form}</h4>
    <p lang="vi">{item.explanation}</p>
    <ol>{item.segments.map(segment => <li key={segment.span.start}>
      <span lang="ja">{segment.surface}</span> — {segment.explanation}
    </li>)}</ol>
    <p className="jp-muted">Dữ liệu: {item.dataset_version}</p>
  </article>;
}

function Occurrence({ item, isScan, renderSpeech }: { item: VocabularyOccurrence; isScan: boolean; renderSpeech?: RenderSpeech | undefined }) {
  return <section className="jp-entry" data-occurrence-start={item.span.start} data-occurrence-end={item.span.end}>
    {!isScan && <h4 lang="ja">{item.surface}</h4>}
    {item.matches.length === 0 ? <p className="jp-muted">Không tìm thấy kết quả.</p>
      : item.matches.map((entry) => <Vocabulary key={entryKey(entry)} entry={entry} surface={item.surface} renderSpeech={renderSpeech} />)}
    {item.conjugations.length > 0 && <details className="jp-entry-details jp-disclosure" open>
      <summary>Biến đổi từ</summary>
      {item.conjugations.length > 1 && <p>Các cách hiểu có thể</p>}
      {item.conjugations.map(conjugation => <Conjugation key={JSON.stringify([conjugation.rule_id, conjugation.rule_version, conjugation.base_form])}
        item={conjugation} alternative={item.conjugations.length > 1} />)}
    </details>}
  </section>;
}

function spanKey(span: ConjugationSpan): string { return `${span.start}:${span.end}`; }

function OccurrenceResults({ occurrences, targetSpan, isScan, renderSpeech }: {
  occurrences: VocabularyOccurrence[];
  targetSpan: PopupShellProps["targetSpan"];
  isScan: boolean;
  renderSpeech?: RenderSpeech | undefined;
}) {
  const [chosenSpan, setChosenSpan] = useState<string>();
  const target = targetSpan && occurrences.find((item) => spanKey(item.span) === spanKey(targetSpan));
  // EARS[Event] YUI-02/YSC-10: A scan uses its verified target and never silently selects its neighbor.
  const active = occurrences.find((item) => spanKey(item.span) === chosenSpan) ??
    (isScan ? target : occurrences.find((item) => item.matches.length > 0) ?? occurrences[0]);
  if (!active) return null;

  return <>
    {!isScan && <ol className="jp-occurrences" role="list" aria-label="Từ trong vùng chọn">
      {occurrences.map((item, index) => {
        const key = spanKey(item.span);
        return <li key={key}>
          <button type="button" data-occurrence-choice={key} aria-pressed={key === spanKey(active.span)} onClick={() => setChosenSpan(key)}>
            <span className="jp-occurrence-number">{index + 1}<span className="jp-sr-only">. </span></span>
            <span lang="ja">{item.surface}</span>
          </button>
        </li>;
      })}
    </ol>}
    <Occurrence key={spanKey(active.span)} item={active} isScan={isScan} renderSpeech={renderSpeech} />
  </>;
}

export function PopupShell({ state, onClose, onRetry, translate, controls, isScan = false, targetSpan, scanOffset, renderSpeech }: PopupShellProps) {
  const data = state.phase === "result" ? state.response.data : null;
  const partial = data !== null && (data.vocabulary_truncated === true || Object.values(data.capabilities).some((status) => status !== "completed"));
  const occurrences = data?.vocabulary_occurrences;
  const noScanTarget = isScan && !targetSpan;
  const noVocabulary = noScanTarget || (occurrences === undefined
    ? data?.dictionary_matches.length === 0 : occurrences.length === 0);
  const emptyVocabularyMessage = isScan
    ? data?.vocabulary_truncated === true ? "Chưa xác định được từ tại điểm quét." : "Không tìm thấy từ tại điểm quét."
    : "Không tìm thấy kết quả.";
  const displayedText = isScan && targetSpan ? state.text.slice(targetSpan.start, targetSpan.end)
    : isScan ? "Tra từ tại con trỏ" : state.text;
  const scannedPoint = scanOffset === undefined ? undefined : state.text.codePointAt(scanOffset);
  const scannedKanjiText = targetSpan ? displayedText : scannedPoint === undefined ? "" : String.fromCodePoint(scannedPoint);
  const kanji = isScan ? data?.kanji.filter(item => scannedKanjiText.includes(item.character)) : data?.kanji;
  return <div className="jp-panel" role="dialog" aria-label="Tra cứu tiếng Nhật" lang="vi" dir="ltr">
    <header className="jp-header jp:flex jp:items-center jp:justify-between">
      <h2>Tra cứu tiếng Nhật</h2>
      <div className="jp-header-actions">{controls}
        <button type="button" onClick={onClose} aria-label="Đóng tra cứu">Đóng <span aria-hidden="true">×</span></button>
      </div>
    </header>
    <p className="jp-selected" lang={noScanTarget ? "vi" : "ja"}>{displayedText}</p>
    <div className="jp-content" aria-busy={state.phase === "loading"}>
      {state.phase === "loading" && <p role="status" className="jp-state">Đang phân tích…</p>}
      {state.phase === "error" && <div role="alert" className="jp-state">
        <p>{failures[state.code] ?? "Dịch vụ phân tích gặp lỗi."}</p>
        {state.retryable && <button type="button" className="jp-retry" onClick={event => { if (event.nativeEvent.isTrusted) onRetry(); }}>Thử lại</button>}
      </div>}
      {!isScan && translate && (state.phase === "result" || (state.phase === "error" && state.code !== "INPUT_TOO_LONG")) &&
        <TranslationPanel key={state.interactionId} text={state.text} interactionId={state.interactionId} translate={translate} />}
      {!isScan && renderSpeech && state.phase === "result" &&
        <details key={`speech:${state.interactionId}`} className="jp-section jp-disclosure">
          <summary><h3>Đọc vùng bôi đen</h3></summary>
          {renderSpeech({ kind: "selection", text: state.text.trim() })}
        </details>}
      {data !== null && <>
        <p role="status" className="jp-result-status">{partial ? "Có kết quả một phần" : "Đã phân tích"}</p>
        <ResultSection title="Từ vựng" status={data.capabilities.vocabulary} empty={noVocabulary} emptyMessage={emptyVocabularyMessage}
          notice={data.vocabulary_truncated === true && <p className="jp-muted">Chỉ hiển thị một phần từ vựng do giới hạn xử lý.</p>}>
          {occurrences === undefined
            ? data.dictionary_matches.map((entry) => <Vocabulary key={entryKey(entry)} entry={entry} surface={data.normalized_text} renderSpeech={renderSpeech} />)
            : <OccurrenceResults key={state.interactionId} occurrences={occurrences} targetSpan={targetSpan} isScan={isScan} renderSpeech={renderSpeech} />}
        </ResultSection>
        <ResultSection title="Kanji" status={data.capabilities.kanji} empty={kanji?.length === 0} disclosure={noVocabulary ? "open" : "closed"}>
          {kanji?.map((item) => <Kanji key={item.canonicalId} item={item} />)}
        </ResultSection>
        <ResultSection title="Ngữ pháp" status={data.capabilities.grammar} empty={data.grammar_occurrences.length === 0} disclosure={noVocabulary ? "open" : "closed"}>
          {data.grammar_occurrences.map((item, index) => <Grammar key={`${item.grammar_id}:${item.span.start}:${item.span.end}:${index}`} item={item} />)}
        </ResultSection>
        {occurrences === undefined && state.phase === "result" && state.response.meta.contract_version !== "1" && <ResultSection title="Biến đổi từ" status={data.capabilities.conjugation} empty={data.conjugations.length === 0} disclosure="open">
          {/* EARS[Event] F09 FR-007: Explain every evidenced reading without ranking meanings. */}
          {data.conjugations.length > 1 && <p>Các cách hiểu có thể</p>}
          {data.conjugations.map(item => <Conjugation key={JSON.stringify([item.rule_id, item.rule_version, item.base_form])}
            item={item} alternative={state.response.meta.contract_version === "3"} />)}
        </ResultSection>}
        {occurrences === undefined && (data.capabilities.conjugation === "completed"
          ? data.capabilities.morphology !== "completed" && <p className="jp-footnote">Tách từ: {data.capabilities.morphology === "failed" ? "gặp lỗi" : "chưa khả dụng"}.</p>
          : <p className="jp-footnote">Tách từ và biến đổi từ: {data.capabilities.morphology === "failed" || data.capabilities.conjugation === "failed" ? "gặp lỗi" : "chưa khả dụng"}.</p>)}
      </>}
    </div>
  </div>;
}
