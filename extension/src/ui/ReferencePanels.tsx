import { useState } from "react";
import type { GrammarOccurrence, KanjiDetail } from "../api/analysisContract.ts";

function KanjiCard({ item }: { item: KanjiDetail }) {
  const hasVietnamese = item.meaningsVi.length > 0;
  const meta = [item.strokeCount !== null ? `${item.strokeCount} nét` : null,
    item.jlptLevel !== null ? `JLPT N${item.jlptLevel} (${item.jlptProvenance ?? "chưa rõ nguồn"})` : null].filter(Boolean).join(" · ");
  return <article className="jp-kanji" data-canonical-id={item.canonicalId}>
    <div className="jp-kanji-glyph" lang="ja">{item.character}</div>
    <div className="jp-kanji-info">
      {item.hanViet && <p className="jp-kanji-hanviet"><span className="jp-kanji-label">Hán Việt</span><span lang="vi">{item.hanViet}</span></p>}
      <p lang={hasVietnamese ? "vi" : "en"}>{!hasVietnamese && item.meaningsEn.length > 0 && <span className="jp-language">EN </span>}
        {(hasVietnamese ? item.meaningsVi : item.meaningsEn).join("; ") || "Chưa có nghĩa."}</p>
      {(item.onReadings.length > 0 || item.kunReadings.length > 0) && <dl className="jp-kanji-readings">
        {item.onReadings.length > 0 && <><dt>On</dt><dd lang="ja">{item.onReadings.join("、")}</dd></>}
        {item.kunReadings.length > 0 && <><dt>Kun</dt><dd lang="ja">{item.kunReadings.join("、")}</dd></>}
      </dl>}
      {meta && <p className="jp-fine">{meta}</p>}
    </div>
  </article>;
}

/** Kanji of the word being read: one detail at a time, with tiles to switch when the word has several. */
export function KanjiList({ items }: { items: readonly KanjiDetail[] }) {
  const [chosen, setChosen] = useState<string>();
  const active = items.find(item => item.canonicalId === chosen) ?? items[0];
  if (!active) return null;
  return <>
    {items.length > 1 && <div className="jp-kanji-tiles" role="group" aria-label="Chọn kanji">
      {items.map(item => <button key={item.canonicalId} type="button" className="jp-kanji-tile" aria-pressed={item === active}
        onClick={() => setChosen(item.canonicalId)}>
        <span lang="ja" className="jp-kanji-tile-char">{item.character}</span>
        {item.hanViet && <span className="jp-kanji-tile-hanviet" lang="vi">{item.hanViet}</span>}
      </button>)}
    </div>}
    <KanjiCard item={active} />
  </>;
}

/** Grammar patterns in the selection; choosing one highlights its matched span in the header sentence. */
export function GrammarList({ items, selected, onSelect }: {
  items: readonly GrammarOccurrence[]; selected: number; onSelect: (index: number) => void;
}) {
  return <>
    <p className="jp-fine jp-hint">{selected >= 0 ? "Đoạn khớp đang được tô trong câu. Bấm lại để bỏ tô." : "Bấm một mẫu để tô đoạn khớp trong câu."}</p>
    <div className="jp-grammar-list">
      {items.map((item, index) => <button key={`${item.grammar_id}:${item.span.start}:${item.span.end}:${index}`} type="button"
        className="jp-grammar" aria-pressed={index === selected} data-canonical-id={item.grammar_id}
        onClick={() => onSelect(index === selected ? -1 : index)}>
        <span className="jp-grammar-top"><span lang="ja" className="jp-grammar-pattern">{item.pattern}</span>
          {item.jlpt_level && <span className="jp-chip">{item.jlpt_level}</span>}</span>
        <span>{item.meaning_vi ?? "Chưa có nghĩa tiếng Việt."}</span>
        <span className="jp-grammar-match">Trong câu: <span lang="ja">{item.matched_text}</span></span>
      </button>)}
    </div>
  </>;
}
