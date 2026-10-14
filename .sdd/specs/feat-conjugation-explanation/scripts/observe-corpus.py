"""Read synthetic corpus and observe the actual image's gRPC path; never write expected answers."""
import importlib.metadata
import json
import logging
import sys
from concurrent import futures
from datetime import datetime, timezone

sys.path.insert(0, "/app")
import grpc
import app
import tokenizer_pb2
import tokenizer_pb2_grpc

logging.getLogger("tokenizer-sidecar").setLevel(logging.ERROR)
with open(sys.argv[1], encoding="utf-8") as stream:
    corpus = json.load(stream)
server = grpc.server(futures.ThreadPoolExecutor(max_workers=2))
tokenizer_pb2_grpc.add_TokenizerServiceServicer_to_server(app.TokenizerServicer(), server)
port = server.add_insecure_port("127.0.0.1:0")
server.start()
observations = []
try:
    with grpc.insecure_channel(f"127.0.0.1:{port}") as channel:
        grpc.channel_ready_future(channel).result(timeout=10)
        stub = tokenizer_pb2_grpc.TokenizerServiceStub(channel)
        for text in sorted({case["input"]["text"] for case in corpus["cases"]}):
            response = stub.Tokenize(tokenizer_pb2.TokenizeRequest(text=text, mode="C"), timeout=5)
            tokens = [{"surface": t.surface, "base_form": t.base_form, "provider_pos": t.pos,
                       "span": {"start": t.span_start, "end": t.span_end, "unit": "utf16"}}
                      for t in response.tokens]
            observations.append({"text": text, "tokens": tokens})
finally:
    server.stop(0).wait()
result = {"schema_version": 1, "corpus_version": corpus["corpus_version"],
          "observed_at_utc": datetime.now(timezone.utc).isoformat(), "split_mode": "C",
          "transport": "real grpc loopback inside isolated Docker container",
          "provider": {name: importlib.metadata.version(name)
                       for name in ("SudachiPy", "sudachidict_core", "grpcio")},
          "semantic_authority": False, "observations": observations}
sys.stdout.write(json.dumps(result, ensure_ascii=False, separators=(",", ":")))
