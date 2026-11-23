import { Fragment, useState, type ReactNode } from "react";
import type { ConjugationExplanation, EntryMatch, Sense, VocabularyOccurrence } from "../api/analysisContract.ts";
import type { SpeechSource } from "../api/readingContracts.ts";
import { dictionarySpeechOptions } from "./dictionarySpeechOptions.ts";
import { partOfSpeechLabel } from "./partOfSpeechLabels.ts";
import { SpeakerIcon } from "./popupIcons.tsx";

export type RenderSpeech = (source: SpeechSource) => ReactNode;

export function entryKey(entry: EntryMatch): string {
  return JSON.stringify([entry.canonicalId, entry.matchedWrittenForm, entry.matchedReading]);
}

/** Speech stays behind an explicit toggle so the provider disclosure is read before anything is sent. */
function ReadingSpeech({ entry, renderSpeech }: { entry: EntryMatch; renderSpeech: RenderSpeech }) {
  const options = dictionarySpeechOptions(entry);
  const [open, setOpen] = useState(false);
  const [chosen, setChosen] = useState("");
  if (options.length === 0) return null;
  const key = (source: (typeof options)[number]["source"]) => `${source.reading_id}:${source.written_form_id ?? ""}`;
  // EARS[Event] YAU-02: a reading must be chosen explicitly when the entry has several applicable pairs.
  const source = options.length === 1 ? options[0]?.source : options.find(option => key(option.source) === chosen)?.source;
  return <>
    <button type="button" className="jp-icon-button jp-speech-toggle" aria-expanded={open}
      aria-label={open ? "Ẩn phần nghe cách đọc" : "Nghe cách đọc bằng giọng AI"} title="Nghe cách đọc bằng giọng AI"
      onClick={() => setOpen(value => !value)}><SpeakerIcon /></button>
    {open && <div className="jp-speech-box">
      {options.length > 1 && <div className="jp-reading-choices" role="radiogroup" aria-label="Cách đọc để nghe">
        {options.map(option => {
          const value = key(option.source);
          return <button key={value} type="button" role="radio" aria-checked={value === chosen} className="jp-reading-choice" lang="ja"
            onClick={() => setChosen(value)}>{option.label}</button>;
        })}
      </div>}
      {source ? <Fragment key={key(source)}>{renderSpeech(source)}</Fragment>
        : <p className="jp-fine">Chọn cách đọc trước khi nghe.</p>}
    </div>}
  </>;
}

interface SenseGroup { readonly partOfSpeech: readonly string[]; readonly senses: readonly { sense: Sense; number: number }[] }

// Consecutive senses with the same parts of speech share one tag row, as JMdict inherits them.
function senseGroups(senses: readonly Sense[]): SenseGroup[] {
  const groups: { partOfSpeech: readonly string[]; senses: { sense: Sense; number: number }[] }[] = [];
  senses.forEach((sense, index) => {
    const last = groups.at(-1);
    if (last && last.partOfSpeech.join("\u0000") === sense.partOfSpeech.join("\u0000")) last.senses.push({ sense, number: index + 1 });
    else groups.push({ partOfSpeech: sense.partOfSpeech, senses: [{ sense, number: index + 1 }] });
  });
  return groups;
}

/**
 * One dictionary entry: headword cluster, readings, meanings by sense and collapsed provenance.
 * The heading is omitted when it would only repeat the selected text shown in the popup header.
 */
export function EntryCard({ entry, surface, headerText, renderSpeech }: {
  entry: EntryMatch; surface: string; headerText: string; renderSpeech?: RenderSpeech | undefined;
}) {
  const forms = new Map(entry.writtenForms.map(form => [form.id, form.form]));
  const readings = new Map(entry.readings.map(reading => [reading.id, reading.readingText]));
  const writtenHeading = entry.matchedWrittenForm ?? (entry.writtenForms.map(item => item.form).join("、") || undefined);
  const headword = writtenHeading ?? entry.matchedReading ?? entry.readings[0]?.readingText;
  // EARS[State] YUI-03: surface, dictionary form and reading stay together without repeating one heading.
  const showHeading = headword !== undefined && (headword !== surface || headerText !== surface);
  return <article className="jp-entry" data-canonical-id={entry.canonicalId}>
    <div className="jp-entry-head">
      <div className="jp-entry-title">
        {showHeading && <h3 className="jp-headword">
          {writtenHeading !== undefined && headword !== surface && <span className="jp-headword-label">Dạng từ điển </span>}
          <span lang="ja">{headword}</span>
        </h3>}
        <div className="jp-readings">
          {entry.readings.map(reading => {
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
        </div>
      </div>
      {renderSpeech && <ReadingSpeech entry={entry} renderSpeech={renderSpeech} />}
    </div>
    {senseGroups(entry.senses).map(group => <Fragment key={group.senses[0]!.sense.senseKey}>
      {group.partOfSpeech.length > 0 && <p className="jp-tags">
        {group.partOfSpeech.map(value => <span key={value} className="jp-tag" title={`JMdict: ${value}`}>{partOfSpeechLabel(value)}</span>)}
      </p>}
      <ol className="jp-senses" start={group.senses[0]!.number}>
        {group.senses.map(({ sense, number }) => <li key={sense.senseKey}>
          <span className="jp-sense-number" aria-hidden="true">{number}</span>
          <div className="jp-sense-body">
            {sense.glosses.map((gloss, index) => <p key={index} lang={gloss.languageTag} className="jp-gloss">
              {gloss.languageTag !== "vi" && <span className="jp-language">{gloss.languageTag.toUpperCase()} </span>}{gloss.glossText}
            </p>)}
            {sense.restrictions.length > 0 && <p className="jp-restriction">Giới hạn nghĩa: <span lang="ja">{sense.restrictions.map(restriction => [
              restriction.writtenFormId === null ? null : forms.get(restriction.writtenFormId) ?? `#${restriction.writtenFormId}`,
              restriction.readingId === null ? null : readings.get(restriction.readingId) ?? `#${restriction.readingId}`,
            ].filter(value => value !== null).join(" / ")).join("; ")}</span></p>}
          </div>
        </li>)}
      </ol>
    </Fragment>)}
    <details className="jp-entry-details">
      <summary>Dạng viết, cách đọc và nguồn</summary>
      {entry.writtenForms.length > 0 && <ul aria-label="Dạng viết">
        {entry.writtenForms.map(item => <li key={item.id}>
          <span lang="ja">{item.form}</span>{item.form === entry.matchedWrittenForm && <small> · Khớp dạng viết</small>}
          {item.info?.length ? <small> · {item.info.join(" · ")}</small> : null}
        </li>)}
      </ul>}
      {entry.readings.length > 0 && <ul aria-label="Cách đọc">
        {entry.readings.map(reading => <li key={reading.id}>
          <span lang="ja">{reading.readingText}</span>{reading.readingText === entry.matchedReading && <small> · Khớp cách đọc</small>}
          {reading.info?.filter(value => value !== "reading:no-kanji").map(value => <small key={value}> · {value}</small>)}
        </li>)}
      </ul>}
      <p className="jp-fine">Mục từ: <code translate="no">{entry.canonicalId}</code></p>
    </details>
  </article>;
}

/** Explains how the selected surface relates to its dictionary form; shown only with backend evidence. */
export function ConjugationBox({ items, alternative }: { items: readonly ConjugationExplanation[]; alternative: boolean }) {
  if (items.length === 0) return null;
  return <section className="jp-conjugation">
    <h3 className="jp-eyebrow">Biến đổi từ</h3>
    {/* EARS[Event] F09 FR-007: Explain every evidenced reading without ranking meanings. */}
    {items.length > 1 && <p className="jp-fine">Các cách hiểu có thể</p>}
    {items.map(item => <article key={JSON.stringify([item.rule_id, item.rule_version, item.base_form])} className="jp-conjugation-item" data-rule-id={item.rule_id}>
      {alternative && <p className="jp-fine">Cách hiểu có thể</p>}
      <p className="jp-conjugation-title" lang="ja">{`${item.surface} → ${item.base_form}`}</p>
      <p lang="vi">{item.explanation}</p>
      <ol className="jp-chain">{item.segments.map(segment => <li key={segment.span.start}>
        <span lang="ja" className="jp-chain-text">{segment.surface}</span>
        <span className="jp-chain-explanation">{segment.explanation}</span>
      </li>)}</ol>
      <p className="jp-fine">Dữ liệu: {item.dataset_version}</p>
    </article>)}
  </section>;
}

/** Every matching entry stays visible; several homographs are announced instead of ranked by position. */
export function EntryList({ entries, surface, headerText, renderSpeech }: {
  entries: readonly EntryMatch[]; surface: string; headerText: string; renderSpeech?: RenderSpeech | undefined;
}) {
  return <>
    {entries.length > 1 && <p className="jp-scope">{entries.length} mục từ cùng khớp với <span lang="ja">「{surface}」</span>. Xem từng mục để chọn nghĩa phù hợp.</p>}
    {entries.map(entry => <EntryCard key={entryKey(entry)} entry={entry} surface={surface} headerText={headerText} renderSpeech={renderSpeech} />)}
  </>;
}

/** Detail of the occurrence chosen in the header sentence (or the verified scan target). */
export function OccurrenceDetail({ item, headerText, renderSpeech }: {
  item: VocabularyOccurrence; headerText: string; renderSpeech?: RenderSpeech | undefined;
}) {
  return <section className="jp-occurrence" data-occurrence-start={item.span.start} data-occurrence-end={item.span.end}>
    <ConjugationBox items={item.conjugations} alternative={item.conjugations.length > 1} />
    {item.matches.length === 0 ? <p className="jp-muted">Không tìm thấy kết quả.</p>
      : <EntryList entries={item.matches} surface={item.surface} headerText={headerText} renderSpeech={renderSpeech} />}
  </section>;
}
