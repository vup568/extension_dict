import { useId, useState, type ReactNode } from "react";
import type { AnalysisState } from "../interaction/analysisController.ts";
import type { CapabilityStatus, ConjugationSpan, VocabularyOccurrence } from "../api/analysisContract.ts";
import type { Translate } from "../platform/translationTransport.ts";
import { CloseIcon, PointerIcon, WarningIcon } from "./popupIcons.tsx";
import { panelId, PopupTabs, tabId, type PopupTab } from "./PopupTabs.tsx";
import { GrammarList, KanjiList } from "./ReferencePanels.tsx";
import { sentenceSegments, type Highlight } from "./sentenceSegments.ts";
import { TranslationPanel } from "./TranslationPanel.tsx";
import { ConjugationBox, EntryList, OccurrenceDetail, type RenderSpeech } from "./VocabularyPanel.tsx";

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

type TabKey = "vocabulary" | "kanji" | "grammar" | "sentence";

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

const capabilityMessage = (status: CapabilityStatus) => status === "failed" ? "Phân tích mục này gặp lỗi." : "Mục này chưa khả dụng.";
const capabilityWarning = (status: CapabilityStatus) => status === "failed" ? "gặp lỗi" : status === "unavailable" ? "chưa khả dụng" : undefined;
const spanKey = (span: { start: number; end: number }) => `${span.start}:${span.end}`;

function TabPanel({ idBase, tab, active, children }: { idBase: string; tab: TabKey; active: boolean; children: ReactNode }) {
  // Inactive panels stay mounted (hidden) so switching tabs never cancels a translation or an audio playback.
  return <div className="jp-tabpanel" role="tabpanel" id={panelId(idBase, tab)} aria-labelledby={tabId(idBase, tab)} hidden={!active}>{children}</div>;
}

/** The selection is the popup title; in a sentence each analysed word is a button that opens its detail. */
function SelectedText({ text, occurrences, activeKey, interactive, highlights, onChoose }: {
  text: string; occurrences: readonly VocabularyOccurrence[]; activeKey: string | undefined; interactive: boolean;
  highlights: readonly Highlight[]; onChoose: (key: string) => void;
}) {
  const segments = sentenceSegments(text, interactive ? occurrences.map(item => item.span) : [], highlights);
  const parts = (items: NonNullable<typeof segments>[number]["parts"]) =>
    items.map((part, index) => <span key={index} className={part.className || undefined}>{part.text}</span>);
  if (segments === null) return <p className="jp-selected" lang="ja">{text}</p>;
  return <p className="jp-selected" lang="ja" {...(interactive ? { role: "group", "aria-label": "Từ trong vùng chọn" } : {})}>
    {segments.map(segment => {
      if (segment.kind === "plain") return <span key={`plain:${segment.start}`}>{parts(segment.parts)}</span>;
      const key = spanKey(segment);
      const active = key === activeKey;
      // EARS[Event] YUI-07: every position is its own choice, so a repeated word opens its own analysis.
      return <button key={key} type="button" className={active ? "jp-word jp-word-active" : "jp-word"} data-occurrence-choice={key}
        aria-pressed={active} onClick={() => onChoose(key)}>{parts(segment.parts)}</button>;
    })}
  </p>;
}

export function PopupShell({ state, onClose, onRetry, translate, controls, isScan = false, targetSpan, scanOffset, renderSpeech }: PopupShellProps) {
  const idBase = `jp${useId().replace(/[^a-zA-Z0-9_-]/g, "")}`;
  const [chosenTab, setChosenTab] = useState<TabKey>();
  const [chosenSpan, setChosenSpan] = useState<string>();
  const [grammarIndex, setGrammarIndex] = useState(-1);

  const data = state.phase === "result" ? state.response.data : null;
  const contract = state.phase === "result" ? state.response.meta.contract_version : undefined;
  // Occurrence and grammar spans are offsets into normalized_text, so a result is always shown in that form.
  const headerText = data?.normalized_text ?? state.text;
  const occurrences = data?.vocabulary_occurrences;
  const noScanTarget = isScan && !targetSpan;
  // EARS[Event] YUI-02/YSC-10: a scan uses its verified target and never silently selects its neighbor.
  const active = occurrences === undefined ? undefined : isScan
    ? (targetSpan ? occurrences.find(item => spanKey(item.span) === spanKey(targetSpan)) : undefined)
    : occurrences.find(item => spanKey(item.span) === chosenSpan) ?? occurrences.find(item => item.matches.length > 0) ?? occurrences[0];
  const wholeWord = occurrences?.length === 1 && occurrences[0]!.span.start === 0 && occurrences[0]!.span.end === headerText.length;
  const interactive = !isScan && occurrences !== undefined && occurrences.length > 0 && !wholeWord;
  const noVocabulary = noScanTarget || (occurrences === undefined ? data?.dictionary_matches.length === 0 : occurrences.length === 0);
  const entries = active ? active.matches : occurrences === undefined ? data?.dictionary_matches ?? [] : [];

  // The Kanji tab follows the word being read; a legacy single-word result keeps every kanji it returned.
  const scannedPoint = scanOffset === undefined ? undefined : headerText.codePointAt(scanOffset);
  const kanjiScope = isScan ? (targetSpan ? headerText.slice(targetSpan.start, targetSpan.end) : scannedPoint === undefined ? "" : String.fromCodePoint(scannedPoint))
    : active?.surface;
  const kanji = data === null ? [] : kanjiScope === undefined ? data.kanji : data.kanji.filter(item => kanjiScope.includes(item.character));
  const grammar = data?.grammar_occurrences ?? [];

  const tabs: PopupTab<TabKey>[] = data === null ? [
    { key: "vocabulary", label: "Từ vựng" }, { key: "kanji", label: "Kanji" }, { key: "grammar", label: "Ngữ pháp" },
  ] : [
    { key: "vocabulary", label: "Từ vựng", count: entries.length > 1 ? entries.length : undefined,
      warning: capabilityWarning(data.capabilities.vocabulary) ?? (data.vocabulary_truncated === true ? "một phần" : undefined) },
    { key: "kanji", label: "Kanji", count: data.capabilities.kanji === "completed" ? kanji.length : undefined, warning: capabilityWarning(data.capabilities.kanji) },
    { key: "grammar", label: "Ngữ pháp", count: data.capabilities.grammar === "completed" ? grammar.length : undefined, warning: capabilityWarning(data.capabilities.grammar) },
  ];
  if (!isScan && (translate || renderSpeech)) tabs.push({ key: "sentence", label: "Câu" });
  // Every interaction opens on Từ vựng, so a "not found" message is never hidden behind another tab.
  const tab = tabs.some(item => item.key === chosenTab) ? chosenTab! : "vocabulary";
  const otherResults = [kanji.length > 0 && data?.capabilities.kanji === "completed" ? "Kanji" : null,
    grammar.length > 0 && data?.capabilities.grammar === "completed" ? "Ngữ pháp" : null].filter(Boolean).join(" và ");

  const highlights: Highlight[] = [];
  if (isScan && targetSpan) highlights.push({ ...targetSpan, className: "jp-target" });
  const highlightedGrammar = tab === "grammar" ? grammar[grammarIndex] : undefined;
  if (highlightedGrammar) highlights.push({ ...highlightedGrammar.span, className: "jp-grammar-highlight" });

  const partial = data !== null && (data.vocabulary_truncated === true || Object.values(data.capabilities).some(status => status !== "completed"));
  const emptyVocabularyMessage = isScan
    ? data?.vocabulary_truncated === true ? "Chưa xác định được từ tại điểm quét." : "Không tìm thấy từ tại điểm quét."
    : "Không tìm thấy kết quả.";
  // EARS[Unwanted] YSC: a pointer scan never offers translation, even after a failed analysis.
  const canTranslate = !isScan && translate !== undefined && (state.phase === "result" || (state.phase === "error" && state.code !== "INPUT_TOO_LONG"));

  return <div className="jp-panel" role="dialog" aria-label="Tra cứu tiếng Nhật" lang="vi" dir="ltr">
    <header className="jp-header">
      <div className="jp-header-main">
        {isScan && <p className="jp-mode"><PointerIcon />Tra bằng con trỏ</p>}
        <SelectedText text={headerText} occurrences={occurrences ?? []} activeKey={active && spanKey(active.span)} interactive={interactive}
          highlights={highlights} onChoose={key => setChosenSpan(key)} />
      </div>
      <div className="jp-header-actions">{controls}
        <button type="button" className="jp-icon-button" onClick={onClose} aria-label="Đóng tra cứu" title="Đóng (Esc)"><CloseIcon /></button>
      </div>
    </header>

    {state.phase === "loading" && <>
      <PopupTabs tabs={tabs} active="vocabulary" onSelect={() => {}} idBase={idBase} disabled />
      <div className="jp-body" aria-busy="true">
        <p role="status" className="jp-status"><span className="jp-spinner" aria-hidden="true" />Đang phân tích…</p>
        <div className="jp-skeleton" aria-hidden="true"><span /><span /><span /><span /><span /></div>
      </div>
    </>}

    {state.phase === "error" && <div className="jp-body">
      <div role="alert" className="jp-alert">
        <p className="jp-alert-title"><WarningIcon />{failures[state.code] ?? "Dịch vụ phân tích gặp lỗi."}</p>
        {state.retryable && <button type="button" className="jp-button jp-button-primary" onClick={event => { if (event.nativeEvent.isTrusted) onRetry(); }}>Thử lại</button>}
      </div>
      {canTranslate && <TranslationPanel key={state.interactionId} text={state.text} interactionId={state.interactionId} translate={translate} />}
    </div>}

    {data !== null && state.phase === "result" && <>
      <PopupTabs tabs={tabs} active={tab} onSelect={setChosenTab} idBase={idBase} />
      <p role="status" className="jp-sr-only">{partial ? "Có kết quả một phần" : "Đã phân tích"}</p>

      <TabPanel idBase={idBase} tab="vocabulary" active={tab === "vocabulary"}>
        {data.capabilities.vocabulary !== "completed" ? <p className="jp-muted">{capabilityMessage(data.capabilities.vocabulary)}</p> : <>
          {data.vocabulary_truncated === true && <p className="jp-banner"><WarningIcon /><span>Chỉ hiển thị một phần từ vựng do giới hạn xử lý.</span></p>}
          {noVocabulary ? <>
            <p className="jp-muted">{emptyVocabularyMessage}</p>
            {otherResults && <p className="jp-fine">Vùng chọn vẫn có kết quả trong tab {otherResults}.</p>}
          </>
            : occurrences === undefined ? <>
              {contract !== "1" && data.capabilities.conjugation === "completed" &&
                <ConjugationBox items={data.conjugations} alternative={contract === "3"} />}
              <EntryList entries={data.dictionary_matches} surface={data.normalized_text} headerText={headerText} renderSpeech={renderSpeech} />
            </>
            : active && <OccurrenceDetail key={spanKey(active.span)} item={active} headerText={headerText} renderSpeech={renderSpeech} />}
        </>}
        {occurrences === undefined && (data.capabilities.conjugation === "completed"
          ? data.capabilities.morphology !== "completed" && <p className="jp-footnote">Tách từ: {data.capabilities.morphology === "failed" ? "gặp lỗi" : "chưa khả dụng"}.</p>
          : <p className="jp-footnote">Tách từ và biến đổi từ: {data.capabilities.morphology === "failed" || data.capabilities.conjugation === "failed" ? "gặp lỗi" : "chưa khả dụng"}.</p>)}
      </TabPanel>

      <TabPanel idBase={idBase} tab="kanji" active={tab === "kanji"}>
        {data.capabilities.kanji !== "completed" ? <p className="jp-muted">{capabilityMessage(data.capabilities.kanji)}</p>
          : kanji.length === 0 ? <p className="jp-muted">{interactive ? "Từ đang chọn không có chữ Hán. Chọn từ khác trong câu để xem kanji." : "Không tìm thấy kết quả."}</p>
          : <>
            {kanjiScope && kanjiScope !== headerText && <p className="jp-scope">Kanji trong <span lang="ja">「{kanjiScope}」</span></p>}
            <KanjiList key={kanjiScope ?? ""} items={kanji} />
          </>}
      </TabPanel>

      <TabPanel idBase={idBase} tab="grammar" active={tab === "grammar"}>
        {data.capabilities.grammar !== "completed" ? <p className="jp-muted">{capabilityMessage(data.capabilities.grammar)}</p>
          : grammar.length === 0 ? <p className="jp-muted">Không tìm thấy kết quả.</p>
          : <GrammarList items={grammar} selected={highlightedGrammar ? grammarIndex : -1} onSelect={setGrammarIndex} />}
      </TabPanel>

      {tabs.some(item => item.key === "sentence") && <TabPanel idBase={idBase} tab="sentence" active={tab === "sentence"}>
        {canTranslate && <TranslationPanel key={state.interactionId} text={state.text} interactionId={state.interactionId} translate={translate} />}
        {renderSpeech && <section className="jp-block" aria-label="Nghe vùng bôi đen">
          <h3 className="jp-block-title">Nghe vùng bôi đen <span className="jp-chip">Giọng AI</span></h3>
          {renderSpeech({ kind: "selection", text: state.text.trim() })}
        </section>}
      </TabPanel>}
    </>}
  </div>;
}
