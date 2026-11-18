#!/usr/bin/env python3
"""Produce unapproved exact-headword candidates for offline human review.

This does not create glosses or infer equivalence of meanings. Inputs are bound
to explicit SHA-256 hashes and output files are created exclusively.
"""

import argparse
import gzip
import hashlib
import json
import xml.etree.ElementTree as ET
from pathlib import Path


def sha256_file(path):
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def index_entries(jmdict):
    # The file hash is verified before parsing. No external entity resolver is used.
    index = {}
    count = 0
    with gzip.open(jmdict, "rb") as source:
        for _, entry in ET.iterparse(source, events=("end",)):
            if entry.tag != "entry":
                continue
            identity = entry.findtext("ent_seq")
            if not identity or not identity.isascii() or not identity.isdecimal():
                raise ValueError("JMdict entry has invalid identity")
            count += 1
            for role, path in (("written", "k_ele/keb"), ("reading", "r_ele/reb")):
                for element in entry.findall(path):
                    if element.text:
                        index.setdefault(element.text, {}).setdefault(identity, set()).add(role)
            entry.clear()
    return index, count


def generate(jmdict, vietnamese, expected_jmdict_hash, expected_vietnamese_hash, output, summary):
    actual_jmdict_hash = sha256_file(jmdict)
    actual_vietnamese_hash = sha256_file(vietnamese)
    if actual_jmdict_hash != expected_jmdict_hash.lower() or actual_vietnamese_hash != expected_vietnamese_hash.lower():
        raise ValueError("Input SHA-256 mismatch")
    if output.resolve() == summary.resolve():
        raise ValueError("Output paths must be distinct")
    index, entries = index_entries(jmdict)
    totals = {"vietnamese_records": 0, "no_candidates": 0, "one_candidate": 0, "multiple_candidates": 0}
    with vietnamese.open("r", encoding="utf-8", errors="strict", newline="") as source, output.open("x", encoding="utf-8", newline="\n") as dest:
        for ordinal, line in enumerate(source, 1):
            raw = line.rstrip("\r\n")
            headword, separator, content = raw.partition("\t")
            if not separator or not headword or not content:
                raise ValueError(f"Malformed Vietnamese record at ordinal {ordinal}")
            matches = index.get(headword, {})
            candidates = [{"jmdict_ent_seq": key, "matched_as": sorted(matches[key])}
                          for key in sorted(matches, key=int)]
            category = "no_candidates" if not candidates else "one_candidate" if len(candidates) == 1 else "multiple_candidates"
            totals["vietnamese_records"] += 1
            totals[category] += 1
            row = {"source_identity": f"{actual_vietnamese_hash}:{ordinal}", "ordinal": ordinal,
                   "headword": headword, "candidates": candidates, "disposition": "quarantined",
                   "reason": "exact-headword-only-no-sense-or-license-approval"}
            dest.write(json.dumps(row, ensure_ascii=False, separators=(",", ":")) + "\n")
    result = {"schema_version": 1, "method": "exact-written-form-or-reading-only",
              "jmdict_sha256": actual_jmdict_hash, "vietnamese_sha256": actual_vietnamese_hash,
              "jmdict_entries": entries, **totals, "approved_mappings": 0,
              "candidates_sha256": sha256_file(output)}
    with summary.open("x", encoding="utf-8", newline="\n") as dest:
        json.dump(result, dest, ensure_ascii=False, indent=2)
        dest.write("\n")
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--jmdict", required=True, type=Path)
    parser.add_argument("--vietnamese", required=True, type=Path)
    parser.add_argument("--jmdict-sha256", required=True)
    parser.add_argument("--vietnamese-sha256", required=True)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--summary", required=True, type=Path)
    args = parser.parse_args()
    print(json.dumps(generate(args.jmdict, args.vietnamese, args.jmdict_sha256,
                              args.vietnamese_sha256, args.output, args.summary), ensure_ascii=False))


if __name__ == "__main__":
    main()
