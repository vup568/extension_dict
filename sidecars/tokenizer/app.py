import os
import sys
import time
import logging
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

try:
    from sudachipy import dictionary, tokenizer
    _sudachi_dict = dictionary.Dictionary(dict="core")
    _tokenizer = _sudachi_dict.create()
except Exception as e:
    _tokenizer = None
    logging.warning(f"SudachiPy initialisation warning: {e}")

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s"
)
logger = logging.getLogger("tokenizer-sidecar")


if tokenizer_pb2_grpc:
    class TokenizerServicer(tokenizer_pb2_grpc.TokenizerServiceServicer):
        def Tokenize(self, request, context):
            start_time = time.time()

            # PRIV-002: Do NOT log raw input text to preserve user privacy
            if not request.text:
                return tokenizer_pb2.TokenizeResponse(tokens=[])

            mode_str = (request.mode or "C").upper()
            if mode_str == "A":
                split_mode = tokenizer.Tokenizer.SplitMode.A
            elif mode_str == "B":
                split_mode = tokenizer.Tokenizer.SplitMode.B
            else:
                split_mode = tokenizer.Tokenizer.SplitMode.C

            token_specs = []
            if _tokenizer:
                morphemes = _tokenizer.tokenize(request.text, split_mode)
                for m in morphemes:
                    pos_str = ",".join(m.part_of_speech())
                    base_form = m.dictionary_form() or m.normalized_form() or m.surface()
                    token_specs.append(
                        tokenizer_pb2.TokenSpec(
                            surface=m.surface(),
                            base_form=base_form,
                            pos=pos_str,
                            span_start=m.begin(),
                            span_end=m.end()
                        )
                    )

            elapsed_ms = (time.time() - start_time) * 1000
            logger.info(f"Tokenized request successfully. Token count: {len(token_specs)}, Duration: {elapsed_ms:.2f}ms")

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
