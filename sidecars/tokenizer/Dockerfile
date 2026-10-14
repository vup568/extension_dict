FROM python:3.11-slim@sha256:e88e9763f943ec1834f992a4b51e0f24500486803e8bc534e5767af9ea65f6ce

ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY proto/ ./proto/
RUN python -m grpc_tools.protoc -I./proto --python_out=. --grpc_python_out=. ./proto/tokenizer.proto

COPY app.py .
COPY test_tokenizer.py .

USER 10001:10001

EXPOSE 50051

CMD ["python", "app.py"]
