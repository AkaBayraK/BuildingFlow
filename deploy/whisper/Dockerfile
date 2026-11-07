# Local speech-to-text for the AI assistant's voice input. Built from the official whisper.cpp release source (MIT),
# pinned by version and SHA256; the model is not baked in and is fetched once into a volume by fetch-model.sh.
FROM debian:bookworm-slim AS build
ARG WHISPER_VERSION=1.9.5
ARG WHISPER_SHA256=ff1a9053feb509ff9d7729703355541ae9690073a6b1c40eb692c962e0dc1720
RUN apt-get update && apt-get install -y --no-install-recommends build-essential cmake curl ca-certificates && rm -rf /var/lib/apt/lists/*
WORKDIR /src
RUN curl -fsSL -o whisper.tar.gz "https://github.com/ggml-org/whisper.cpp/archive/refs/tags/v${WHISPER_VERSION}.tar.gz" \
 && echo "${WHISPER_SHA256}  whisper.tar.gz" | sha256sum -c - \
 && tar xzf whisper.tar.gz --strip-components=1 && rm whisper.tar.gz \
 && cmake -B build -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=OFF -DGGML_NATIVE=OFF -DWHISPER_BUILD_TESTS=OFF \
 && cmake --build build -j --config Release --target whisper-server

FROM debian:bookworm-slim
RUN apt-get update && apt-get install -y --no-install-recommends curl ca-certificates libgomp1 && rm -rf /var/lib/apt/lists/* \
 && useradd --system --uid 10001 --home /models whisper && mkdir -p /models && chown whisper /models
COPY --from=build /src/build/bin/whisper-server /usr/local/bin/whisper-server
# The upstream MIT licence travels with the binary it covers.
COPY --from=build /src/LICENSE /usr/share/licenses/whisper.cpp/LICENSE
COPY fetch-model.sh /usr/local/bin/fetch-model
RUN chmod 0755 /usr/local/bin/fetch-model
USER whisper
VOLUME ["/models"]
EXPOSE 8080
