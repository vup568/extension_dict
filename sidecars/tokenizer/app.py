import os
import sys
import time
import logging
import threading
from concurrent import futures

import grpc

# Add proto generated code directory to Python path if available
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

try:
    import tokenizer_pb2
    import tokenizer_pb2_grpc
except ImportError:
    # Helper fallback message if protoc compiler hasn't run yet
    tokenizer_pb2 = None
    tokenizer_pb2_grpc = None

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s"
)
logger = logging.getLogger("tokenizer-sidecar")

try:
    from sudachipy import dictionary, tokenizer
    _sudachi_dict = dictionary.Dictionary(dict="core")
    _tokenizer = _sudachi_dict.create()
except Exception:
    _tokenizer = None
    # PRIV-002: Provider exception details may contain user/environment data.
    logger.warning("Tokenizer initialization failed; provider unavailable.")

_tokenizer_lock = threading.Lock()
MAX_INPUT_UTF16_UNITS = 2000


def _utf16_offsets(text):
    """Map Python code-point boundaries to browser/.NET UTF-16 boundaries."""
    offsets = [0]
    for character in text:
        offsets.append(offsets[-1] + (2 if ord(character) > 0xFFFF else 1))
    return offsets


if tokenizer_pb2_grpc:
    class TokenizerServicer(tokenizer_pb2_grpc.TokenizerServiceServicer):
        def Tokenize(self, request, context):
            start_time = time.perf_counter()

            # EARS[Unwanted]: Failed initialization is unavailable, never success[].
            if _tokenizer is None:
                context.abort(grpc.StatusCode.UNAVAILABLE, "Tokenizer unavailable.")

            mode_str = (request.mode or "C").upper()
            if mode_str not in {"A", "B", "C"}:
                context.abort(grpc.StatusCode.INVALID_ARGUMENT, "Unsupported split mode.")

            # Same UTF-16 input bound as the existing Grammar/API selection boundary.
            offsets = _utf16_offsets(request.text)
            if offsets[-1] > MAX_INPUT_UTF16_UNITS:
                context.abort(grpc.StatusCode.INVALID_ARGUMENT, "Tokenizer input too long.")
            if not request.text:
                return tokenizer_pb2.TokenizeResponse(tokens=[])
            split_mode = getattr(tokenizer.Tokenizer.SplitMode, mode_str)

            token_specs = []
            # EARS[Unwanted]: Sudachi's mutable tokenizer cannot be shared unsafely.
            # Keep conversion under the lock; morphemes may reference tokenizer state.
            with _tokenizer_lock:
                if not context.is_active():
                    context.abort(grpc.StatusCode.CANCELLED, "Tokenizer request cancelled.")
                try:
                    morphemes = _tokenizer.tokenize(request.text, split_mode)
                    for m in morphemes:
                        pos_str = ",".join(m.part_of_speech())
                        base_form = m.dictionary_form() or m.normalized_form() or m.surface()
                        # EARS[Event]: Wire spans are half-open UTF-16, not code points.
                        token_specs.append(
                            tokenizer_pb2.TokenSpec(
                                surface=m.surface(),
                                base_form=base_form,
                                pos=pos_str,
                                span_start=offsets[m.begin()],
                                span_end=offsets[m.end()]
                            )
                        )
                except Exception:
                    logger.warning("Tokenizer provider request failed.")
                    context.abort(grpc.StatusCode.INTERNAL, "Tokenizer request failed.")

            elapsed_ms = (time.perf_counter() - start_time) * 1000
            # PRIV-002: Log counts/timing only, no raw text, surfaces or exception details.
            logger.info("Tokenized request. Token count: %d, Duration: %.2fms", len(token_specs), elapsed_ms)

            return tokenizer_pb2.TokenizeResponse(tokens=token_specs)


def serve(port=50051):
    if not tokenizer_pb2_grpc:
        logger.error("tokenizer_pb2_grpc not found. Please run protoc before starting server.")
        return

    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    tokenizer_pb2_grpc.add_TokenizerServiceServicer_to_server(TokenizerServicer(), server)
    server.add_insecure_port(f"[::]:{port}")
    logger.info(f"Sudachi Python Tokenizer Sidecar listening on port {port}...")
    server.start()
    server.wait_for_termination()


if __name__ == "__main__":
    serve()
