# -*- coding: utf-8 -*-
"""
Generate TTS files (one per question) from an XML question bank using ChatterboxTTS.
- Reads file:// URI or local Windows path to questions.xml
- Converts LaTeX/inline code to speakable English
- Speaks Intro + Question, short pause, then Options (no 'Topic' or 'Question N')
- Saves WAVs to ./tts_out/qNN.wav (and optional extras)
"""

import os
import re
import xml.etree.ElementTree as ET
from pathlib import Path
from urllib.parse import urlparse, unquote
from urllib.request import url2pathname

import torch
import torchaudio as ta
from chatterbox.tts import ChatterboxTTS


appdata_path = os.getenv(r'APPDATA')
# -------------------- CONFIG --------------------
# Your XML location (either a file:// URI or a normal path)
XML_URI_OR_PATH = r"file:///" + appdata_path + r"/DHBW-Game/questions.xml"

# Your voice prompt WAV
AUDIO_PROMPT_PATH = r"DHBW-Game\TTS\chatterbox\Aufzeichnung.wav"

# Output directory for generated WAVs
OUT_DIR = Path("tts_out")

# Speak style: "natural" = friendlier, "literal" = more explicit symbols
READING_MODE = "natural"   # or "literal"u

# The intro before each question:
INTRO_PROMPT = "Not so fast, I have a question for you."

# Short pause between the question sentence and the options (in seconds)
PAUSE_SECONDS = 0.35  # ~350 ms

# Optional: include answer/explanation (spoken)
INCLUDE_CORRECT_ANSWER = False
INCLUDE_EXPLANATION = False
# If True, explanation in a separate file; if False and INCLUDE_EXPLANATION=True, append to question audio
EXPLANATION_SEPARATE_FILE = True
# ------------------ END CONFIG ------------------


def resolve_path(maybe_uri: str) -> Path:
    """Accept a file:// URI or a normal path and return a Path."""
    if maybe_uri.strip().lower().startswith("file://"):
        parsed = urlparse(maybe_uri)
        win_path = url2pathname(unquote(parsed.path.lstrip("/")))
        return Path(win_path)
    return Path(maybe_uri)


def safe_slug(s: str, maxlen: int = 40) -> str:
    s = re.sub(r"[^\w\-]+", "_", s.strip())
    s = re.sub(r"_+", "_", s).strip("_")
    return (s[:maxlen] or "untitled")


# --- Text normalization: make math/code speakable in English ---
LATEX_PAIRS = [
    (r"\\\(", ""), (r"\\\)", ""),                 # remove \( \)
    (r"\\\[", ""), (r"\\\]", ""),                 # remove \[ \]
    (r"\\log\b", "log"),
    (r"\\times\b", " times "),
    (r"\\cdot\b", " times "),
    (r"\\begin\{pmatrix\}", "the matrix: "),
    (r"\\end\{pmatrix\}", ""),
    (r"\\\\", "; "),                              # row break
    (r"&", ", "),                                 # col sep / also covers &amp;
    (r"\\[a-zA-Z]+", ""),                         # drop unknown commands last
]

def speakify_math(text: str) -> str:
    t = text
    # Strip inline code backticks
    t = re.sub(r"`([^`]+)`", r"\1", t)
    # Apply LaTeX replacements
    for pat, rep in LATEX_PAIRS:
        t = re.sub(pat, rep, t)
    # O(...) -> "Big O of ..."
    t = re.sub(r"\bO\s*\(\s*([^)]+)\s*\)", r"Big O of \1", t)
    # Powers like n^2
    t = re.sub(r"(\w)\s*\^\s*(\d+)", r"\1 to the power of \2", t)
    # Common symbols
    t = (t.replace("≥", " greater or equal to ")
           .replace("≤", " less or equal to ")
           .replace("→", " arrow ")
           .replace("∈", " in ")
           .replace("∑", " sum ")
           .replace("∏", " product ")
           .replace("√", " square root of "))
    t = re.sub(r"\s+", " ", t).strip()
    return t

def literalize_symbols(text: str) -> str:
    rep = {
        "(": " open parenthesis ", ")": " close parenthesis ",
        "[": " open bracket ", "]": " close bracket ",
        ":": " colon ", ",": " comma ", "?": " question mark ",
        "=": " equals ", "+": " plus ", "-": " minus ",
        "*": " times ", "/": " slash ", "^": " caret ",
    }
    t = text
    for k, v in rep.items():
        t = t.replace(k, v)
    t = re.sub(r"\s+", " ", t).strip()
    return t

def to_spoken_english(raw: str) -> str:
    base = speakify_math(raw)
    return literalize_symbols(base) if READING_MODE.lower() == "literal" else base


def build_question_parts(q_idx: int, topic: str, q_text: str, options: list[str]) -> tuple[str, str]:
    """
    Build spoken parts WITHOUT mentioning 'Topic' or 'Question N'.
    Returns a tuple: (intro+question sentence, options block)
    """
    q_spoken = to_spoken_english(q_text)

    # Build options
    spoken_opts = []
    letters = ["A", "B", "C", "D", "E", "F"]
    for i, opt in enumerate(options):
        m = re.match(r"\s*([A-Za-z])\s*:\s*(.*)", opt)  # preserve label if present
        if m:
            letter, body = m.group(1).upper(), m.group(2)
        else:
            letter, body = letters[i], opt
        spoken_opts.append(f"Option {letter}: {to_spoken_english(body)}.")

    opts_joined = " ".join(spoken_opts)

    part_intro_question = f"{INTRO_PROMPT} {q_spoken}."
    part_options = opts_joined
    return part_intro_question, part_options


def build_answer_text(correct_idx: int, options: list[str]) -> str:
    letters = ["A", "B", "C", "D", "E", "F"]
    letter = letters[correct_idx] if 0 <= correct_idx < len(letters) else "A"
    raw = options[correct_idx]
    m = re.match(r"\s*[A-Za-z]\s*:\s*(.*)", raw)
    body = m.group(1) if m else raw
    return f"The correct answer is option {letter}: {to_spoken_english(body)}."

def build_explanation_text(expl: str) -> str:
    return f"Explanation: {to_spoken_english(expl)}"


def concat_with_silence(w1: torch.Tensor, w2: torch.Tensor, sr: int, pause_seconds: float) -> torch.Tensor:
    """Ensure (C, N) shape, then concatenate with a silence gap."""
    if w1.dim() == 1:
        w1 = w1.unsqueeze(0)
    if w2.dim() == 1:
        w2 = w2.unsqueeze(0)
    C1, _ = w1.shape
    C2, _ = w2.shape
    if C1 != C2:
        # If different channel counts, downmix to mono
        w1 = w1.mean(dim=0, keepdim=True)
        w2 = w2.mean(dim=0, keepdim=True)
    device = w1.device
    dtype = w1.dtype
    gap = torch.zeros((w1.shape[0], int(sr * pause_seconds)), device=device, dtype=dtype)
    return torch.cat([w1, gap, w2], dim=1)


def main():
    xml_path = resolve_path(XML_URI_OR_PATH)
    prompt_wav = Path(AUDIO_PROMPT_PATH)

    if not xml_path.exists():
        raise FileNotFoundError(f"questions.xml not found: {xml_path}")
    if not prompt_wav.exists():
        raise FileNotFoundError(f"Voice prompt WAV not found: {prompt_wav}")

    # Device pick
    if torch.cuda.is_available():
        device = "cuda"
    elif torch.backends.mps.is_available():
        device = "mps"
    else:
        device = "cpu"
    print(f"Using device: {device}")

    # Load model
    model = ChatterboxTTS.from_pretrained(device=device)

    # Parse XML
    tree = ET.parse(xml_path)
    root = tree.getroot()

    OUT_DIR.mkdir(parents=True, exist_ok=True)

    count = 0
    for idx, q in enumerate(root.findall("Question"), start=1):
        topic = q.get("Topic", "General")
        text_el = q.find("Text")
        opts_el = q.find("Options")
        corr_el = q.find("CorrectOptionIndex")
        expl_el = q.find("Explanation")

        if text_el is None or text_el.text is None:
            print(f"Skipping question {idx}: no <Text>")
            continue

        q_text = (text_el.text or "").strip()

        options = []
        if opts_el is not None:
            for opt_el in opts_el.findall("Option"):
                opt_text = (opt_el.text or "").strip()
                if opt_text:
                    options.append(opt_text)

        # --- Build spoken parts (intro+question, options) ---
        part_q, part_opts = build_question_parts(idx, topic, q_text, options)

        # ---- Generate audio with your voice ----
        wav_q = model.generate(part_q, audio_prompt_path=str(prompt_wav))
        wav_opts = model.generate(part_opts, audio_prompt_path=str(prompt_wav))

        # Join with short silence
        wav_full = concat_with_silence(wav_q, wav_opts, model.sr, PAUSE_SECONDS)

        # Filename: qNN.wav  (no topic/question in audio text)
        q_fname = f"q{idx:02d}.wav"
        q_path = OUT_DIR / q_fname
        ta.save(str(q_path), wav_full, model.sr)
        print(f"Saved question: {q_path}")
        count += 1

        # --- Optional extras: answer/explanation ---
        spoken_ans = None
        spoken_expl = None

        if INCLUDE_CORRECT_ANSWER and corr_el is not None and corr_el.text is not None:
            try:
                correct_idx = int(corr_el.text.strip())  # zero-based in your sample
                if 0 <= correct_idx < len(options):
                    spoken_ans = build_answer_text(correct_idx, options)
            except ValueError:
                pass

        if INCLUDE_EXPLANATION and expl_el is not None and expl_el.text:
            spoken_expl = build_explanation_text(expl_el.text.strip())

        if spoken_ans or spoken_expl:
            if EXPLANATION_SEPARATE_FILE:
                if spoken_ans:
                    wav_a = model.generate(spoken_ans, audio_prompt_path=str(prompt_wav))
                    a_path = OUT_DIR / f"q{idx:02d}_answer.wav"
                    ta.save(str(a_path), wav_a, model.sr)
                    print(f"Saved answer:   {a_path}")
                    count += 1
                if spoken_expl:
                    wav_e = model.generate(spoken_expl, audio_prompt_path=str(prompt_wav))
                    e_path = OUT_DIR / f"q{idx:02d}_explanation.wav"
                    ta.save(str(e_path), wav_e, model.sr)
                    print(f"Saved expl.:    {e_path}")
                    count += 1
            else:
                combined = " ".join(x for x in [part_q, part_opts, spoken_ans, spoken_expl] if x)
                wav_c = model.generate(combined, audio_prompt_path=str(prompt_wav))
                c_path = OUT_DIR / f"q{idx:02d}_combined.wav"
                ta.save(str(c_path), wav_c, model.sr)
                print(f"Saved combined: {c_path}")

    print(f"Done. Generated {count} file(s) in: {OUT_DIR.resolve()}")

if __name__ == "__main__":
    main()
