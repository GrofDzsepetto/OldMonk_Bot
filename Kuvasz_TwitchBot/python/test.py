import argparse
import math
import numpy as np
from scipy.io import wavfile
from scipy.signal import resample_poly
from faster_whisper import WhisperModel




def load_wav_mono_16k(path: str) -> np.ndarray:
sr, data = wavfile.read(path)
# int16/float32 kezelése
if data.dtype == np.int16:
    audio = data.astype(np.float32) / 32768.0
elif data.dtype == np.int32:
    audio = data.astype(np.float32) / 2147483648.0
elif data.dtype == np.float32:
    audio = data
else:
    raise ValueError(f"Nem támogatott WAV dtype: {data.dtype}")


# Sztereóból mono
if audio.ndim == 2:
    audio = audio.mean(axis=1)


# Mintavétel 16 kHz-re
target_sr = 16000
if sr != target_sr:
    g = math.gcd(sr, target_sr)
    up = target_sr // g
    down = sr // g
    audio = resample_poly(audio, up, down).astype(np.float32)
else:
    audio = audio.astype(np.float32)


return audio




def main():
    parser = argparse.ArgumentParser(description="WAV → szöveg (hu) – faster-whisper, egy lépésben")
    parser.add_argument("wav_path", help="WAV fájl elérési útja")
    parser.add_argument("--language", default="hu", help="Nyelv (pl. hu)")
    parser.add_argument("--task", default="transcribe", choices=["transcribe", "translate"], help="Feladat: átirat vagy fordítás")
    parser.add_argument("--model-size", default="large-v3", help="Whisper modell méret (tiny/base/small/medium/large-v3)")
    parser.add_argument("--device", default="auto", help="auto|cpu|cuda")
    parser.add_argument("--compute-type", default="int8_float16", help="int8_float16|int8|float16|float32")
    parser.add_argument("--beam-size", type=int, default=6)
    parser.add_argument("--best-of", type=int, default=5)
    args = parser.parse_args()


    # WAV betöltése
    audio = load_wav_mono_16k(args.wav_path)


    # Modell betöltése
    model = WhisperModel(
    args.model_size,
    device=args.device,
    compute_type=args.compute_type,
    )


    # Átirat
    segments, info = model.transcribe(
    audio=audio,
    language=args.language,
    task=args.task,
    beam_size=args.beam_size,
    best_of=args.best_of,
    temperature=0.0,
    )


    # Szegmensek összefűzése és kiírás
    text = "".join(seg.text for seg in segments).strip()
    print(text)




if __name__ == "__main__":
    main()