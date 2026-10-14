"""Runtime regressions; run `python -m unittest -v test_tokenizer` in the image."""
import importlib.util
import pathlib
import sys
import threading
import time
import unittest
from concurrent.futures import ThreadPoolExecutor
from unittest.mock import Mock, patch

import grpc

import app
import tokenizer_pb2 as proto


class Aborted(grpc.RpcError):
    def __init__(self, code, details):
        self.status = code
        self.details = details


class Context:
    def __init__(self, active=True):
        self.active = active

    def is_active(self):
        return self.active

    def abort(self, code, details):
        raise Aborted(code, details)


class TokenizerTests(unittest.TestCase):
    def tokenize(self, text, mode="C", context=None):
        return app.TokenizerServicer().Tokenize(
            proto.TokenizeRequest(text=text, mode=mode), context or Context()
        )

    def assert_abort(self, status, text, mode="C", context=None):
        with self.assertRaises(Aborted) as caught:
            self.tokenize(text, mode, context)
        self.assertEqual(status, caught.exception.status)
        return caught.exception

    def test_dictionary_form_and_complete_pos(self):
        result = self.tokenize("食べました")
        self.assertEqual(["食べ", "まし", "た"], [t.surface for t in result.tokens])
        self.assertEqual("食べる", result.tokens[0].base_form)
        self.assertTrue(all(len(t.pos.split(",")) == 6 for t in result.tokens))

    def test_supplementary_spans_match_utf16_slices(self):
        for text in ("😀食べました", "𠮷野で食べました", "😀𠮷食べました"):
            with self.subTest(text=text):
                wire_text = text.encode("utf-16-le")
                result = self.tokenize(text)
                previous_end = 0
                for token in result.tokens:
                    self.assertEqual(previous_end, token.span_start)
                    surface = wire_text[token.span_start * 2:token.span_end * 2].decode("utf-16-le")
                    self.assertEqual(token.surface, surface)
                    previous_end = token.span_end
                self.assertEqual(len(wire_text) // 2, previous_end)

    def test_empty_input_with_available_provider(self):
        self.assertEqual([], list(self.tokenize("").tokens))

    def test_modes_are_validated_and_default_is_c(self):
        for mode in ("", "A", "B", "C", "c"):
            self.assertTrue(self.tokenize("食べました", mode).tokens)
        self.assert_abort(grpc.StatusCode.INVALID_ARGUMENT, "食べました", "invalid")

    def test_input_bound_counts_utf16_units(self):
        self.assertTrue(self.tokenize("食" * 2000).tokens)
        self.assert_abort(grpc.StatusCode.INVALID_ARGUMENT, "食" * 2001)
        self.assert_abort(grpc.StatusCode.INVALID_ARGUMENT, "😀" * 1001)

    def test_missing_provider_aborts_even_for_empty_input(self):
        with patch.object(app, "_tokenizer", None):
            self.assert_abort(grpc.StatusCode.UNAVAILABLE, "食べました")
            self.assert_abort(grpc.StatusCode.UNAVAILABLE, "")

    def test_initialization_failure_is_sanitized(self):
        secret = "selected-private-text-and-provider-secret"
        fake_sudachi = Mock()
        fake_sudachi.dictionary.Dictionary.side_effect = RuntimeError(secret)
        module_spec = importlib.util.spec_from_file_location("unavailable_app", pathlib.Path(app.__file__))
        unavailable_app = importlib.util.module_from_spec(module_spec)
        with patch.dict(sys.modules, {"sudachipy": fake_sudachi}):
            with self.assertLogs(app.logger, level="WARNING") as captured:
                module_spec.loader.exec_module(unavailable_app)
        self.assertIsNone(unavailable_app._tokenizer)
        self.assertNotIn(secret, "\n".join(captured.output))
        with self.assertRaises(Aborted) as caught:
            unavailable_app.TokenizerServicer().Tokenize(proto.TokenizeRequest(text="食べました"), Context())
        self.assertEqual(grpc.StatusCode.UNAVAILABLE, caught.exception.status)

    def test_provider_failure_has_sanitized_logs_and_status(self):
        secret = "私の秘密の文章"
        provider = Mock()
        provider.tokenize.side_effect = RuntimeError(secret)
        with patch.object(app, "_tokenizer", provider):
            with self.assertLogs(app.logger, level="WARNING") as captured:
                error = self.assert_abort(grpc.StatusCode.INTERNAL, secret)
        self.assertNotIn(secret, error.details)
        self.assertNotIn(secret, "\n".join(captured.output))

    def test_success_logs_have_no_input_or_token_surfaces(self):
        with self.assertLogs(app.logger, level="INFO") as captured:
            self.tokenize("食べました")
        output = "\n".join(captured.output)
        for private_text in ("食べました", "食べ", "まし", "食べる"):
            self.assertNotIn(private_text, output)
        self.assertIn("Token count: 3", output)

    def test_cancelled_request_does_not_call_provider(self):
        provider = Mock()
        with patch.object(app, "_tokenizer", provider):
            self.assert_abort(grpc.StatusCode.CANCELLED, "食べました", context=Context(False))
        provider.tokenize.assert_not_called()

    def test_shared_tokenizer_calls_are_serialized(self):
        provider = Mock()
        state_lock = threading.Lock()
        active = 0
        maximum = 0

        def tokenize(text, mode):
            nonlocal active, maximum
            with state_lock:
                active += 1
                maximum = max(maximum, active)
            time.sleep(0.002)
            with state_lock:
                active -= 1
            return []

        provider.tokenize.side_effect = tokenize
        with patch.object(app, "_tokenizer", provider):
            with ThreadPoolExecutor(max_workers=10) as pool:
                list(pool.map(self.tokenize, ["食べました"] * 30))
        self.assertEqual(1, maximum)
        self.assertEqual(30, provider.tokenize.call_count)

    def test_real_concurrent_requests_are_deterministic(self):
        texts = ["食べました", "書きました", "𠮷野で食べました", "😀高くなかった"] * 10
        expected = {text: self.tokenize(text).SerializeToString() for text in texts}
        with ThreadPoolExecutor(max_workers=10) as pool:
            results = list(pool.map(self.tokenize, texts))
        self.assertTrue(all(result.SerializeToString() == expected[text] for text, result in zip(texts, results)))


if __name__ == "__main__":
    unittest.main()
