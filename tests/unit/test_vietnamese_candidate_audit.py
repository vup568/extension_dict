import gzip
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[2] / "scripts" / "audit-vietnamese-candidates.py"
SPEC = importlib.util.spec_from_file_location("vietnamese_candidate_audit", SCRIPT)
AUDIT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(AUDIT)


class VietnameseCandidateAuditTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.jmdict = self.root / "jmdict.gz"
        self.vietnamese = self.root / "vietnamese.tab"
        with gzip.open(self.jmdict, "wb") as stream:
            stream.write("""<JMdict>
              <entry><ent_seq>1</ent_seq><k_ele><keb>橋</keb></k_ele><r_ele><reb>はし</reb></r_ele></entry>
              <entry><ent_seq>2</ent_seq><k_ele><keb>箸</keb></k_ele><r_ele><reb>はし</reb></r_ele></entry>
              <entry><ent_seq>3</ent_seq><k_ele><keb>かな</keb></k_ele><r_ele><reb>かな</reb></r_ele></entry>
              </JMdict>""".encode("utf-8"))
        self.vietnamese.write_text("はし\tmeaning one\n橋\tmeaning two\nかな\tmeaning three\n未知\tmeaning four\n", encoding="utf-8")
        self.out = self.root / "candidates.jsonl"
        self.summary = self.root / "summary.json"

    def tearDown(self):
        self.tmp.cleanup()

    def generate(self, jmdict_hash=None):
        return AUDIT.generate(self.jmdict, self.vietnamese, jmdict_hash or AUDIT.sha256_file(self.jmdict),
                              AUDIT.sha256_file(self.vietnamese), self.out, self.summary)

    def test_homographs_and_unique_spelling_are_all_unapproved(self):
        result = self.generate()
        rows = [json.loads(line) for line in self.out.read_text(encoding="utf-8").splitlines()]
        self.assertEqual((result["jmdict_entries"], result["vietnamese_records"]), (3, 4))
        self.assertEqual((result["no_candidates"], result["one_candidate"], result["multiple_candidates"]), (1, 2, 1))
        self.assertEqual(rows[0]["candidates"], [{"jmdict_ent_seq": "1", "matched_as": ["reading"]},
                                                  {"jmdict_ent_seq": "2", "matched_as": ["reading"]}])
        self.assertEqual(rows[2]["candidates"][0]["matched_as"], ["reading", "written"])
        self.assertTrue(all(row["disposition"] == "quarantined" for row in rows))
        self.assertEqual(result["approved_mappings"], 0)
        self.assertEqual(result["candidates_sha256"], AUDIT.sha256_file(self.out))

    def test_wrong_hash_does_not_create_output(self):
        with self.assertRaisesRegex(ValueError, "SHA-256 mismatch"):
            self.generate("0" * 64)
        self.assertFalse(self.out.exists())
        self.assertFalse(self.summary.exists())

    def test_existing_output_is_never_overwritten(self):
        self.out.write_text("previous content", encoding="utf-8")
        with self.assertRaises(FileExistsError):
            self.generate()
        self.assertEqual(self.out.read_text(encoding="utf-8"), "previous content")


if __name__ == "__main__":
    unittest.main()
